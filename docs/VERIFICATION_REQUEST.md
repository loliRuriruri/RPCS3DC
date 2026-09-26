# 검증 요청서 — Dragon's Crown PC Edition for RPCS3 (Phase 1)

작성: 2026-09-26 · 검증 대상: 로컬 환경 `E:\PS3` + GitHub 저장소
목적: 본 문서의 **주장(Claim)** 을 **증거(Evidence)** 와 대조 검증하고,
**미검증(Pending)** 항목이 완료로 잘못 표기되지 않았는지 확인한다.

---

## 0. 검증자(GPT)에게 요청하는 것

1. 각 주장에 대해 증거 파일/로그가 실제로 존재하고 주장과 일치하는지 확인
2. **과장 여부** 지적: "검증됨"이라고 표기된 항목 중 근거가 약한 것
3. **안전성 원칙 위반** 여부: 원본 RPCS3/게임 파일/세이브가 변경·삭제되었는지
4. **일관성** 확인: 문서·프로필·런처·로그의 버전/수치가 서로 모순되지 않는지
5. 미검증 항목을 "완료"로 오인할 표현이 있는지 지적

---

## 1. 환경 (실측)

| 항목 | 값 | 확인 방법 |
|---|---|---|
| OS | Windows 11 25H2, Build 26200.9457 (레지스트리 ProductName은 "Windows 10 Pro"로 표기) | `Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion"` |
| CPU | AMD Ryzen 7 5800X (8C/16T) | `Get-CimInstance Win32_Processor` |
| RAM | 32 GB | `Get-CimInstance Win32_PhysicalMemory` |
| GPU | NVIDIA GeForce RTX 5080 (16,303 MiB) | `nvidia-smi` |
| GPU driver | 616.56 | `nvidia-smi --query-gpu=driver_version` |
| 디스플레이 | 3840x2160 @ 60 Hz (배율 150%, VRR 없음) | `Get-CimInstance Win32_VideoController` |
| RPCS3 | v0.0.42-20053-38eba804 Alpha \| master | `E:\PS3\RPCS3\log\RPCS3.log` 첫 줄 |
| PS3 펌웨어 | 04.9300 | `E:\PS3\RPCS3\dev_flash\vsh\etc\version.txt` |
| 게임 | Dragon's Crown (Asia, Zh/Ko) — BCAS20298, APP_VER 01.09 | `dev_hdd0\game\BCAS20298\PARAM.SFO` |
| PPU hash | PPU-bc3ee27f265ee62d1d26ebe2323b69384db5708b | RPCS3.log `PPU executable hash:` |

---

## 2. 산출물과 해시

