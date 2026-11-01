param([Parameter(Mandatory)][string]$OldProgramDirectory,[Parameter(Mandatory)][string]$NewProgramDirectory,[Parameter(Mandatory)][string]$SigningKeyPath,[string]$TestRoot=(Join-Path ([IO.Path]::GetTempPath()) ('fontfilter-update-test-'+[guid]::NewGuid().ToString('N'))))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $TestRoot -Force|Out-Null
$target=Join-Path $TestRoot '已安装 FontFilter';$data=Join-Path $TestRoot '用户数据';$feed=Join-Path $TestRoot 'feed'
New-Item -ItemType Directory -Path $data,$feed -Force|Out-Null
Copy-Item -LiteralPath $OldProgramDirectory -Destination $target -Recurse
$helper=Join-Path $OldProgramDirectory 'FontFilter.Updater.exe'
Copy-Item -LiteralPath $helper -Destination (Join-Path $NewProgramDirectory 'FontFilter.Updater.exe') -Force
$version=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $NewProgramDirectory 'FontFilter.dll')).Version.ToString(3)
[IO.Compression.ZipFile]::CreateFromDirectory($NewProgramDirectory,(Join-Path $feed 'FontFilter-Windows-x64.zip'),[IO.Compression.CompressionLevel]::Optimal,$true)
Copy-Item -LiteralPath (Join-Path $feed 'FontFilter-Windows-x64.zip') -Destination (Join-Path $feed 'FontFilter-macOS-Intel.zip')
& (Join-Path $PSScriptRoot 'New-UpdateManifest.ps1') -ReleaseDirectory $feed -SigningKeyPath $SigningKeyPath -Version $version
New-Item -ItemType Directory -Path (Join-Path $target 'Data') -Force|Out-Null
[IO.File]::WriteAllText((Join-Path $target 'Data/settings.json'),'portable-marker')
[IO.File]::WriteAllText((Join-Path $data 'settings.json'),'{"Size":41,"AutomaticUpdates":true,"TagUsage":{"保留测试":7}}')
$env:FONTFILTER_DATA=$data
$app=Start-Process -FilePath (Join-Path $target 'FontFilter.exe') -ArgumentList @('--verify-stage-update',('"'+$feed+'"')) -WindowStyle Hidden -PassThru -Wait
if($app.ExitCode -ne 0){if(Test-Path (Join-Path $data 'verify-error.txt')){Get-Content (Join-Path $data 'verify-error.txt')};throw "Application exited $($app.ExitCode)"}
$deadline=[DateTime]::UtcNow.AddSeconds(45)
while(!(Test-Path (Join-Path $data 'last-update.json')) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 150}
if(!(Test-Path (Join-Path $data 'last-update.json'))){Get-ChildItem $data -Filter '*error*'|ForEach-Object{Get-Content -LiteralPath $_.FullName};Get-Content (Join-Path $data 'update-check.txt') -ErrorAction SilentlyContinue;throw 'Helper did not install'}
if([Reflection.AssemblyName]::GetAssemblyName((Join-Path $target 'FontFilter.dll')).Version.ToString(3) -ne $version){throw 'Installed version mismatch'}
if([IO.File]::ReadAllText((Join-Path $target 'Data/settings.json')) -ne 'portable-marker'){throw 'Portable data lost'}
$settings=Get-Content (Join-Path $data 'settings.json') -Raw|ConvertFrom-Json
if($settings.Size -ne 41 -or $settings.TagUsage.'保留测试' -ne 7){throw 'User settings lost'}
if(!(Test-Path (Join-Path $data 'silent-before-close-pass.txt'))){throw 'Active-window safety check missing'}
$installed=Get-Content (Join-Path $data 'last-update.json') -Raw|ConvertFrom-Json
if(!(Test-Path -LiteralPath $installed.Backup)){throw 'Previous program backup missing'}
$tagCheck=Start-Process -FilePath (Join-Path $target 'FontFilter.exe') -ArgumentList '--verify-tags' -WindowStyle Hidden -PassThru -Wait
if($tagCheck.ExitCode -ne 0){throw 'Updated application did not reopen/verify'}
"PASS real application normal exit -> separate helper -> verified install $version -> reopen; user settings and portable data preserved. $TestRoot"
