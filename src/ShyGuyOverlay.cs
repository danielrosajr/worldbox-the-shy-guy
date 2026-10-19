using System;
using System.Reflection;
using UnityEngine;

namespace SCP096TheShyGuy
{
    // ScreenSpaceOverlay sits above the WorldFall 3D renderer. The RawImage is
    // only enabled for SCP-096 witness/death events (or an explicit F5 preview).
    // OnGUI remains a safe fallback if UnityEngine.UI is not loadable by NML.
    // Last-pass IMGUI for WorldFall's own 3D presentation. The Canvas works in
    // map mode but was observed behind the FirstPerson renderer in v0.2.8.
    [DefaultExecutionOrder(32000)]
    internal sealed class ShyGuyOverlayLateGui : MonoBehaviour
    {
        internal ShyGuyOverlay Owner;
        private void OnGUI()
        {
            if (Owner != null) Owner.DrawWorldfallLastPass();
        }
    }

    internal sealed class ShyGuyOverlay
    {
        private readonly Action<string> _log;
        private readonly Texture2D _image;
        private GameObject _root;
        private object _raw;
        private Component _imageRect;
        private PropertyInfo _sizeDelta, _anchoredPosition;
        private PropertyInfo _color;
        private float _startAt = -1000f, _duration = 1.35f, _opacity = .45f;
        private bool _triedCanvas, _reportedUiFailure;
        private bool _worldfallMode, _preferWorldfall;
        private int _lastPassFrames;
        private bool _lastPassLogged;
        internal bool Enabled = true;
        internal bool IsActive => _image != null && Enabled && Time.unscaledTime < _startAt + _duration;
        internal string Status => "canvas=" + (_root != null) + " active=" + IsActive +
            " texture=" + (_image != null) + " worldfall=" + _worldfallMode +
            " lateGuiFrames=" + _lastPassFrames;

