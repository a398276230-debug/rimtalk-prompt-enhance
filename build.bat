@echo off
echo Building RimTalk Health Enhance...
cd Source
dotnet build -c Release
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! DLL output to 1.6\Assemblies\
) else (
    echo.
    echo Build failed! Check errors above.
)
pause
