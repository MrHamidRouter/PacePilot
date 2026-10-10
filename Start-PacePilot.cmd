@echo off
setlocal EnableExtensions DisableDelayedExpansion
cd /d "%~dp0"
chcp 65001 >nul
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "APP=%~dp0Bin\PacePilot.exe"
set "LOG=%TEMP%\PacePilot-selftest.log"
echo ================================================
echo PacePilot 2.0 - Developer: Mr.HamidRouter
echo One-click build / automated test / launch
echo ================================================
if not exist "%CSC%" (
 echo [FAILED] Windows .NET Framework 4.x C# compiler not found.
 echo Enable .NET Framework 4.x in Windows Features then retry.
 goto :fail
)
if not exist "Bin" mkdir "Bin"
echo [1/4] Checking RTSS installation (non-blocking, GUI offers auto-install if missing)...
if exist "%ProgramFiles(x86)%\RivaTuner Statistics Server\RTSS.exe" (echo [OK] RTSS detected.) else if exist "%ProgramFiles%\RivaTuner Statistics Server\RTSS.exe" (echo [OK] RTSS detected.) else echo [INFO] RTSS not found in default location. PacePilot will offer official RTSS winget installation when launched.
echo [2/4] Building fresh Windows EXE...
rem Compile to a random filename to avoid locking an EXE from a previous run.
set "TEMP_EXE=%~dp0Bin\PacePilot_%RANDOM%_%RANDOM%.exe"
"%CSC%" /nologo /target:winexe /platform:x64 /codepage:65001 /optimize+ /out:"%TEMP_EXE%" /win32icon:"Assets\PacePilot.ico" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Runtime.Serialization.dll "Source\Program.cs Source\StudioForm.cs"
if errorlevel 1 goto :fail
if not exist "%TEMP_EXE%" goto :fail
echo [3/4] Running built-in UI self-test...
del /q "%LOG%" >nul 2>&1
start "" /wait "%TEMP_EXE%" --selftest
if not exist "%LOG%" (
 echo [FAILED] Self-test log missing. Exit prior PacePilot from system tray.
 goto :fail
)
findstr /b /c:"PASS:" "%LOG%" >nul
if errorlevel 1 (
 type "%LOG%"
 echo [FAILED] UI self-test failed.
 goto :fail
)
echo [OK] UI self-test passed.
echo [4/4] Preparing stable PacePilot.exe...
rem Replacing a locked EXE can fail, so keep a verified newly-built fallback.
copy /y "%TEMP_EXE%" "%APP%" >nul 2>&1
if errorlevel 1 (
 echo [INFO] PacePilot.exe is already in use. Launching freshly built version.
 set "APP=%TEMP_EXE%"
) else (
 del /q "%TEMP_EXE%" >nul 2>&1
)
echo [OK] Launching program. EXE in Bin directory.
start "" "%APP%"
exit /b 0
:fail
echo.
echo Setup or test failed. Read the message above and README_FA.md.
pause
exit /b 1
