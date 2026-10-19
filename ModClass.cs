using System;
using System.Globalization;
using System.Collections.Generic;
using NeoModLoader.api;
using UnityEngine;

namespace SCP096TheShyGuy
{
    public sealed class ModClass : BasicMod<ModClass>
    {
        private const string Version = "0.2.3";
        private const int MaxAttackAttempts = 3;
        private WorldBridge _world;
        private WorldfallSight _worldfallSight;
        private bool _worldfallLookEnabled = true;
        private bool _allowProximityWithWorldfall = false;
        private float _worldfallLookAt, _worldfallMaxTiles = 35f, _worldfallLookHold = .45f;
        private float _worldfallRecheckSeconds = .2f;
        
        private ShyGuyBrain _brain;
        private readonly WitnessQueue _queue = new WitnessQueue();
        private readonly HashSet<object> _sessionOnlyWitnesses = new HashSet<object>();
        private object _scp;
        private object _victim; // always mirrors the FIFO head
        private object _lastIssuedVictim; // distinguishes SCP kills from unrelated deaths
        private object _observedMap;
        private float _tickAt, _scanAt, _proximityScanAt, _fullWitnessScanAt, _chaseAt, _behaviorAt;
        private float _lastProgressAt, _lastProgressX, _lastProgressY;
        private float _attackRetryAt;
        private bool _progressInitialized, _stuckWarningSent, _movementMethodLogged;
        private bool _enableChase = true;
        private bool _enableProximity = true;
        private bool _traitsRegistered, _rageTraitApplied;
        private bool _killPending;
        private int _attackAttempts, _killsConfirmed;
        private bool _initialized, _showPanel = true, _placeOnMap, _selectRemote;
        private string _status = "Inicializando...";
        private float _triggerDelay = 8f, _calmingDelay = 4f;
        private float _tickSeconds = .25f, _attackDistance = 1.7f;
        private float _proximityDistance = 5f, _proximityTickSeconds = 1.5f;
        private int _maxWitnesses = 64;
        private bool _debugLogs = true;
        private Vector2 _scroll;

        private Rect PanelRect => new Rect(Mathf.Max(4f, Screen.width - 362f), 48f, 354f,
            Mathf.Min(625f, Mathf.Max(225f, Screen.height - 65f)));

        protected override void OnModLoad()
        {
            _world = new WorldBridge();
            _worldfallSight = new WorldfallSight();
            _brain = new ShyGuyBrain();
            ReadSettings();
            _traitsRegistered = ShyGuyTraits.Register();
            Log("Traits: " + (_traitsRegistered ? "registradas: Witness, RageSpeed" : "REGISTRO FALHOU; ver Player.log"));
            string reason;
            if (_world.TryRegisterAsset(GetDeclaration().FolderPath, out reason))
            {
                Log("Asset registrado: " + WorldBridge.ScpId + " a partir de " + _world.SourcePrototype);
                Log("Visual: " + _world.AppearanceStatus + "; flags: " + _world.ExtraFlagsStatus);
            }
            else Log("Asset pendente: " + reason);
            _initialized = true;
            Log("v" + Version + " bridge real FirstPerson.WorldBoxMod/get_Host/get_View; F10 diagnostico; F11 TESTE MANUAL aldeao 3D; auto-olhar aguarda validacao GPU");
        }

