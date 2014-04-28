@echo off
setlocal
cd "%~dp0"
set MSBUILD=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
if not exist "%MSBUILD%" (
    echo The .NET Framework 4.0 does not appear to be installed on this 
    echo machine, which is required to build the solution.
    exit /b 1
)
 call    :msbuild 3.5 Debug   %* ^
 && call :msbuild 3.5 Release %* ^
 && call :msbuild 4.0 Debug   %* ^
 && call :msbuild 4.0 Release %*
goto :EOF

:msbuild
"%MSBUILD%" Gini.sln "/p:Configuration=NETFX %1 %2" /v:m %3 %4 %5 %6 %7 %8 %9
goto :EOF
