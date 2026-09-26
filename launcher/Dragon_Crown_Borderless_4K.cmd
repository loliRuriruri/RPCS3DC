@echo off
rem ============================================================
rem  Dragon's Crown - Borderless 4K (기본 디스플레이 모드)
rem  RPCS3 는 Vulkan Windowed 로 실행되고, 게임 렌더 창만
rem  외부 Win32 스타일 제어로 테두리 없이 모니터 전체(4K)가 됩니다.
rem  Alt+Enter 를 보내지 않으며, 게임 종료 시 원래 창 스타일로 복원됩니다.
rem  ReShade / RPCN / 컨트롤러 / Resolution Scale 은 변경되지 않습니다.
rem ============================================================
setlocal
set "PROFILE=%~1"
if "%PROFILE%"=="" set "PROFILE=DC_4K_ULTRA"
set "MODE=Borderless4K"
if /I "%~2"=="Fullscreen" set "MODE=Fullscreen"
if /I "%~2"=="Windowed"   set "MODE=Windowed"
echo [Dragon's Crown] profile=%PROFILE%  mode=%MODE%
powershell -NoProfile -ExecutionPolicy Bypass -File "E:\PS3\Tools\launch_with_mode.ps1" -Profile "%PROFILE%" -Mode "%MODE%"
exit /b 0
