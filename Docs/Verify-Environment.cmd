@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if exist "%CSC%" (echo [OK] Windows .NET Framework x64 compiler) else (echo [MISSING] .NET Framework x64 compiler)
if exist "%ProgramFiles(x86)%\RivaTuner Statistics Server\RTSS.exe" (echo [OK] RTSS binary found) else (echo [INFO] RTSS may not be installed at its standard location)
if exist "%ProgramFiles(x86)%\RivaTuner Statistics Server\RTSSHooks64.dll" (echo [OK] RTSS native API found) else (echo [INFO] RTSS API not found at standard location)
echo.
echo Launch Run-PacePilot.cmd to build and run.
pause