| 산출물 | 경로 | 크기 | 해시 |
|---|---|---|---|
| GUI 런처 | `E:\PS3\Launcher\DragonCrownProEnhanced.exe` | 63.24 MB | SHA256 `BF1A98EB2E96BA24B66EF094BA22672AD7D1DB407D80507A88ECA33F2FB2C511` |
| 런처 소스 | `E:\PS3\Launcher\src\` (7 파일) + GitHub `launcher/src` | — | — |
| RPCS3 KnownGood | `E:\PS3\RPCS3\KnownGood\` | 874 MB(RPCS3 전체) | `rpcs3.exe` SHA256 원본과 동일 |
| ReShade (설치본) | `C:\ProgramData\ReShade\ReShade64.dll` | 5,581,824 B | 6.8.0.1 / SHA256 `FFCAB1B2…E181` (ZERO-BANNER 자체 빌드) |
| ReShade (공식 백업) | `E:\PS3\Mods_Patches\ReShade\Official_Backup\ReShade64.dll` | 5,592,064 B | 6.8.0.2155 / SHA256 `0CEE63F9…94F7` |
| ZERO-BANNER 빌드 | `…\ZeroBanner\src\bin\x64\Release\ReShade64.dll` | 5,581,824 B | SHA256 `FFCAB1B2…E181` (설치본과 동일) |
| 공유 패키지 | `E:\PS3\Backups\DragonCrown_PRO_Enhanced_Setup.zip` | 58.02 MB | 개인정보/게임파일 미포함 검사 완료 |
| GitHub | https://github.com/loliRuriruri/RPCS3DC | 59 파일 | 최신 커밋 `9ece272` |

---

## 3. 주장 & 증거

### 3.1 원본 보존 / 안전성

| # | 주장 | 증거 | 확인 명령 | 상태 |
|---|---|---|---|---|
| A1 | 원본 RPCS3(`C:\…\rpcs3-v0.0.42-20053-…`)는 변경되지 않았다 | 원본 `config.yml` SHA256 = 백업본과 동일 `684CF554…7829`, 원본 `SAVE0.DAT` = `B6FD91C4…56CF` | `Get-FileHash` 비교 | 검증됨 |
| A2 | 세이브/트로피를 삭제하지 않았다 | `Saves\Dragons_Crown\ORIGINAL`(SHA256 `B6FD91C4…`) + `AUTO_BACKUP` 타임스탬프 백업 3세대 + RPCS3 savedata 원본 유지 | `Get-ChildItem E:\PS3\Saves -Recurse` | 검증됨 |
| A3 | 게임 파일(EBOOT/스크립트)을 수정하지 않았다 | 게임 덤프 폴더를 쓰기 없이 읽기만 사용(런처는 경로만 참조). v1.09는 **공식 PKG 설치**로 `dev_hdd0\game\BCAS20298`에 적용 | `Get-ChildItem <게임폴더>` 타임스탬프 | 검증됨(주장) |
| A4 | `rpcs3.exe` 바이너리를 수정하지 않았다 | RPCS3는 원본 배포본 복제 + 공식 v1.09 PKG 설치만 수행 | `rpcs3.exe` SHA256 = 원본 복제 시 값 `F3C58A95…9C26` | 검증됨 |

### 3.2 게임/버전

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| B1 | TITLE_ID = BCAS20298 | PARAM.SFO + RPCS3.log `Dragon's Crown [BCAS20298]` | 검증됨 |
| B2 | APP_VER = 01.09 (공식 업데이트 적용) | `dev_hdd0\game\BCAS20298\PARAM.SFO` APP_VER=01.09, 로그 `Updates found at /dev_hdd0/game/BCAS20298/` | 검증됨 |
| B3 | 업데이트 PKG 무결성 | Sony titlepatch XML sha1sum과 **No-Intro PSN DB**의 md5/sha1/crc32/크기 일치(47,301,728 B) | 검증됨 |
| B4 | v1.09 업데이트 주의사항을 사용자에게 고지 | 공식 changelog(한국어/중국어) ‘뼈 32→28, 29번째 이후 삭제’ → 문서/보고에 기록 | 검증됨 |

### 3.3 그래픽

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| C1 | Graphics A(Standard) = 4K 300% + AF 16x + VSync Full + VBlank 60 + Frame Skip Off | 프로필 `DC_PRO_4K` 값 실측 | 검증됨 |
| C2 | Standard에서 ReShade가 로드되지 않는다 | `--launch STANDARD` 실행 시 `ReShade.log` 크기 13990 → 13990 (불변) | 검증됨 |
| C3 | Graphics B(Pro Enhanced) = ReShade 프리셋 적용 | `--launch PROENHANCED` 시 ReShade.log에 Deband/CAS/SMAA/**Levels**/**Vibrance** 6종 컴파일 | 검증됨 |
| C4 | 프리셋 전환 시 ReShade.ini PresetPath가 자동 변경 | `--preset Standard|ProEnhanced` 후 `ReShade.ini`의 PresetPath 확인 | 검증됨 |
| C5 | 60fps 유지 | 100/300/400/500/600% 전 구간 59.5~60.1 fps (RPCS3 자체 창 제목 카운터, `Logs\fps_by_scale.csv`) | 검증됨(타이틀 화면) |
| C6 | 400% 내부 5120x2880 | 프로필 scale=400 (RPCS3 tooltip: 기준 1280x720) | 검증됨 |
| C7 | UI 깨짐/글자 가독성 | Stretch Off · 16:9 · MSAA Auto 유지(문서화) | **미검증(육안 필요)** |

