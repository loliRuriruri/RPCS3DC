@echo off
rem ============================================================
rem  DragonCrownRemoteCoopSetup.exe 빌드 (Remote Co-op Helper v1)
rem  요구사항: .NET 8 SDK  https://dotnet.microsoft.com/download
rem  산출물: <ROOT>\Launcher\DragonCrownRemoteCoopSetup.exe
rem          (.NET 8 / WPF / x64 / self-contained / single-file)
rem ============================================================
setlocal
set "SRC=%~dp0..\remote-coop\src"
set "OUT=%~dp0..\Launcher"
if not exist "%SRC%\RemoteCoopSetup.csproj" ( echo [ERROR] source not found: %SRC% & pause & exit /b 1 )
echo Building ... (single-file, self-contained, x64)
dotnet publish "%SRC%\RemoteCoopSetup.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUT%"
if errorlevel 1 ( echo [ERROR] build failed & pause & exit /b 1 )
echo Done: %OUT%\DragonCrownRemoteCoopSetup.exe
pause
