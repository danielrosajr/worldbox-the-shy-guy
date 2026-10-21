# v0.2.15 — 3D Reborn

## GLB design

The source file is `scripts/build_scp096_glb.py` (Python 3, numpy, trimesh).
Running `python scripts/build_scp096_glb.py` from the mod directory rebuilds
`Worldfall/models/animal_scp096_shyguy.glb`. No Python dependencies are needed
to install or play the mod.

The 3D model reuses the old orientation convention: +Z up and facial forward
toward -Y. Height is approximately 2.21 units, retaining the old model's 2.185-unit
scale. Eleven meshes include six legacy-named geometry groups:
`body`, `head`, `claw_l`, `claw_r`, `leg_l`, and `leg_r`,
plus eye/tear, mouth/teeth and subtle shadow details.

It uses vertex-colored geometry rather than third-party texture images; no
skeletal rig, glTF animation clips, blending or automatic state-based pose changes.

Approximate cost: 14,230 triangles, 260 KB GLB; optimized as a *moderate
low-poly model*, not a photoreal high-resolution sculpt.

## WorldFall installation

This is crucial: the NeoModLoader mod folder and WorldFall pack folder are
different. Updating `Mods/SCP096-TheShyGuy-0.2.15` alone does not guarantee
the existing pack within `FirstPerson/packs` gets refreshed.

Run `bash scripts/install-worldfall-pack.sh` from the unpacked mod directory.
The script copies `Worldfall/worldfall.json` and the new GLB to:
`~/.config/unity3d/mkarpenko/WorldBox/FirstPerson/packs/arvak_scp096_worldfall/`
Then activate the pack in the WorldFall Mods menu (F2), if not active.

## Acceptance criteria

1. GLB loads without crashes / incomplete surfaces.
2. Front of SCP is -Y as in v0.2.14; large black eyes and mouth are visible.
3. Feet stand near ground (Z=0.03) and height matches old (~2.2).
4. Hands and fingers are clearly separate, not fused.
5. FPS performance remains acceptable on integrated Intel graphics.
6. Dread/FGM audio/AI continue as in v0.2.14.

## Animation roadmap

v0.2.16: inspect actual WorldFall GLB loader for glTF skins, joints and
animation clips; implement procedural idle/weeping only if supported.
v0.2.17: rage / sprint / execution transitions with tested playback.
Never claim baked glTF animations are supported until the actual loader is inspected.

## Safety

The standalone GLB uses original generated vertex-colored geometry. The
personal-test ZIP also contains user-supplied sounds/images whose rights
for public distribution have not been established; omit those in GitHub.
