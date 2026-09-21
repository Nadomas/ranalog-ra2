# Shared helpers for scripts/ai (dot-source from other scripts)
# Requires: PowerShell 5+ / pwsh

$script:AiRepoRoot = if ($PSScriptRoot) {
    (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    (Get-Location).Path
}

$script:AiProjectPath = Join-Path $script:AiRepoRoot "UnityProject\ra2-analog"
$script:AiArtifactsDir = Join-Path $script:AiRepoRoot "artifacts\ai"

function Get-AiRepoRoot { $script:AiRepoRoot }
function Get-AiProjectPath { $script:AiProjectPath }
function Get-AiArtifactsDir {
    if (-not (Test-Path $script:AiArtifactsDir)) {
        New-Item -ItemType Directory -Force -Path $script:AiArtifactsDir | Out-Null
    }
    $script:AiArtifactsDir
}

function Find-UnityEditor {
    if ($env:UNITY_EDITOR_PATH -and (Test-Path $env:UNITY_EDITOR_PATH)) {
        return (Resolve-Path $env:UNITY_EDITOR_PATH).Path
    }

    $candidates = @()

    $hubRoot = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor"
    if (Test-Path $hubRoot) {
        Get-ChildItem $hubRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $exe = Join-Path $_.FullName "Editor\Unity.exe"
            if (Test-Path $exe) { $candidates += $exe }
        }
    }

    $pf86 = ${env:ProgramFiles(x86)}
    if ($pf86) {
        $hubRoot86 = Join-Path $pf86 "Unity\Hub\Editor"
        if (Test-Path $hubRoot86) {
            Get-ChildItem $hubRoot86 -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                $exe = Join-Path $_.FullName "Editor\Unity.exe"
                if (Test-Path $exe) { $candidates += $exe }
            }
        }
    }

    # Prefer project pin 6000.5.9f1 when present
    $pinned = $candidates | Where-Object { $_ -match "6000\.5\.9f1" } | Select-Object -First 1
    if ($pinned) { return $pinned }

    if ($candidates.Count -gt 0) {
        return ($candidates | Sort-Object -Descending | Select-Object -First 1)
    }

    return $null
}

function Test-AiProjectExists {
    $proj = Get-AiProjectPath
    $versionFile = Join-Path $proj "ProjectSettings\ProjectVersion.txt"
    return (Test-Path $proj) -and (Test-Path $versionFile)
}

function Write-AiLogHeader {
    param([string]$Title, [string]$LogPath)
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    @"
==== $Title ====
time: $stamp
repo: $(Get-AiRepoRoot)
project: $(Get-AiProjectPath)
unity: $(Find-UnityEditor)
"@ | Set-Content -Path $LogPath -Encoding UTF8
}