        internal ShyGuyOverlay(Texture2D image, Action<string> log)
        {
            _image = image; _log = log;
        }
        private static Type Lookup(string name)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType(name, false);
                if (t != null) return t;
            }
            return null;
        }
        private bool EnsureCanvas()
        {
            if (_root != null) return true;
            if (_triedCanvas || _image == null) return false;
            _triedCanvas = true;
            GameObject root = null;
            try
            {
                Type canvas = Lookup("UnityEngine.Canvas");
                Type rectType = Lookup("UnityEngine.RectTransform");
                Type rawType = Lookup("UnityEngine.UI.RawImage");
                if (canvas == null || rectType == null || rawType == null)
                    throw new InvalidOperationException("Canvas/RectTransform/RawImage nao encontrados");
                root = new GameObject("SCP096_WorldfallWitnessOverlay", new Type[] { rectType });
                object canvasObj = root.AddComponent(canvas);
                PropertyInfo render = canvas.GetProperty("renderMode");
                render.SetValue(canvasObj, Enum.ToObject(render.PropertyType, 0), null);
                canvas.GetProperty("sortingOrder").SetValue(canvasObj, 32767, null);
                PropertyInfo sorting = canvas.GetProperty("overrideSorting");
                if (sorting != null) sorting.SetValue(canvasObj, true, null);
                ShyGuyOverlayLateGui top = root.AddComponent<ShyGuyOverlayLateGui>();
                top.Owner = this;
                GameObject image = new GameObject("SCP096_WitnessFace", new Type[] { rectType });
                image.transform.SetParent(root.transform, false);
                _raw = image.AddComponent(rawType);
                rawType.GetProperty("texture").SetValue(_raw, _image, null);
                PropertyInfo raycast = rawType.GetProperty("raycastTarget");
                if (raycast != null) raycast.SetValue(_raw, false, null);
                _color = rawType.GetProperty("color");
                Component bounds = image.GetComponent(rectType);
                Type rt = bounds.GetType();
                rt.GetProperty("anchorMin").SetValue(bounds, new Vector2(.5f, .5f), null);
                rt.GetProperty("anchorMax").SetValue(bounds, new Vector2(.5f, .5f), null);
                _sizeDelta = rt.GetProperty("sizeDelta");
                _anchoredPosition = rt.GetProperty("anchoredPosition");
                _imageRect = bounds;
                SetImageSize();
                UnityEngine.Object.DontDestroyOnLoad(root);
                root.SetActive(false);
                _root = root;
                _log("FLASH CANVAS 3D CRIADO: ScreenSpaceOverlay sortingOrder=32767 raycast=false");
                return true;
            }
            catch (Exception ex)
            {
                if (root != null) UnityEngine.Object.Destroy(root);
                _root = null; _raw = null;
                if (!_reportedUiFailure)
                {
                    _reportedUiFailure = true;
                    _log("FLASH CANVAS 3D indisponivel, fallback IMGUI: " + ex.GetType().Name + " " + ex.Message);
                }
                return false;
            }
        }
        internal void Show(float duration, float opacity, string cause, bool fromWorldfall = false)
        {
            if (!Enabled || _image == null) return;
            _startAt = Time.unscaledTime;
            _preferWorldfall = fromWorldfall;
            _lastPassFrames = 0;
            _lastPassLogged = false;
            _duration = Mathf.Clamp(duration, .35f, 6f);
            _opacity = Mathf.Clamp(opacity, .05f, .85f);
            bool canvas = EnsureCanvas();
            if (canvas) _root.SetActive(true);
            _log("FLASH SCP096 EXIBIDO: cause=" + cause + " canvas=" + canvas +
                 " worldfallPresentation=" + fromWorldfall +
                 " duration=" + _duration.ToString("0.00") + " opacity=" + _opacity.ToString("0.00"));
        }
        private float Fade()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _startAt) / Mathf.Max(.1f, _duration));
            if (t < .20f) return Mathf.SmoothStep(0f, 1f, t / .20f);
            if (t > .76f) return Mathf.SmoothStep(1f, 0f, (t - .76f) / .24f);
            return 1f;
        }
        private void SetImageSize()
        {
            if (_imageRect == null || _sizeDelta == null || _image == null) return;
            float h = Screen.height * .92f;
            float aspect = (float)_image.width / Mathf.Max(1f, _image.height);
            float w = Mathf.Min(Screen.width * .95f, h * aspect);
            h = w / Mathf.Max(.01f, aspect);
            _sizeDelta.SetValue(_imageRect, new Vector2(w, h), null);
            if (_anchoredPosition != null) _anchoredPosition.SetValue(_imageRect, Vector2.zero, null);
        }
        internal void LateUpdate(bool worldfall3D)
        {
            _worldfallMode = worldfall3D || (_preferWorldfall && IsActive);
            if (_root == null) return;
            if (!IsActive)
            {
                if (_root.activeSelf) _root.SetActive(false);
                return;
            }
            if (!_root.activeSelf) _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            SetImageSize();
            try
            {
                if (_raw != null && _color != null)
                    _color.SetValue(_raw, new Color(1f, 1f, 1f, _opacity * Fade()), null);
            }
            catch (Exception ex) { _log("FLASH CANVAS update: " + ex.GetType().Name); }
        }
        internal void DrawWorldfallLastPass()
        {
            // Don't draw a second image in map mode. WorldFall's render target
            // might overlap regular Canvas layers; this owns the last IMGUI pass.
            if (!_worldfallMode || !IsActive || Event.current.type != EventType.Repaint) return;
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            try
            {
                GUI.depth = -32767;
                GUI.color = new Color(1f, 1f, 1f, _opacity * Fade());
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height),
                    _image, ScaleMode.ScaleAndCrop, true);
                _lastPassFrames++;
                if (!_lastPassLogged)
                {
                    _lastPassLogged = true;
                    _log("FLASH 3D LATE GUI: primeiro frame desenhado em WorldFall (ainda requer confirmacao visual)");
                }
            }
            finally { GUI.color = previousColor; GUI.depth = previousDepth; }
        }
        internal void OnGUIFallback()
        {
            if (_root != null || !IsActive || Event.current.type != EventType.Repaint) return;
            int beforeDepth = GUI.depth;
            Color before = GUI.color;
            try
            {
                GUI.depth = -32000;
                GUI.color = new Color(1f, 1f, 1f, _opacity * Fade());
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height),
                    _image, ScaleMode.ScaleAndCrop, true);
            }
            finally { GUI.color = before; GUI.depth = beforeDepth; }
        }
        internal void Reset()
        {
            _startAt = -1000f;
            _worldfallMode = _preferWorldfall = false;
            if (_root != null) _root.SetActive(false);
        }
        internal void Destroy()
        {
            Reset();
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _raw = null; _imageRect = null;
        }
    }
}