        private void Update()
        {
            if (!_initialized) return;
            float now = Time.unscaledTime;
            if (Input.GetKeyDown(KeyCode.F5)) _showPanel = !_showPanel;
            if (Input.GetKeyDown(KeyCode.Escape)) { _placeOnMap = false; _selectRemote = false; }
            if (Input.GetKeyDown(KeyCode.F6))
                Spawn(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) MarkVictimAnywhere(now);
                else MarkVictim(now);
            }
            if (Input.GetKeyDown(KeyCode.F8)) CancelTarget();
            if (Input.GetKeyDown(KeyCode.F9)) Diagnostic();
            // Press while possessing a villager, without opening F5/F2 menus.
            if (Input.GetKeyDown(KeyCode.F10))
                Log("CAPTURA F10 (durante jogo): " + _worldfallSight.DetailedSnapshot(_world, _scp));
            if (Input.GetKeyDown(KeyCode.F11))
                ManualWorldfallWitness(now);
            if (Input.GetMouseButtonDown(0) && !PointerInsidePanel())
            {
                if (_placeOnMap) { _placeOnMap = false; SpawnOnClickedTile(); }
                else if (_selectRemote) { _selectRemote = false; MarkVictim(now); }
            }
            if (now < _tickAt) return;
            _tickAt = now + _tickSeconds;
            if (!_world.HasMap) return;
            if (!ReferenceEquals(_observedMap, _world.Map))
            {
                _observedMap = _world.Map;
                _scp = _victim = _lastIssuedVictim = null;
                _queue.Clear();
                _sessionOnlyWitnesses.Clear();
                _brain.Reset(now);
                _scanAt = _proximityScanAt = _fullWitnessScanAt = 0f;
                _rageTraitApplied = false;
                _placeOnMap = _selectRemote = false;
                ResetChaseProgress(now);
                _worldfallSight.Reset();
            }

            if (now >= _scanAt)
            {
                _scanAt = now + 2.5f;
                if (!_world.IsAlive(_scp))
                {
                    _scp = _world.FindExistingScp();
                    _victim = null;
                    _queue.Clear();
                    _sessionOnlyWitnesses.Clear();
                    _brain.Reset(now);
                    _rageTraitApplied = false;
                    ResetChaseProgress(now);
                    if (_scp != null && _traitsRegistered)
                        _world.RemoveTrait(_scp, ShyGuyTraits.RageId);
                }
            }
            if (_scp == null) return;

            // Safe third-person detection: it never guesses actor/model coordinates.
            // No confirmed Worldfall-owned actor/camera/model => no witness registered.
            if (_worldfallLookEnabled && now >= _worldfallLookAt)
            {
                _worldfallLookAt = now + _worldfallRecheckSeconds;
                object observer;
                if (_worldfallSight.Check(_world, _scp, _worldfallMaxTiles, _worldfallLookHold,
                    now, out observer) && observer != null)
                    EnqueueWitness(observer, now, "rosto na camera Worldfall (3D)");
                string transition = _worldfallSight.TakeTransition();
                if (!string.IsNullOrEmpty(transition)) Log("WorldfallSight: " + transition);
            }

            if (now >= _proximityScanAt)
            {
                _proximityScanAt = now + _proximityTickSeconds;
                bool fullScan = now >= _fullWitnessScanAt;
                if (fullScan) _fullWitnessScanAt = now + 12f;
                ReconcileWitnesses(now, fullScan);
            }
            UpdateCurrentTarget(now);

            ShyGuyState before = _brain.State;
            _brain.Tick(now, _victim != null, _triggerDelay, _calmingDelay);
            if (before != _brain.State)
            {
                Log("Estado: " + before + " -> " + _brain.State + " (" + _brain.LastReason + ")");
                if (_brain.State == ShyGuyState.Rage)
                {
                    _rageTraitApplied = _traitsRegistered && _world.AddTrait(_scp, ShyGuyTraits.RageId);
                    if (!_rageTraitApplied) Log("AVISO: bonus de velocidade da trait Rage nao foi aplicado");
                    ResetChaseProgress(now);
                    _world.TryInterruptBehaviors(_scp);
                }
                else if (before == ShyGuyState.Rage)
                {
                    if (_traitsRegistered) _world.RemoveTrait(_scp, ShyGuyTraits.RageId);
                    _rageTraitApplied = false;
                    _world.TryInterruptBehaviors(_scp);
                }
            }

