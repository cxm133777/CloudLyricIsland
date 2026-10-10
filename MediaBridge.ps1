param([string]$Root,[int]$OwnerPid)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager,Windows.Media.Control,ContentType=WindowsRuntime] | Out-Null
$script:asTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
function Await($op,$type) { $t=$script:asTask.MakeGenericMethod($type).Invoke($null,@($op)); if(-not $t.Wait(2500)){throw '媒体会话响应超时'}; return $t.Result }
$manager=$null
while(Get-Process -Id $OwnerPid -ErrorAction SilentlyContinue){
  try {
    if($null -eq $manager){$manager=Await ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync()) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager])}
    $session=$manager.GetSessions() | Where-Object { $_.SourceAppUserModelId -match 'cloudmusic|netease|163music' } | Select-Object -First 1
    $state=@{connected=$false; stamp=[DateTime]::UtcNow.ToString('o')}
    if($null -ne $session){
      $props=Await ($session.TryGetMediaPropertiesAsync()) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties])
      $timeline=$session.GetTimelineProperties(); $playing=$session.GetPlaybackInfo().PlaybackStatus.ToString() -eq 'Playing'
      $pos=$timeline.Position.TotalSeconds
      $duration=($timeline.EndTime-$timeline.StartTime).TotalSeconds
      $hasTimeline=$duration -gt 0 -and $timeline.LastUpdatedTime.Year -gt 2000
      if($playing -and $hasTimeline){$age=([DateTimeOffset]::UtcNow-$timeline.LastUpdatedTime).TotalSeconds; if($age -ge 0 -and $age -lt 86400){$pos+=$age}}
      $state=@{connected=$true;hasTimeline=$hasTimeline;title=$props.Title;artist=$props.Artist;playing=$playing;position=[Math]::Max(0,[Math]::Min($pos,$duration));duration=$duration;stamp=[DateTime]::UtcNow.ToString('o')}
    }
    $json=$state | ConvertTo-Json -Compress
    [IO.File]::WriteAllText((Join-Path $Root 'media.tmp'),$json,[Text.Encoding]::UTF8)
    Move-Item -LiteralPath (Join-Path $Root 'media.tmp') -Destination (Join-Path $Root 'media.json') -Force
    foreach($file in Get-ChildItem -LiteralPath $Root -Filter 'command-*.json'){
      try {
        $c=Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        if($session){
          $op=switch($c.action){'play'{$session.TryTogglePlayPauseAsync()};'next'{$session.TrySkipNextAsync()};'previous'{$session.TrySkipPreviousAsync()}}
          if($op){$ok=Await $op ([bool]); if(-not $ok){throw '网易云未接受播放控制'}}
        }
      } finally { Remove-Item -LiteralPath $file.FullName -Force }
    }
  } catch {
    [IO.File]::WriteAllText((Join-Path $Root 'bridge-error.txt'),$_.Exception.Message)
    $manager=$null
  }
  Start-Sleep -Milliseconds 450
}
