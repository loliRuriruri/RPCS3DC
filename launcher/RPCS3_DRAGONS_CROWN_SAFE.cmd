@echo off
rem ============================================================
rem  RPCS3 Dragon's Crown - SAFE (100%, compatibility first)
rem  profile : DC_SAFE (E:\PS3\Profiles\DC_SAFE\config.yml)
rem  game    : Dragon's Crown (BCAS20298)
rem  This script only *reads* files. It never modifies saves,
rem  never deletes caches and needs no administrator rights.
rem ============================================================
setlocal
set "RPCS3_DIR=E:\PS3\RPCS3"
set "RPCS3=E:\PS3\RPCS3\rpcs3.exe"
set "PROFILE=E:\PS3\Profiles\DC_SAFE\config.yml"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
if not exist "%RPCS3%" ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%" ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"

start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
