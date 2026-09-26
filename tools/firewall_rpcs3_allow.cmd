@echo off
rem ============================================================
rem  RPCS3 RPCN 넷플레이용 인바운드 방화벽 규칙 추가/확인 (관리자 필요)
rem  경로는 이 스크립트 위치(<ROOT>\Tools)를 기준으로 자동 계산됩니다.
rem ============================================================
net session >nul 2>&1 || (echo [ERROR] Administrator rights required. & pause & exit /b 1)
set "RPCS3EXE=%~dp0..\RPCS3\rpcs3.exe"
if not exist "%RPCS3EXE%" (echo [ERROR] rpcs3.exe not found: "%RPCS3EXE%" & pause & exit /b 1)
echo RPCS3: "%RPCS3EXE%"
powershell -NoProfile -Command "foreach ($p in 'Private','Public') { if (-not (Get-NetFirewallRule -DisplayName \"RPCS3 ($p)\" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -DisplayName \"RPCS3 ($p)\" -Direction Inbound -Program $env:RPCS3EXE -Protocol Any -Profile $p -Action Allow -Description 'RPCS3 RPCN netplay (Host)' | Out-Null; Write-Host ('added   RPCS3 (' + $p + ')') } else { Write-Host ('exists  RPCS3 (' + $p + ')') } }; Get-NetFirewallRule -DisplayName 'RPCS3 (*)' | Select-Object DisplayName,Enabled,Profile"
pause
