#!/usr/bin/env bash
set -euo pipefail
SOURCE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)/Worldfall"
DEST="$HOME/.config/unity3d/mkarpenko/WorldBox/FirstPerson/packs/arvak_scp096_worldfall"
if [[ ! -f "$SOURCE/worldfall.json" || ! -f "$SOURCE/models/animal_scp096_shyguy.glb" ]]; then
  echo 'Pack Worldfall incompleto: faltam worldfall.json ou GLB.' >&2
  exit 1
fi
mkdir -p "$(dirname "$DEST")"
mkdir -p "$DEST"
cp -a "$SOURCE"/. "$DEST"/
echo "Pack instalado em: $DEST"
echo 'No jogo: F2 > Mods > Manage > mover o pack SCP-096 para Active.'
