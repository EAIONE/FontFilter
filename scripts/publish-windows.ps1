$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet publish src/FontFilter.Desktop -c Release -r win-x64 --self-contained true -o release/windows/FontFilter
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
$helperOutput=Join-Path ([IO.Path]::GetTempPath()) ('fontfilter-helper-'+[guid]::NewGuid().ToString('N'))
dotnet publish src/FontFilter.Updater -c Release -r win-x64 --self-contained true -o $helperOutput
if ($LASTEXITCODE -ne 0) { throw "Updater publish failed" }
Copy-Item -LiteralPath (Join-Path $helperOutput 'FontFilter.Updater.exe') -Destination release/windows/FontFilter/FontFilter.Updater.exe
Copy-Item README.md release/windows/FontFilter/README.md
Copy-Item -Recurse -Force packaging/licenses release/windows/FontFilter/licenses
Copy-Item THIRD-PARTY-NOTICES.md release/windows/FontFilter/
Compress-Archive -Path release/windows/FontFilter -DestinationPath release/FontFilter-Windows-x64.zip -Force
# After the matching Mac archive is built, run New-UpdateManifest.ps1 with the local release key.
