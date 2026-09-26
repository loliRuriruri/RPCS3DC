@echo off
rem Copy current save data into the KnownGood snapshot (backup first).
setlocal
set "SRC=E:\PS3\RPCS3\dev_hdd0\home\00000001"
set "DST=E:\PS3\RPCS3\KnownGood\dev_hdd0\home\00000001"
echo Syncing savedata + trophy from Current to KnownGood ...
if not exist "%DST%" ( echo [ERROR] KnownGood dev_hdd0 missing & pause & exit /b 1 )
robocopy "%SRC%\savedata" "%DST%\savedata" /E /COPY:DAT /R:1 /W:1
robocopy "%SRC%\trophy"   "%DST%\trophy"   /E /COPY:DAT /R:1 /W:1
echo Done. (Current install is unchanged.)
pause
