@echo off
rem ============================================================
rem  Dragon's Crown CHEAT OFFLINE (4K, network disabled)
rem  profile : DC_CHEAT_OFFLINE
rem  game    : Dragon's Crown (BCAS20298) v1.09
rem  Read-only script. No admin rights. Never deletes saves.
rem ============================================================
setlocal
set "RPCS3_DIR=E:\PS3\RPCS3"
set "RPCS3=E:\PS3\RPCS3\rpcs3.exe"
set "PROFILE=E:\PS3\Profiles\DC_CHEAT_OFFLINE\config.yml"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
if not exist "%RPCS3%"  ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%"    ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"

rem --- 1) verified save backup before any cheat session ---
echo [1/3] backing up save data ...
powershell -NoProfile -ExecutionPolicy Bypass -File "E:\PS3\Tools\save_backup.ps1" -Label "cheat_offline"
if errorlevel 2 ( echo [ERROR] save backup failed - aborting. & pause & exit /b 1 )

rem --- 2) offline guard: profile must have RPCN disabled ---
echo [2/3] verifying offline profile ...
findstr /C:"PSN status: Disconnected" "%PROFILE%" >nul || ( echo [ERROR] profile is not offline. & pause & exit /b 1 )
findstr /C:"Internet enabled: Disconnected" "%PROFILE%" >nul || ( echo [ERROR] internet is not disabled. & pause & exit /b 1 )

rem --- 3) ReShade off + cheat reminder ---
echo [3/3] ReShade disabled for this session.
set "DISABLE_VK_LAYER_reshade_1=1"
echo.
echo  CHEAT MODE: RPCN/online is disabled. Do NOT use this profile for netplay.
echo  Cheats are enabled inside RPCS3 via: Game list -^> right click game -^> Cheats
echo.
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
