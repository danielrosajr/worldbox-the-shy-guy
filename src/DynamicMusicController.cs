using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace SCP096TheShyGuy
{
    // Only game soundtrack is affected. SCP sounds are played on FMOD Core's
    // master channel group and remain audible when the Studio music bus is ducked.
    // Snapshot original volumes ONCE, then fade back to exactly those values.
    internal sealed class DynamicMusicController
    {
        private sealed class UnityMusic
        {
            internal UnityEngine.Object Source;
            internal PropertyInfo Volume;
            internal float Original;
        }

        private readonly Action<string> _log;
        private readonly List<UnityMusic> _unityMusic = new List<UnityMusic>();
        private Bus _bus;
        private float _busOriginal = 1f, _factor = 1f;
        private bool _busCaptured, _requested, _logMissing, _loggedStart;
        private float _nextProbe, _nextUnityProbe;
        private string _busName = "nenhum";
        internal bool Enabled = true;
        internal float FadeDownSeconds = 2.8f;
        internal float FadeUpSeconds = 3.5f;
        internal float DuckFraction = 1f;
        internal string Status => "active=" + _requested + " fade=" + _factor.ToString("0.00") +
            " bus=" + _busName + " base=" + _busOriginal.ToString("0.00") +
            " unityMusic=" + _unityMusic.Count;

        internal DynamicMusicController(Action<string> log) { _log = log; }
        internal void Request(bool active)
        {
            bool want = Enabled && active;
            if (_requested == want) return;
            _requested = want;
            if (want)
            {
                _loggedStart = false;
                _log("BGM OVERRIDE: jogador marcado; silenciar trilha de jogo durante TRIGGERED/RAGE");
            }
            else _log("BGM OVERRIDE: finalizado; restaurando a musica gradualmente");
        }

        private void FindBus(float now)
        {
            if (_busCaptured || now < _nextProbe) return;
            _nextProbe = now + 4f;
            string[] paths = { "bus:/Music", "bus:/music", "bus:/Master/Music", "bus:/BGM" };
            foreach (string path in paths)
            {
                try
                {
                    Bus found;
                    if (RuntimeManager.StudioSystem.getBus(path, out found) != FMOD.RESULT.OK ||
                        !found.isValid()) continue;
                    float original, final;
                    if (found.getVolume(out original, out final) != FMOD.RESULT.OK) continue;
                    _bus = found;
                    _busCaptured = true;
                    _busOriginal = Mathf.Clamp01(original);
                    _busName = path;
                    _log("BGM OVERRIDE: FMOD music bus encontrado=" + path +
                        " original=" + _busOriginal.ToString("0.000"));
                    break;
                }
                catch (Exception ex)
                {
                    if (!_logMissing) _log("BGM OVERRIDE: pesquisa FMOD: " + ex.GetType().Name);
                }
            }
            if (!_busCaptured && !_logMissing)
            {
                _logMissing = true;
                _log("BGM OVERRIDE: bus de musica nao localizado. Procurando fontes Unity de musica; master/SFX intocados");
            }
        }

        private static bool ClearlyMusic(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.Contains("music") || n.Contains("bgm") || n.Contains("soundtrack");
        }
        private void FindUnityMusic(float now)
        {
            if (now < _nextUnityProbe) return;
            _nextUnityProbe = now + 5f;
            try
            {
                Type sourceType = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    sourceType = assembly.GetType("UnityEngine.AudioSource", false);
                    if (sourceType != null) break;
                }
                if (sourceType == null) return;
                PropertyInfo vol = sourceType.GetProperty("volume");
                PropertyInfo clip = sourceType.GetProperty("clip");
                if (vol == null || clip == null) return;
                UnityEngine.Object[] sources = UnityEngine.Object.FindObjectsOfType(sourceType);
                foreach (UnityEngine.Object source in sources)
                {
                    if (source == null) continue;
                    bool already = false;
                    foreach (UnityMusic entry in _unityMusic)
                        if (ReferenceEquals(entry.Source, source)) { already = true; break; }
                    if (already) continue;
                    UnityEngine.Object audioClip = clip.GetValue(source, null) as UnityEngine.Object;
                    if (!ClearlyMusic(source.name) && !ClearlyMusic(audioClip == null ? null : audioClip.name)) continue;
                    float value = Convert.ToSingle(vol.GetValue(source, null));
                    if (float.IsNaN(value) || value < 0f || value > 1f) continue;
                    _unityMusic.Add(new UnityMusic { Source = source, Volume = vol, Original = value });
                    _log("BGM OVERRIDE: Unity music source=" + source.name + " original=" + value.ToString("0.00"));
                }
            }
            catch (Exception ex) { _log("BGM OVERRIDE: unity source scan " + ex.GetType().Name); }
        }

        internal void Tick(float now, float unscaledDeltaTime)
        {
            bool inFade = _requested || _factor < .9999f;
            if (!inFade) return;
            FindBus(now);
            FindUnityMusic(now);
            float wanted = _requested ? Mathf.Clamp01(1f - DuckFraction) : 1f;
            float seconds = _requested ? Mathf.Max(.1f, FadeDownSeconds) : Mathf.Max(.1f, FadeUpSeconds);
            _factor = Mathf.MoveTowards(_factor, wanted, Mathf.Clamp(unscaledDeltaTime, 0f, .1f) / seconds);
            if (_busCaptured)
            {
                try
                {
                    if (!_bus.isValid() || _bus.setVolume(_busOriginal * _factor) != FMOD.RESULT.OK)
                    {
                        _busCaptured = false;
                        _nextProbe = now + 4f;
                        _log("BGM OVERRIDE: bus perdeu validade; refazendo leitura");
                    }
                }
                catch { _busCaptured = false; _nextProbe = now + 4f; }
            }
            for (int i = _unityMusic.Count - 1; i >= 0; i--)
            {
                UnityMusic m = _unityMusic[i];
                if (m.Source == null) { _unityMusic.RemoveAt(i); continue; }
                try { m.Volume.SetValue(m.Source, m.Original * _factor, null); }
                catch { _unityMusic.RemoveAt(i); }
            }
            if (!_loggedStart && _requested && _factor < .03f)
            {
                _loggedStart = true;
                _log("BGM OVERRIDE: silencio alcançado; " + Status);
            }
            if (!_requested && _factor >= .9999f)
            {
                // Clear snapshots only AFTER the saved original volumes are restored.
                _busCaptured = false;
                _busName = "nenhum";
                _unityMusic.Clear();
                _log("BGM OVERRIDE: volume de musica restaurado");
            }
        }
        internal void RestoreImmediately()
        {
            bool hadChanges = _requested || _busCaptured || _unityMusic.Count > 0 || _factor < .9999f;
            _requested = false;
            if (_busCaptured)
            {
                try { if (_bus.isValid()) _bus.setVolume(_busOriginal); } catch { }
            }
            foreach (UnityMusic m in _unityMusic)
                try { if (m.Source != null) m.Volume.SetValue(m.Source, m.Original, null); } catch { }
            _unityMusic.Clear();
            _busCaptured = false;
            _busName = "nenhum";
            _factor = 1f;
            _nextProbe = _nextUnityProbe = 0f;
            if (hadChanges) _log("BGM OVERRIDE: restauracao imediata solicitada");
        }
    }
}
