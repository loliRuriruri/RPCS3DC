@echo off
rem ============================================================
rem  RPCS3 Dragon's Crown - RPCN NETPLAY (netplay validation baseline)
rem  profile : E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml
rem             (Vulkan / 300%% / AF Auto / MSAA Auto / WCB Off / Strict Off / ReShade Off)
rem             (Net: Internet Connected, PSN status RPCN, UPNP Off)
rem  game    : Dragon's Crown (BCAS20298) v1.09
rem  This script only *reads* files. No admin rights required.
rem ============================================================
setlocal
set "RPCS3_DIR=E:\PS3\RPCS3"
set "RPCS3=E:\PS3\RPCS3\rpcs3.exe"
set "PROFILE=E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"

if not exist "%RPCS3%"  ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%"    ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )

rem --- ReShade OFF for the first netplay validation (Vulkan layer opt-out) ---
set "DISABLE_VK_LAYER_reshade_1=1"

rem --- Make sure NordVPN / other VPN tunnels are disconnected before netplay ---
rem     (check: Windows Settings > Network, or NordVPN app > Disconnect)

cd /d "%RPCS3_DIR%"
echo ============================================================
echo  RPCN netplay checklist
echo   1) RPCS3 GUI -^> Network: PSN Status = RPCN, RPCN account logged in
echo   2) Both players: same TITLE_ID (BCAS20298), APP_VER 01.09, same RPCS3 build
echo   3) Host enters an actual stage, then Guest joins the friend's room
echo   4) Success = both players move in the same stage (not just room list)
echo ============================================================
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