            if (_brain.State == ShyGuyState.Rage && _victim != null && _enableChase)
            {
                if (_world.Within(_scp, _victim, _attackDistance))
                {
                    if (!_killPending && now >= _attackRetryAt && _attackAttempts < MaxAttackAttempts)
                    {
                        _attackAttempts++;
                        _attackRetryAt = now + 3f;
                        _killPending = _world.TryKill(_victim);
                        if (_killPending) _lastIssuedVictim = _victim;
                        Log((_killPending ? "EXECUCAO SOLICITADA" : "ATAQUE NAO ENVIADO") +
                            " tentativa=" + _attackAttempts + "; distancia no ataque=" + _world.TargetDistance(_scp, _victim) + "; alcance=" + _attackDistance.ToString("0.0", CultureInfo.InvariantCulture) + " tiles; " + _world.LastKillSignature);
                    }
                    else if (_killPending && now >= _attackRetryAt)
                    {
                        // Delayed confirmation: only retry if still actually alive.
                        _killPending = false;
                        if (_attackAttempts >= MaxAttackAttempts)
                            Log("ATAQUE SEM CONFIRMACAO apos 3 tentativas; alvo ainda vivo. Verificar diagnostico.");
                    }
                }
                else
                {
                    // Don't keep an old kill pending after the target escapes.
                    TrackChaseProgress(now);
                    if (now >= _behaviorAt)
                    {
                        // This pattern is taken from the working The Unrelenting.
                        bool interrupted = _world.TryInterruptBehaviors(_scp);
                        _behaviorAt = now + 3f;
                        if (interrupted) _chaseAt = 0f;
                    }
                    if (now >= _chaseAt)
                    {
                        float distance = _world.Distance(_scp, _victim);
                        _chaseAt = now + MotionTickSeconds(distance);
                        if (_world.TryChase(_scp, _victim))
                        {
                            if (!_movementMethodLogged)
                            {
                                _movementMethodLogged = true;
                                Log("Perseguicao: " + _world.LastMoveSignature +
                                    "; distancia inicial=" + _world.TargetDistance(_scp, _victim));
                            }
                        }
                        else LogOnceMovementError();
                    }
                }
            }
        }

        // F11 is a MANUAL integration test, not facial recognition.
        // Avoids false positives while GPU head/occlusion is being validated.
        private void ManualWorldfallWitness(float now)
        {
            object actor = _worldfallSight.CurrentControlledActor;
            if (actor == null) { Log("F11: nenhum Actor possuido no WorldFall (ver diagnostico)"); return; }
            if (_scp == null || !_world.IsAlive(_scp)) { Log("F11: crie o SCP primeiro"); return; }
            if (!_world.Within(_scp, actor, _worldfallMaxTiles))
            { Log("F11: jogador distante demais do SCP (limite " + _worldfallMaxTiles + " tiles)"); return; }
            EnqueueWitness(actor, now, "F11 TESTE MANUAL WorldFall (nao confirma olhar)");
            UpdateCurrentTarget(now);
        }

        private bool _movementErrorLogged;
        private void LogOnceMovementError()
        {
            if (_movementErrorLogged) return;
            _movementErrorLogged = true;
            Log("Perseguicao falhou: " + _world.LastMoveSignature);
        }

        private static float MotionTickSeconds(float distance)
        {
            if (distance > 50f) return 1.6f;
            if (distance > 15f) return .8f;
            if (distance > 5f) return .35f;
            return .18f;
        }

