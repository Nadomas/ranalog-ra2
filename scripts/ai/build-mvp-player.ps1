$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path (Join-Path $root 'UnityProject/ra2-analog'))) {
  $root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
}

$exe = Join-Path $root 'UnityProject/ra2-analog/Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe'
$logDir = Join-Path $root 'artifacts/ai'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$buildLog = Join-Path $logDir 'mvp-player-build.txt'
$smokeLog = Join-Path $logDir 'mvp-player-smoke.txt'

Write-Host "Building MVP player via Unity MCP menu..."
# Prefer Editor menu when MCP is up; fallback message otherwise.
try {
  $id = [guid]::NewGuid().ToString('N')
  $payload = @{
    jsonrpc = '2.0'
    id = $id
    method = 'tools/call'
    params = @{
      name = 'execute_menu_item'
      arguments = @{ menu_path = 'Tools/RA2/Build MVP Windows Player (S11-07)' }
    }
  } | ConvertTo-Json -Depth 20 -Compress
  $bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
  Invoke-WebRequest -Uri 'http://localhost:8080/' -Method POST -Body $bytes -ContentType 'application/json; charset=utf-8' -UseBasicParsing -TimeoutSec 120 | Out-Null
} catch {
  Write-Warning "MCP menu invoke failed: $($_.Exception.Message). Use Unity menu Tools/RA2/Build MVP Windows Player (S11-07) manually."
}

$deadline = (Get-Date).AddMinutes(20)
while ((Get-Date) -lt $deadline) {
  Start-Sleep 10
  if (Test-Path $exe) {
    $age = (Get-Date) - (Get-Item $exe).LastWriteTime
    if ($age.TotalMinutes -lt 25) { break }
  }
  Write-Host ("waiting for exe... {0}" -f (Get-Date -Format 'HH:mm:ss'))
}

if (-not (Test-Path $exe)) {
  Write-Error "PLAYER_MISSING: $exe"
  exit 1
}

Write-Host "EXE=$exe"
Write-Host "Running smoke (-ra2-mvp-smoke)..."
if (Test-Path $smokeLog) { Remove-Item $smokeLog -Force }
$p = Start-Process -FilePath $exe -ArgumentList @('-ra2-mvp-smoke','-batchmode','-nographics',"-logfile",$smokeLog) -Wait -PassThru
Write-Host "exit=$($p.ExitCode)"
if (Test-Path $smokeLog) {
  Select-String -Path $smokeLog -Pattern 'S11-07' | ForEach-Object { $_.Line }
}
if ($p.ExitCode -ne 0) { exit $p.ExitCode }
exit 0
