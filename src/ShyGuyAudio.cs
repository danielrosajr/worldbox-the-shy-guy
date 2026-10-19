using System;
using System.IO;
using UnityEngine;
using FMOD;
using FMODUnity;

namespace SCP096TheShyGuy
{
    internal sealed class ShyGuyAudio
    {
        private const float LocalRadius = 28f;
        private readonly Action<string> _log;
        private readonly string _folder;
        private FMOD.System _system;
        private FMOD.ChannelGroup _master;
        private FMOD.Sound _crySound, _cueSound, _musicSound, _rageSound, _deathSound;
        private FMOD.Channel _cryChannel, _cueChannel, _musicChannel, _rageChannel, _deathChannel;
        private bool _cryPlaying, _cuePlaying, _musicPlaying, _ragePlaying, _deathPlaying;
        private bool _cryReady, _cueReady, _musicReady, _rageReady, _deathReady;
        private bool _initialized, _reportedReady;
        private float _nextInitAttempt, _triggerMusicAt = -1f, _flashAt = -100f;
        private float _rageDuration = 10f, _cueDuration = 0f, _musicDuration = 0f;
        private Texture2D _face;

        internal bool Enabled = true;
        internal bool VisualCueEnabled = true;
        internal float MasterVolume = .75f;
        internal float FlashOpacity = .60f;
        internal float FlashDuration = 1.65f;
        internal Texture2D FaceImage => _face;
        internal string Summary => "FMOD=" + (_initialized ? "pronto" : "aguardando") +
            ", cry=" + _cryReady + ", cue=" + _cueReady + ", music=" + _musicReady +
            ", rage=" + _rageReady + ", death=" + _deathReady + ", flash=" + (_face != null) +
            ", cueDur=" + _cueDuration.ToString("0.0") + ", musicDur=" + _musicDuration.ToString("0.0");
        internal float SuggestedTriggerDelay => Mathf.Max(10f, _cueDuration + _musicDuration);

        internal ShyGuyAudio(string directory, Action<string> logger)
        {
            _log = logger;
            _folder = Path.Combine(directory, "PrivateMedia");
            string flashPath = Path.Combine(_folder, "face_flash.png");
            try
            {
                if (File.Exists(flashPath))
                {
                    _face = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!_face.LoadImage(File.ReadAllBytes(flashPath)))
                    { UnityEngine.Object.Destroy(_face); _face = null; }
                }
            }
            catch (Exception ex) { _log("Flash: arquivo indisponivel: " + ex.Message); }
            _log("Audio: usando FMOD nativo; inicializacao adiada para gameplay. " + Summary);
        }

