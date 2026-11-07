@echo off
setlocal
set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
pushd "%~dp0"
if not exist "%azimuthCsc%" (
  echo .NET Framework C# compiler was not found.
  set "azimuthResult=1"
  goto finish
)
"%azimuthCsc%" /nologo /out:RandomizerTests.exe /main:Azimuth.RandomizerTests /r:System.Core.dll ..\TypeTable.cs ..\ShipModifiers.cs ..\ShipRandomizer.cs ..\WeaponRandomizer.cs ..\WeaponModifiers.cs ..\TypeOverrideFile.cs RandomizerTestLocalization.cs RandomizerTests.cs
if errorlevel 1 (
  set "azimuthResult=1"
  goto finish
)
RandomizerTests.exe .. > RandomizerTests.log 2>&1
set "azimuthResult=%ERRORLEVEL%"
type RandomizerTests.log
:finish
echo.
echo Test exit code: %azimuthResult%
if /I not "%~1"=="--no-pause" pause
popd
exit /b %azimuthResult%
