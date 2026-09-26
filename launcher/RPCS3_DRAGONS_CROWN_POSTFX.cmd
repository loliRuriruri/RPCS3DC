@echo off
rem ============================================================
rem  RPCS3 Dragon's Crown - 4K ULTRA + ReShade (optional post-processing)
rem  profile : DC_4K_ULTRA (E:\PS3\Profiles\DC_4K_ULTRA\config.yml)
rem  game    : Dragon's Crown (BCAS20298)
rem  This script only *reads* files. It never modifies saves,
rem  never deletes caches and needs no administrator rights.
rem ============================================================
setlocal
set "RPCS3_DIR=E:\PS3\RPCS3"
set "RPCS3=E:\PS3\RPCS3\rpcs3.exe"
set "PROFILE=E:\PS3\Profiles\DC_4K_ULTRA\config.yml"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
if not exist "%RPCS3%" ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%" ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"

rem ReShade loads automatically for rpcs3.exe (Vulkan implicit layer + ReShadeApps.ini).
rem Preset: E:\PS3\Mods_Patches\ReShade\Presets\DC_POSTFX_MINIMAL.ini  (overlay key: HOME)
rem To verify the layer is enabled: reg query "HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers"
rem To turn it off again: E:\PS3\Mods_Patches\ReShade\ReShade_Disable.cmd
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
