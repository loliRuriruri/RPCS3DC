@echo off
rem RPCS3 RPCN 넷플레이용 인바운드 허용 규칙 추가/확인 (관리자 권한)
net session >nul 2>&1 || (echo [ERROR] Administrator rights required. & pause & exit /b 1)
powershell -NoProfile -Command "foreach ($p in 'Private','Public') { if (-not (Get-NetFirewallRule -DisplayName \"RPCS3 ($p)\" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -DisplayName \"RPCS3 ($p)\" -Direction Inbound -Program 'E:\PS3\RPCS3\rpcs3.exe' -Protocol Any -Profile $p -Action Allow -Description 'RPCS3 RPCN netplay (Host)' | Out-Null } }; Get-NetFirewallRule -DisplayName 'RPCS3 (*)' | Select-Object DisplayName,Enabled,Profile"
pause
