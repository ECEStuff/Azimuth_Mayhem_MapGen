@echo off
setlocal
set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" (
  echo .NET Framework compiler not found.
  exit /b 2
)
pushd "%~dp0"
"%azimuthCsc%" /nologo /out:OwnershipRulesTests.exe ..\SectorOwnershipRules.cs OwnershipRulesTests.cs
if errorlevel 1 (popd & exit /b 1)
OwnershipRulesTests.exe
set "azimuthResult=%ERRORLEVEL%"
popd
exit /b %azimuthResult%
