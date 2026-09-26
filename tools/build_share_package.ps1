<#
DragonCrown PRO Enhanced — 공유 패키지 빌더 (Phase 1 RC, 휴대용)

포함: 런처 exe, 프로필 5종, ReShade 프리셋/셰이더/ZERO-BANNER(선택), 도구(방화벽·보더리스·세이브 백업), 문서, 설치 가이드
제외: 게임 파일, PS3 펌웨어, 업데이트 PKG, DLC/라이선스, rpcn.yml, 계정/토큰, savedata, trophy,
      스크린샷, 로그, 개인 config(PSID 스크럽), 개인 치트 세이브

사용: powershell -File build_share_package.ps1 [-Out <zip 경로>]
#>
param([string]$Out = "")
$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrEmpty($Out)) { $Out = Join-Path $root "Backups\DragonCrown_PRO_Enhanced_Setup.zip" }

$stage = Join-Path $env:TEMP ("dcpro_pkg_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
"ROOT  : $root"
"STAGE : $stage"

function Copy-Safe([string]$src, [string]$rel) {
    if (-not (Test-Path -LiteralPath $src)) { return }
    $dst = Join-Path $stage $rel
    New-Item -ItemType Directory -Path (Split-Path $dst -Parent) -Force | Out-Null
    if ((Get-Item -LiteralPath $src).PSIsContainer) {
        robocopy $src $dst /E /COPY:DAT /R:1 /W:1 /NFL /NDL /NP /XF "rpcn.yml" "config.yml" "CurrentSettings.ini" "*.log" "*.png" "*.jpg" "*.dat" | Out-Null
    } else {
        Copy-Item -LiteralPath $src $dst -Force
    }
}

function Scrub-Text([string]$path) {
    try {
        $t = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
        $orig = $t
        $t = $t -replace [regex]::Escape($env:USERNAME), '<user>'
        $t = $t -replace '<user>', '<user>'
        $t = [regex]::Replace($t, '0x[A-Fa-f0-9]{32}', '0x00000000000000000000000000000000')
        $t = $t -replace '\{587e3323-3e1e-49d5-b277-0b433f41398d\}', '{00000000-0000-0000-0000-000000000000}'
        if ($t -ne $orig) { [System.IO.File]::WriteAllText($path, $t, [System.Text.UTF8Encoding]::new($false)) }
    } catch { }
}

# ---------------------------------------------------------------- 1) 런처
Copy-Safe "$root\Launcher\DragonCrownProEnhanced.exe" "Launcher\DragonCrownProEnhanced.exe"

# ---------------------------------------------------------------- 2) 프로필 5종 (PSID 스크럽)
foreach ($p in 'DC_PRO_4K','DC_PRO_MAX_5K','DC_NETPLAY','NETPLAY_SAFE','DC_CHEAT_OFFLINE') {
    $srcCfg = "$root\Profiles\Dragons_Crown\$p\config.yml"
    if (-not (Test-Path -LiteralPath $srcCfg)) { continue }
    $dstCfg = Join-Path $stage "Profiles\Dragons_Crown\$p\config.yml"
    New-Item -ItemType Directory -Path (Split-Path $dstCfg -Parent) -Force | Out-Null
    $txt = [System.IO.File]::ReadAllText($srcCfg, [System.Text.Encoding]::UTF8)
    $txt = [regex]::Replace($txt, '(?m)^(\s*Console PSID:\s*).*$', '${1}0x00000000000000000000000000000000')
    $txt = $txt -replace [regex]::Escape($env:USERNAME), '<user>'
    $txt = [regex]::Replace($txt, '(?m)^(\s*System Name:\s*).*$', '${1}RPCS3-PC-EDITION')
    [System.IO.File]::WriteAllText($dstCfg, $txt, [System.Text.UTF8Encoding]::new($false))
}

# ---------------------------------------------------------------- 3) ReShade (프리셋 + 셰이더 + 선택적 ZERO-BANNER)
Copy-Safe "$root\ReShade\Presets" "ReShade\Presets"
Copy-Safe "$root\Mods_Patches\ReShade\Shaders" "ReShade\Shaders"
Copy-Safe "$root\Mods_Patches\ReShade\ZeroBanner\src\bin\x64\Release\ReShade64.dll" "ReShade\ZeroBanner\ReShade64.dll"

@"
@echo off
rem ============================================================
rem  ZERO-BANNER ReShade64.dll 설치 (ReShade 6.8.0 자체 빌드 / 시작 배너 제거)
rem  사전 조건: reshade.me 에서 ReShade 6.8.0 Addon(Vulkan)을 rpcs3.exe 대상 설치
rem  관리자 권한으로 실행하세요.
rem ============================================================
net session >nul 2>&1 || (echo [ERROR] Run as administrator. & pause & exit /b 1)
set "SRC=%~dp0ZeroBanner\ReShade64.dll"
set "DST=C:\ProgramData\ReShade\ReShade64.dll"
set "BAK=%~dp0Official_Backup\ReShade64.dll"
if not exist "%SRC%" (echo [ERROR] missing "%SRC%" & pause & exit /b 1)
if not exist "%DST%" (echo [ERROR] ReShade not installed. Install ReShade 6.8.0 (Vulkan, rpcs3.exe) first. & pause & exit /b 1)
if not exist "%BAK%" (copy /Y "%DST%" "%BAK%" >nul & echo [1/3] official DLL backed up)
copy /Y "%SRC%" "%DST%" >nul
echo [2/3] ZERO-BANNER installed
powershell -NoProfile -Command "$h=(Get-FileHash $env:DST -Algorithm SHA256).Hash; Write-Host ('  SHA256=' + $h)"
echo [3/3] done. Rollback: Restore_Official.cmd
pause
"@ | Set-Content -LiteralPath (Join-Path $stage "ReShade\Install_ZeroBanner.cmd") -Encoding ascii

@"
@echo off
rem 공식 ReShade DLL 로 되돌립니다 (관리자 필요)
net session >nul 2>&1 || (echo [ERROR] Run as administrator. & pause & exit /b 1)
set "BAK=%~dp0Official_Backup\ReShade64.dll"
set "DST=C:\ProgramData\ReShade\ReShade64.dll"
if not exist "%BAK%" (echo [ERROR] no backup: "%BAK%" & pause & exit /b 1)
copy /Y "%BAK%" "%DST%" >nul
echo official ReShade restored.
pause
"@ | Set-Content -LiteralPath (Join-Path $stage "ReShade\Restore_Official.cmd") -Encoding ascii

@"
ReShade 사용 안내 (Dragon's Crown PC Edition)
=============================================

1. 기본은 'Standard' 그래픽이며 ReShade 를 사용하지 않습니다.
   'Pro Enhanced' 를 쓰려면 ReShade 설치가 필요합니다.

2. 설치 순서
   a) https://reshade.me 에서 ReShade Setup 6.8.0 (Addon) 다운로드
   b) 설치 대상: <ROOT>\RPCS3\rpcs3.exe / 렌더러: Vulkan 선택
   c) (선택) 시작 배너까지 없애려면: ReShade\Install_ZeroBanner.cmd 를 관리자로 실행
      - ZERO-BANNER 는 ReShade 6.8.0 소스를 1줄 수정한 자체 빌드입니다.
      - 되돌리기: ReShade\Restore_Official.cmd

