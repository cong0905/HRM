@echo off
title HRM Windows Application
cd /d "%~dp0db\src\HRM.GUI\bin\Debug\net10.0-windows"
start "" "%~dp0.dotnet\dotnet.exe" "%~dp0db\src\HRM.GUI\bin\Debug\net10.0-windows\HRM.GUI.dll"
