@echo off
setlocal EnableExtensions DisableDelayedExpansion
cd /d "%~dp0"
title PacePilot RTSS Setup - Mr.HamidRouter

echo ================================================
echo PacePilot Studio 2.0 - Developer: Mr.HamidRouter
echo ================================================
echo Checking RivaTuner Statistics Server (RTSS)...

set "RTSS_FOUND="
if exist "%ProgramFiles(x86)%\RivaTuner Statistics Server\RTSS.exe" set "RTSS_FOUND=1"
if exist "%ProgramFiles%\RivaTuner Statistics Server\RTSS.exe" set "RTSS_FOUND=1"

if not defined RTSS_FOUND (
    where winget >nul 2>&1
    if not errorlevel 1 (
        winget list --id Guru3D.RTSS --exact 2>nul | findstr /i /c:"Guru3D.RTSS" >nul
        if not errorlevel 1 set "RTSS_FOUND=1"
    )
)

if defined RTSS_FOUND (
    echo [OK] RTSS already installed. Skipping download.
    goto :launch
)

where winget >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Windows Package Manager ^(winget^) is unavailable.
    goto :manual
)

echo [INSTALL] Downloading and installing RTSS via the official winget package...
echo [INFO] An Administrator UAC prompt may appear.
winget install --id Guru3D.RTSS --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity
if errorlevel 1 (
    echo [ERROR] RTSS installation failed.
    goto :manual
)
echo [OK] winget completed successfully.

:launch
if /i "%~1"=="--rtss-only" exit /b 0
if exist "%~dp0PacePilot.exe" (
    echo Launching PacePilot.exe...
    start "" "%~dp0PacePilot.exe"
    exit /b 0
)
if exist "%~dp0Bin\PacePilot.exe" (
    echo Launching Bin\PacePilot.exe...
    start "" "%~dp0Bin\PacePilot.exe"
    exit /b 0
)
echo [ERROR] PacePilot.exe not found in the extracted folder.
echo Download the ready-to-use Windows ZIP from the GitHub Releases page.
pause
exit /b 2

:manual
echo.
echo Official RTSS download: https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/
echo When winget is unavailable, install RTSS manually and retry.
start "" "https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/"
pause
exit /b 1
