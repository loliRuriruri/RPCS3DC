@echo off
rem ============================================================
rem  RPCS3 CLEAN baseline (GUI, global CLEAN config)
rem  profile : global config.yml (CLEAN)
rem  game    : Dragon's Crown (BCAS20298)
rem  This script only *reads* files. It never modifies saves,
rem  never deletes caches and needs no administrator rights.
rem ============================================================
setlocal
set "RPCS3_DIR=E:\PS3\RPCS3"
set "RPCS3=E:\PS3\RPCS3\rpcs3.exe"
set "PROFILE=E:\PS3\RPCS3\config\config.yml"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
if not exist "%RPCS3%" ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%" ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"

rem The global config of this copy is the CLEAN baseline. No game is booted automatically.
rem Guarantee ReShade stays out of the CLEAN environment (Vulkan layer opt-out):
set "DISABLE_VK_LAYER_reshade_1=1"
start "" "%RPCS3%"
exit /b 0
