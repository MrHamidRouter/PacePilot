@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "%~dp0Build-EXE.cmd"
if errorlevel 1 exit /b 1
set "LAST_EXE="
for /f "usebackq delims=" %%F in ("Bin\last-build.txt") do set "LAST_EXE=%%F"
if not defined LAST_EXE ( echo ERROR: Missing EXE path. & pause & exit /b 1 )
if exist "%TEMP%\PacePilot-selftest.log" del "%TEMP%\PacePilot-selftest.log"
start "" /wait "%~dp0%LAST_EXE%" --selftest
if exist "%TEMP%\PacePilot-selftest.log" (
 type "%TEMP%\PacePilot-selftest.log"
) else (
 echo FAIL: No test result. Exit any older PacePilot from its tray icon, then retry.
 pause
 exit /b 1
)
findstr /b /c:"PASS:" "%TEMP%\PacePilot-selftest.log" >nul
if errorlevel 1 ( echo SELF-TEST FAILED & pause & exit /b 1 )
echo SELF-TEST PASSED
pause
exit /b 0