### 3.4 디스플레이 / Borderless

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| D1 | RPCS3는 Vulkan Windowed 유지, 게임 창만 Borderless | 런타임 config `Start games in fullscreen mode: false` + Win32 스타일 제어 | 검증됨 |
| D2 | 게임 렌더 창만 정확히 식별 | 제목 `FPS: … \| Vulkan \| …` 패턴, GUI 메인 창/더미 창 제외 | 검증됨 |
| D3 | 모니터 실제 해상도 자동 탐지 | DPI aware로 `\\.\DISPLAY1 3840x2160` (작업영역 3840x2088) | 검증됨 |
| D4 | 적용/복원 | 적용: `3862x2110/0x96CF0000` → `3840x2160/0x96000000`, 복원: 원래 style/rect 복귀 | 검증됨 |
| D5 | Alt+Enter 미사용 | 코드에 키 입력 주입 없음(Win32 `SetWindowLongPtr`/`SetWindowPos`만) | 검증됨 |
| D6 | 마우스 좌표 불일치 방지 | Stretch Off · 16:9 유지 | **미검증(육안 필요)** |

### 3.5 ReShade

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| E1 | 시작 시 ReShade 문구/메뉴 없음 | Silent(`[OVERLAY] TutorialProgress=4`, Show*=0) + ZERO-BANNER 빌드(마커 로그) | 검증됨(로그) / 육안 미검증 |
| E2 | Home 키로 메뉴 호출 | `KeyOverlay=36,0,0,0` (VK_HOME) | 검증됨(설정) / 육안 미검증 |
| E3 | ZERO-BANNER는 공식 소스 최소 패치 | upstream v6.8.0 `18deaa52…` + `runtime_gui.cpp` 1파일 패치, 패치 SHA256 `2F9ED1A8…` | 검증됨 |
| E4 | 공식 ReShade로 1회 원복 가능 | `ReShade_Switch_Official.cmd`(관리자) | 검증됨(스크립트 존재) |
| E5 | 과도한 효과 미사용 | 프리셋에 Bloom/MXAO/RTGI/DOF/MotionBlur/CA/FilmGrain 없음 | 검증됨 |

### 3.6 멀티플레이

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| F1 | Local/Remote/RPCN 3종 메뉴 제공 | 런처 UI + `Docs\MULTIPLAYER.md` | 검증됨(구현) |
| F2 | Local 2P 동작 | 2P 핸들러 진단만 구현 | **미검증 — 2P 패드 없음(진단 결과 Null)** |
| F3 | Remote Co-op(Sunshine+Moonlight) | Sunshine 상태 진단 + 절차 문서 | **미검증 — Sunshine 미설치** |
| F4 | RPCN 계정/설정 | 계정 미설정(진단 결과 미설정), 프로필/Ready 검사 구현 | **미검증 — 계정 필요** |
| F5 | Netplay Safe 프로필 | `NETPLAY_SAFE` 존재(보수값 고정) | 검증됨(파일) |
| F6 | Private RPCN 서버/자체 매치메이킹 미구축 | 요구사항대로 미구현 | 검증됨(부재) |
| F7 | RPCN 실제 매치 | — | **미검증 — 2대 PC 필요** |

### 3.7 치트/세이브

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| G1 | RPCS3 native Cheat Manager 존재 확인 | exe 문자열/소스(`rpcs3qt/cheat_manager.cpp`) | 검증됨 |
| G2 | CHEAT OFFLINE 실행 시 세이브 자동 백업 | `--launch CHEAT` 시 `Saves\…\AutoBackup\DC_SAVE_<시각>_cheat_offline` + SHA256 manifest | 검증됨 |
| G3 | Gold/Skill Point 주소 검증 | — | **미검증 — 게임 내 값 변화 필요** |
| G4 | 외부 치트 import 검증기 | `Tools\validate_cheat_import.py` 양성 PASS/Artemis 파일 REJECTED 실측 | 검증됨 |
| G5 | Artemis 자료 미적용 | BLUS30767 v1.00(PPU 60f7a7f5…) → 현재와 불일치로 거부 | 검증됨 |

### 3.8 진단/백업/복구

| # | 주장 | 증거 | 상태 |
|---|---|---|---|
| H1 | Diagnostics 전체 항목(런타임 카운트, Closeout 시점 35) + 원인 안내 | `Logs\diagnostics.txt` (예: Controller 2 Null → 2P 필요, Sunshine 미설치 → 설치 안내) | 검증됨 |
| H2 | 설정 백업/복원 | `Backups\<시각>_<라벨>` (config·custom config·ReShade.ini·CurrentSettings·프로필·입력설정), 복원 전 재백업 | 검증됨 |
| H3 | Reset Graphics/Multiplayer | AF16·VSync Full·VBlank60·FrameSkip Off / RPCN On·UPNP Off·Clans Off 복원 실측 | 검증됨 |
| H4 | 세이브는 Reset/복원 대상이 아님 | 코드상 savedata 경로 접근 없음(백업 기능만) | 검증됨(코드) |

