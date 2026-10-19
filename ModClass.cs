using System;
using System.Globalization;
using System.Collections.Generic;
using NeoModLoader.api;
using UnityEngine;

namespace SCP096TheShyGuy
{
    public sealed class ModClass : BasicMod<ModClass>
    {
        private const string Version = "0.2.9";
        private const int MaxAttackAttempts = 3;
        private WorldBridge _world;
        private WorldfallSight _worldfallSight;
        private ShyGuyAudio _audio;
        private DynamicMusicController _music;
        private ShyGuyOverlay _overlay;
        private object _localThreatActor, _pendingLocalDeath;
        private bool _testMusicOverride, _musicOverrideEnabled = true;
        private float _musicFadeDown = 2.8f, _musicFadeUp = 3.5f, _musicDuckingPercent = 100f;
        private bool _deathFlashEnabled = true;
        private float _deathFlashDuration = 2.6f;
        private float _cooldownUntil, _cooldownSeconds = 15f;
        private bool _worldfallLookEnabled = true;
        private bool _allowProximityWithWorldfall = false;
        private float _worldfallLookAt, _worldfallMaxTiles = 35f, _worldfallLookHold = .45f;
        private float _worldfallRecheckSeconds = .18f;
        private bool _worldfallRequireFacing = true;
        private float _worldfallFacingOffset = 0f, _worldfallFaceCone = 65f;
        
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
        private int _rageSpeedStage = -1;
        private float _rageTargetStartedAt;
        private float _rageRampSeconds = 45f;
        private bool _regenerationEnabled = true;
        private float _regenerationAt, _rageRegenPerSecond = 2000f, _docileRegenPerSecond = 700f;
        private bool _regenDiagnosticPrinted;
        private string _lastRegenDetail = "not checked";
        private bool _killPending;
        private int _attackAttempts, _killsConfirmed;
        private bool _initialized, _showPanel = true, _placeOnMap, _selectRemote;
        private string _status = "Inicializando...";
        private bool _soundEnabled = true, _visualCueEnabled = true;
        private float _volume = .7f;
        private float _flashDuration = 1.65f, _flashOpacity = .60f;
        private float _triggerDelay = 55f, _calmingDelay = 4f;
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
            _audio = new ShyGuyAudio(GetDeclaration().FolderPath, Log);
            _audio.Enabled = _soundEnabled;
            _audio.VisualCueEnabled = _visualCueEnabled;
            _audio.MasterVolume = _volume;
            _audio.FlashDuration = _flashDuration;
            _audio.FlashOpacity = _flashOpacity;
            _music = new DynamicMusicController(Log);
            _music.Enabled = _musicOverrideEnabled;
            _music.FadeDownSeconds = _musicFadeDown;
            _music.FadeUpSeconds = _musicFadeUp;
            _music.DuckFraction = _musicDuckingPercent / 100f;
            _overlay = new ShyGuyOverlay(_audio.FaceImage, Log);
            _overlay.Enabled = _visualCueEnabled;
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
            Log("v" + Version + " WorldFall Canvas overlay + dynamic BGM override; FMOD + chase preserved");
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
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (_overlay != null && _audio != null)
                        _overlay.Show(_audio.FlashDuration, _audio.FlashOpacity, "preview SHIFT+F10 durante WorldFall", true);
                }
                else Log("CAPTURA F10 (durante jogo): " + _worldfallSight.DetailedSnapshot(_world, _scp));
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    _testMusicOverride = !_testMusicOverride;
                    if (_music != null) _music.Request(_testMusicOverride);
                    Log("PREVIEW BGM SHIFT+F11: " + _testMusicOverride);
                }
                else ManualWorldfallWitness(now);
            }
            if (Input.GetMouseButtonDown(0) && !PointerInsidePanel())
            {
                if (_placeOnMap) { _placeOnMap = false; SpawnOnClickedTile(); }
                else if (_selectRemote) { _selectRemote = false; MarkVictim(now); }
            }
            if (now < _tickAt) return;
            _tickAt = now + _tickSeconds;
            if (!_world.HasMap)
            {
                _localThreatActor = _pendingLocalDeath = null;
                _testMusicOverride = false;
                if (_music != null) _music.RestoreImmediately();
                if (_overlay != null) _overlay.Reset();
                return;
            }
            if (!ReferenceEquals(_observedMap, _world.Map))
            {
                if (_audio != null) _audio.Reset();
                if (_music != null) _music.RestoreImmediately();
                if (_overlay != null) _overlay.Reset();
                _localThreatActor = _pendingLocalDeath = null;
                _testMusicOverride = false;
                _cooldownUntil = 0f;
                _observedMap = _world.Map;
                _scp = _victim = _lastIssuedVictim = null;
                _queue.Clear();
                _sessionOnlyWitnesses.Clear();
                _brain.Reset(now);
                _scanAt = _proximityScanAt = _fullWitnessScanAt = 0f;
                _rageTraitApplied = false;
                _rageSpeedStage = -1;
                _regenerationAt = 0f;
                _regenDiagnosticPrinted = false;
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
                    if (_audio != null) _audio.Reset();
                    if (_music != null) _music.RestoreImmediately();
                    if (_overlay != null) _overlay.Reset();
                    _localThreatActor = _pendingLocalDeath = null;
                    _testMusicOverride = false;
                    _cooldownUntil = 0f;
                    _rageTraitApplied = false;
                    _rageSpeedStage = -1;
                    _regenerationAt = 0f;
                    _regenDiagnosticPrinted = false;
                    ResetChaseProgress(now);
                    if (_scp != null && _traitsRegistered)
                        ShyGuyTraits.ClearRage(_world, _scp);
                }
            }
            if (_scp == null) return;

            // Repeated gradual healing. Not immortality: lethal damage can still kill
            // before the next 1-second recovery tick, including extreme environmental damage.
            if (_regenerationEnabled && now >= _regenerationAt)
            {
                _regenerationAt = now + 1f;
                int heal = Mathf.RoundToInt(_brain.State == ShyGuyState.Rage ||
                    _brain.State == ShyGuyState.Triggered ? _rageRegenPerSecond : _docileRegenPerSecond);
                bool success = _world.TryRegenerate(_scp, heal, out _lastRegenDetail);
                if (!_regenDiagnosticPrinted)
                {
                    _regenDiagnosticPrinted = true;
                    Log("REGEN: " + (success ? "metodo disponivel" : "indisponivel") +
                        " -> " + _lastRegenDetail);
                }
            }

            // Safe third-person detection: it never guesses actor/model coordinates.
            // No confirmed Worldfall-owned actor/camera/model => no witness registered.
            if (_worldfallLookEnabled && now >= _worldfallLookAt)
            {
                _worldfallLookAt = now + _worldfallRecheckSeconds;
                _worldfallSight.Configure(_worldfallFacingOffset, _worldfallFaceCone, _worldfallRequireFacing);
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
            float effectiveTriggerDelay = _audio == null ? _triggerDelay : Mathf.Max(_triggerDelay, _audio.SuggestedTriggerDelay);
            _brain.Tick(now, _victim != null, effectiveTriggerDelay, _calmingDelay);
            if (before != _brain.State)
            {
                Log("Estado: " + before + " -> " + _brain.State + " (" + _brain.LastReason + ")");
                if (_audio != null) _audio.StateChanged(_brain.State, now);
                if (_brain.State == ShyGuyState.Docile && before == ShyGuyState.Calming)
                {
                    _cooldownUntil = now + _cooldownSeconds;
                    Log("COOLDOWN DOCILE iniciado: " + _cooldownSeconds.ToString("0") + "s sem novas testemunhas");
                }
                if (_brain.State == ShyGuyState.Rage)
                {
                    ResetChaseProgress(now);
                    _rageTraitApplied = _traitsRegistered && ShyGuyTraits.SetRageStage(_world, _scp, 0);
                    _rageSpeedStage = _rageTraitApplied ? 0 : -1;
                    if (!_rageTraitApplied) Log("AVISO: bonus de velocidade da trait Rage nao foi aplicado");
                    _world.TryInterruptBehaviors(_scp);
                }
                else if (before == ShyGuyState.Rage)
                {
                    if (_traitsRegistered) ShyGuyTraits.ClearRage(_world, _scp);
                    _rageTraitApplied = false;
                    _rageSpeedStage = -1;
                    _world.TryInterruptBehaviors(_scp);
                }
            }

            if (_music != null)
            {
                bool markedPlayerAlive = _localThreatActor != null && _world.IsAlive(_localThreatActor);
                bool stillThreatening = _brain.State == ShyGuyState.Triggered || _brain.State == ShyGuyState.Rage;
                // Keep ducking throughout the entire chase, NOT just the trigger timer.
                _music.Request(_testMusicOverride || (markedPlayerAlive && stillThreatening));
            }
            if (_audio != null) _audio.Update(now, _brain.State, _world, _scp, _worldfallSight);

            if (_brain.State == ShyGuyState.Rage && _victim != null && _enableChase)
            {
                // Four discrete speed stages; no stacking. The timer resets on
                // every new FIFO target, so many victims never mean infinite speed.
                if (_traitsRegistered && _rageRampSeconds > 0f)
                {
                    int stage = Mathf.Clamp(Mathf.FloorToInt(
                        (now - _rageTargetStartedAt) / (_rageRampSeconds / 3f)), 0, 3);
                    if (stage != _rageSpeedStage)
                    {
                        bool applied = ShyGuyTraits.SetRageStage(_world, _scp, stage);
                        if (applied)
                        {
                            _rageSpeedStage = stage;
                            _rageTraitApplied = true;
                            Log("RAGE ACCEL: vitima atual estagio=" + stage + "/3; bonus speed=" +
                                new int[] { 85, 105, 125, 145 }[stage] + " (nao acumulativo)");
                        }
                        else Log("RAGE ACCEL: nao foi possivel aplicar estagio " + stage);
                    }
                }
                if (_world.Within(_scp, _victim, _attackDistance))
                {
                    if (!_killPending && now >= _attackRetryAt && _attackAttempts < MaxAttackAttempts)
                    {
                        _attackAttempts++;
                        _attackRetryAt = now + 3f;
                        float soundGain = _audio == null ? 0f : _audio.LocalGain(_world, _scp, _worldfallSight);
                        bool controlledBeforeDeath = _worldfallSight != null &&
                            ReferenceEquals(_victim, _worldfallSight.CurrentControlledActor);
                        _killPending = _world.TryKill(_victim);
                        if (_killPending)
                        {
                            _lastIssuedVictim = _victim;
                            if (_audio != null) _audio.Execution(soundGain);
                            if (controlledBeforeDeath && _deathFlashEnabled)
                            {
                                _pendingLocalDeath = _victim;
                                if (!_world.IsAlive(_victim)) ConfirmLocalDeath();
                            }
                        }
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
                // In WorldFall, NPCs still use the lightweight 2D proximity behavior.
                // The possessed human must see the FACE to become a witness.
                bool isControlled = _worldfallSight.Is3DWorldfallView &&
                    ReferenceEquals(actor, _worldfallSight.CurrentControlledActor);
                bool newlyObserved = _enableProximity &&
                    (!isControlled || _allowProximityWithWorldfall) &&
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
            // Cooldown only after an entire RAGE/Calming cycle. New witnesses can still
            // join while actively triggered or raging, without restarting the soundtrack.
            if (_brain.State == ShyGuyState.Calming ||
                (_brain.State == ShyGuyState.Docile && now < _cooldownUntil)) return;
            if (_traitsRegistered && !_world.AddTrait(candidate, ShyGuyTraits.WitnessId))
            {
                _sessionOnlyWitnesses.Add(candidate);
                Log("AVISO: nao foi possivel colocar trait Witness; alvo permanece apenas nesta sessao");
            }
            if (!_queue.Enqueue(candidate, _maxWitnesses)) return;
            Log("TESTEMUNHA MARCADA via " + source + "; fila=" + _queue.Count +
                "; distancia=" + _world.TargetDistance(_scp, candidate));
            if (_brain.State == ShyGuyState.Docile &&
                _brain.Transition(ShyGuyState.Triggered, now, "primeira testemunha: " + source))
            {
                if (_audio != null) _audio.StateChanged(ShyGuyState.Triggered, now);
            }
            if (_audio != null)
            {
                // Only show face cue if the viewer is actually the possessed player, or
                // in 2D when the SCP is visibly on the local map screen.
                bool playerWitness = _worldfallSight.Is3DWorldfallView &&
                    ReferenceEquals(candidate, _worldfallSight.CurrentControlledActor);
                bool mapWitness = !_worldfallSight.Is3DWorldfallView &&
                    _brain.State == ShyGuyState.Triggered && source.StartsWith("proximidade") &&
                    ScpVisibleOnMap();
                if (playerWitness)
                {
                    _localThreatActor = candidate;
                    if (_music != null) _music.Request(true);
                }
                if ((playerWitness || mapWitness) && _overlay != null && _audio.VisualCueEnabled)
                    _overlay.Show(_audio.FlashDuration,
                        playerWitness ? _audio.FlashOpacity : Mathf.Min(.42f, _audio.FlashOpacity),
                        playerWitness ? "rosto visto pelo jogador WorldFall" : "proximidade 2D", playerWitness);
            }
        }

        private bool ScpVisibleOnMap()
        {
            if (Camera.main == null || !_world.Position(_scp, out float x, out float y)) return false;
            Vector3 view = Camera.main.WorldToViewportPoint(new Vector3(x, y, 0f));
            return view.z > 0f && view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f;
        }

        private void UpdateCurrentTarget(float now)
        {
            object next = _queue.First;
            if (ReferenceEquals(next, _victim)) return;
            object old = _victim;
            if (old != null && !_world.IsAlive(old))
            {
                if (ReferenceEquals(old, _pendingLocalDeath)) ConfirmLocalDeath();
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

        private void ConfirmLocalDeath()
        {
            if (_pendingLocalDeath == null) return;
            _pendingLocalDeath = null;
            if (_deathFlashEnabled && _overlay != null)
                _overlay.Show(_deathFlashDuration, Mathf.Max(.55f, _audio == null ? .55f : _audio.FlashOpacity),
                    "morte confirmada causada pelo SCP", true);
            // Player is no longer alive. Restore music, while allowing the death flash.
            _localThreatActor = null;
            if (_music != null) _music.Request(false);
        }

        private void ResetChaseProgress(float now)
        {
            _chaseAt = _behaviorAt = 0f;
            _progressInitialized = _stuckWarningSent = _movementMethodLogged = false;
            _movementErrorLogged = _killPending = false;
            _attackAttempts = 0;
            _attackRetryAt = 0f;
            _lastProgressAt = now;
            _rageTargetStartedAt = now;
            _rageSpeedStage = -1;
            // Already in rage and changing target: immediately drop speed back
            // to stage zero rather than inheriting accumulated momentum.
            if (_brain.State == ShyGuyState.Rage && _scp != null && _traitsRegistered)
            {
                _rageTraitApplied = ShyGuyTraits.SetRageStage(_world, _scp, 0);
                _rageSpeedStage = _rageTraitApplied ? 0 : -1;
                Log("RAGE ACCEL: novo alvo; velocidade voltou ao estagio 0");
            }
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
        private void LateUpdate()
        {
            if (!_initialized) return;
            if (_music != null) _music.Tick(Time.unscaledTime, Time.unscaledDeltaTime);
            if (_overlay != null) _overlay.LateUpdate(
                _worldfallSight != null && _worldfallSight.Is3DWorldfallView);
        }
        private void OnDisable()
        {
            if (_music != null) _music.RestoreImmediately();
            if (_overlay != null) _overlay.Reset();
        }
        private void OnDestroy()
        {
            if (_music != null) _music.RestoreImmediately();
            if (_overlay != null) _overlay.Destroy();
        }
        private void OnGUI()
        {
            if (!_initialized) return;
            if (_overlay != null) _overlay.OnGUIFallback();
            if (_worldfallSight != null && _worldfallSight.Is3DWorldfallView && !string.IsNullOrEmpty(_worldfallSight.HudHint))
            {
                string hint = _worldfallSight.HudHint;
                Rect hr = new Rect((Screen.width - 420f) * .5f, Screen.height - 94f, 420f, 32f);
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, .55f);
                GUI.DrawTexture(new Rect(hr.x, hr.y, hr.width, hr.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(hr.x + 10f, hr.y + 6f, hr.width - 20f, 22f), "SCP-096: " + hint);
                GUI.color = old;
            }
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
            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0, 0, 324, 1330));
            float y = 0, x = 3;
            GUI.Label(new Rect(x,y,315,23), "Mapa: " + _world.Readiness); y+=23;
            GUI.Label(new Rect(x,y,315,23), "SCP: " + (_scp != null ? "encontrado" : "nenhum") +
                "  |  Estado: " + _brain.State); y+=23;
            GUI.Label(new Rect(x,y,315,23), "Fila de testemunhas: " + _queue.Count +
                " | Eliminadas: " + _killsConfirmed); y+=23;
            GUI.Label(new Rect(x,y,315,23), "Alvo: " + (_victim == null ? "nenhum" : _world.TargetDistance(_scp, _victim))); y+=25;
            _worldfallLookEnabled = GUI.Toggle(new Rect(x,y,315,23), _worldfallLookEnabled, "Worldfall: detectar olhar (experimental)"); y+=24;
            _worldfallRequireFacing = GUI.Toggle(new Rect(x,y,315,23), _worldfallRequireFacing,
                "Exigir SCP de frente (Heading)"); y+=24;
            if (GUI.Button(new Rect(x,y,316,25), "Inverter sentido do rosto (offset 180)") )
                _worldfallFacingOffset = _worldfallFacingOffset == 0f ? 180f : 0f;
            y+=29;
            GUI.Label(new Rect(x,y,315,23), "Offset rosto: " + _worldfallFacingOffset.ToString("0") +
                " deg | F10 para diagnostico"); y+=24;
            GUI.Label(new Rect(x,y,315,40), "Visao 3D: " + _worldfallSight.ShortStatus); y+=42;
            _enableChase = GUI.Toggle(new Rect(x,y,315,23), _enableChase, "Perseguir e eliminar"); y+=25;
            _enableProximity = GUI.Toggle(new Rect(x,y,320,23), _enableProximity,
                "Marcar NPCs a " + _proximityDistance.ToString("0") + " tiles (2D)"); y+=26;
            if (_audio != null)
            {
                _audio.Enabled = GUI.Toggle(new Rect(x,y,315,23), _audio.Enabled,
                    "Audio de terror (global/local)"); y+=24;
                _audio.VisualCueEnabled = GUI.Toggle(new Rect(x,y,315,23), _audio.VisualCueEnabled,
                    "Flash SCP-096 (Canvas WorldFall)"); y+=24;
                if (_overlay != null) _overlay.Enabled = _audio.VisualCueEnabled;
                GUI.Label(new Rect(x,y,315,23), "Volume " + (int)(_audio.MasterVolume * 100f) +
                    "% | DOCILE cooldown " + Mathf.Max(0f, _cooldownUntil - Time.unscaledTime).ToString("0") + "s"); y+=22;
                _audio.MasterVolume = GUI.HorizontalSlider(new Rect(x,y+2f,315,15),
                    _audio.MasterVolume, 0f, 1f); y+=22;
                GUI.Label(new Rect(x,y,315,20), "Fade do flash: " +
                    _audio.FlashDuration.ToString("0.00") + "s | opacidade: " +
                    _audio.FlashOpacity.ToString("0.00")); y+=21;
                _audio.FlashDuration = GUI.HorizontalSlider(new Rect(x,y+2f,315,15),
                    _audio.FlashDuration, .4f, 3.5f); y+=20;
                _audio.FlashOpacity = GUI.HorizontalSlider(new Rect(x,y+2f,315,15),
                    _audio.FlashOpacity, .05f, .85f); y+=24;
                if (GUI.Button(new Rect(x,y,152,28), "Testar som global")) _audio.TestCue();
                if (GUI.Button(new Rect(x+160,y,156,28), "Testar musica")) _audio.TestMusic();
                y+=34;
                if (GUI.Button(new Rect(x,y,316,27), "Parar sons de teste")) _audio.StopTests();
                y+=33;
                if (GUI.Button(new Rect(x,y,316,28), "Preview flash 3D / 2D"))
                    if (_overlay != null) _overlay.Show(_audio.FlashDuration, _audio.FlashOpacity, "preview F5");
                y+=34;
                if (_music != null)
                {
                    _music.Enabled = GUI.Toggle(new Rect(x,y,315,23), _music.Enabled,
                        "Silenciar BGM somente quando voce for vitima");
                    y += 26;
                }
                if (GUI.Button(new Rect(x,y,152,28), _testMusicOverride ? "Restaurar BGM" : "Testar silenciar BGM"))
                {
                    _testMusicOverride = !_testMusicOverride;
                    if (_music != null) _music.Request(_testMusicOverride);
                }
                if (GUI.Button(new Rect(x+160,y,156,28), "Restaurar musica"))
                {
                    _testMusicOverride = false;
                    if (_music != null) _music.RestoreImmediately();
                }
                y+=34;
                if (GUI.Button(new Rect(x,y,316,26), "Preview flash de morte (sem morrer)"))
                    if (_overlay != null) _overlay.Show(_deathFlashDuration, .68f, "preview de morte F5");
                y+=31;
                _deathFlashEnabled = GUI.Toggle(new Rect(x,y,315,23), _deathFlashEnabled,
                    "Jumpscare apenas na morte pelo SCP");
                y+=27;
                GUI.Label(new Rect(x,y,315,22), "Override: " + (_music == null ? "n/a" : _music.Status));
                y+=26;
            }
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
            _cooldownUntil = 0f;
            if (_audio != null) _audio.Reset();
            if (_overlay != null) _overlay.Reset();
            if (_music != null) _music.RestoreImmediately();
            _localThreatActor = _pendingLocalDeath = null;
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
                " proximidade=" + _enableProximity + " distancia=" + _world.TargetDistance(_scp, _victim) +
                " rageStage=" + _rageSpeedStage + " regen=" + _lastRegenDetail);
            Log(_world.DiagnosticMethods(_scp));
            Log(_world.DiagnosticShadows());
            Log("Ultimo movimento=" + _world.LastMoveSignature + " ultima execucao=" + _world.LastKillSignature);
            Log("MUSIC=" + (_music == null ? "null" : _music.Status) +
                " OVERLAY=" + (_overlay == null ? "null" : _overlay.Status));
            Log("Audio=" + (_audio == null ? "null" : _audio.Summary) +
                " cooldownRestante=" + Mathf.Max(0f, _cooldownUntil - Time.unscaledTime).ToString("0.0"));
            Log(_worldfallSight.Diagnostic(_world, _scp));
        }
        private void ReadSettings()
        {
            try
            {
                _triggerDelay = ConfigFloat("TriggerDelay", 55f, 10f, 120f);
                _rageRampSeconds = ConfigFloatOptional("RageRampSeconds", 45f, 9f, 240f);
                _rageRegenPerSecond = ConfigFloatOptional("RageRegenPerSecond", 2000f, 0f, 10000f);
                _docileRegenPerSecond = ConfigFloatOptional("DocileRegenPerSecond", 700f, 0f, 5000f);
                try { object v = GetConfig()["SCP096"]["RegenerationEnabled"].GetValue();
                    if (v is bool) _regenerationEnabled = (bool)v; } catch { }
                _cooldownSeconds = ConfigFloat("DocileCooldownSeconds", 15f, 0f, 120f);
                _volume = ConfigFloat("SoundVolume", .7f, 0f, 1f);
                _flashDuration = ConfigFloatOptional("FaceFlashDuration", 1.65f, .4f, 3.5f);
                _flashOpacity = ConfigFloatOptional("FaceFlashOpacity", .60f, .05f, .85f);
                object audioSwitch = GetConfig()["SCP096"]["SoundEnabled"].GetValue();
                if (audioSwitch is bool) _soundEnabled = (bool)audioSwitch;
                object flashSwitch = GetConfig()["SCP096"]["FaceFlashEnabled"].GetValue();
                if (flashSwitch is bool) _visualCueEnabled = (bool)flashSwitch;
                _deathFlashDuration = ConfigFloatOptional("DeathFlashDuration", 2.6f, .4f, 6f);
                try
                {
                    object v = GetConfig()["SCP096"]["DeathFlashEnabled"].GetValue();
                    if (v is bool) _deathFlashEnabled = (bool)v;
                }
                catch { }
                try
                {
                    object v = GetConfig()["SCP096"]["MusicOverrideEnabled"].GetValue();
                    if (v is bool) _musicOverrideEnabled = (bool)v;
                }
                catch { }
                _musicFadeDown = ConfigFloatOptional("MusicFadeDownSeconds", 2.8f, .2f, 12f);
                _musicFadeUp = ConfigFloatOptional("MusicFadeUpSeconds", 3.5f, .2f, 12f);
                _musicDuckingPercent = ConfigFloatOptional("MusicDuckingPercent", 100f, 0f, 100f);
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
                _worldfallFaceCone = ConfigFloat("WorldfallFaceConeDegrees", 65f, 10f, 89f);
                _worldfallFacingOffset = ConfigFloat("WorldfallFacingOffsetDegrees", 0f, -360f, 360f);
                object facing = GetConfig()["SCP096"]["WorldfallRequireFacing"].GetValue();
                if (facing is bool) _worldfallRequireFacing = (bool)facing;
                object allowProximity = GetConfig()["SCP096"]["AllowProximityWhilePossessing"].GetValue();
                if (allowProximity is bool) _allowProximityWithWorldfall = (bool)allowProximity;
                object debug = GetConfig()["SCP096"]["DebugLogs"].GetValue();
                if (debug is bool) _debugLogs = (bool)debug;
            }
            catch (Exception ex) { Log("Usando defaults: " + ex.Message); }
        }
        // Older NML user configs may not contain fields added in this update.
        // Do not abort the rest of ReadSettings when they are missing.
        private float ConfigFloatOptional(string key, float fallback, float min, float max)
        {
            try { return ConfigFloat(key, fallback, min, max); }
            catch { return fallback; }
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
