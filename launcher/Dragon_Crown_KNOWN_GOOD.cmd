@echo off
rem ============================================================
rem  Dragon's Crown KNOWN GOOD fallback (verified snapshot)
rem  snapshot : E:\PS3\RPCS3\KnownGood   (v0.0.42-20053-38eba804, boot-tested)
rem  Note: the snapshot has its own dev_hdd0 -> save data may differ from Current.
rem        Sync saves first with:  E:\PS3\Tools\sync_saves_to_known_good.cmd
rem ============================================================
setlocal
set "KG=E:\PS3\RPCS3\KnownGood"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
set "PROFILE=%KG%\config\custom_configs\BCAS20298.yml"
if not exist "%KG%\rpcs3.exe" ( echo [ERROR] KnownGood snapshot missing: "%KG%\rpcs3.exe" & pause & exit /b 1 )
if not exist "%PROFILE%"     ( echo [ERROR] KnownGood profile missing & pause & exit /b 1 )
if not exist "%GAME%"        ( echo [ERROR] game dump not found & pause & exit /b 1 )
cd /d "%KG%"
start "" "%KG%\rpcs3.exe" --config "%PROFILE%" "%GAME%"
exit /b 0
