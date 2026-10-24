@echo off
setlocal

rem Builds CamelCavalry.dll into bin\Win64_Shipping_Client.
rem Extra arguments go to MSBuild, for example: build.cmd /p:DeployToGame=true

set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" goto fallback
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"

:fallback
if not defined MSBUILD set "MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if not exist "%MSBUILD%" (
  echo MSBuild was not found. Install Visual Studio or the Visual Studio Build Tools.
  exit /b 1
)

"%MSBUILD%" "%~dp0CamelCavalry.csproj" /nologo /verbosity:minimal %*
exit /b %ERRORLEVEL%
