@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: .NET Framework x64 C# compiler not found.
  pause
  exit /b 1
)
if not exist "Bin" mkdir "Bin"
rem NEVER overwrite a running EXE. Produce a unique executable for each build.
set "OUTPUT=Bin\PacePilot_v1.4_%RANDOM%_%RANDOM%_%RANDOM%.exe"
if exist "Bin\last-build.txt" del /q "Bin\last-build.txt" >nul 2>&1
echo Building PacePilot v1.4 to a new EXE (safe while older builds are running)...
"%CSC%" /nologo /target:winexe /platform:x64 /codepage:65001 /optimize+ /out:"%OUTPUT%" /win32icon:"Assets\PacePilot.ico" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Runtime.Serialization.dll "Source\Program.cs"
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Please send the messages above.
  pause
  exit /b 1
)
> "Bin\last-build.txt" echo %OUTPUT%
echo.
echo Build OK: %OUTPUT%
exit /b 0