3. 런처가 자동으로 처리하는 것
   - ReShade.ini 의 EffectSearchPaths / TextureSearchPaths / PresetPath 를 이 폴더 기준으로 수정
   - Silent 모드([OVERLAY]) 적용: 시작 문구/튜토리얼/스크린샷 메시지 숨김, Home 키로 메뉴 호출

4. 셰이더 출처
   - ReShade\Shaders 는 공식 reshade-shaders 저장소(MIT)에서 가져온 파일입니다.

5. ZERO-BANNER 바이너리 고지
   - ReShade (c) 2015 Patrick Mours, BSD 3-Clause License.
   - 본 빌드는 ReShade v6.8.0 (commit 18deaa52) 소스에 시작 배너 비활성화 1줄 패치를 적용한 것입니다.
   - 라이선스 전문: ReShade\ZERO_BANNER_LICENSE.txt
"@ | Set-Content -LiteralPath (Join-Path $stage "ReShade\README.txt") -Encoding utf8

@"
ReShade BSD 3-Clause License (적용 대상: ReShade\ZeroBanner\ReShade64.dll)
==========================================================================
Copyright (c) 2015 Patrick Mours. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

 1. Redistributions of source code must retain the above copyright notice, this
    list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
 3. Neither the name of the copyright holder nor the names of its contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
"@ | Set-Content -LiteralPath (Join-Path $stage "ReShade\ZERO_BANNER_LICENSE.txt") -Encoding utf8

# ---------------------------------------------------------------- 4) 도구 (이식 가능 경로)
Copy-Safe "$root\Tools\firewall_rpcs3_allow.cmd" "Tools\firewall_rpcs3_allow.cmd"
Copy-Safe "$root\Tools\firewall_rpcs3_remove.cmd" "Tools\firewall_rpcs3_remove.cmd"
Copy-Safe "$root\Tools\borderless_4k.ps1" "Tools\borderless_4k.ps1"
Copy-Safe "$root\Tools\launch_with_mode.ps1" "Tools\launch_with_mode.ps1"
Copy-Safe "$root\Tools\save_backup.ps1" "Tools\save_backup.ps1"
Copy-Safe "$root\Tools\validate_cheat_import.py" "Tools\validate_cheat_import.py"

# ---------------------------------------------------------------- 5) 문서
Copy-Safe "$root\Docs" "Docs"

# ---------------------------------------------------------------- 6) 설치 가이드
@"
Dragon's Crown PC Edition (Phase 1 RC) — 설치 가이드
=====================================================

[이 패키지에 포함된 것]
  Launcher\DragonCrownProEnhanced.exe   GUI 런처 (.NET 8 self-contained)
  Profiles\Dragons_Crown\*              4K 300% / 5K 400% / NETPLAY / NETPLAY_SAFE / CHEAT
  ReShade\                              프리셋 · 셰이더 · ZERO-BANNER(선택 설치) · 안내
  Tools\                                방화벽 · 보더리스 · 세이브 백업 · 치트 검증
  Docs\                                 문서 일체 (Acceptance/멀티플레이/문제해결 등)

