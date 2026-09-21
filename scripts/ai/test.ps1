<#
.SYNOPSIS
  Run Unity Test Runner in batchmode (EditMode by default).
.PARAMETER PlayMode
  Use PlayMode platform instead of EditMode.
.NOTES
  If the project has few/no Test Runner tests, Unity may exit 0 with zero tests — treat spike verifiers as the runtime DoD.
#>
[CmdletBinding()]
param(
    [switch]$PlayMode,
    [string]$ProjectPath = "",
    [string]$TestFilter = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "_common.ps1")

if (-not (Test-AiProjectExists)) {
    Write-Error "Unity project not found at $(Get-AiProjectPath)."
    exit 2
}

$unity = Find-UnityEditor
if (-not $unity) {
    Write-Host "ERROR: Unity Editor not found. Set UNITY_EDITOR_PATH. See scripts/ai/build.ps1 help." -ForegroundColor Red
    exit 3
}

$proj = if ($ProjectPath) { $ProjectPath } else { Get-AiProjectPath }
$artifacts = Get-AiArtifactsDir
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$platform = if ($PlayMode) { "PlayMode" } else { "EditMode" }
$logFile = Join-Path $artifacts "test_${platform}_$stamp.log"
$resultsFile = Join-Path $artifacts "test_results_${platform}_$stamp.xml"

Write-AiLogHeader -Title "test.ps1 ($platform)" -LogPath $logFile
Write-Host "Unity: $unity"
Write-Host "Platform: $platform"
Write-Host "Log: $logFile"
Write-Host "Results: $resultsFile"

$argsList = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $proj,
    "-runTests",
    "-testPlatform", $platform,
    "-testResults", $resultsFile,
    "-logFile", $logFile,
    "-quit"
)
if ($TestFilter) {
    $argsList += @("-editorTestsFilter", $TestFilter)
}

$proc = Start-Process -FilePath $unity -ArgumentList $argsList -Wait -PassThru
$code = $proc.ExitCode

$failed = $code -ne 0
if (Test-Path $resultsFile) {
    $xmlText = Get-Content $resultsFile -Raw -ErrorAction SilentlyContinue
    if ($xmlText -match 'result="Failed"' -or $xmlText -match "testcase.*outcome=`"Failed`"") {
        $failed = $true
    }
}

Add-Content -Path $logFile -Value "`n==== scan exitCode=$code failed=$failed ===="

if ($failed) {
    Write-Host "TESTS FAILED (exit=$code). See $logFile / $resultsFile" -ForegroundColor Red
    exit 1
}

Write-Host "TESTS OK (exit=$code). Results: $resultsFile" -ForegroundColor Green
Write-Host "Note: Prefer MCP run_tests + Play Mode verifiers for physics/net spikes when Editor is available."
exit 0
