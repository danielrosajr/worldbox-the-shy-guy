# WorldFall UI / audio inspection notes — v0.2.8

**Sources inspected:** supplied `Worldfall-inspection.tar.gz` (Worldfall.dll) and supplied `TheUnrelenting.zip` C# source.

## Confirmed working in this user's earlier SCP version

- `FirstPerson.WorldBoxMod` is instantiated and exposes `get_Host()`, `get_View()`, and `get_IsFirstPerson()`, as confirmed by Player.log in v0.2.3+.
- WorldFall camera sight and SCP-096 face trigger succeeded in v0.2.4.
- TheUnrelenting's source includes a `Canvas` with a dynamically resolved `UnityEngine.UI.RawImage`, ScreenSpaceOverlay, sortingOrder 32767, and DontDestroyOnLoad.
- TheUnrelenting's source probes `FMODUnity.RuntimeManager.StudioSystem`, calls `getBus("bus:/Music")`, captures `getVolume`, and restores saved volume after the event.

## Identified but not yet verified for this mod

- The Worldfall.dll's metadata contains identifiers `DrawDeathScreen` and `DeathScreenSeconds`. A named signature alone does not establish the type, signature, or hook safety. **This version does not patch the WorldFall death screen.**
- The WorldFall-specific music routing has not been verified in-game for this build; the controller only uses bus:/Music or explicitly named Unity music sources.
- A ScreenSpaceOverlay is a supported Unity technique but may still be superseded by later interfaces; a logged runtime preview is required.

## Safety

- No Harmony patches; all UI objects belong to the SCP mod and are destroyed on unload.
- The controlled actor is identified before SCP dieSimpleNone() is called. The death overlay is launched only after that kill request was accepted and victim death was confirmed.
- Reset restores the music snapshot immediately on game-world switch or mod unload. UI preview does not modify WorldFall death logic.
