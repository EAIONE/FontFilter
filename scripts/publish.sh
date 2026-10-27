#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_dir"
if [[ -x .tools/dotnet/dotnet ]]; then dotnet_cmd="$project_dir/.tools/dotnet/dotnet"; else dotnet_cmd="dotnet"; fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
# Stage outside synced Documents folders: File Provider can re-add FinderInfo,
# which makes strict signature validation fail even immediately after cleaning.
stage_dir="$(mktemp -d "${TMPDIR:-/tmp}/fontfilter-publish.XXXXXX")"
mac_app="$stage_dir/mac/字里行间.app"
mkdir -p "$mac_app/Contents/Resources/licenses" release
"$dotnet_cmd" publish src/FontFilter.Desktop -c Release -r osx-x64 --self-contained true -o "$mac_app/Contents/MacOS"
cp packaging/Info.plist "$mac_app/Contents/Info.plist"
cp packaging/FontFilter.icns "$mac_app/Contents/Resources/FontFilter.icns"
cp -R packaging/licenses/. "$mac_app/Contents/Resources/licenses/"
cp THIRD-PARTY-NOTICES.md "$mac_app/Contents/Resources/"
xattr -cr "$mac_app"
chmod +x "$mac_app/Contents/MacOS/FontFilter"
codesign --force --deep --sign - "$mac_app"
codesign --verify --deep --strict "$mac_app"
"$dotnet_cmd" publish src/FontFilter.Desktop -c Release -r win-x64 --self-contained true -o release/windows/FontFilter
cp README.md "$stage_dir/mac/README.md"
cp README.md release/windows/FontFilter/README.md
mkdir -p release/windows/FontFilter/licenses
cp -R packaging/licenses/. release/windows/FontFilter/licenses/
cp THIRD-PARTY-NOTICES.md release/windows/FontFilter/
ditto -c -k --sequesterRsrc "$stage_dir/mac" "$stage_dir/FontFilter-macOS-Intel.zip"
python3 - "$stage_dir/FontFilter-Windows-x64.zip" <<'PYZIP'
from pathlib import Path
import sys,zipfile
with zipfile.ZipFile(sys.argv[1],'w',zipfile.ZIP_DEFLATED) as archive:
 for p in Path('release/windows/FontFilter').rglob('*'):
  if p.is_file():archive.write(p,arcname='FontFilter/'+str(p.relative_to('release/windows/FontFilter')))
PYZIP
cp "$stage_dir/FontFilter-macOS-Intel.zip" release/
cp "$stage_dir/FontFilter-Windows-x64.zip" release/
printf 'Packages saved to %s/release\nTemporary stage: %s\n' "$project_dir" "$stage_dir"
