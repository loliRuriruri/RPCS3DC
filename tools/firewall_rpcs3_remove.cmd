@echo off
rem RPCS3 RPCN 넷플레이용 인바운드 규칙 제거 (관리자 권한)
net session >nul 2>&1 || (echo [ERROR] Administrator rights required. & pause & exit /b 1)
powershell -NoProfile -Command "Remove-NetFirewallRule -DisplayName 'RPCS3 (Private)' -ErrorAction SilentlyContinue; Remove-NetFirewallRule -DisplayName 'RPCS3 (Public)' -ErrorAction SilentlyContinue; Write-Host 'removed RPCS3 rules'"
pause
