<#
.SYNOPSIS
  Batchmode compile smoke for UnityProject/ra2-analog.
.DESCRIPTION
  Locates Unity Editor (UNITY_EDITOR_PATH or Hub), runs -batchmode -quit with a log under artifacts/ai/.
  Exit 0 if log has no compile errors; non-zero otherwise.
.NOTES
  Honest limitation: does not replace Editor/MCP Play Mode verification for physics spikes.
#>
[CmdletBinding()]
param(
    [string]$ProjectPath = "",
    [switch]$NoQuit
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "_common.ps1")

if (-not (Test-AiProjectExists)) {
    Write-Error "Unity project not found at $(Get-AiProjectPath). Expected UnityProject/ra2-analog."
    exit 2
}

$unity = Find-UnityEditor
if (-not $unity) {
    Write-Host @"
ERROR: Unity Editor not found.
Set UNITY_EDITOR_PATH to the full path of Unity.exe, e.g.
  `$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe'
Or install Unity Hub + editor 6000.5.9f1 matching ProjectVersion.txt.
"@ -ForegroundColor Red
    exit 3
}

$proj = if ($ProjectPath) { $ProjectPath } else { Get-AiProjectPath }
$artifacts = Get-AiArtifactsDir
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = Join-Path $artifacts "build_$stamp.log"

Write-AiLogHeader -Title "build.ps1" -LogPath $logFile
Write-Host "Unity: $unity"
Write-Host "Project: $proj"
Write-Host "Log: $logFile"

$argsList = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $proj,
    "-logFile", $logFile
)
if (-not $NoQuit) { $argsList += "-quit" }

$proc = Start-Process -FilePath $unity -ArgumentList $argsList -Wait -PassThru
$code = $proc.ExitCode

# Append scan summary
$raw = Get-Content -Path $logFile -Raw -ErrorAction SilentlyContinue
$hasError = $false
if ($raw -match "(?im)error CS\d+" -or $raw -match "(?im)Scripts have compiler errors" -or $raw -match "(?im)Compilation failed") {
    $hasError = $true
}
if ($code -ne 0) { $hasError = $true }

Add-Content -Path $logFile -Value "`n==== scan exitCode=$code hasCompileError=$hasError ===="

if ($hasError) {
    Write-Host "BUILD FAILED (exit=$code). See $logFile" -ForegroundColor Red
    exit 1
}

Write-Host "BUILD OK (exit=$code). Log: $logFile" -ForegroundColor Green
exit 0
