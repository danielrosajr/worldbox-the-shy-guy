#!/usr/bin/env python3
"""Lightweight *static* checks. Does NOT compile or test against WorldBox."""
from pathlib import Path
import json
import struct

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'mod.json').read_text())
config = json.loads((root / 'default_config.json').read_text())
assert manifest['version'] == '0.1.1'
assert manifest['name'] == 'SCP-096 The Shy Guy'
assert 'SCP096' in config and all(x['Id'] for x in config['SCP096'])
for f in ['ModClass.cs', 'src/WorldBridge.cs', 'src/Reflect.cs', 'src/StateMachine.cs']:
    code = (root / f).read_text()
    assert code.count('{') == code.count('}'), f
    assert 'SCP096TheShyGuy' in code
for f in ['icon.png', 'Assets/Sprites/scp096_preview.png',
          'GameResources/actors/scp096_shyguy/Main/idle_1.png',
          'GameResources/actors/scp096_shyguy/Main/walk_1.png']:
    data=(root / f).read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>I',data[16:20])[0] > 0
assert 'private void OnGUI()' in (root / 'ModClass.cs').read_text()
assert 'SpawnOnClickedTile' in (root / 'ModClass.cs').read_text()
assert 'SpawnTestHuman' in (root / 'src/WorldBridge.cs').read_text()
for f in ['README.md', 'CHANGELOG.md', 'LICENSE.md', 'CREDITS.md','docs/TEST_PLAN.md']:
    assert (root / f).stat().st_size > 80
print('PASS: JSON, source braces, PNG files, documentation. NOT a game/runtime test.')
