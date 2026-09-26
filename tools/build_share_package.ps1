<#
DragonCrown_PRO_Enhanced_Setup.zip 빌더 (공유용)
포함: 런처 exe, 프로필 템플릿, ReShade 프리셋, 문서, 설치 가이드
제외: 게임 파일, PS3 펌웨어, DLC/라이선스, rpcn.yml, 계정/토큰, savedata, trophy, 스크린샷, 로그, 개인 config
#>
param([string]$Out = "E:\PS3\Backups\DragonCrown_PRO_Enhanced_Setup.zip")
$ErrorActionPreference = "Stop"
$root = "E:\PS3"
$stage = Join-Path $env:TEMP ("dcpro_pkg_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
New-Item -ItemType Directory -Path $stage -Force | Out-Null

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

# 1) 런처
Copy-Safe "$root\Launcher\DragonCrownProEnhanced.exe" "Launcher\DragonCrownProEnhanced.exe"
Copy-Safe "$root\Launcher\Dragon_Crown_Launcher.cmd" "Launcher\_Internal\Dragon_Crown_Launcher.cmd"

# 2) 프로필 템플릿 (4개) - 개인 식별자/경로 살균 후 포함
foreach ($p in 'DC_PRO_4K','DC_PRO_MAX_5K','DC_NETPLAY','DC_CHEAT_OFFLINE') {
    $srcCfg = "$root\Profiles\Dragons_Crown\$p\config.yml"
    if (-not (Test-Path -LiteralPath $srcCfg)) { continue }
    $dstCfg = Join-Path $stage "Profiles\Dragons_Crown\$p\config.yml"
    New-Item -ItemType Directory -Path (Split-Path $dstCfg -Parent) -Force | Out-Null
    $txt = [System.IO.File]::ReadAllText($srcCfg, [System.Text.Encoding]::UTF8)
    $txt = [regex]::Replace($txt, '(?m)^(\s*Console PSID:\s*).*$', '${1}0x00000000000000000000000000000000')
    $txt = $txt -replace [regex]::Escape($env:USERNAME), '<user>'
    $txt = [regex]::Replace($txt, '(?m)^(\s*System Name:\s*).*$', '${1}RPCS3-PRO-ENHANCED')
    [System.IO.File]::WriteAllText($dstCfg, $txt, [System.Text.UTF8Encoding]::new($false))
}

# 3) ReShade 프리셋/스크립트 (바이너리는 제외)
Copy-Safe "$root\ReShade\Presets" "ReShade\Presets"
Copy-Safe "$root\Mods_Patches\ReShade\ReShade_Switch_Official.cmd" "ReShade\ReShade_Switch_Official.cmd"
Copy-Safe "$root\Mods_Patches\ReShade\ReShade_Switch_ZeroBanner.cmd" "ReShade\ReShade_Switch_ZeroBanner.cmd"

# 4) 도구/문서
Copy-Safe "$root\Tools\launch_with_mode.ps1" "Tools\launch_with_mode.ps1"
Copy-Safe "$root\Tools\borderless_4k.ps1" "Tools\borderless_4k.ps1"
Copy-Safe "$root\Tools\save_backup.ps1" "Tools\save_backup.ps1"
Copy-Safe "$root\Tools\validate_cheat_import.py" "Tools\validate_cheat_import.py"
Copy-Safe "$root\Docs" "Docs"

# 5) 설치 가이드
@"
Dragon's Crown PRO Enhanced for RPCS3 - 설치 가이드
==================================================

1. 이 패키지를 원하는 위치에 풀어주세요. 예: D:\PS3
   (Launcher 폴더가 <ROOT>\Launcher 가 되도록)

2. RPCS3 설치
   - https://rpcs3.net/download 에서 최신 Windows build 를 받아 <ROOT>\RPCS3 에 풀어주세요.
   - RPCS3 를 실행해 PS3 firmware(4.93 권장)와 자신의 Dragon's Crown 덤프를 등록하세요.
   - 게임 업데이트가 있으면 적용하세요(권장 v1.09).

3. Launcher 실행
   - <ROOT>\Launcher\DragonCrownProEnhanced.exe
   - ROOT 자동 탐지가 실패하면 SETTINGS -> ROOT 경로 다시 지정 에서 rpcs3.exe 를 선택하세요.

4. RPCN (넷플레이를 쓸 경우)
   - RPCS3 -> RPCN -> Create Account 로 자신의 계정을 만들고 로그인하세요.
   - 계정 정보는 RPCS3 가 관리하며 이 패키지에는 포함되지 않습니다.

5. ReShade (선택)
   - https://reshade.me 에서 ReShade Setup(6.8.0 Addon) 을 받아 rpcs3.exe 대상 Vulkan 으로 설치하세요.
   - 프리셋은 ReShade\Presets 를 사용하세요.

6. 실행
   - PLAY (4K 300%, Borderless, VSync, ReShade Silent)
   - PRO MAX (5K 400%) / NETPLAY (RPCN) / CHEAT OFFLINE (치트, 오프라인 전용)

주의
- 이 패키지에는 게임 파일, PS3 펌웨어, DLC/라이선스, RPCN 계정, 세이브, 스크린샷, 로그가 포함되지 않습니다.
- 세이브/트로피는 사용자의 기존 RPCS3 데이터를 그대로 사용합니다.
"@ | Set-Content -LiteralPath (Join-Path $stage "설치_가이드.txt") -Encoding utf8

if (Test-Path $Out) { Remove-Item $Out -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $Out -CompressionLevel Optimal
Remove-Item $stage -Recurse -Force
"패키지 생성: $Out ($([Math]::Round((Get-Item $Out).Length/1MB,2)) MB)"
