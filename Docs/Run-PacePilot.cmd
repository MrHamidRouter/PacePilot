@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "%~dp0Build-EXE.cmd"
if errorlevel 1 exit /b 1
set "LAST_EXE="
for /f "usebackq delims=" %%F in ("Bin\last-build.txt") do set "LAST_EXE=%%F"
if not defined LAST_EXE ( echo ERROR: Could not locate compiled executable. & pause & exit /b 1 )
if not exist "%LAST_EXE%" ( echo ERROR: Compiled executable missing. & pause & exit /b 1 )
start "" "%~dp0%LAST_EXE%"
exit /b 0
