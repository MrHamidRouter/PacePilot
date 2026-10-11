@echo off
setlocal EnableExtensions DisableDelayedExpansion
cd /d "%~dp0"
chcp 65001 >nul
title PacePilot Studio - Developer Build
echo ===================================================
echo   PacePilot Studio - SOURCE BUILD
echo   Developer: Mr.HamidRouter
echo ===================================================
echo This is a developer build. End users should download
echo the SHA-256 verified ZIP from GitHub Releases instead.
echo.

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
 echo [ERROR] Windows .NET Framework 4.x C# compiler is unavailable.
 goto :failed
)
where nuget >nul 2>&1
if errorlevel 1 (
 echo [ERROR] NuGet CLI is required to restore the LibreHardwareMonitor library.
 echo Install the official NuGet CLI or build using GitHub Actions.
 echo We do not silently download an additional executable.
 goto :failed
)
if not exist "Bin" mkdir "Bin"
if not exist "Vendor" mkdir "Vendor"
echo [1/4] Restoring pinned open-source hardware libraries...
nuget install LibreHardwareMonitorLib -Version 0.9.4 -Framework net472 -Source https://api.nuget.org/v3/index.json -OutputDirectory Vendor -ExcludeVersion -NonInteractive -DependencyVersion Lowest
if errorlevel 1 goto :failed
powershell.exe -NoProfile -Command "$ErrorActionPreference='Stop'; $tfms=@('net472','net471','net47','net462','net461','net46','net452','net45','net40','netstandard2.0'); foreach($p in (Get-ChildItem Vendor -Directory)){foreach($tfm in $tfms){$dir=Join-Path $p.FullName ('lib\'+$tfm);if(Test-Path $dir){Get-ChildItem $dir -Filter '*.dll' -File|Copy-Item -Destination Bin -Force;break}}}"
if errorlevel 1 goto :failed
if not exist "Bin\LibreHardwareMonitorLib.dll" goto :missing
if not exist "Bin\HidSharp.dll" goto :missing

echo [2/4] Compiling the complete PacePilot application...
set "TEST=Bin\PacePilot-selftest.exe"
set "DEST=Bin\PacePilot.exe"
set "OPTS=/nologo /target:winexe /platform:x64 /codepage:65001 /unsafe /optimize+ /win32icon:Assets\PacePilot.ico /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Runtime.Serialization.dll /reference:System.Management.dll"
set "SRC=Source\Program.cs Source\StudioForm.cs Source\GameDiscovery.cs Source\GitHubUpdates.cs Source\RtssOsdBridge.cs Source\HwInfoBridge.cs Source\FrameActivityBar.cs Source\PremiumVisuals.cs Source\HardwareFallback.cs Source\AfterburnerBridge.cs Source\BuiltInSensors.cs Source\HardwareInventoryService.cs Source\HardwareSensorService.cs Source\SensorMappingForm.cs Source\ReleaseTrust.cs"
"%CSC%" %OPTS% /out:"%TEST%" %SRC%
if errorlevel 1 goto :failed

echo [3/4] Running UI and integrity self-tests...
del /q "%TEMP%\PacePilot-selftest.log" >nul 2>&1
start "" /wait "%TEST%" --selftest
if not exist "%TEMP%\PacePilot-selftest.log" goto :failed
findstr /b /c:"PASS:" "%TEMP%\PacePilot-selftest.log" >nul
if errorlevel 1 (
 type "%TEMP%\PacePilot-selftest.log"
 goto :failed
)

echo [4/4] Compiling the final EXE with standard UAC manifest...
rem Stop a previous copy of PacePilot before building; never force-kill other apps.
"%CSC%" %OPTS% /out:"%DEST%" /win32manifest:Assets\PacePilot.Administrator.manifest %SRC%
if errorlevel 1 (
 echo [ERROR] Could not replace PacePilot.exe. Exit PacePilot via tray and retry.
 goto :failed
)
echo [OK] Local build passed. Output: %CD%\%DEST%
echo [NOTE] A local build has NO GitHub provenance and may be unsigned.
echo [NOTE] Real publisher reputation requires a trusted Authenticode certificate.
echo Run "%DEST%" after reviewing Windows UAC/SmartScreen warnings.
exit /b 0

:missing
echo [ERROR] Required hardware-monitoring DLLs are missing after restore.
:failed
echo.
echo Source build failed. The official ZIP is on:
echo https://github.com/MrHamidRouter/PacePilot/releases/latest
pause
exit /b 1
