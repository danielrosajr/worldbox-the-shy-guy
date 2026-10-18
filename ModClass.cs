using System;
using System.Globalization;
using NeoModLoader.api;
using UnityEngine;

namespace SCP096TheShyGuy
{
    public sealed class ModClass : BasicMod<ModClass>
    {
        private WorldBridge _world;
        private ShyGuyBrain _brain;
        private object _scp;
        private object _victim;
        private object _observedMap;
        private float _tickAt;
        private float _scanAt;
        private float _chaseAt;
        private bool _movementWarningSent;
        private bool _movementMethodLogged;
        private bool _enableChase = true;
        private float _lastProgressAt;
        private float _lastProgressX;
        private float _lastProgressY;
        private bool _progressInitialized;
        private bool _stuckWarningSent;
        private bool _attackWarningSent;
        private float _attackRetryAt;
        private int _attackAttempts;
        private const int MaxAttackAttempts = 3;
        private bool _initialized;
        private bool _showPanel = true;
        private bool _placeOnMap;
        private string _status = "Inicializando...";
        private string _registrationError = "";
        private float _triggerDelay = 8f;
        private float _calmingDelay = 4f;
        private float _tickSeconds = 0.30f;
        private float _attackDistance = 1.7f;
        private bool _debugLogs = true;

        private Rect PanelRect => new Rect(Mathf.Max(4, Screen.width - 352), 72, 345, 478);

        protected override void OnModLoad()
        {
            _world = new WorldBridge();
            _brain = new ShyGuyBrain();
            ReadSettings();
            string reason;
            if (_world.TryRegisterAsset(GetDeclaration().FolderPath, out reason))
                Log("Asset registrado: " + WorldBridge.ScpId);
            else
            {
                _registrationError = reason;
                Log("Registro pendente: " + reason + " (tentaremos novamente ao gerar).");
            }
            _initialized = true;
            Log("v0.1.3 alpha: teste de morte dieSimpleNone(); F5 painel; F6 spawn; F7 alvo; F8 cancelar; F9 diagnostico.");
        }