---

## 4. 재현 명령 (검증자용)

```powershell
# 1) 런처 해시
Get-FileHash "E:\PS3\Launcher\DragonCrownProEnhanced.exe" -Algorithm SHA256

# 2) 게임 버전 / PPU 해시 / 빌드
Get-Content "E:\PS3\RPCS3\log\RPCS3.log" | Select-String 'RPCS3 v0','PPU executable hash','Updates found'

# 3) 프로필 값 일괄 확인
Get-ChildItem "E:\PS3\Profiles\Dragons_Crown" -Directory | ForEach-Object {
  $p = Join-Path $_.FullName 'config.yml'
  if (Test-Path $p) { "--- $($_.Name) ---"; Select-String -Path $p -Pattern '^  (Resolution Scale|Anisotropic Filter Override|VSync Mode|Vblank Rate|Enable Frame Skip|PSN status|Stretch To Display Area):' | ForEach-Object { $_.Line.Trim() } }
}

# 4) 진단 (전체 항목 — 런타임 카운트)
& "E:\PS3\Launcher\DragonCrownProEnhanced.exe" --diag "E:\PS3\Logs\diag_recheck.txt"; Start-Sleep 10; Get-Content "E:\PS3\Logs\diag_recheck.txt"

# 5) Standard 실행(ReShade OFF 확인) — ReShade.log 크기 불변
(Get-Item "E:\PS3\RPCS3\ReShade.log").Length
& "E:\PS3\Launcher\DragonCrownProEnhanced.exe" --launch STANDARD --no-wait; Start-Sleep 35
(Get-Item "E:\PS3\RPCS3\ReShade.log").Length

# 6) Borderless 실측 (게임 실행 중)
powershell -File "E:\PS3\Tools\inspect_windows.ps1"   # rect=3840x2160, style 0x96000000 확인

# 7) 원본 무변경
(Get-FileHash "C:\Users\<user>\Downloads\rpcs3-v0.0.42-20053-38eba804_win64_msvc\config\config.yml").Hash
```

---

## 5. 미검증 / 한계 (완료로 간주하면 안 되는 항목)

| 항목 | 사유 | 필요한 것 |
|---|---|---|
| PRO MAX(400%) vs PRO(300%) 시각 A/B | 캡처 도구 없음(자동 세션) | 사용자 F12 캡처 비교 |
| 30분 연속 플레이 | 타이틀 방치 시 게임이 스스로 종료(`_sys_process_exit`) | 실제 플레이 |
| ReShade 문구 없음/Home 메뉴 (육안) | 화면 확인 불가 | 사용자 육안 |
| UI 깨짐·글자 가독성·마우스 좌표 | 육안/조작 필요 | 사용자 확인 |
| Local 2P / Remote Co-op / RPCN 매치 | 2P 패드·Sunshine·RPCN 계정·2대 PC 없음 | 사용자 준비 |
| Gold/Skill Point 주소 | 게임 내 값 변화 필요 | 플레이 + Cheat Manager |
| black box/alpha 회귀 | 시각 판정 필요 | baseline 캡처 |
| 게임 내 한국어 표시 | 게임 리소스 의존(GameTDB는 ZHTW 표기) | 실플레이 확인 |

---

## 6. 스스로 공개하는 잠재 약점 (검증자 지적 대상)

