@echo off
setlocal
if "%~1"=="" (
  echo Usage: RunGenerationTests.cmd "C:\path\to\bin\Release\Azimuth.exe" [--matrix]
  exit /b 2
)
set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" exit /b 2
"%azimuthCsc%" /nologo /out:"%~dp1Azimuth.GenerationTests.exe" /r:"%~f1" /r:"%~dp1Newtonsoft.Json.dll" /r:System.Drawing.dll /r:System.Windows.Forms.dll "%~dp0GenerationTests.cs"
if errorlevel 1 exit /b 1
pushd "%~dp0"
"%~dp1Azimuth.GenerationTests.exe" %2
set "azimuthResult=%ERRORLEVEL%"
popd
exit /b %azimuthResult%
