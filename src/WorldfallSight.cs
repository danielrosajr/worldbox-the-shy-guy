using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SCP096TheShyGuy
{
    // Uses the actual Worldfall.dll inspected on 2026-10-18.
    // GPU/CPU renderers already resolve which BaseSimObject is under the centre aim.
    // We READ WorldBoxMod.get_Picked, never request GPU readbacks or change renderer state.
    // SceneCollector._headings is an observed WorldFall cache, NOT a published API.
    // Every unknown or unavailable check fails closed; fallback gameplay remains untouched.
    internal sealed class WorldfallSight
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private MonoBehaviour _mod;
        private Type _type;
        private MethodInfo _isFirstPerson, _getHost, _getView, _getPicked;
        private FieldInfo _viewX, _viewY, _viewZ, _viewYaw, _viewPitch, _viewFov;
        private object _controlled, _picked;
        private float _x, _y, _z, _yaw, _pitch, _fov;
        private float _nextDiscovery, _nextRead, _lookSince = -1f;
        private int _reads, _successfulPicks;
        private bool _loaded, _in3D, _markedThisLook;
        private string _transition = "", _status = "aguardando WorldFall";
        private string _aimDetail = "sem leitura", _faceDetail = "sem leitura", _lastReason = "aguardando";
        private float _faceOffsetDegrees, _faceConeDegrees = 65f;
        private bool _requireFacing = true;
        internal string ShortStatus => _status.Length > 62 ? _status.Substring(0, 62) : _status;
        internal bool Is3DWorldfallView => _loaded && _in3D;
        internal bool IsPossessing => Is3DWorldfallView && _controlled != null;
        internal object CurrentControlledActor => IsPossessing ? _controlled : null;
        internal string TakeTransition() { string s = _transition; _transition = ""; return s; }
        internal string LastReason => _lastReason;
        internal void Configure(float faceOffset, float cone, bool requireFacing)
        {
            _faceOffsetDegrees = faceOffset;
            _faceConeDegrees = Mathf.Clamp(cone, 10f, 89f);
            _requireFacing = requireFacing;
        }
        internal void Reset()
        {
            _controlled = _picked = null;
            _in3D = _loaded = _markedThisLook = false;
            _lookSince = -1f;
            _nextDiscovery = _nextRead = 0;
            _reads = _successfulPicks = 0;
            _transition = ""; _status = "aguardando WorldFall";
            _lastReason = "aguardando";
            _mod = null; _type = null;
        }
        private static float ReadNumber(FieldInfo f, object o)
        {
            try { return f == null ? float.NaN : Convert.ToSingle(f.GetValue(o)); }
            catch { return float.NaN; }
        }
        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        private void Discover(float now)
        {
            if (_mod != null || now < _nextDiscovery) return;
            _nextDiscovery = now + 4f;
            try
            {
                foreach (MonoBehaviour candidate in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (candidate == null || candidate.GetType().FullName != "FirstPerson.WorldBoxMod") continue;
                    _mod = candidate; _type = candidate.GetType();
                    _isFirstPerson = _type.GetMethod("get_IsFirstPerson", Any);
                    _getHost = _type.GetMethod("get_Host", Any);
                    _getView = _type.GetMethod("get_View", Any);
                    _getPicked = _type.GetMethod("get_Picked", Any);
                    _loaded = _isFirstPerson != null && _getHost != null &&
                        _getView != null && _getPicked != null;
                    _status = _loaded ? "WorldFall bridge+pick encontrado" : "WorldFall getters incompletos";
                    _transition = "Bridge v0.2.4: camera, host e get_Picked=" + _loaded;
                    return;
                }
                _status = "WorldFall nao instanciado";
            }
            catch (Exception e) { _status = "Descoberta WorldFall: " + e.GetType().Name; }
        }
        private void Read(float now)
        {
            if (now < _nextRead) return;
            _nextRead = now + .18f;
            Discover(now);
            if (!_loaded || _mod == null) return;
            try
            {
                bool active = (bool)_isFirstPerson.Invoke(_mod, null);
                object host = active ? _getHost.Invoke(_mod, null) : null;
                bool before = _in3D;
                _in3D = active;
                _controlled = host != null && host.GetType().Name == "Actor" ? host : null;
                if (before != active)
                {
                    _transition = active ? "WORLD FALL 3D ATIVO: Host=" +
                        (_controlled == null ? "null" : "Actor") : "WorldFall voltou ao mapa";
                    ClearHold();
                }
                if (!active) { _picked = null; _status = "WorldFall modo mapa"; return; }
                object view = _getView.Invoke(_mod, null);
                if (view == null) { _status = "WorldFall View nula"; _picked = null; return; }
                Type t = view.GetType();
                if (_viewX == null || _viewX.DeclaringType != t)
                {
                    _viewX = t.GetField("X", Any); _viewY = t.GetField("Y", Any);
                    _viewZ = t.GetField("Z", Any); _viewYaw = t.GetField("Yaw", Any);
                    _viewPitch = t.GetField("Pitch", Any); _viewFov = t.GetField("Fov", Any);
                }
                _x = ReadNumber(_viewX, view); _y = ReadNumber(_viewY, view);
                _z = ReadNumber(_viewZ, view); _yaw = ReadNumber(_viewYaw, view);
                _pitch = ReadNumber(_viewPitch, view); _fov = ReadNumber(_viewFov, view);
                _picked = _getPicked.Invoke(_mod, null);
                _reads++;
                if (_picked != null) _successfulPicks++;
                _status = _controlled == null ? "WorldFall: Host nulo" :
                    "WorldFall: mira 3D disponivel; verificando face";
            }
            catch (Exception e)
            {
                _status = "WorldFall Read: " + e.GetBaseException().GetType().Name;
                _picked = null; ClearHold();
            }
        }
        private void ClearHold() { _lookSince = -1f; _markedThisLook = false; }

        // WorldFall: SceneCollector._headings : Dictionary<Actor, Heading>.
        // Heading.Angle : float (renderer-facing yaw in radians, inferred).
        // If the visible Actor has no heading available, do NOT infer facing from movement.
        private bool FaceTowardsViewer(object scp, float sx, float sy, out string description)
        {
            description = "Heading indisponivel";
            if (!Finite(_x) || !Finite(_y) || !Finite(sx) || !Finite(sy)) return false;
            float viewAngle = Mathf.Atan2(_y - sy, _x - sx) * Mathf.Rad2Deg;
            try
            {
                object angleValue = null;
                string source = "nenhuma";
                // WorldFall has TWO actor pose caches. Our sheep-derived SCP may
                // use Creatures.States, not the SceneCollector._headings cache.
                Type creatureType = _type == null ? null : _type.Assembly.GetType("FirstPerson.Creatures");
                object creatureStates = Reflect.Member(creatureType, "States");
                IDictionary animals = creatureStates as IDictionary;
                if (animals != null && animals.Contains(scp))
                {
                    angleValue = Reflect.Member(animals[scp], "Heading");
                    source = "Creatures.States.Heading";
                }
                if (angleValue == null)
                {
                    object scene = Reflect.Member(_mod, "_scene");
                    object raw = Reflect.Member(scene, "_headings");
                    IDictionary headings = raw as IDictionary;
                    if (headings != null && headings.Contains(scp))
                    {
                        angleValue = Reflect.Member(headings[scp], "Angle");
                        source = "SceneCollector._headings.Angle";
                    }
                }
                if (angleValue == null) { description = "Sem heading do SCP nos caches Creatures/SceneCollector"; return false; }
                float angle = Convert.ToSingle(angleValue);
                if (!Finite(angle)) { description = "Heading.Angle nao finito"; return false; }
                float faceDeg = angle * Mathf.Rad2Deg + _faceOffsetDegrees;
                float diff = Mathf.Abs(Mathf.DeltaAngle(faceDeg, viewAngle));
                description = source + "=" + angle.ToString("0.00") + "rad face=" +
                    faceDeg.ToString("0") + "deg alvo=" + viewAngle.ToString("0") +
                    "deg delta=" + diff.ToString("0") + "deg limite=" +
                    _faceConeDegrees.ToString("0") + "deg offset=" +
                    _faceOffsetDegrees.ToString("0");
                return diff <= _faceConeDegrees;
            }
            catch (Exception e)
            {
                description = "Heading erro: " + e.GetBaseException().GetType().Name;
                return false;
            }
        }
        internal bool Check(WorldBridge world, object scp, float maxTiles,
            float holdSeconds, float now, out object observer)
        {
            observer = null;
            Read(now);
            if (!IsPossessing || scp == null || ReferenceEquals(_controlled, scp) ||
                !world.IsAlive(_controlled))
            { _lastReason = "sem host/3D/SCP valido"; ClearHold(); return false; }
            if (!world.Position(scp, out float sx, out float sy) ||
                !Finite(_x) || !Finite(_y))
            { _lastReason = "sem posicoes validas"; ClearHold(); return false; }
            float dx = sx - _x, dy = sy - _y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance > maxTiles || distance < .4f)
            { _lastReason = "fora de alcance: " + distance.ToString("0.0"); ClearHold(); return false; }
            bool pickedScp = ReferenceEquals(_picked, scp);
            _aimDetail = "get_Picked=" + (_picked == null ? "null" : world.AssetId(_picked)) +
                " matchSCP=" + pickedScp + " dist=" + distance.ToString("0.0");
            if (!pickedScp)
            { _lastReason = "SCP nao esta sob mira renderizada"; ClearHold(); return false; }
            // Renderer picking selects the nearest tagged object, so solid walls
            // and other picked actors should prevent a match. It cannot identify
            // head polygons; this is a guarded approximated-face trigger.
            bool front = FaceTowardsViewer(scp, sx, sy, out _faceDetail);
            if (_requireFacing && !front)
            { _lastReason = "SCP visivel, mas face nao confirmada: " + _faceDetail; ClearHold(); return false; }
            if (_lookSince < 0) _lookSince = now;
            float held = now - _lookSince;
            _lastReason = "SCP sob mira, frente=" + front + ", olhar=" + held.ToString("0.00") + "s";
            if (held >= holdSeconds && !_markedThisLook)
            {
                _markedThisLook = true;
                observer = _controlled;
                return true;
            }
            return false;
        }
        internal string DetailedSnapshot(WorldBridge world, object scp)
        {
            Read(Time.unscaledTime);
            if (scp != null && world.Position(scp, out float sx, out float sy))
            {
                FaceTowardsViewer(scp, sx, sy, out _faceDetail);
                _aimDetail = "get_Picked=" + (_picked == null ? "null" : world.AssetId(_picked)) +
                    " matchSCP=" + ReferenceEquals(_picked, scp) +
                    " dist=" + Mathf.Sqrt((sx-_x)*(sx-_x)+(sy-_y)*(sy-_y)).ToString("0.0");
            }
            return Diagnostic(world, scp) + " | AIM " + _aimDetail +
                " | FACE " + _faceDetail + " | RESULT " + _lastReason;
        }
        internal string Diagnostic(WorldBridge world, object scp)
        {
            Read(Time.unscaledTime);
            return "WorldfallSight v0.2.4 bridge=" + _loaded + " firstperson=" + _in3D +
                " host=" + (_controlled == null ? "null" : world.AssetId(_controlled)) +
                " view=(" + _x.ToString("0.00") + "," + _y.ToString("0.00") +
                "," + _z.ToString("0.00") + ") yaw=" + _yaw.ToString("0.000") +
                " pitch=" + _pitch.ToString("0.000") + " fov=" + _fov.ToString("0.000") +
                " reads=" + _reads + " pickedReads=" + _successfulPicks +
                " strictFace=" + _requireFacing + " status=" + _status;
        }
    }
}
