@echo off
setlocal
chcp 1252 > nul
for %%i in (NuGet.exe) do set nuget=%%~dpnx$PATH:i
if "%nuget%"=="" goto :nonuget
cd "%~dp0"
call build /v:m ^
&& NuGet pack Gini.Source.nuspec ^
&& NuGet pack Gini.nuspec -Symbols
goto :EOF

:nonuget
echo NuGet executable not found in PATH
echo For more on NuGet, see http://nuget.codeplex.com
exit /b 2
