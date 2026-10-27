#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_dir"
if [[ -x .tools/dotnet/dotnet ]]; then dotnet_cmd="$project_dir/.tools/dotnet/dotnet"; else dotnet_cmd="dotnet"; fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
"$dotnet_cmd" publish src/FontFilter.Desktop -c Release -r osx-x64 --self-contained true -o 'release/mac/字里行间.app/Contents/MacOS'
mkdir -p 'release/mac/字里行间.app/Contents/Resources'
cp packaging/Info.plist 'release/mac/字里行间.app/Contents/Info.plist'
cp packaging/FontFilter.icns 'release/mac/字里行间.app/Contents/Resources/FontFilter.icns'
chmod +x 'release/mac/字里行间.app/Contents/MacOS/FontFilter'
codesign --force --deep --sign - 'release/mac/字里行间.app'
"$dotnet_cmd" publish src/FontFilter.Desktop -c Release -r win-x64 --self-contained true -o release/windows/FontFilter
cp README.md release/mac/使用说明.md
cp README.md release/windows/使用说明.md
(cd release/mac && ditto -c -k --sequesterRsrc --keepParent '字里行间.app' ../FontFilter-macOS-Intel.zip)
(cd release/windows && ditto -c -k --sequesterRsrc --keepParent FontFilter ../FontFilter-Windows-x64.zip)
