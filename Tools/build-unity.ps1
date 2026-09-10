param([ValidateSet('Verify','Apk','Aab')][string]$Mode = 'Apk', [string]$UnityPath)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = ((Get-Content -LiteralPath "$projectRoot\ProjectSettings\ProjectVersion.txt" | Where-Object { $_ -like 'm_EditorVersion:*' }) -split ': ',2)[1]
if (-not $UnityPath) { $UnityPath = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity $version was not found. Pass -UnityPath with the editor executable." }
$lockPath = Join-Path $projectRoot 'Temp\UnityLockfile'
$isOpen = $false
if (Test-Path -LiteralPath $lockPath) { try { $handle = [IO.File]::Open($lockPath,'Open','ReadWrite','None'); $handle.Dispose() } catch { $isOpen = $true } }
if ($isOpen) {
    if ($Mode -eq 'Aab') { throw 'The project is open. Use Space Station > 5. Build Release AAB in Unity after entering the signing passwords.' }
    $action = if ($Mode -eq 'Verify') { 'verify' } else { 'build-apk' }
    @{action=$action} | ConvertTo-Json | Set-Content -LiteralPath "$projectRoot\.station-command.json" -Encoding utf8
    Write-Output 'Build request sent to the open Unity editor. Check output/ and .station-response.json.'
    exit
}
$method = if ($Mode -eq 'Verify') { 'BatchVerify' } elseif ($Mode -eq 'Apk') { 'BatchBuild' } else { 'BuildAab' }
New-Item -ItemType Directory -Path "$projectRoot\output" -Force | Out-Null
$arguments = @('-batchmode','-nographics','-projectPath',('"'+$projectRoot+'"'),'-buildTarget','Android','-executeMethod',"SecondWind.SpaceStation.Editor.StationProject.$method",'-quit','-job-worker-count','2','-logFile',('"'+$projectRoot+'\output\Unity-'+$Mode+'.log"'))
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See output/Unity-$Mode.log." }
Write-Output "Unity $Mode complete. See output/."
