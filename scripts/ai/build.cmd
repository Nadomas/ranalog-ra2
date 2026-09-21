@echo off
REM Thin wrapper for cmd.exe users (prefers pwsh, falls back to Windows PowerShell)
where pwsh >nul 2>&1 && (
  pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
  exit /b %ERRORLEVEL%
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