        private void ReconcileWitnesses(float now, bool fullScan)
        {
            int removed = _queue.RemoveInvalid(a =>
                _world.IsAlive(a) && (!_traitsRegistered || _sessionOnlyWitnesses.Contains(a) ||
                    _world.HasTrait(a, ShyGuyTraits.WitnessId)));
            if (removed > 0) Log("Fila: " + removed + " alvo(s) removido(s) por morte/trait ausente");
            _sessionOnlyWitnesses.RemoveWhere(a => !_world.IsAlive(a) || !_queue.Contains(a));
            // One bounded scan per 1.5s, never one raycast for every actor each frame.
            foreach (object actor in _world.Actors())
            {
                if (ReferenceEquals(actor, _scp)) continue;
                if (_queue.Count >= _maxWitnesses) break;
                bool newlyObserved = _enableProximity &&
                    (_allowProximityWithWorldfall || !_worldfallSight.Is3DWorldfallView) &&
                    _world.Within(_scp, actor, _proximityDistance);
                // Full-world trait scan only every 12 seconds, not every frame.
                if (!newlyObserved && !fullScan) continue;
                bool alreadyMarked = _traitsRegistered && _world.HasTrait(actor, ShyGuyTraits.WitnessId);
                if (!alreadyMarked && !newlyObserved) continue;
                if (_world.AssetId(actor) == WorldBridge.ScpId) continue;
                if (_queue.Contains(actor)) continue;
                EnqueueWitness(actor, now, alreadyMarked ? "trait recuperada do save" : "proximidade experimental 5 tiles");
            }
        }

        private void EnqueueWitness(object candidate, float now, string source)
        {
            if (candidate == null || !_world.IsAlive(candidate) ||
                ReferenceEquals(candidate, _scp) || _world.AssetId(candidate) == WorldBridge.ScpId ||
                _queue.Contains(candidate) || _queue.Count >= _maxWitnesses) return;
            if (_traitsRegistered && !_world.AddTrait(candidate, ShyGuyTraits.WitnessId))
            {
                _sessionOnlyWitnesses.Add(candidate);
                Log("AVISO: nao foi possivel colocar trait Witness; alvo permanece apenas nesta sessao");
            }
            if (!_queue.Enqueue(candidate, _maxWitnesses)) return;
            Log("TESTEMUNHA MARCADA via " + source + "; fila=" + _queue.Count +
                "; distancia=" + _world.TargetDistance(_scp, candidate));
            if (_brain.State == ShyGuyState.Docile || _brain.State == ShyGuyState.Calming)
                _brain.Transition(ShyGuyState.Triggered, now, "primeira testemunha: " + source);
        }

        private void UpdateCurrentTarget(float now)
        {
            object next = _queue.First;
            if (ReferenceEquals(next, _victim)) return;
            object old = _victim;
            if (old != null && !_world.IsAlive(old))
            {
                if (ReferenceEquals(old, _lastIssuedVictim))
                {
                    _killsConfirmed++;
                    Log("MORTE CONFIRMADA: eliminacao solicitada pelo SCP; total=" + _killsConfirmed);
                }
                else Log("Alvo morreu ou desapareceu sem execucao solicitada pelo SCP");
            }
            if (ReferenceEquals(old, _lastIssuedVictim)) _lastIssuedVictim = null;
            _victim = next;
            ResetChaseProgress(now);
            if (_victim != null)
                Log("PROXIMA VITIMA: fila=" + _queue.Count +
                    "; distancia=" + _world.TargetDistance(_scp, _victim));
        }

        private void ResetChaseProgress(float now)
        {
            _chaseAt = _behaviorAt = 0f;
            _progressInitialized = _stuckWarningSent = _movementMethodLogged = false;
            _movementErrorLogged = _killPending = false;
            _attackAttempts = 0;
            _attackRetryAt = 0f;
            _lastProgressAt = now;
        }
        private void TrackChaseProgress(float now)
        {
            if (!_world.Position(_scp, out float x, out float y)) return;
            if (!_progressInitialized)
            {
                _lastProgressX = x;
                _lastProgressY = y;
                _lastProgressAt = now;
                _progressInitialized = true;
                return;
            }
            float dx = x - _lastProgressX, dy = y - _lastProgressY;
            if (dx * dx + dy * dy >= .75f * .75f)
            {
                _lastProgressX = x;
                _lastProgressY = y;
                _lastProgressAt = now;
                _stuckWarningSent = false;
            }
            else if (!_stuckWarningSent && now - _lastProgressAt > 8f)
            {
                Log("AVISO: SCP parado 8s durante RAGE. Movimento=" + _world.LastMoveSignature +
                    "; distancia=" + _world.TargetDistance(_scp, _victim));
                _stuckWarningSent = true;
            }
        }

