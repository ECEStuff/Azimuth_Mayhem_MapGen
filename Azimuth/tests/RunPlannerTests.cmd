@echo off
setlocal
set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" set "azimuthCsc=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%azimuthCsc%" (
  echo .NET Framework C# compiler was not found.
  exit /b 1
)
pushd "%~dp0"
"%azimuthCsc%" /nologo /out:TerritoryPlannerTests.exe /main:Azimuth.TerritoryPlannerTests ..\TerritoryPlanner.cs TerritoryPlannerTests.cs
if errorlevel 1 (popd & exit /b 1)
TerritoryPlannerTests.exe
set "azimuthResult=%ERRORLEVEL%"
popd
exit /b %azimuthResult%
