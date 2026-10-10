$ErrorActionPreference='Stop'
$fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf=Join-Path $fx 'WPF'
& (Join-Path $fx 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ /win32icon:"$PSScriptRoot\assets\cloud-island.ico" /win32manifest:"$PSScriptRoot\App.manifest" /out:"$PSScriptRoot\云词岛.exe" /reference:"$wpf\PresentationFramework.dll" /reference:"$wpf\PresentationCore.dll" /reference:"$wpf\WindowsBase.dll" /reference:"$fx\System.Xaml.dll" /reference:"$fx\System.Web.Extensions.dll" /reference:"$fx\System.Net.Http.dll" /reference:"$fx\System.Windows.Forms.dll" /reference:"$fx\System.Drawing.dll" "$PSScriptRoot\Native.cs" "$PSScriptRoot\App.cs" "$PSScriptRoot\Library.cs" "$PSScriptRoot\Control.cs" "$PSScriptRoot\Features.cs" "$PSScriptRoot\AppInfo.cs"
if($LASTEXITCODE -ne 0){throw '编译失败'}
