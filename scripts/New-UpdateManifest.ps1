param([string]$ReleaseDirectory=(Join-Path $PSScriptRoot '../release'),[string]$SigningKeyPath=(Join-Path $PSScriptRoot '../.tools/update-signing-key.pem'),[string]$Version='1.1.0')
$ErrorActionPreference='Stop'
if ($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') { throw 'Use a stable three-part version' }
$key=[Security.Cryptography.ECDsa]::Create()
try {
 $key.ImportFromPem([IO.File]::ReadAllText((Resolve-Path -LiteralPath $SigningKeyPath)))
 $expected=[Security.Cryptography.ECDsa]::Create()
 try { $expected.ImportFromPem([IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/FontFilter.Core/Updates/update-public-key.pem'))); if ([Convert]::ToBase64String($key.ExportSubjectPublicKeyInfo()) -ne [Convert]::ToBase64String($expected.ExportSubjectPublicKeyInfo())) { throw 'Release key does not match embedded client key' } } finally { $expected.Dispose() }
 $packages=@()
 foreach ($platform in @('win-x64','osx-x64')) {
  $name=if($platform -eq 'win-x64'){'FontFilter-Windows-x64.zip'}else{'FontFilter-macOS-Intel.zip'}
  $file=Get-Item -LiteralPath (Join-Path $ReleaseDirectory $name)
  $packages+=@{Platform=$platform;FileName=$name;Url="https://github.com/EAIONE/FontFilter/releases/download/v$Version/$name";Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant();Size=$file.Length}
 }
 $json=@{Version=$Version;Packages=$packages}|ConvertTo-Json -Depth 5
 $bytes=[Text.Encoding]::UTF8.GetBytes($json)
 [IO.File]::WriteAllBytes((Join-Path $ReleaseDirectory 'update.json'),$bytes)
 [IO.File]::WriteAllText((Join-Path $ReleaseDirectory 'update.sig'),[Convert]::ToBase64String($key.SignData($bytes,[Security.Cryptography.HashAlgorithmName]::SHA256)))
 'Created authenticated update.json and update.sig'
} finally { $key.Dispose() }
