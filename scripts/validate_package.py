#!/usr/bin/env python3
"""Lightweight *static* checks. Does NOT compile or test against WorldBox."""
from pathlib import Path
import json
import struct

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'mod.json').read_text())
config = json.loads((root / 'default_config.json').read_text())
assert manifest['version'] == '0.2.4'
assert manifest['name'] == 'SCP-096 The Shy Guy'
assert 'SCP096' in config and all(x['Id'] for x in config['SCP096'])
for f in ['ModClass.cs', 'src/WorldBridge.cs', 'src/Reflect.cs', 'src/StateMachine.cs', 'src/WitnessQueue.cs', 'src/Traits.cs', 'src/WorldfallSight.cs']:
    code = (root / f).read_text()
    assert code.count('{') == code.count('}'), f
    assert 'SCP096TheShyGuy' in code
for f in ['icon.png', 'Assets/Sprites/scp096_preview.png',
          'GameResources/actors/scp096_shyguy/Main/idle_1.png',
          'GameResources/actors/scp096_shyguy/Main/walk_1.png',
          'GameResources/actors/scp096_shyguy/child/idle_1.png',
          'GameResources/actors/scp096_shyguy/Main/swim_0.png',
          'GameResources/actors/scp096_shyguy/child/swim_3.png',
          'GameResources/ui/icons/scp096_witness.png',
          'GameResources/ui/icons/scp096_rage.png']:
    data=(root / f).read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>I',data[16:20])[0] > 0
assert 'private void OnGUI()' in (root / 'ModClass.cs').read_text()
assert 'SpawnOnClickedTile' in (root / 'ModClass.cs').read_text()
assert 'SpawnTestHuman' in (root / 'src/WorldBridge.cs').read_text()
assert 'SetupCustomTexture(asset, modFolder)' not in (root / 'src/WorldBridge.cs').read_text()
assert 'ResetChaseProgress' in (root / 'ModClass.cs').read_text()
assert 'TestKillVictim()' in (root / 'ModClass.cs').read_text()
assert 'NearestVictimAnywhere' in (root / 'src/WorldBridge.cs').read_text()
assert 'MarkVictimAnywhere' in (root / 'ModClass.cs').read_text()
assert 'ReconcileWitnesses' in (root / 'ModClass.cs').read_text()
assert 'goTo' in (root / 'src/WorldBridge.cs').read_text()
assert 'cancelAllBeh' in (root / 'src/WorldBridge.cs').read_text()
assert 'ArvakSCP096_Witness' in (root / 'src/Traits.cs').read_text()
assert 'private const string BaseId = "sheep"' in (root / 'src/WorldBridge.cs').read_text()
assert 'dieSimpleNone' in (root / 'src/WorldBridge.cs').read_text()
assert 'Perseguir e eliminar' in (root / 'ModClass.cs').read_text()
for f in ['README.md', 'CHANGELOG.md', 'LICENSE.md', 'CREDITS.md','docs/TEST_PLAN.md']:
    assert (root / f).stat().st_size > 80
print('PASS: JSON, source braces, PNG files, documentation. NOT a game/runtime test.')

# Regression check for trait menu labels. The game expects trait_<ID> and trait_<ID>_info.
for lang in ('en', 'pt_BR', 'pt'):
    loc = json.loads((root / 'Locales' / (lang + '.json')).read_text(encoding='utf-8'))
    for trait in ('ArvakSCP096_Witness', 'ArvakSCP096_RageSpeed'):
        assert loc.get('trait_' + trait), (lang, trait, 'name missing')
        assert loc.get('trait_' + trait + '_info'), (lang, trait, 'info missing')
assert 'distancia no ataque=' in (root / 'ModClass.cs').read_text()
print('PASS: trait localizations and attack-range diagnostics.')

# Optional Worldfall pack is a genuine binary glTF 2.0 .glb and never bundles Worldfall.dll.
pack = root / 'Worldfall'
glb = (pack / 'models/animal_scp096_shyguy.glb').read_bytes()
assert glb[:4] == b'glTF' and struct.unpack('<I', glb[4:8])[0] == 2
meta = json.loads((pack / 'worldfall.json').read_text())
assert meta['id'] == 'arvak_scp096_worldfall'
assert 'WorldfallFaceDetection' in [x['Id'] for x in config['SCP096']]
assert b'Worldfall.dll' not in glb
print('PASS: Worldfall 3D model GLB header, pack metadata, optional detection configuration.')

# Regression: v0.2.0 accidentally referenced an undeclared field and did not compile.
assert '_worldfallSightLogged' not in (root / 'ModClass.cs').read_text()
print('PASS: missing-field CS0103 regression guard.')

# v0.2.4 source-only feature guards.
sight=(root/'src/WorldfallSight.cs').read_text()
assert 'get_Picked' in sight and '_headings' in sight and 'holdSeconds' in sight
assert 'FaceTowardsViewer' in sight and 'ClearHold' in sight
assert 'TryKill' not in sight and 'Worldfall.dll' not in str(list(root.glob('**/*')))
assert (root/'docs/WORLDFALL_SIGHT_024.md').is_file()
print('PASS: v0.2.4 automatic 3D selection/facing/hold source guards.')
