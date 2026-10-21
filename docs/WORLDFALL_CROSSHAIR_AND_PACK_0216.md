# v0.2.16 WorldFall first-person interaction

- Reticle `FirstPerson.WorldBoxMod.get_Picked` must resolve to the actual SCP `Actor` for 1.5 seconds continuously, at most `WorldfallMaxLookTiles=35` tiles. Does **not** compare `Heading` when `WorldfallReticleMode=true`.
- On each confirmed local controlled actor within 4 WorldBox tiles of SCP, mark witness immediately, even when looking away. Only player-controlled 3D actor; NPC proximity scan remains separate.
- Existing `WorldfallLookHoldSeconds=0.45` may persist in old NML configs, therefore use new `WorldfallReticleHoldSeconds=1.5` with optional fallback to default 1.5 seconds.
- `WorldfallProximityEnabled` and `WorldfallProximityTiles` are new config keys, default true and 4.
- No changes to 2D NPC proximity, FIFO, warning/rage audio, Dread or gameplay.

## Model not updated in WorldFall

WorldFall loads separate `~/.config/unity3d/mkarpenko/WorldBox/FirstPerson/packs/arvak_scp096_worldfall/` directory, which is NOT automatically replaced when the NML mod is updated.

Use F5 > `INSTALAR / VERIFICAR MODELO 3D`, then fully close/restart WorldBox, and confirm the pack is active in WorldFall F2 > Mods > Manage. Source GLB should be 260,012 bytes; exact comparison is used rather than size alone. The v0.2.16 GLB intentionally retains the 0.2.15 design until loading is verified.

The F5 tool accesses only those two SCP pack files (worldfall.json and models/animal_scp096_shyguy.glb). It never replaces engine DLLs or other packs. It intentionally does not auto-run at startup.

## Acceptance checks

- In F5, GLB status `ANTIGO/DIFERENTE` means installed pack is stale and should be updated.
- In F5, GLB status `NOVO OK` means on-disk files match; it does NOT prove that WorldFall has activated them.
- Looking at SCP's back for a continuous 1.5 seconds triggers the SCP (within 35 tiles).
- Holding gaze for less than 1.5 seconds and then looking away does not trigger by gaze (stay farther than 4 tiles while testing).
- Coming within 4 tiles triggers by proximity even without gaze.
- Witnesses are not duplicated in FIFO.
- All preexisting audio, rage and Dread remain operational.

Gameplay test required before release: NeoModLoader compilation and WorldFall reticle behavior are not available here.
