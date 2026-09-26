@echo off
rem ============================================================
rem  DragonCrownProEnhanced.exe 빌드 (한 번만 실행)
rem  요구사항: .NET 8 SDK  https://dotnet.microsoft.com/download
rem  결과물: <ROOT>\Launcher\DragonCrownProEnhanced.exe
rem ============================================================
setlocal
set "SRC=%~dp0..\launcher\src"
set "OUT=%~dp0..\Launcher"
if not exist "%SRC%\DragonCrownProEnhanced.csproj" ( echo [ERROR] source not found: %SRC% & pause & exit /b 1 )
echo Building ... (single-file, self-contained, x64)
dotnet publish "%SRC%\DragonCrownProEnhanced.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUT%"
if errorlevel 1 ( echo [ERROR] build failed & pause & exit /b 1 )
echo Done: %OUT%\DragonCrownProEnhanced.exe
pause