        private bool PointerInsidePanel()
        {
            if (!_showPanel) return false;
            Vector3 mouse = Input.mousePosition;
            return PanelRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
        }
        private void OnGUI()
        {
            if (!_initialized) return;
            Rect r = PanelRect;
            if (!_showPanel)
            {
                if (GUI.Button(new Rect(Mathf.Max(4f, Screen.width - 141f), 48, 133, 31), "SCP-096 [F5]"))
                    _showPanel = true;
                return;
            }
            GUI.Box(r, "SCP-096 - THE SHY GUY | v" + Version);
            if (GUI.Button(new Rect(r.x + r.width - 28, r.y + 3, 24, 22), "X")) _showPanel = false;
            Rect viewport = new Rect(r.x + 6, r.y + 28, r.width - 12, r.height - 35);
            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0, 0, 324, 815));
            float y = 0, x = 3;
            GUI.Label(new Rect(x,y,315,23), "Mapa: " + _world.Readiness); y+=23;
            GUI.Label(new Rect(x,y,315,23), "SCP: " + (_scp != null ? "encontrado" : "nenhum") +
                "  |  Estado: " + _brain.State); y+=23;
            GUI.Label(new Rect(x,y,315,23), "Fila de testemunhas: " + _queue.Count +
                " | Eliminadas: " + _killsConfirmed); y+=23;
            GUI.Label(new Rect(x,y,315,23), "Alvo: " + (_victim == null ? "nenhum" : _world.TargetDistance(_scp, _victim))); y+=25;
            _worldfallLookEnabled = GUI.Toggle(new Rect(x,y,315,23), _worldfallLookEnabled, "Worldfall: detectar rosto em 3D (seguro)"); y+=24;
            GUI.Label(new Rect(x,y,315,40), "Visao 3D: " + _worldfallSight.ShortStatus); y+=42;
            _enableChase = GUI.Toggle(new Rect(x,y,315,23), _enableChase, "Perseguir e eliminar"); y+=25;
            _enableProximity = GUI.Toggle(new Rect(x,y,320,23), _enableProximity,
                "Marcar NPCs a " + _proximityDistance.ToString("0") + " tiles (2D)"); y+=28;
            if (GUI.Button(new Rect(x,y,152,31), "Gerar aqui [F6]")) Spawn(false);
            if (GUI.Button(new Rect(x+160,y,156,31), "Gerar isolado")) Spawn(true);
            y+=38;
            if (GUI.Button(new Rect(x,y,316,31), _placeOnMap ? "CLIQUE NO MAPA (ESC cancela)" : "Posicionar SCP no mapa"))
            {
                _placeOnMap = !_placeOnMap;
                if (_placeOnMap) _selectRemote = false;
            }
            y+=39;
            if (GUI.Button(new Rect(x,y,152,31), "Alvo [F7]")) MarkVictim(Time.unscaledTime);
            if (GUI.Button(new Rect(x+160,y,156,31), _selectRemote ? "CLIQUE NO NPC" : "Escolher alvo mapa"))
            {
                _selectRemote = !_selectRemote;
                if (_selectRemote) _placeOnMap = false;
            }
            y+=39;
            if (GUI.Button(new Rect(x,y,316,29), "Cancelar fila [F8]")) CancelTarget(); y+=37;
            if (GUI.Button(new Rect(x,y,152,29), "Teste: humano")) SpawnTestHuman();
            if (GUI.Button(new Rect(x+160,y,156,29), "Diagnostico [F9]")) Diagnostic();
            y+=37;
            if (GUI.Button(new Rect(x,y,316,31), "Teste: eliminar primeiro alvo")) TestKillVictim();
            y+=36;
            if (GUI.Button(new Rect(x,y,316,29), "Diagnosticar camera Worldfall"))
                Log(_worldfallSight.DetailedSnapshot(_world, _scp));
            y+=35;
            if (GUI.Button(new Rect(x,y,316,29), "Teste WorldFall: marcar jogador [F11]"))
                ManualWorldfallWitness(Time.unscaledTime);
            y+=34;
            GUI.Label(new Rect(x,y,315,20), "Ultimo resultado:"); y+=21;
            GUIStyle feedback = new GUIStyle(GUI.skin.label); feedback.wordWrap = true;
            GUI.Label(new Rect(x,y,314,105), _status, feedback);
            GUI.EndScrollView();
        }

        private void Spawn(bool isolated)
        {
            if (!_world.HasMap) { Log("Nao foi possivel gerar: " + _world.Readiness); return; }
            object tile = isolated ? _world.IsolatedTile() : _world.VisibleTile();
            if (tile == null) { Log("Sem tile valido. Use posicionamento manual."); return; }
            SpawnAtTile(tile);
        }
        private void SpawnOnClickedTile()
        {
            if (!_world.HasMap) { Log("Sem mapa carregado"); return; }
            object tile = _world.MouseTile();
            if (tile == null) { Log("Tile do mouse indisponivel"); return; }
            SpawnAtTile(tile);
        }
        private void SpawnAtTile(object tile)
        {
            _scp = _world.FindExistingScp();
            if (_scp != null)
            {
                Log("Ja existe SCP-096 neste mundo. Coordenadas=" +
                    _world.TileCoordinates(_world.ActorTile(_scp)));
                return;
            }
            if (!_world.AssetRegistered && !_world.TryRegisterAsset(GetDeclaration().FolderPath, out string error))
            { Log("REGISTRO FALHOU: " + error); return; }
            object actor = _world.SpawnAt(tile, out string reason);
            if (actor == null) { Log("SPAWN FALHOU: " + reason); return; }
            _scp = actor;
            _victim = null;
            _queue.Clear();
            _brain.Reset(Time.unscaledTime);
            ResetChaseProgress(Time.unscaledTime);
            Log("SCP-096 criado em " + _world.TileCoordinates(tile) + "; visual=" + _world.AppearanceStatus);
        }
        private void SpawnTestHuman()
        {
            object tile = _world.VisibleTile();
            if (tile == null) { Log("Tile indisponivel"); return; }
            object human = _world.SpawnTestHuman(tile, out string reason);
            Log(human == null ? "Teste humano falhou: " + reason :
                "Teste humano OK em " + _world.TileCoordinates(tile));
        }
        private void MarkVictim(float now)
        {
            _scp = _world.FindExistingScp();
            if (_scp == null) { Log("Primeiro crie SCP-096"); return; }
            object cursor = _world.MouseTile();
            object victim = cursor == null ? null : _world.NearestVictim(_scp, cursor, 12f);
            if (victim == null) { Log("Nenhuma unidade viva ate 12 tiles do cursor"); return; }
            EnqueueWitness(victim, now, "cursor F7");
            UpdateCurrentTarget(now);
        }
        private void MarkVictimAnywhere(float now)
        {
            _scp = _world.FindExistingScp();
            if (_scp == null) { Log("Primeiro crie SCP-096"); return; }
            object victim = _world.NearestVictimAnywhere(_scp);
            if (victim == null) { Log("Nenhuma unidade viva no mapa"); return; }
            EnqueueWitness(victim, now, "cursor global");
            UpdateCurrentTarget(now);
        }
        private void TestKillVictim()
        {
            if (_victim == null || !_world.IsAlive(_victim))
            { Log("Marque primeiro uma vitima viva"); return; }
            _killPending = _world.TryKill(_victim);
            if (_killPending) _lastIssuedVictim = _victim;
            _attackRetryAt = Time.unscaledTime + 3f;
            Log((_killPending ? "TESTE DE MORTE SOLICITADO" : "TESTE DE MORTE FALHOU") +
                ": " + _world.LastKillSignature);
        }
        private void CancelTarget()
        {
            if (_traitsRegistered)
                foreach (object actor in _queue.Snapshot())
                    if (_world.IsAlive(actor)) _world.RemoveTrait(actor, ShyGuyTraits.WitnessId);
            _queue.Clear();
            _sessionOnlyWitnesses.Clear();
            _victim = _lastIssuedVictim = null;
            ResetChaseProgress(Time.unscaledTime);
            Log("Fila cancelada; SCP retornara a DOCILE pelo ciclo normal");
        }
        private void Diagnostic()
        {
            Log("DIAGNOSTICO v" + Version + ": " + _world.DiagnosticReadiness());
            Log("SCP=" + (_scp != null) + " state=" + _brain.State + " fila=" + _queue.Count +
                " traits=" + _traitsRegistered + " rageTrait=" + _rageTraitApplied +
                " proximidade=" + _enableProximity + " distancia=" + _world.TargetDistance(_scp, _victim));
            Log(_world.DiagnosticMethods(_scp));
            Log(_world.DiagnosticShadows());
            Log("Ultimo movimento=" + _world.LastMoveSignature + " ultima execucao=" + _world.LastKillSignature);
            Log(_worldfallSight.Diagnostic(_world, _scp));
        }
        private void ReadSettings()
        {
            try
            {
                _triggerDelay = ConfigFloat("TriggerDelay", 8f, 0f, 120f);
                _calmingDelay = ConfigFloat("CalmingDelay", 4f, 0f, 120f);
                _tickSeconds = ConfigFloat("TickSeconds", .25f, .1f, 5f);
                _attackDistance = ConfigFloat("AttackDistance", 1.7f, .5f, 10f);
                _proximityDistance = ConfigFloat("ProximityDistance", 5f, 1f, 20f);
                _proximityTickSeconds = ConfigFloat("ProximityScanSeconds", 1.5f, .5f, 10f);
                _maxWitnesses = (int)ConfigFloat("MaxWitnesses", 64f, 1f, 256f);
                object proximity = GetConfig()["SCP096"]["ProximityWitnesses"].GetValue();
                if (proximity is bool) _enableProximity = (bool)proximity;
                object cameraFlag = GetConfig()["SCP096"]["WorldfallFaceDetection"].GetValue();
                if (cameraFlag is bool) _worldfallLookEnabled = (bool)cameraFlag;
                _worldfallMaxTiles = ConfigFloat("WorldfallMaxLookTiles", 35f, 2f, 120f);
                _worldfallLookHold = ConfigFloat("WorldfallLookHoldSeconds", .45f, .1f, 3f);
                object allowProximity = GetConfig()["SCP096"]["AllowProximityWhilePossessing"].GetValue();
                if (allowProximity is bool) _allowProximityWithWorldfall = (bool)allowProximity;
                object debug = GetConfig()["SCP096"]["DebugLogs"].GetValue();
                if (debug is bool) _debugLogs = (bool)debug;
            }
            catch (Exception ex) { Log("Usando defaults: " + ex.Message); }
        }
        private float ConfigFloat(string key, float fallback, float min, float max)
        {
            object v = GetConfig()["SCP096"][key].GetValue();
            if (v == null || !float.TryParse(Convert.ToString(v), NumberStyles.Float,
                CultureInfo.InvariantCulture, out float n)) return fallback;
            return Mathf.Clamp(n, min, max);
        }
        private void Log(string message)
        {
            _status = message;
            if (_debugLogs) Debug.Log("[SCP096] " + message);
        }
    }
}
