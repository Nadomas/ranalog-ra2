<#
.SYNOPSIS
  Quick smoke: status + batchmode compile.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== AI smoke-test ===" -ForegroundColor Cyan
& (Join-Path $root "status.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $root "build.ps1")
exit $LASTEXITCODE
