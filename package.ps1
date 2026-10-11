$ErrorActionPreference='Stop'
$version='1.0.1'
$output=Join-Path $PSScriptRoot 'dist'
$stage=Join-Path $output ('CloudLyricIsland-v'+$version+'-win-x64')
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$files=@('云词岛.exe','Island.xaml','SettingsTheme.xaml','WindowTheme.xaml','MediaBridge.ps1','ClientRuntime.js','README.md','LICENSE','DISCLAIMER.md','THIRD_PARTY_NOTICES.md')
foreach($file in $files){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $stage $file) -Force}
$assets=Join-Path $stage 'assets'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
foreach($file in @('cloud-island.ico','cloud-island.png','设计说明.md')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('assets\'+$file)) -Destination $assets -Force}
$archive=Join-Path $output ('CloudLyricIsland-v'+$version+'-win-x64.zip')
Compress-Archive -LiteralPath $stage -DestinationPath $archive -CompressionLevel Optimal -Force
$hash=Get-FileHash -LiteralPath $archive -Algorithm SHA256
$checksum=$hash.Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($archive)
[IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS.txt'),$checksum+[Environment]::NewLine,[Text.Encoding]::ASCII)
Write-Output $archive
Write-Output $checksum
