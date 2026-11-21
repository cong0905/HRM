@echo off
setlocal
if exist "%~dp0..\.dotnet\dotnet.exe" (
  "%~dp0..\.dotnet\dotnet.exe" %*
) else (
  dotnet %*
)
exit /b %errorlevel%
