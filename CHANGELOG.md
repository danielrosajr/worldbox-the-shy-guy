# Changelog

## 0.2.8-alpha — WorldFall overlay and music state fix
- feat: `ShyGuyOverlay` ScreenSpaceOverlay RawImage at sortingOrder 32767, persistent across WorldFall transitions, with fading and IMGUI fallback.
- feat: visual witness flash when the controlled WorldFall actor sees SCP-096, plus death overlay only on a confirmed SCP kill of that actor.
- fix: replaced the old flash-timer-based music muting with a state-aware `DynamicMusicController`.
- fix: capture and restore original `bus:/Music` volume (and clearly identified Unity music sources), without touching global Master/SFX.
- feat: 2.8s fade out, 3.5s fade in, config keys and controls; preserve silence throughout TRIGGERED and RAGE for the affected player.
- feat: F5 preview buttons and Shift+F10 / Shift+F11 hotkeys available in WorldFall 3D.
- chore: separate personal media testing from code-only publication.
- regression: pursuit, FIFO queue, kill range, automatic WorldFall gaze and global/local FMOD cues unchanged.
- status: source and package static checks only; NML compilation and 3D runtime verification pending.

## 0.2.7-alpha — Timing and mix prototype
- Triggered preparation length set to preserve audio timing and rage scream localized.
- The original music-override lifetime was tied to a finite flash timer (fixed in 0.2.8).