[이 패키지에 없는 것 — 각자 직접 준비]
  · 게임 덤프 (본인 소유분)         — 저작권 보호를 위해 포함하지 않습니다.
  · PS3 펌웨어 (본인 설치)          — Sony 저작물, 포함하지 않습니다.
  · 업데이트 PKG (v1.09 권장)       — RPCS3 안에서 본인이 다운로드합니다.
  · 세이브 / 트로피 / RPCN 계정     — 개인 데이터, 포함하지 않습니다.

[설치 순서]
  1. 압축을 원하는 폴더에 풉니다. 예: D:\PS3
     (Launcher 폴더가 <ROOT>\Launcher 위치가 되도록)
  2. RPCS3 설치
     - https://rpcs3.net/download → 최신 Windows build 를 <ROOT>\RPCS3 에 풉니다.
     - 검증 기준 빌드: v0.0.42-20053 (다른 빌드도 대체로 동작하나 재검증 필요)
  3. RPCS3 실행 → PS3 firmware(4.93 권장) 설치 → 본인 게임 덤프 등록
     → 게임 우클릭 → Check for updates (v1.09 권장)
  4. Launcher 실행: <ROOT>\Launcher\DragonCrownProEnhanced.exe
     - 게임 경로/루트는 자동 탐지합니다 (SETTINGS → Advanced 에서 확인 가능)
  5. (선택) ReShade: reshade.me 에서 6.8.0 Addon(Vulkan) 을 rpcs3.exe 대상 설치
     → ReShade\Install_ZeroBanner.cmd (관리자) 로 시작 배너 제거
     - 런처가 ReShade 경로/프리셋/Silent 설정을 자동으로 맞춥니다.
  6. 컨트롤러: RPCS3 GUI → 게임패드 설정에서 1P/2P 지정 (로컬 2인은 2P 필요)
  7. PLAY: SETTINGS → Graphics 에서 Standard / Pro Enhanced + 4K 300% / 5K 400% 선택 → PLAY

[멀티플레이]
  · Local        : 같은 PC 2인 (2P 패드 필요)
  · Remote Co-op : Sunshine + Moonlight — 친구는 게임/펌웨어가 필요 없습니다 (Moonlight 클라이언트만).
  · RPCN Online  : 각자 계정 필요 (RPCS3 → RPCN → Create Account).
                   첫 테스트는 Standard + Netplay Safe(기본 ON) + 유선 LAN + VPN OFF 권장.
                   성공 판정: 같은 stage 입장 + 동시 조작 + 최소 1회 전투

[주의]
  · RPCS3 자동 업데이트를 끄려면: RPCS3 GUI 설정에서 "Check for updates on startup" 해제.
    (이 패키지의 검증 기준은 v0.0.42-20053 입니다)
  · 세이브/트로피는 이 패키지에 없습니다. 각자 자신의 RPCS3 데이터를 사용합니다.
  · 상세 검증 항목: Docs\ACCEPTANCE_TEST.md
"@ | Set-Content -LiteralPath (Join-Path $stage "설치_가이드.txt") -Encoding utf8

# ---------------------------------------------------------------- 7) 텍스트 스크럽
Get-ChildItem $stage -Recurse -File | Where-Object { $_.Extension -match '^\.(md|txt|ps1|cmd|yml|py|json|ini)$' } | ForEach-Object { Scrub-Text $_.FullName }

# ---------------------------------------------------------------- 8) 제외 감사
$bad = Get-ChildItem $stage -Recurse -File | Where-Object {
    $_.Name -match '^(EBOOT\.BIN|rpcn\.yml|SAVE.*\.DAT|PARAM\.SFO|PARAM\.SFY|.*\.pkg|.*\.pup|.*\.pdb|CurrentSettings\.ini)$' -or
    $_.Extension -match '^\.(png|jpg|log|dat)$'
}
if ($bad) {
    "!! 제외 대상 파일 발견:"
    $bad | ForEach-Object { "   " + $_.FullName.Replace($stage, '') }
    throw "package audit failed"
}
$leak = Get-ChildItem $stage -Recurse -File | Where-Object { $_.Extension -match '^\.(md|txt|ps1|cmd|yml|py|json|ini)$' } |
    Select-String -Pattern '<user>|0x(?!0{32})[A-Fa-f0-9]{32}|587e3323' -ErrorAction SilentlyContinue
if ($leak) {
    "!! 개인정보 잔여 발견:"
    $leak | Select-Object -First 10 | ForEach-Object { "   $($_.Filename):$($_.LineNumber)" }
    throw "privacy audit failed"
}
"감사 통과: 게임/펌웨어/세이브/계정/개인정보 없음"

# ---------------------------------------------------------------- 9) ZIP
if (Test-Path $Out) { Remove-Item $Out -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $Out -CompressionLevel Optimal
Remove-Item $stage -Recurse -Force
"패키지 생성: $Out ($([Math]::Round((Get-Item $Out).Length/1MB,2)) MB)"
