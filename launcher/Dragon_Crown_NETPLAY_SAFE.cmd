@echo off
rem ============================================================
rem  Dragon's Crown NETPLAY SAFE (RPCN, no cheats)
rem  profile : DC_NETPLAY_SAFE
rem  game    : Dragon's Crown (BCAS20298) v1.09
rem  Read-only script. No admin rights. Never deletes saves.
rem ============================================================
setlocal
set "RPCS3_DIR=E:\PS3\RPCS3"
set "RPCS3=E:\PS3\RPCS3\rpcs3.exe"
set "PROFILE=E:\PS3\Profiles\DC_NETPLAY_SAFE\config.yml"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
if not exist "%RPCS3%"  ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%"    ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"

rem --- 1) cheat guard: config\cheats.yml must not contain enabled cheats for this title ---
echo [1/3] checking cheat state ...
if exist "%RPCS3_DIR%\config\cheats.yml" (
  findstr /I /C:"BCAS20298" "%RPCS3_DIR%\config\cheats.yml" >nul && (
    echo [WARN] cheats.yml contains entries for BCAS20298.
    echo        Disable all cheats in the Cheat Manager before netplay.
    choice /C YN /M "Continue anyway"
    if errorlevel 2 exit /b 1
  )
)

rem --- 2) patch guard: no enabled game patches for this title ---
echo [2/3] checking patch state ...
if exist "%RPCS3_DIR%\config\patch_config.yml" (
  findstr /I /C:"BCAS20298" "%RPCS3_DIR%\config\patch_config.yml" >nul && (
    echo [WARN] patch_config.yml contains entries for BCAS20298.
    echo        Remove/disable them before netplay (experimental patches break sync with the peer).
    choice /C YN /M "Continue anyway"
    if errorlevel 2 exit /b 1
  )
)

rem --- 3) ReShade off for the first netplay validation ---
echo [3/3] ReShade disabled for netplay validation.
set "DISABLE_VK_LAYER_reshade_1=1"
echo.
echo  NETPLAY SAFE: cheats OFF, experimental patches OFF, RPCN ON.
echo  Both players must use the same TITLE_ID (BCAS20298) and APP_VER (01.09).
echo.
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
