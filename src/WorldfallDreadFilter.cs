using System;
using System.Reflection;
using UnityEngine;

namespace SCP096TheShyGuy
{
    // First-person fear without UI overlays. WorldFall's 3D renderer runs in its
    // own display pipeline, so a ScreenSpaceOverlay/OnGUI may be invisible there.
    // Use inspected, native float visual controls instead of Canvas.
    // The filter never touches WorldBox 2D, and restores original renderer values.
    internal sealed class WorldfallDreadFilter
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Action<string> _log;
        private object _renderer, _gpu;
        private FieldInfo _cpuVignette, _cpuShadow, _gpuShadow;
        private float _baseVignette, _baseCpuShadow, _baseGpuShadow;
        private bool _capturedCpu, _capturedGpu, _loggedActive;
        private float _intensity;
        private float _nextDiscovery;
        private bool _lastWanted;
        internal bool Enabled = true;
        internal float Strength = .85f;
        internal string Status => "povDread=" + _intensity.ToString("0.00") +
            " cpu=" + _capturedCpu + " gpu=" + _capturedGpu + " target=" + _lastWanted;

        internal WorldfallDreadFilter(Action<string> log) { _log = log; }

        private void Discover(float now)
        {
            if (_capturedCpu && _capturedGpu) return;
            if (now < _nextDiscovery) return;
            _nextDiscovery = now + 3f;
            try
            {
                foreach (MonoBehaviour c in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (c == null || c.GetType().FullName != "FirstPerson.WorldBoxMod") continue;
                    Type t = c.GetType();
                    object cpu = t.GetField("Renderer", Any)?.GetValue(c);
                    object gpu = t.GetField("_gpu", Any)?.GetValue(c);
                    if (cpu != null && (!ReferenceEquals(cpu, _renderer) || !_capturedCpu))
                    {
                        RestoreCpu();
                        _renderer = cpu;
                        _cpuVignette = cpu.GetType().GetField("Vignette", Any);
                        _cpuShadow = cpu.GetType().GetField("ShadowDarkness", Any);
                        if (_cpuVignette != null && _cpuShadow != null &&
                            _cpuVignette.FieldType == typeof(float) && _cpuShadow.FieldType == typeof(float))
                        {
                            _baseVignette = (float)_cpuVignette.GetValue(cpu);
                            _baseCpuShadow = (float)_cpuShadow.GetValue(cpu);
                            _capturedCpu = true;
                        }
                    }
                    if (gpu != null && (!ReferenceEquals(gpu, _gpu) || !_capturedGpu))
                    {
                        RestoreGpu();
                        _gpu = gpu;
                        _gpuShadow = gpu.GetType().GetField("ShadowDarkness", Any);
                        if (_gpuShadow != null && _gpuShadow.FieldType == typeof(float))
                        {
                            _baseGpuShadow = (float)_gpuShadow.GetValue(gpu);
                            _capturedGpu = true;
                        }
                    }
                    if (_capturedCpu || _capturedGpu)
                        _log("DREAD POV: renderizadores encontrados; " + Status +
                             " vignetteBase=" + _baseVignette.ToString("0.00") +
                             " shadowGpuBase=" + _baseGpuShadow.ToString("0.00"));
                    return;
                }
            }
            catch (Exception e) { _log("DREAD POV: busca " + e.GetBaseException().GetType().Name); }
        }

        private void RestoreCpu()
        {
            if (!_capturedCpu) return;
            try { _cpuVignette.SetValue(_renderer, _baseVignette); } catch { }
            try { _cpuShadow.SetValue(_renderer, _baseCpuShadow); } catch { }
            _capturedCpu = false; _renderer = null;
        }
        private void RestoreGpu()
        {
            if (!_capturedGpu) return;
            try { _gpuShadow.SetValue(_gpu, _baseGpuShadow); } catch { }
            _capturedGpu = false; _gpu = null;
        }

        internal void Tick(float now, float unscaledDelta, bool isFirstVictim)
        {
            _lastWanted = Enabled && isFirstVictim;
            float desired = _lastWanted ? Mathf.Clamp01(Strength) : 0f;
            float speed = _lastWanted ? 1.1f : 2.3f;
            _intensity = Mathf.MoveTowards(_intensity, desired,
                Mathf.Clamp(unscaledDelta, 0f, .12f) * speed);
            if (_intensity < .0001f)
            {
                if (_capturedCpu || _capturedGpu) RestoreImmediately();
                _loggedActive = false;
                return;
            }
            Discover(now);
            try
            {
                if (_capturedCpu)
                {
                    _cpuVignette.SetValue(_renderer, Mathf.Clamp01(
                        Mathf.Max(_baseVignette, _baseVignette + .68f * _intensity)));
                    _cpuShadow.SetValue(_renderer, Mathf.Clamp01(
                        _baseCpuShadow + .52f * _intensity));
                }
                if (_capturedGpu)
                    _gpuShadow.SetValue(_gpu, Mathf.Clamp01(
                        _baseGpuShadow + .52f * _intensity));
                if (!_loggedActive && _intensity > .12f)
                {
                    _loggedActive = true;
                    _log("DREAD POV: proxima vitima controlada; escurecendo vinheta/sombras no renderer 3D (nao Canvas)");
                }
            }
            catch (Exception e)
            {
                _log("DREAD POV: erro filtro " + e.GetBaseException().GetType().Name);
                RestoreImmediately();
            }
        }

        internal void RestoreImmediately()
        {
            RestoreCpu(); RestoreGpu();
            _intensity = 0f;
            _lastWanted = false;
            _loggedActive = false;
            _nextDiscovery = 0f;
        }
    }
}
