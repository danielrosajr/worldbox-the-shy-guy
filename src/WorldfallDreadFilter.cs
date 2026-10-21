using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCP096TheShyGuy
{
    // The GPU renderer has ShadowDarkness=1 by default, so increasing it via
    // Mathf.Clamp01 was a no-op in 0.2.11/0.2.12. 2D Unity Canvas and OnGUI
    // also render behind WorldFall's custom presentation in the user's build.
    // This module tints the actual WorldFall RenderTexture after its LateUpdate.
    // Zero hard dependency on WorldFall.dll. All reflection fails closed.
    internal sealed class WorldfallDreadFilter
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Action<string> _log;
        private MonoBehaviour _worldfall;
        private object _gpu;
        private FieldInfo _gpuField, _gpuPresent, _gpuOutput, _gpuFrame;
        private float _nextSearch, _nextGpuWaitLog;
        private bool _lastWanted, _reportedActive, _reportedPreview;
        private float _intensity;
        private int _lastWorldfallFrame = -1, _lastUnityFrame = -1, _tintedFrames;
        private RenderTexture _lastTarget;
        private Material _tintMaterial;
        private GameObject _latePassObject;
        private WorldfallDreadLatePass _latePass;
        private string _reason = "aguardando";

        internal bool Enabled = true;
        internal float Strength = .85f;
        internal string Status => "povDread=" + _intensity.ToString("0.00") +
            " target=" + _lastWanted + " gpu=" + (_gpu != null) +
            " latePass=" + (_latePass != null) + " tintFrames=" + _tintedFrames +
            " targetRT=" + (_lastTarget == null ? "none" : _lastTarget.name) + " reason=" + _reason;

        internal WorldfallDreadFilter(Action<string> log)
        {
            _log = log;
        }

        internal void EnsureLatePass()
        {
            if (_latePass != null) return;
            try
            {
                _latePassObject = new GameObject("SCP096_WorldFall_Dread_Presenter");
                _latePassObject.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(_latePassObject);
                _latePass = _latePassObject.AddComponent<WorldfallDreadLatePass>();
                _latePass.Attach(this);
                _log("DREAD RT: componente LateUpdate criado (execucao tardia)");
            }
            catch (Exception ex)
            {
                _reason = "LatePass: " + ex.GetType().Name;
                _log("DREAD RT: " + _reason);
            }
        }

        private void Discover(float now)
        {
            if (_worldfall != null && _gpu != null) return;
            if (now < _nextSearch) return;
            _nextSearch = now + 3f;
            try
            {
                foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (mb == null || mb.GetType().FullName != "FirstPerson.WorldBoxMod") continue;
                    _worldfall = mb;
                    _gpuField = mb.GetType().GetField("_gpu", InstanceFields);
                    if (_gpuField == null) { _reason = "_gpu indisponivel"; return; }
                    object renderer = _gpuField.GetValue(mb);
                    if (renderer == null) { _reason = "_gpu ainda nulo"; return; }
                    SetGpu(renderer);
                    return;
                }
                _reason = "WorldFall nao instanciado";
            }
            catch (Exception e)
            {
                _reason = "Discover " + e.GetBaseException().GetType().Name;
            }
        }

        private void SetGpu(object renderer)
        {
            if (ReferenceEquals(_gpu, renderer)) return;
            _gpu = renderer;
            Type t = renderer.GetType();
            // Inspected in the actual uploaded Worldfall.dll: both are RenderTexture.
            _gpuPresent = t.GetField("_present", InstanceFields);
            _gpuOutput = t.GetField("Output", InstanceFields);
            _gpuFrame = t.GetField("FrameNumber", InstanceFields);
            _lastWorldfallFrame = -1;
            _lastUnityFrame = -1;
            _lastTarget = null;
            _reason = "GPU output pronto";
            _log("DREAD RT: WorldFall GPU encontrado; _present=" + (_gpuPresent != null) +
                " Output=" + (_gpuOutput != null) + " FrameNumber=" + (_gpuFrame != null));
        }

        private void EnsureMaterial()
        {
            if (_tintMaterial != null) return;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                _reason = "shader Hidden/Internal-Colored ausente";
                return;
            }
            _tintMaterial = new Material(shader);
            _tintMaterial.hideFlags = HideFlags.HideAndDontSave;
            _tintMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _tintMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _tintMaterial.SetInt("_Cull", (int)CullMode.Off);
            _tintMaterial.SetInt("_ZWrite", 0);
            _tintMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            _reason = "material de tint preparado";
            _log("DREAD RT: material AlphaBlend criado para a imagem WorldFall");
        }

        internal void Tick(float now, float unscaledDelta, bool isFirstVictim)
        {
            _lastWanted = Enabled && isFirstVictim;
            float desired = _lastWanted ? Mathf.Clamp01(Strength) : 0f;
            float speed = _lastWanted ? 0.85f : 1.6f;
            _intensity = Mathf.MoveTowards(_intensity, desired,
                Mathf.Clamp(unscaledDelta, 0f, .12f) * speed);
            if (_intensity < .001f)
            {
                _intensity = 0f;
                _reportedActive = false;
                return;
            }
            Discover(now);
            if (_gpu == null)
            {
                if (_intensity >= .15f && now >= _nextGpuWaitLog)
                {
                    _nextGpuWaitLog = now + 8f;
                    _log("DREAD RT: esperando renderizador 3D; reason=" + _reason);
                }
                return;
            }
            EnsureMaterial();
            if (_tintMaterial == null)
            {
                if (now >= _nextGpuWaitLog)
                {
                    _nextGpuWaitLog = now + 8f;
                    _log("DREAD RT: aguardando material; reason=" + _reason);
                }
                return;
            }
            if (!_reportedActive && _intensity >= .15f)
            {
                _reportedActive = true;
                _log("DREAD RT: POV ativo; compositor direto no RenderTexture (nao GPU ShadowDarkness=1)");
            }
        }

        // Runs at DefaultExecutionOrder(30000), so WorldFall normally has
        // finished rendering its frame into _present by this point.
        internal void PaintLate()
        {
            if (_intensity <= .001f || !_lastWanted || _gpu == null || _tintMaterial == null)
                return;
            if (Time.frameCount == _lastUnityFrame) return;
            _lastUnityFrame = Time.frameCount;
            try
            {
                // Fail closed if WorldFall's renderer is swapped/unloaded.
                if (_worldfall == null || _gpuField == null ||
                    !ReferenceEquals(_gpuField.GetValue(_worldfall), _gpu))
                { _gpu = null; _worldfall = null; _reason = "GPU trocado"; return; }

                int worldfallFrame = -1;
                if (_gpuFrame != null)
                {
                    object frame = _gpuFrame.GetValue(_gpu);
                    if (frame is int) worldfallFrame = (int)frame;
                }
                // Do not keep darkening a stale frame repeatedly.
                if (worldfallFrame >= 0 && worldfallFrame == _lastWorldfallFrame) return;
                RenderTexture target = null;
                if (_gpuPresent != null) target = _gpuPresent.GetValue(_gpu) as RenderTexture;
                string where = "_present";
                if (target == null || !target.IsCreated())
                {
                    target = _gpuOutput == null ? null : _gpuOutput.GetValue(_gpu) as RenderTexture;
                    where = "Output";
                }
                if (target == null || !target.IsCreated())
                { _reason = "sem RenderTexture criado"; return; }
                if (target.width < 8 || target.height < 8) return;

                // Darken the WORLD FALL picture, not the WorldBox/UI/audio.
                // Intensity .85 = alpha .55. This is a tint, not destructive
                // readback; every new WorldFall frame replaces the previous one.
                float alpha = Mathf.Clamp(_intensity * .65f, 0f, .68f);
                RenderTexture original = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    GL.PushMatrix();
                    try
                    {
                        GL.LoadOrtho();
                        if (!_tintMaterial.SetPass(0))
                        { _reason = "material.SetPass false"; return; }
                        GL.Begin(GL.QUADS);
                        GL.Color(new Color(.015f, .017f, .025f, alpha));
                        GL.Vertex3(0f, 0f, 0f);
                        GL.Vertex3(0f, 1f, 0f);
                        GL.Vertex3(1f, 1f, 0f);
                        GL.Vertex3(1f, 0f, 0f);
                        GL.End();
                    }
                    finally { GL.PopMatrix(); }
                }
                finally { RenderTexture.active = original; }

                _lastWorldfallFrame = worldfallFrame;
                _lastTarget = target;
                _tintedFrames++;
                _reason = "pintado em " + where + " (" + target.width + "x" + target.height + ")";
                if (_tintedFrames == 1 || _tintedFrames == 30 || _tintedFrames == 120)
                    _log("DREAD RT: frame=" + _tintedFrames + " WorldFall=" + worldfallFrame +
                        " target=" + where + " size=" + target.width + "x" + target.height +
                        " alpha=" + alpha.ToString("0.00") + " [confirmar visualmente]");
            }
            catch (Exception e)
            {
                _reason = "compositor falhou " + e.GetBaseException().GetType().Name;
                _log("DREAD RT: " + _reason + ": " + e.GetBaseException().Message);
                _gpu = null; _worldfall = null;
            }
        }

        internal void RestoreImmediately()
        {
            // Pixel compositor does not change original graphics settings.
            // A new WorldFall frame naturally replaces the last tinted frame.
            _intensity = 0f;
            _lastWanted = false;
            _reportedActive = false;
            _lastWorldfallFrame = -1;
        }

        internal void Dispose()
        {
            RestoreImmediately();
            if (_latePassObject != null) UnityEngine.Object.Destroy(_latePassObject);
            if (_tintMaterial != null) UnityEngine.Object.Destroy(_tintMaterial);
            _latePassObject = null; _latePass = null; _tintMaterial = null;
        }
    }

    [DefaultExecutionOrder(30000)]
    public sealed class WorldfallDreadLatePass : MonoBehaviour
    {
        private WorldfallDreadFilter _filter;
        internal void Attach(WorldfallDreadFilter filter) { _filter = filter; }
        private void LateUpdate()
        {
            if (_filter != null) _filter.PaintLate();
        }
    }
}
