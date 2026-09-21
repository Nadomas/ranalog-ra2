<#
.SYNOPSIS
  Print Unity path, project pin, artifacts dir, and short git status for AI pipeline.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "_common.ps1")

$unity = Find-UnityEditor
$proj = Get-AiProjectPath
$okProj = Test-AiProjectExists
$pin = "(missing)"
if ($okProj) {
    $vf = Join-Path $proj "ProjectSettings\ProjectVersion.txt"
    $line = Get-Content $vf -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($line) { $pin = $line }
}

Write-Host "repo:      $(Get-AiRepoRoot)"
Write-Host "project:   $proj"
Write-Host "exists:    $okProj"
Write-Host "pin:       $pin"
Write-Host "unity:     $(if ($unity) { $unity } else { '(not found — set UNITY_EDITOR_PATH)' })"
Write-Host "artifacts: $(Get-AiArtifactsDir)"
Write-Host "mcp:       .cursor/mcp.json → http://localhost:8080/ (Editor Window → Unity MCP)"
Write-Host "queue:     docs/ai/TASK_QUEUE.md"

Push-Location (Get-AiRepoRoot)
try {
    Write-Host "--- git ---"
    git status -sb 2>$null
    git rev-parse --abbrev-ref HEAD 2>$null
} finally {
    Pop-Location
}

if (-not $okProj) { exit 2 }
if (-not $unity) { exit 3 }
exit 0
