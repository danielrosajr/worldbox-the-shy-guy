#!/usr/bin/env bash
set -euo pipefail
SOURCE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)/Worldfall/models/animal_scp096_shyguy.glb"
DEST="$HOME/.config/unity3d/mkarpenko/WorldBox/FirstPerson/packs/arvak_scp096_worldfall/models/animal_scp096_shyguy.glb"
[[ -f "$SOURCE" ]] || { echo "GLB NOVO AUSENTE: $SOURCE"; exit 2; }
if [[ ! -f "$DEST" ]]; then echo "GLB NAO INSTALADO: $DEST"; exit 1; fi
printf 'Novo:     %s bytes\n' "$(wc -c < "$SOURCE")"
printf 'Instalado:%s bytes\n' "$(wc -c < "$DEST")"
sha256sum -- "$SOURCE" "$DEST"
if cmp -s -- "$SOURCE" "$DEST"; then
    echo 'IGUAIS: versao nova do GLB esta no diretorio do WorldFall.'
    echo 'Se visual nao mudou, reinicie WorldBox e confira pack ACTIVE no WorldFall F2.'
else
    echo 'DIFERENTES: execute bash scripts/install-worldfall-pack.sh'; exit 1
fi
