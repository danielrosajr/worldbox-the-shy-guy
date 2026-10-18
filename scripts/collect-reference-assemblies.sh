#!/usr/bin/env bash
set -euo pipefail
GAME="${1:-$HOME/.steam/debian-installation/steamapps/common/worldbox}"
OUT="${2:-$HOME/scp096-assemblies}"
mkdir -p "$OUT"
for p in \
  'worldbox_Data/Managed/Assembly-CSharp.dll' \
  'worldbox_Data/StreamingAssets/mods/NeoModLoader.dll' \
  'worldbox_Data/StreamingAssets/mods/NML/Assemblies/0Harmony.dll'; do
  if [[ -f "$GAME/$p" ]]; then
    cp -v "$GAME/$p" "$OUT/"
  else
    echo "NOT FOUND: $GAME/$p"
  fi
done
find "$GAME" -type f -iname '*WorldFall*.dll' -print 2>/dev/null || true
echo "\nReview files before sharing. Output: $OUT"
