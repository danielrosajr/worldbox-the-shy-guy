# SCP-096 v0.2.8-alpha — WorldFall overlay & sustained music override

Experimental patch based on the working v0.2.7 chase, face detection and FMOD sounds.
**The code has not been compiled/run inside the user's WorldBox.**

## Changes

- Witness flash uses a priority **ScreenSpaceOverlay Canvas** with a RawImage; IMGUI is only a fallback if Unity UI is unavailable. Works without replacing the WorldFall death UI.
- On confirmed SCP-096 execution of the controlled WorldFall actor, an optional 2.6-second death overlay is displayed. Unrelated deaths remain untouched.
- A new `DynamicMusicController` attempts `FMODUnity.RuntimeManager.StudioSystem.getBus("bus:/Music")`, saves the music bus volume, fades it to silence in 2.8 seconds, and restores the original volume in 3.5 seconds. It also scans clearly named Unity AudioSources at intervals.
- **Only when the local possessed actor is marked**, the game soundtrack is muted through the entire `TRIGGERED → RAGE` cycle, until the victim dies or the chase ends. Other NPCs triggering SCP-096 do not cause music ducking.
- Global `trigger.wav` and `warning_music.wav` continue playing on separate Core FMOD channels. Proximity cry/rage SFX and ambient sound stay unchanged.
- WorldFall flash defaults to 1.65 seconds and 60% opacity; 2D flash is capped at 42%.
- F5 includes separate **preview flash**, **preview death flash**, **test BGM mute**, **restore music** controls.
- WorldFall hotkeys: **Shift+F10 = preview flash**, **Shift+F11 = toggle BGM override test**. F10 and F11 without Shift retain the prior diagnostics/manual witness behavior.

## Installation

1. Close WorldBox.
2. Remove old `SCP096-TheShyGuy-0.2.x` directories from the game's `Mods` folder.
3. Extract the **personal-test** ZIP's `SCP096-TheShyGuy-0.2.8` folder into `Mods`.
4. Create a temporary test world, spawn the SCP with F6, and enter WorldFall possession with G.
5. **Shift+F10** while inside WorldFall: confirm overlay on first-person screen. **Shift+F11**: confirm the game soundtrack fades out and fades back when the shortcut is pressed again.
6. Look at the SCP face to become its witness; the soundtrack should remain reduced while SCP is still chasing you.
7. Trigger an unrelated NPC instead: music should keep playing normally.

The Worldfall 3D model/GLB did not change and does not need to be reinstalled.

## Logs

```bash
grep -nE 'SCP096|BGM OVERRIDE|FLASH CANVAS|FLASH SCP096|error CS|Exception' \
~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -180
```

Expect lines such as `FLASH CANVAS 3D CRIADO`, `BGM OVERRIDE: FMOD music bus encontrado=bus:/Music`, `BGM OVERRIDE: silencio alcançado`, and `BGM OVERRIDE: volume de musica restaurado`. When music was not found, the logs say so; never mute master or general SFX as a fallback.

## Music caveat

If music originates from another WorldFall-specific mixer not routed to `bus:/Music` and no clearly identifiable Unity music source exists, the mod **will not** silence it. Inspect the actual audio graph before touching any other bus. This is a guarded attempt, not a guaranteed override.

## Media / distribution

The `personal-test` archive has the user's sound and visual assets **only for local testing**. The `code-only` package omits those assets due to unverified individual copyright licenses; it will run without external WAV/PNG but cannot display the supplied flash.
