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
        // WorldFall owns an additional soundtrack separate from WorldBox's
        // bus:/Music. These exact names were inspected in Worldfall.dll.
        // Reflection keeps WorldFall optional and avoids muting its environment/SFX.
        private object _worldfallAudio;
        private FieldInfo _worldfallMusicField;
        private float _worldfallOriginal;
        private bool _worldfallCaptured;
        private float _nextWorldfallProbe;
        private bool _worldfallReportedMissing;
        // Separate native FMOD channel group used for WorldFall's OWN soundtrack.
        // Inspected in Worldfall.dll: FirstPersonAudio._music : FMOD.ChannelGroup.
        // MusicVolume controls preference, but this channel group may keep playing.
        private FieldInfo _worldfallOwnGroupField;
        private FMOD.ChannelGroup _worldfallOwnGroup;
        private bool _worldfallOwnCaptured, _worldfallOwnMuteOriginal;
        private float _worldfallOwnVolumeOriginal = 1f;
        private bool _worldfallOwnMissingLogged;
        internal bool Enabled = true;
        internal float FadeDownSeconds = .5f;
        internal float FadeUpSeconds = 3.5f;
        internal float DuckFraction = 1f;
        internal string Status => "active=" + _requested + " fade=" + _factor.ToString("0.00") +
            " bus=" + _busName + " base=" + _busOriginal.ToString("0.00") +
            " unityMusic=" + _unityMusic.Count +
            " worldfallGroup=" + (_worldfallOwnCaptured ? "captured" : "unavailable") +
            " worldfallMusic=" + (_worldfallCaptured ?
                ("captured(original=" + _worldfallOriginal.ToString("0.00") + ")") : "unavailable");

        internal DynamicMusicController(Action<string> log) { _log = log; }
        internal void Request(bool active)
        {
            bool want = Enabled && active;
            if (_requested == want) return;
            _requested = want;
            if (want)
            {
                _loggedStart = false;
                _log("BGM GLOBAL: SCP em TRIGGERED/RAGE; silenciando trilhas do WorldBox e WorldFall para todos os gatilhos");
            }
            else _log("BGM GLOBAL: ameaca encerrada; restaurando trilhas gradualmente");
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

        private void FindWorldfallMusic(float now)
        {
            if (now < _nextWorldfallProbe) return;
            _nextWorldfallProbe = now + 3f;
            try
            {
                foreach (MonoBehaviour obj in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (obj == null || obj.GetType().FullName != "FirstPerson.WorldBoxMod") continue;
                    MethodInfo audioGetter = obj.GetType().GetMethod("get_Audio",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (audioGetter == null) break;
                    object audio = audioGetter.Invoke(obj, null);
                    if (audio == null) break;
                    if (_worldfallCaptured && ReferenceEquals(_worldfallAudio, audio)) return;
                    // If WorldFall recreated its audio runtime after mode/scene changes,
                    // release the previous instance before capturing the new baseline.
                    if (_worldfallCaptured) SetWorldfallMusic(1f);
                    RestoreOwnWorldfallGroup();
                    _worldfallCaptured = false;
                    _worldfallAudio = null;
                    _worldfallMusicField = null;
                    FieldInfo field = audio.GetType().GetField("MusicVolume",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null || field.IsLiteral || field.IsInitOnly ||
                        (field.FieldType != typeof(float) && field.FieldType != typeof(double))) break;
                    object value = field.GetValue(audio);
                    float original = Convert.ToSingle(value);
                    if (float.IsNaN(original) || float.IsInfinity(original) || original < 0f || original > 10f) break;
                    _worldfallAudio = audio;
                    _worldfallMusicField = field;
                    _worldfallOwnGroupField = audio.GetType().GetField("_music",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _worldfallOriginal = original;
                    _worldfallCaptured = true;
                    _log("BGM GLOBAL: WorldFall FirstPersonAudio.MusicVolume encontrado; original=" +
                        _worldfallOriginal.ToString("0.000"));
                    return;
                }
                if (!_worldfallReportedMissing)
                {
                    _worldfallReportedMissing = true;
                    _log("BGM GLOBAL: WorldFall Audio/MusicVolume ainda indisponivel; FMOD bus:/Music continua protegido");
                }
            }
            catch (Exception ex)
            {
                if (!_worldfallReportedMissing)
                {
                    _worldfallReportedMissing = true;
                    _log("BGM GLOBAL: falha consulta FirstPersonAudio: " + ex.GetBaseException().GetType().Name);
                }
            }
        }

        private void SetWorldfallMusic(float factor)
        {
            if (!_worldfallCaptured) return;
            try
            {
                float value = _worldfallOriginal * Mathf.Clamp01(factor);
                if (_worldfallMusicField.FieldType == typeof(double))
                    _worldfallMusicField.SetValue(_worldfallAudio, (double)value);
                else _worldfallMusicField.SetValue(_worldfallAudio, value);
            }
            catch (Exception ex)
            {
                _log("BGM GLOBAL: WorldFall MusicVolume nao pode ser ajustado: " +
                    ex.GetBaseException().GetType().Name);
                _worldfallCaptured = false;
                _worldfallMusicField = null;
                _worldfallAudio = null;
                _nextWorldfallProbe = Time.unscaledTime + 5f;
            }
        }

        private void RestoreOwnWorldfallGroup()
        {
            if (!_worldfallOwnCaptured) return;
            try
            {
                _worldfallOwnGroup.setVolume(_worldfallOwnVolumeOriginal);
                _worldfallOwnGroup.setMute(_worldfallOwnMuteOriginal);
            }
            catch { }
            _worldfallOwnCaptured = false;
            _worldfallOwnGroup = default(FMOD.ChannelGroup);
        }

        private void ApplyWorldfallOwnMusic(float factor)
        {
            if (!_worldfallCaptured || _worldfallOwnGroupField == null ||
                _worldfallOwnGroupField.FieldType != typeof(FMOD.ChannelGroup)) return;
            try
            {
                object value = _worldfallOwnGroupField.GetValue(_worldfallAudio);
                if (!(value is FMOD.ChannelGroup)) return;
                FMOD.ChannelGroup group = (FMOD.ChannelGroup)value;
                // FMOD Core.ChannelGroup has getVolume(out float).
                // FMOD Studio.Bus has getVolume(out float, out float). Do not mix APIs.
                float checkVolume;
                if (group.getVolume(out checkVolume) != FMOD.RESULT.OK) return;
                if (!_worldfallOwnCaptured || !group.Equals(_worldfallOwnGroup))
                {
                    RestoreOwnWorldfallGroup();
                    bool muted;
                    if (group.getMute(out muted) != FMOD.RESULT.OK) return;
                    _worldfallOwnGroup = group;
                    _worldfallOwnMuteOriginal = muted;
                    _worldfallOwnVolumeOriginal = Mathf.Clamp01(checkVolume);
                    _worldfallOwnCaptured = true;
                    _log("BGM GLOBAL: WorldFall _music FMOD.ChannelGroup capturado: volume=" +
                         _worldfallOwnVolumeOriginal.ToString("0.00") +
                         " muteOriginal=" + muted + " (somente trilha WorldFall)");
                }
                float target = Mathf.Clamp01(factor);
                // WorldFall can change its own per-channel levels during UpdateMusic.
                // Muting the dedicated music group prevents its tracks from leaking.
                group.setVolume(_worldfallOwnVolumeOriginal * target);
                group.setMute(_worldfallOwnMuteOriginal || target < .045f);
            }
            catch (Exception e)
            {
                if (!_worldfallOwnMissingLogged)
                {
                    _worldfallOwnMissingLogged = true;
                    _log("BGM GLOBAL: falha controle grupo FMOD do WorldFall: " +
                         e.GetBaseException().GetType().Name);
                }
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
            FindWorldfallMusic(now);
            FindUnityMusic(now);
            float wanted = _requested ? Mathf.Clamp01(1f - DuckFraction) : 1f;
            float seconds = _requested ? Mathf.Max(.1f, FadeDownSeconds) : Mathf.Max(.1f, FadeUpSeconds);
            _factor = Mathf.MoveTowards(_factor, wanted, Mathf.Clamp(unscaledDeltaTime, 0f, .1f) / seconds);
            // Reapply every LateUpdate because WorldFall can refresh its music
            // settings while the player enters or leaves the 3D view.
            SetWorldfallMusic(_factor);
            ApplyWorldfallOwnMusic(_factor);
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
                _worldfallCaptured = false;
                _worldfallAudio = null;
                RestoreOwnWorldfallGroup();
                _worldfallMusicField = null;
                _worldfallOwnGroupField = null;
                _worldfallReportedMissing = false;
                _log("BGM GLOBAL: volume de musicas restaurado (WorldBox/WorldFall quando presentes)");
            }
        }
        internal void RestoreImmediately()
        {
            bool hadChanges = _requested || _busCaptured || _worldfallCaptured ||
                _unityMusic.Count > 0 || _factor < .9999f;
            _requested = false;
            SetWorldfallMusic(1f);
            RestoreOwnWorldfallGroup();
            if (_busCaptured)
            {
                try { if (_bus.isValid()) _bus.setVolume(_busOriginal); } catch { }
            }
            foreach (UnityMusic m in _unityMusic)
                try { if (m.Source != null) m.Volume.SetValue(m.Source, m.Original, null); } catch { }
            _unityMusic.Clear();
            _busCaptured = false;
            _busName = "nenhum";
            _worldfallCaptured = false;
            _worldfallAudio = null;
            _worldfallMusicField = null;
            _worldfallOwnGroupField = null;
            _worldfallOwnMissingLogged = false;
            _worldfallReportedMissing = false;
            _factor = 1f;
            _nextProbe = _nextUnityProbe = _nextWorldfallProbe = 0f;
            if (hadChanges) _log("BGM OVERRIDE: restauracao imediata solicitada");
        }
    }
}
