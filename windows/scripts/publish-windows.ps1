$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet publish src/FontFilter.Desktop -c Release -r win-x64 --self-contained true -o release/windows/FontFilter
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
Copy-Item README.md release/windows/FontFilter/README.md
Copy-Item -Recurse -Force packaging/licenses release/windows/FontFilter/licenses
Copy-Item THIRD-PARTY-NOTICES.md release/windows/FontFilter/
Compress-Archive -Path release/windows/FontFilter -DestinationPath release/FontFilter-Windows-x64.zip -Force