1. **성능 수치는 타이틀 화면 기준**이다. 전투/보스 등 부하 장면 수치는 미측정.
2. **`DC_RPCN_NETPLAY` 프로필은 이전 단계 산출물**로, Phase 1의 `DC_NETPLAY`/`NETPLAY_SAFE`와 값이 다르다(AF Auto·VSync Disabled). → 런처는 Phase 1 프로필만 사용하며, 문서상 구 프로필은 legacy 로 취급해야 한다.
3. **ReShade ZERO-BANNER 빌드는 로컬 빌드(6.8.0.1 UNOFFICIAL)** 로, 공식 6.8.0.2155와 버전 문자열이 다르다(의도적 식별 목적). 기능 동등성은 이펙트 컴파일/훅 로그로만 확인했다.
4. **Borderless는 창모드 변형**이므로 독점 전체화면과 입력 지연/전체화면 최적화 동작이 다를 수 있다(60 Hz 패널).
5. **`Launcher` 폴더에 빌드 중간 산출물**(`src\bin`, `src\obj`)이 남아 있어 폴더 크기가 227 MB이다(재빌드용, 삭제 가능).
6. **공유 패키지(58 MB)는 로컬 검증만** 했고 실제 타 PC 설치 테스트는 하지 않았다.
7. **RPCN 은 `RPCN_PARTIAL` 로 분류한다**(연결 기능 존재 · 친구 기반 로비/합류 사례 · 실제 Match 부분 지원 · Random matchmaking 신뢰 안 함 · Connecting 멈춤 사례). “RPCN Guaranteed” 로 표기하지 않으며, 커뮤니티 접속 실패 보고(#15283)가 있다 → “동작 보장”이 아니라 “가드/진단 구현”으로 표기해야 한다.
8. **치트는 주소 미확보 상태**이므로 “Cheat 기능 완성”이 아니라 “도구/절차 완성”이다.

---

## 7. 출처

* RPCS3: https://rpcs3.net/download · https://rpcs3.net/compatibility · https://github.com/RPCS3/rpcs3 (v0.0.42)
* RPCS3 Wiki: Dragon's Crown / Default Settings / RPCN Compatibility List
* Issues: #17200 · #17502 · #4133 · #9150 · #15283
* ReShade: https://reshade.me · https://github.com/crosire/reshade (tag v6.8.0 = `18deaa52…`)
* Sony 공식 업데이트 서버(titlepatch XML / PKG), No-Intro PSN 업데이트 DB(RPCS3 공식 API)
* 프로젝트 저장소: https://github.com/loliRuriruri/RPCS3DC

---

## 8. Closeout 패치 검증 항목 (2026-09-26 추가)

Phase 1 Closeout 에서 수정된 P0/P1 항목의 검증 포인트:

| # | 항목 | 증거 | 상태 |
|---|---|---|---|
| I1 | 해상도 선택(4K/5K)이 실제 프로필에 연결 | `settings.json` `ResolutionProfile`, 런타임 config `Dragons_Crown_DC_PRO_MAX_5K__Borderless4K.yml` (Scale 400) | 검증됨 |
| I2 | 5K 400% 부팅 | 창 제목 `FPS: 59.83~60.02 … [BCAS20298]` | 검증됨 |
| I3 | Netplay Safe ReShade runtime 강제 차단 | 사용자 설정 `ReShade=1` 상태에서 Safe 실행 → `ReShade.log` 크기 불변 | 검증됨 |
| I4 | Safe 후 사용자 설정 보존 | `settings.json` 이 `GraphicsPreset=ProEnhanced, ResolutionProfile=5K` 유지 | 검증됨 |
| I5 | v1.09 엄격 검사 | `Game version (BCAS20298 v01.09)` 진단 항목 + RPCN 차단 로직 | 검증됨(코드/진단) |
| I6 | RPCN 사전 검사 차단 | 미설정 상태에서 `--launch RPCNSAFE` → `RPCN account is not configured` 로그, RPCS3 미실행 | 검증됨 |
| I7 | `--restore-window` 즉시 복원 + 유지 | 로그 `cli --restore-window (immediate restore + stop flag)`, 창 `0x96CF0000 / 3862x2186` 8초 유지, `stop requested -> restoring and exiting watcher` | 검증됨 |
| I8 | Borderless launchPid 타겟팅 | 로그 `borderless: target pid=<launched pid>` | 검증됨 |
| I9 | 진단 실제값/구분 | `Logs\diag_closeout_4k.txt`(35항목), 치트/패치 존재↔활성 분리, RPCN Configured↔Login 분리 | 검증됨 |
| I10 | Backup/Restore/Reset/CHEAT/KnownGood | `Backups\20260926_163948_closeout_test`, `Saves\...\DC_SAVE_..._cheat_offline`, KnownGood PID 기동 | 검증됨 |

주의: I1~I10 은 **자동 검증**이다. 30분 플레이·Local 2P·Remote Co-op·RPCN 2-PC 는
`Docs\ACCEPTANCE_TEST.md` 의 사용자 Acceptance 로 남는다 (PENDING).
