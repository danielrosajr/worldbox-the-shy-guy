#!/usr/bin/env bash
set -euo pipefail
SOURCE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)/Worldfall"
DEST="$HOME/.config/unity3d/mkarpenko/WorldBox/FirstPerson/packs/arvak_scp096_worldfall"
if [[ ! -f "$SOURCE/worldfall.json" || ! -f "$SOURCE/models/animal_scp096_shyguy.glb" ]]; then
    echo 'Pack WorldFall incompleto na pasta Mods; falta worldfall.json ou GLB' >&2
    exit 1
fi
mkdir -p "$DEST/models"
cp -- "$SOURCE/worldfall.json" "$DEST/worldfall.json"
cp -- "$SOURCE/models/animal_scp096_shyguy.glb" "$DEST/models/animal_scp096_shyguy.glb"
if ! cmp -s -- "$SOURCE/models/animal_scp096_shyguy.glb" "$DEST/models/animal_scp096_shyguy.glb"; then
    echo 'ERRO: modelo nao foi copiado corretamente' >&2; exit 1
fi
printf 'Modelo novo instalado: %s\n' "$DEST/models/animal_scp096_shyguy.glb"
sha256sum -- "$SOURCE/models/animal_scp096_shyguy.glb" "$DEST/models/animal_scp096_shyguy.glb"
echo 'IMPORTANTE: reinicie o WorldBox completamente e ative o pack SCP no F2 > Mods > Manage do WorldFall.'