        private bool EnsureReady(float now)
        {
            if (_initialized) return true;
            if (now < _nextInitAttempt) return false;
            _nextInitAttempt = now + 5f;
            try
            {
                FMOD.RESULT result = RuntimeManager.StudioSystem.getCoreSystem(out _system);
                if (result != FMOD.RESULT.OK)
                {
                    _log("Audio FMOD aguardando CoreSystem: " + result);
                    return false;
                }
                result = _system.getMasterChannelGroup(out _master);
                if (result != FMOD.RESULT.OK)
                {
                    _log("Audio FMOD aguardando MasterChannelGroup: " + result);
                    return false;
                }
                _cryReady = Load("cry.wav", true, out _crySound);
                _cueReady = Load("trigger.wav", false, out _cueSound);
                _musicReady = Load("warning_music.wav", false, out _musicSound);
                _rageReady = Load("rage_scream.wav", true, out _rageSound);
                _deathReady = Load("execution.wav", false, out _deathSound);
                _initialized = true;
                _log("Audio FMOD inicializado: " + Summary + "; triggerSugerido=" + SuggestedTriggerDelay.ToString("0.0") + "s");
                return true;
            }
            catch (Exception ex)
            {
                _log("Audio FMOD inicializacao pendente: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool Load(string file, bool loop, out FMOD.Sound sound)
        {
            sound = default(FMOD.Sound);
            string path = Path.Combine(_folder, file);
            if (!File.Exists(path))
            {
                _log("Audio FMOD: arquivo opcional ausente: " + file);
                return false;
            }
            try
            {
                FMOD.MODE mode = FMOD.MODE._2D | FMOD.MODE.CREATESTREAM |
                    (loop ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);
                FMOD.RESULT r = _system.createSound(path, mode, out sound);
                if (r != FMOD.RESULT.OK)
                {
                    _log("Audio FMOD ERRO createSound " + file + ": " + r);
                    return false;
                }
                if (loop) sound.setLoopCount(-1);
                uint milliseconds;
                if (sound.getLength(out milliseconds, FMOD.TIMEUNIT.MS) == FMOD.RESULT.OK)
                {
                    float seconds = Mathf.Max(.05f, milliseconds / 1000f);
                    if (file == "rage_scream.wav") _rageDuration = seconds;
                    else if (file == "trigger.wav") _cueDuration = seconds;
                    else if (file == "warning_music.wav") _musicDuration = seconds;
                    _log("Audio FMOD carregado: " + file + " (" + seconds.ToString("0.0") + "s)");
                }
                else _log("Audio FMOD carregado: " + file);
                return true;
            }
            catch (Exception ex)
            {
                _log("Audio FMOD excecao em " + file + ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool StartSound(FMOD.Sound sound, bool ready, float volume, string label,
            out FMOD.Channel channel)
        {
            channel = default(FMOD.Channel);
            if (!_initialized || !ready || !Enabled) return false;
            try
            {
                FMOD.RESULT r = _system.playSound(sound, _master, false, out channel);
                if (r != FMOD.RESULT.OK)
                {
                    _log("Audio FMOD ERRO playSound " + label + ": " + r);
                    return false;
                }
                r = channel.setVolume(Mathf.Clamp01(volume));
                if (r != FMOD.RESULT.OK) _log("Audio FMOD ERRO setVolume " + label + ": " + r);
                _log("Audio FMOD PLAY " + label + " volume=" + Mathf.Clamp01(volume).ToString("0.00"));
                return true;
            }
            catch (Exception ex)
            {
                _log("Audio FMOD ERRO " + label + ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static void Stop(ref FMOD.Channel channel, ref bool playing)
        {
            if (!playing) return;
            try { channel.stop(); } catch { }
            playing = false;
        }
        private static void Volume(ref FMOD.Channel channel, ref bool playing, float volume)
        {
            if (!playing) return;
            try
            {
                if (channel.setVolume(Mathf.Clamp01(volume)) != FMOD.RESULT.OK) playing = false;
            }
            catch { playing = false; }
        }

        internal void Reset()
        {
            Stop(ref _cryChannel, ref _cryPlaying);
            Stop(ref _cueChannel, ref _cuePlaying);
            Stop(ref _musicChannel, ref _musicPlaying);
            Stop(ref _rageChannel, ref _ragePlaying);
            Stop(ref _deathChannel, ref _deathPlaying);
            _triggerMusicAt = -1f;
            _flashAt = -100f;
        }

        internal void StateChanged(ShyGuyState state, float now)
        {
            if (!Enabled) { Reset(); return; }
            EnsureReady(now);
            if (state == ShyGuyState.Triggered)
            {
                Stop(ref _cryChannel, ref _cryPlaying);
                Stop(ref _rageChannel, ref _ragePlaying);
                Stop(ref _cueChannel, ref _cuePlaying);
                Stop(ref _musicChannel, ref _musicPlaying);
                _cuePlaying = StartSound(_cueSound, _cueReady, MasterVolume, "trigger GLOBAL", out _cueChannel);
                _triggerMusicAt = now + Mathf.Max(.05f, _cueDuration);
            }
            else if (state == ShyGuyState.Rage)
            {
                _triggerMusicAt = -1f;
                Stop(ref _cueChannel, ref _cuePlaying);
                Stop(ref _musicChannel, ref _musicPlaying);
                Stop(ref _cryChannel, ref _cryPlaying);
                Stop(ref _rageChannel, ref _ragePlaying);
                _ragePlaying = StartSound(_rageSound, _rageReady, 0f,
                    "grito perseguicao LOCAL", out _rageChannel);
            }
            else if (state == ShyGuyState.Calming || state == ShyGuyState.Docile)
            {
                Stop(ref _cueChannel, ref _cuePlaying);
                Stop(ref _musicChannel, ref _musicPlaying);
                Stop(ref _rageChannel, ref _ragePlaying);
                _triggerMusicAt = -1f;
                if (state == ShyGuyState.Docile)
                {
                    Stop(ref _cryChannel, ref _cryPlaying);
                    _cryPlaying = StartSound(_crySound, _cryReady, 0f, "choro LOCAL", out _cryChannel);
                }
            }
        }

        internal void Execution(float gain)
        {
            if (!Enabled || !EnsureReady(Time.unscaledTime)) return;
            Stop(ref _deathChannel, ref _deathPlaying);
            if (gain > .01f)
                _deathPlaying = StartSound(_deathSound, _deathReady, MasterVolume * gain,
                    "execucao LOCAL", out _deathChannel);
        }

        internal void TestCue()
        {
            if (!Enabled) { _log("TESTE AUDIO: som desligado no painel"); return; }
            if (!EnsureReady(Time.unscaledTime)) { _log("TESTE AUDIO: FMOD ainda indisponivel"); return; }
            Stop(ref _cueChannel, ref _cuePlaying);
            _cuePlaying = StartSound(_cueSound, _cueReady, MasterVolume,
                "TESTE cue GLOBAL", out _cueChannel);
            if (!_cuePlaying) _log("TESTE AUDIO: cue nao tocou; " + Summary);
        }
        internal void TestMusic()
        {
            if (!Enabled) { _log("TESTE AUDIO: som desligado no painel"); return; }
            if (!EnsureReady(Time.unscaledTime)) { _log("TESTE AUDIO: FMOD ainda indisponivel"); return; }
            Stop(ref _musicChannel, ref _musicPlaying);
            _musicPlaying = StartSound(_musicSound, _musicReady, MasterVolume,
                "TESTE musica GLOBAL", out _musicChannel);
            if (!_musicPlaying) _log("TESTE AUDIO: musica nao tocou; " + Summary);
        }
        internal void StopTests()
        {
            Stop(ref _cueChannel, ref _cuePlaying);
            Stop(ref _musicChannel, ref _musicPlaying);
            _log("TESTE AUDIO: canais de aviso/musica interrompidos");
        }

        internal void Update(float now, ShyGuyState state, WorldBridge world, object scp, WorldfallSight sight)
        {
            if (!Enabled) { Reset(); return; }
            if (!EnsureReady(now)) return;
            if (!_reportedReady)
            {
                _log("Audio FMOD operacional (sem Unity AudioClip.SetData): " + Summary);
                _reportedReady = true;
            }


            if (state == ShyGuyState.Triggered && _triggerMusicAt > 0f && now >= _triggerMusicAt)
            {
                _triggerMusicAt = -1f;
                Stop(ref _musicChannel, ref _musicPlaying);
                _musicPlaying = StartSound(_musicSound, _musicReady, MasterVolume,
                    "musica preparacao GLOBAL", out _musicChannel);
            }

            float gain = LocalGain(world, scp, sight);
            if (state == ShyGuyState.Docile && !_cryPlaying)
                _cryPlaying = StartSound(_crySound, _cryReady, 0f, "choro LOCAL", out _cryChannel);
            else if (state != ShyGuyState.Docile)
                Stop(ref _cryChannel, ref _cryPlaying);

            Volume(ref _cryChannel, ref _cryPlaying,
                state == ShyGuyState.Docile ? MasterVolume * .52f * gain : 0f);
            Volume(ref _rageChannel, ref _ragePlaying,
                state == ShyGuyState.Rage ? MasterVolume * gain : 0f);

        }

        internal float LocalGain(WorldBridge world, object scp, WorldfallSight sight)
        {
            if (scp == null || !world.Position(scp, out float sx, out float sy)) return 0f;
            float vx, vy;
            if (sight.Is3DWorldfallView)
            {
                object actor = sight.CurrentControlledActor;
                if (actor == null || !world.Position(actor, out vx, out vy)) return 0f;
            }
            else
            {
                if (Camera.main == null) return 0f;
                Vector3 center = Camera.main.ScreenToWorldPoint(
                    new Vector3(Screen.width * .5f, Screen.height * .5f, 0f));
                vx = center.x;
                vy = center.y;
            }
            float dist = Mathf.Sqrt((sx - vx) * (sx - vx) + (sy - vy) * (sy - vy));
            if (dist >= LocalRadius) return 0f;
            float normalized = Mathf.Clamp01(1f - dist / LocalRadius);
            return normalized * normalized;
        }
    }
}
