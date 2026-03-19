@echo off
setlocal
cd /d "%~dp0"

if not exist "DesktopClockExe\bin\Release\net8.0-windows\win-x64\publish\DesktopClockExe.exe" (
    echo Could not find DesktopClockExe.exe
    pause
    exit /b 1
)

start "" "DesktopClockExe\bin\Release\net8.0-windows\win-x64\publish\DesktopClockExe.exe"
