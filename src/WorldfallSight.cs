using System;
using System.Reflection;
using UnityEngine;

namespace SCP096TheShyGuy
{
    // WorldFall 2026-10-16 assembly-inspected, OPTIONAL adapter.
    // Worldfall's view is NOT a UnityEngine.Camera. It is a ViewState consumed
    // by its own CPU/GPU renderer. Finding one via Camera.allCameras is wrong.
    // DO NOT auto-mark without trustworthy 3D visibility/face verification.
    internal sealed class WorldfallSight
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private MonoBehaviour _mod;
        private Type _modType;
        private MethodInfo _isFirstPerson, _getHost, _getView;
        private FieldInfo _viewX, _viewY, _viewZ, _viewYaw, _viewPitch, _viewFov;
        private bool _loaded, _in3D;
        private object _controlled;
        private float _x, _y, _z, _yaw, _pitch, _fov;
        private float _nextDiscovery, _nextRead;
        private int _reads;
        private string _transition = "", _status = "aguardando WorldFall";
        internal string ShortStatus => _status.Length > 62 ? _status.Substring(0, 62) : _status;
        internal bool Is3DWorldfallView => _loaded && _in3D;
        internal bool IsPossessing => Is3DWorldfallView && _controlled != null;
        internal object CurrentControlledActor => IsPossessing ? _controlled : null;
        internal string TakeTransition() { string s = _transition; _transition = ""; return s; }
        internal void Reset()
        {
            _controlled = null; _in3D = false; _transition = "";
            _nextDiscovery = _nextRead = 0f; _reads = 0;
            _status = "aguardando WorldFall";
            // WorldFall might be recreated on map reload; force a fresh reference.
            _mod = null; _modType = null; _loaded = false;
        }
        private static float FloatField(FieldInfo f, object value)
        {
            try { return f == null ? float.NaN : Convert.ToSingle(f.GetValue(value)); }
            catch { return float.NaN; }
        }
        private void Discover(float now)
        {
            if (_mod != null) return;
            if (now < _nextDiscovery) return;
            _nextDiscovery = now + 4f;
            try
            {
                // The inspected assembly identifies FirstPerson.WorldBoxMod as MonoBehaviour.
                foreach (MonoBehaviour c in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (c == null || c.GetType().FullName != "FirstPerson.WorldBoxMod") continue;
                    _mod = c;
                    _modType = c.GetType();
                    _isFirstPerson = _modType.GetMethod("get_IsFirstPerson", Any);
                    _getHost = _modType.GetMethod("get_Host", Any);
                    _getView = _modType.GetMethod("get_View", Any);
                    _loaded = _isFirstPerson != null && _getHost != null && _getView != null;
                    _status = _loaded ? "WorldFall API real encontrada" : "WorldFall: getters incompletos";
                    _transition = "Bridge v0.2.3: componente WorldBoxMod " + (_loaded ? "encontrado" : "incompleto");
                    return;
                }
                _status = "Worldfall.WorldBoxMod nao instanciado";
            }
            catch (Exception e)
            {
                _status = "Worldfall discover: " + e.GetType().Name;
            }
        }
        private void Read(float now)
        {
            if (now < _nextRead) return;
            _nextRead = now + .4f;
            Discover(now);
            if (!_loaded || _mod == null) return;
            try
            {
                bool active = (bool)_isFirstPerson.Invoke(_mod, null);
                object host = active ? _getHost.Invoke(_mod, null) : null;
                bool was = _in3D;
                _in3D = active;
                _controlled = host != null && host.GetType().Name == "Actor" ? host : null;
                if (was != _in3D)
                    _transition = _in3D ? "WORLD FALL 3D ATIVO: Host=" +
                        (_controlled != null ? "Actor" : "indisponivel") : "WorldFall voltou ao mapa";
                if (!_in3D) { _status = "WorldFall modo mapa (2D)"; return; }
                object view = _getView.Invoke(_mod, null);
                if (view == null) { _status = "Worldfall View nulo"; return; }
                Type t = view.GetType();
                if (_viewX == null || _viewX.DeclaringType != t)
                {
                    _viewX = t.GetField("X", Any); _viewY = t.GetField("Y", Any);
                    _viewZ = t.GetField("Z", Any); _viewYaw = t.GetField("Yaw", Any);
                    _viewPitch = t.GetField("Pitch", Any); _viewFov = t.GetField("Fov", Any);
                }
                _x = FloatField(_viewX, view); _y = FloatField(_viewY, view);
                _z = FloatField(_viewZ, view); _yaw = FloatField(_viewYaw, view);
                _pitch = FloatField(_viewPitch, view); _fov = FloatField(_viewFov, view);
                _reads++;
                _status = _controlled == null ? "WorldFall 3D: Host nao encontrado" :
                    "WorldFall 3D: Actor+ViewState OK; oclusao pendente";
            }
            catch (Exception e)
            {
                _in3D = false; _controlled = null;
                _status = "WorldFall Read: " + e.GetType().Name;
            }
        }
        internal bool Check(WorldBridge world, object scp, float maxTiles,
            float holdSeconds, float now, out object observer)
        {
            observer = null;
            Read(now);
            if (!IsPossessing || scp == null || ReferenceEquals(_controlled, scp) ||
                !world.IsAlive(_controlled)) return false;
            // The next step is to correlate GPU picker entity tags with the SCP
            // and restrict hits to its head. Until proven, NEVER infer direct
            // line-of-sight from distance, or victims will trigger through walls.
            // This explicit fail-closed behavior is intentional in v0.2.3.
            return false;
        }
        internal string DetailedSnapshot(WorldBridge world, object scp)
        {
            Read(Time.unscaledTime);
            string relative = "sem posicao SCP";
            if (scp != null && world.Position(scp, out float sx, out float sy) &&
                !float.IsNaN(_x) && !float.IsNaN(_y))
            {
                float dx = sx - _x, dy = sy - _y;
                relative = "SCP dx=" + dx.ToString("0.0") +
                    " dy=" + dy.ToString("0.0") +
                    " bearing_atan2_deg=" + (Mathf.Atan2(dy, dx) * Mathf.Rad2Deg).ToString("0.0") +
                    " (calibracao eixo/direcao pendente)";
            }
            return Diagnostic(world, scp) + " | " + relative;
        }
        internal string Diagnostic(WorldBridge world, object scp)
        {
            Read(Time.unscaledTime);
            return "WorldfallSight v0.2.3 bridge=" + _loaded +
                " component=" + (_mod != null ? _mod.GetType().FullName : "null") +
                " firstperson=" + _in3D + " host=" +
                (_controlled != null ? world.AssetId(_controlled) : "null") +
                " view=(" + _x.ToString("0.00") + "," + _y.ToString("0.00") +
                "," + _z.ToString("0.00") + ") yaw=" + _yaw.ToString("0.000") +
                " pitch=" + _pitch.ToString("0.000") +
                " fov=" + _fov.ToString("0.000") + " reads=" + _reads +
                " status=" + _status + " autotrigger=false (GPU face/occlusion pending)";
        }
    }
}