        private void Update()
        {
            if (!_initialized) return;
            float now = Time.unscaledTime;
            if (Input.GetKeyDown(KeyCode.F5)) _showPanel = !_showPanel;
            if (Input.GetKeyDown(KeyCode.Escape)) _placeOnMap = false;
            if (Input.GetKeyDown(KeyCode.F6))
                Spawn(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (Input.GetKeyDown(KeyCode.F7)) MarkVictim(now);
            if (Input.GetKeyDown(KeyCode.F8)) CancelTarget();
            if (Input.GetKeyDown(KeyCode.F9)) Diagnostic();
            if (_placeOnMap && Input.GetMouseButtonDown(0) && !PointerInsidePanel())
            {
                _placeOnMap = false;
                SpawnOnClickedTile();
            }

            if (now < _tickAt) return;
            _tickAt = now + _tickSeconds;
            if (!_world.HasMap) return;
            if (!ReferenceEquals(_observedMap, _world.Map))
            {
                _observedMap = _world.Map;
                _scp = null;
                _victim = null;
                _brain.Reset(now);
                _scanAt = 0;
                _placeOnMap = false;
                ResetChaseProgress(now);
            }
            if (now >= _scanAt)
            {
                _scanAt = now + 2.5f;
                if (!_world.Exists(_scp))
                {
                    _scp = _world.FindExistingScp();
                    _victim = null;
                    _brain.Reset(now);
                    _movementWarningSent = false;
                    _attackWarningSent = false;
                    _movementMethodLogged = false;
                    ResetChaseProgress(now);
                }
                if (_victim != null && !_world.IsAlive(_victim))
                {
                    Log("Vitima nao esta mais viva; encerrando alvo.");
                    _victim = null;
                }
            }
            if (_scp == null) return;
            ShyGuyState before = _brain.State;
            _brain.Tick(now, _victim != null, _triggerDelay, _calmingDelay);
            if (before != _brain.State)
            {
                Log("Estado: " + before + " -> " + _brain.State + " (" + _brain.LastReason + ")");
                _world.TrySetSpeed(_brain.State == ShyGuyState.Rage ? 100f : 12f);
                if (_brain.State == ShyGuyState.Rage) ResetChaseProgress(now);
            }
            if (_brain.State == ShyGuyState.Rage && _victim != null && _enableChase)
            {
                if (_world.Within(_scp, _victim, _attackDistance))
                {
                    if (now >= _attackRetryAt && _attackAttempts < MaxAttackAttempts)
                    {
                        _attackRetryAt = now + 2f;
                        _attackAttempts++;
                        if (_world.TryKill(_victim))
                        {
                            Log("EXECUCAO CONFIRMADA: " + _world.LastKillSignature);
                            _victim = null;
                        }
                        else if (!_attackWarningSent)
                        {
                            Log("Ataque tentativa " + _attackAttempts + ": " + _world.LastKillSignature);
                            _attackWarningSent = true;
                        }
                        else if (_attackAttempts >= MaxAttackAttempts)
                            Log("ATAQUE INTERROMPIDO apos 3 tentativas: " + _world.LastKillSignature +
                                ". Use F9 e envie Player.log.");
                    }
                }
                else
                {
                    TrackChaseProgress(now);
                    if (now >= _chaseAt)
                    {
                        // Avoid repeatedly resetting a live path on every sub-second tick.
                        _chaseAt = now + 1.5f;
                        if (!_world.TryChase(_scp, _victim))
                        {
                            if (!_movementWarningSent)
                            {
                                Log("Perseguicao nao confirmada: " + _world.LastMoveSignature);
                                _movementWarningSent = true;
                            }
                        }
                        else if (!_movementMethodLogged)
                        {
                            _movementMethodLogged = true;
                            Log("Perseguicao enviada por " + _world.LastMoveSignature +
                                ". Isto nao garante deslocamento real; observe a distancia no painel.");
                        }
                    }
                }
            }
        }

        private void ResetChaseProgress(float now)
        {
            _chaseAt = 0f;
            _progressInitialized = false;
            _stuckWarningSent = false;
            _lastProgressAt = now;
            _attackRetryAt = 0;
            _attackAttempts = 0;
            _attackWarningSent = false;
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
            float dx = x - _lastProgressX;
            float dy = y - _lastProgressY;
            if (dx * dx + dy * dy >= 0.75f * 0.75f)
            {
                _lastProgressX = x;
                _lastProgressY = y;
                _lastProgressAt = now;
                _stuckWarningSent = false;
            }
            else if (!_stuckWarningSent && now - _lastProgressAt > 8f)
            {
                // Diagnostic only: do not teleport or edit actor position directly.
                Log("AVISO: SCP parado ha mais de 8s durante RAGE. Metodo=" + _world.LastMoveSignature);
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
                if (GUI.Button(new Rect(Mathf.Max(4, Screen.width - 133), 72, 125, 30), "SCP-096 [F5]"))
                    _showPanel = true;
                return;
            }

            GUI.Box(r, "SCP-096 - THE SHY GUY  |  v0.1.3");
            float x = r.x + 12, y = r.y + 30;
            GUI.Label(new Rect(x, y, 315, 23), "Mapa: " + _world.Readiness);
            y += 24;
            GUI.Label(new Rect(x, y, 315, 23), "Asset: " +
                (_world.AssetRegistered ? "REGISTRADO" : "PENDENTE (ver diagnostico)"));
            y += 24;
            GUI.Label(new Rect(x, y, 315, 23), "Criatura: " + (_scp != null ? "encontrada" : "nao encontrada") +
                "  |  Estado: " + _brain.State);
            y += 24;
            GUI.Label(new Rect(x, y, 319, 22), "Alvo: " + (_victim == null ? "nenhum" : _world.TargetDistance(_scp, _victim)));
            y += 22;
            bool enableChase = GUI.Toggle(new Rect(x, y, 315, 22), _enableChase,
                "Perseguir/atacar (desmarcar p/ teste)");
            if (enableChase != _enableChase)
            {
                _enableChase = enableChase;
                ResetChaseProgress(Time.unscaledTime);
                Log(_enableChase ? "Perseguicao habilitada." :
                    "Perseguicao e ataque do SCP suspensos para teste de compatibilidade.");
            }
            y += 28;
            if (GUI.Button(new Rect(x, y, 153, 32), "Gerar aqui [F6]")) Spawn(false);
            if (GUI.Button(new Rect(x + 161, y, 158, 32), "Gerar isolado")) Spawn(true);
            y += 39;
            if (GUI.Button(new Rect(x, y, 319, 32), _placeOnMap ? "AGUARDANDO CLIQUE NO MAPA (ESC cancela)" :
                "Posicionar SCP: clicar no mapa"))
            {
                _placeOnMap = !_placeOnMap;
                if (_placeOnMap) Log("Modo posicionamento ativado: clique em uma area de terra no mapa.");
            }
            y += 39;
            if (GUI.Button(new Rect(x, y, 153, 30), "Marcar alvo [F7]")) MarkVictim(Time.unscaledTime);
            if (GUI.Button(new Rect(x + 161, y, 158, 30), "Cancelar [F8]")) CancelTarget();
            y += 38;
            if (GUI.Button(new Rect(x, y, 153, 29), "Teste: humano")) SpawnTestHuman();
            if (GUI.Button(new Rect(x + 161, y, 158, 29), "Diagnostico [F9]")) Diagnostic();
            y += 37;
            if (GUI.Button(new Rect(x, y, 319, 29), "Teste: eliminar alvo selecionado (manual)")) TestKillVictim();
            y += 36;
            GUI.Label(new Rect(x, y, 319, 20), "Resultado da ultima acao:");
            y += 21;
            GUIStyle feedbackStyle = new GUIStyle(GUI.skin.label);
            feedbackStyle.wordWrap = true;
            GUI.Label(new Rect(x, y, 319, 76), _status, feedbackStyle);
            if (GUI.Button(new Rect(r.x + r.width - 26, r.y + 5, 20, 20), "X")) _showPanel = false;
        }

        private void Spawn(bool isolated)
        {
            if (!_world.HasMap) { Log("Nao foi possivel gerar: " + _world.Readiness); return; }
            object tile = isolated ? _world.IsolatedTile() : _world.VisibleTile();
            if (tile == null)
            {
                Log(isolated ? "Nao encontrei area isolada. Use posicionamento manual."
                    : "Centro da tela sem terreno valido. Use 'Posicionar SCP' e clique na terra.");
                return;
            }
            SpawnAtTile(tile);
        }

        private void SpawnOnClickedTile()
        {
            if (!_world.HasMap) { Log("Nao foi possivel gerar: " + _world.Readiness); return; }
            object tile = _world.MouseTile();
            if (tile == null) { Log("Nao foi possivel localizar o tile do mouse; teste F6 e confira o diagnostico."); return; }
            SpawnAtTile(tile);
        }

        private void SpawnAtTile(object tile)
        {
            if (_world.FindExistingScp() != null)
            {
                _scp = _world.FindExistingScp();
                Log("Ja existe um SCP-096 neste mundo. Nao criei duplicata. Coordenadas: " +
                    _world.TileCoordinates(_world.ActorTile(_scp)));
                return;
            }
            if (!_world.AssetRegistered)
            {
                if (!_world.TryRegisterAsset(GetDeclaration().FolderPath, out string error))
                {
                    _registrationError = error;
                    Log("REGISTRO FALHOU: " + error);
                    return;
                }
                _registrationError = "";
            }
            object actor = _world.SpawnAt(tile, out string reason);
            if (actor == null)
            {
                Log("SPAWN FALHOU em " + _world.TileCoordinates(tile) + ": " + reason);
                return;
            }
            _scp = actor;
            _victim = null;
            _brain.Reset(Time.unscaledTime);
            _movementWarningSent = _attackWarningSent = _movementMethodLogged = false;
            ResetChaseProgress(Time.unscaledTime);
            Log("SCP-096 criado em " + _world.TileCoordinates(tile) +
                ". Visual pode aparecer como humano ate validarmos as texturas.");
        }

        private void SpawnTestHuman()
        {
            if (!_world.HasMap) { Log("Teste humano falhou: " + _world.Readiness); return; }
            object tile = _world.VisibleTile();
            if (tile == null) { Log("Teste humano: nenhum tile visivel valido."); return; }
            object human = _world.SpawnTestHuman(tile, out string reason);
            Log(human == null ? "Teste humano FALHOU: " + reason :
                "Teste humano OK em " + _world.TileCoordinates(tile) +
                ". Se SCP falhar, envie o diagnostico e logs.");
        }

        private void MarkVictim(float now)
        {
            if (!_world.HasMap) { Log("Marcar alvo falhou: " + _world.Readiness); return; }
            _scp = _world.FindExistingScp();
            if (_scp == null) { Log("Primeiro crie um SCP-096 usando o painel ou F6."); return; }
            object cursor = _world.MouseTile();
            object victim = cursor == null ? null : _world.NearestVictim(_scp, cursor, 12f);
            if (victim == null) victim = _world.NearestVictim(_scp, _world.ActorTile(_scp), 70f);
            if (victim == null) { Log("Nenhum personagem encontrado perto do cursor ou SCP."); return; }
            _victim = victim;
            ResetChaseProgress(now);
            _movementMethodLogged = false;
            if (_brain.State == ShyGuyState.Docile || _brain.State == ShyGuyState.Calming)
                _brain.Transition(ShyGuyState.Triggered, now, "testemunha marcada manualmente");
            Log("Alvo marcado. Estado=" + _brain.State + "; preparacao=" + _triggerDelay + "s.");
        }

        private void TestKillVictim()
        {
            if (!_world.HasMap || _victim == null || !_world.IsAlive(_victim))
            {
                Log("Teste de morte: marque primeiro uma vitima viva com F7.");
                return;
            }
            // Explicit debug-only action: unlike RAGE, ignores target distance.
            // NEVER use in a non-disposable world; this intentionally kills one NPC.
            if (_world.TryKill(_victim))
            {
                Log("TESTE DE MORTE OK: " + _world.LastKillSignature);
                _victim = null;
                ResetChaseProgress(Time.unscaledTime);
            }
            else
                Log("TESTE DE MORTE FALHOU: " + _world.LastKillSignature);
        }

        private void CancelTarget()
        {
            _victim = null;
            ResetChaseProgress(Time.unscaledTime);
            Log("Alvo removido. SCP voltara a DOCILE pelo ciclo normal.");
        }

        private void Diagnostic()
        {
            string details = _world.DiagnosticReadiness();
            Log("Diagnostico: " + details);
            Log("Estado=" + _brain.State + ", SCP=" + (_scp != null) + ", alvo=" + (_victim != null) +
                ", chaseEnabled=" + _enableChase + ", lastMove=" + _world.LastMoveSignature +
                ", distancia=" + _world.TargetDistance(_scp, _victim));
            Log(_world.DiagnosticMethods(_scp));
            Log(_world.DiagnosticShadows());
            Log("Ultimo ataque=" + _world.LastKillSignature + ", tentativas=" + _attackAttempts);
            _status = "Diagnostico gravado em Player.log. " + details;
        }

        private void ReadSettings()
        {
            try
            {
                _triggerDelay = ConfigFloat("TriggerDelay", 8, 0, 120);
                _calmingDelay = ConfigFloat("CalmingDelay", 4, 0, 120);
                _tickSeconds = ConfigFloat("TickSeconds", .30f, .1f, 5f);
                _attackDistance = ConfigFloat("AttackDistance", 1.7f, .5f, 10f);
                object debug = GetConfig()["SCP096"]["DebugLogs"].GetValue();
                if (debug is bool) _debugLogs = (bool)debug;
            }
            catch (Exception e) { Log("Configuracao padrao aplicada: " + e.Message); }
        }

        private float ConfigFloat(string id, float fallback, float min, float max)
        {
            object val = GetConfig()["SCP096"][id].GetValue();
            if (val == null) return fallback;
            if (!float.TryParse(Convert.ToString(val), NumberStyles.Float, CultureInfo.InvariantCulture, out float n))
                return fallback;
            return Mathf.Clamp(n, min, max);
        }

        private void Log(string message)
        {
            _status = message;
            if (_debugLogs) Debug.Log("[SCP096] " + message);
        }
    }
}
