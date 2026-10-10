@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "%~dp0Build-EXE.cmd"
if errorlevel 1 exit /b 1
set "LAST_EXE="
for /f "usebackq delims=" %%F in ("Bin\last-build.txt") do set "LAST_EXE=%%F"
if not defined LAST_EXE exit /b 1
if exist "%TEMP%\PacePilot-rtss-diagnostic.txt" del "%TEMP%\PacePilot-rtss-diagnostic.txt"
start "" /wait "%~dp0%LAST_EXE%" --diagnose-rtss
if exist "%TEMP%\PacePilot-rtss-diagnostic.txt" (
 type "%TEMP%\PacePilot-rtss-diagnostic.txt"
 echo.
 echo Copy the diagnostic text above if FPS still does not apply.
) else (
 echo Error: RTSS diagnostic was not generated.
)
pause
