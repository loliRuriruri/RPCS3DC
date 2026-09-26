# RPCS3 SETUP REPORT — E:\PS3 구축

작성일: 2026-09-25
작업 대상: RPCS3 v0.0.42-20053-38eba804 (Windows, portable)
원칙: 원본 C:\ 설치본은 **읽기만** 했고 변경하지 않았습니다.

---

## 1. 원본 / 대상

| 항목 | 값 |
|---|---|
| SOURCE RPCS3 | `C:\Users\<user>\Downloads\rpcs3-v0.0.42-20053-38eba804_win64_msvc` (원본 유지, 무변경) |
| TARGET RPCS3 | `E:\PS3\RPCS3` |
| 복제 방식 | robocopy /E /COPY:DAT /DCOPY:DAT (파일 1,740개, 396,397,434 바이트, 3초) |
| 무결성 검증 | rpcs3.exe SHA256 원본=대상 동일 `F3C58A95…489C26`, 파일 수/바이트 수 완전 일치 |
| 배치 형태 | portable (모든 데이터가 설치 폴더 내부) — rpcs3.exe + config/ + dev_flash/ + dev_hdd0/ + qt6/ 등 |

## 2. 현재 RPCS3 조사 결과 (실제 실행 로그로 확인)

```
RPCS3 v0.0.42-20053-38eba804 Alpha | master      ← E:\PS3\RPCS3\log\RPCS3.log (실행 중 기록)
commit 38eba804f4d2c68e3564d3b78d9d5487f94f4d72 (author 2026-09-24T21:35Z / commit 2026-09-25T05:37Z)
Qt version: Compiled against Qt 6.11.2 | Run-time uses Qt 6.11.2
Architecture: x64
Operating system: Windows, Major: 10, Minor: 0, Build: 26200, Service Pack: none
Vulkan-compatible GPU: 'NVIDIA GeForce RTX 5080' running on driver 616.56.0.0
Vulkan SDK Revision: 341
```

| 항목 | 값 |
|---|---|
| CPU | AMD Ryzen 7 5800X (8코어 / 16스레드, 3.8 GHz) |
| RAM | 32 GB |
| GPU | NVIDIA GeForce RTX 5080 (16,303 MiB VRAM), 드라이버 **616.56** |
| 디스플레이 | 3840x2160 @ 60 Hz, Windows 배율 150 % (논리 2560x1440), VRR 미사용(60 Hz 고정) |
| OS | Windows 11 25H2, 빌드 26200.9457 (레지스트리 ProductName은 "Windows 10 Pro"로 표기됨) |
| PS3 펌웨어 | **4.93** (dev_flash/vsh/etc/version.txt: `release:04.9300`, build 68500, CEX-ww) |

### 2.1 최신 빌드 여부 확인

RPCS3는 rolling release이므로 0.0.42 태그만으로 판단하지 않고 커밋 단위로 확인했습니다.

| 확인 항목 | 결과 |
|---|---|
| 현재 빌드 커밋 | `38eba804` (2026-09-24/25) |
| 확인 시점 master HEAD | `e447511a` (2026-09-25T13:22Z) — **CI 워크플로 변경 1건뿐** (에뮬레이터 코드 변경 없음) |
| GitHub 릴리스 태그 | v0.0.42 (2026-07-31, “landmark” 태그이며 stable 아님) |
| 결론 | **현재 빌드는 사실상 최신 공식 rolling build** → 교체 불필요. 교체하지 않고 그대로 사용 |

### 2.2 조사한 데이터 (원본 상태)

| 항목 | 상태 |
|---|---|
| config/config.yml | Renderer Vulkan / Resolution 1920x1080 / **Resolution Scale 350** / Output Scaling FSR / CAS 50 / VSync Disabled / Frame limit Auto / WCB Off / Strict Off / MTRSX Off / SPU Block Size Safe / System Language **Korean** / License Area **SCEK** |
| config/custom_configs | **없음** (이번에 생성) |
| config/games.yml | `BCAS20298: C:/Users/<user>/Downloads/Dragon's Crown (Asia) (Zh,Ko)/…` |
| config/input_configs | Player 1 = **DualSense** ("DualSense Pad #1"), 나머지 Null, active=global:Default |
| GuiConfigs | compat_database.dat(6.5 MB, 6,754 타이틀) / config_database.dat / CurrentSettings.ini (language=en, 스타일 "Darker Style by TheMitoSan") |
| patches | 비어 있음 → 공식 patch.yml 배치함(§6) |
| dev_flash | PS3 4.93 펌웨어 설치됨 |
| dev_hdd0 | home/00000001 (savedata `BCAS20298-AUTO_0-`, trophy `NPWR03852_00`), game/ (TEST12345, `＄locks\BCAS20298_v01.00` 빈 폴더) |
| cache | `cache/BCAS20298` (셰이더 126개/36.9 MB) + PPU/SPU 모듈 캐시 144개 + iso_cache |
| persistent_settings.dat | 플레이타임 BCAS20298=1,672,294 ms (약 28분), 최근 플레이 2026-09-25 |
| 로그 | log/TTY.log 0 byte (게임 TTY 출력 없음) |

## 3. E:\PS3 구조 (생성 완료)

```
E:\PS3\
  RPCS3\                     ← RPCS3 전체 복제본 (실사용)
  Games\                     (게임 덤프 보관용, 현재 비어 있음)
  Updates_DLC\BCAS20298_v1.09\  ← 공식 v1.09 PKG + manifest + PARAM.HIP
  Mods_Patches\
    RPCS3_Patches\           ← 공식 patch.yml(v1.2) + No-Intro psn_update.dat
    ReShade\                 ← Shaders / Textures / Presets / Enable·Disable·Uninstall 스크립트
    RPCS3_Translation\       ← rpcs3_ko.ts / rpcs3_all.ts / rpcs3_ko.qm
  Profiles\                  ← 17개 프로필 (각 config.yml)
  Screenshots_AB\Dragons_Crown\
  Logs\                      ← 부팅/성능 테스트 원본 로그 + perf CSV
  Backups\01_original / 02_before_coldboot_test / 03_before_reshade / 04_rollback
  Launchers\                 ← .cmd 런처 8종
  Docs\                      ← 본 문서 포함 보고서
  Tools\                     ← 재현용 스크립트(.py/.ps1) 및 RPCS3 소스(번역용)
```

## 4. 프로필 (전역/게임별 완전 분리)

`E:\PS3\Profiles\<이름>\config.yml` = **완전한 config.yml**(원본 기반 + 해당 키만 변경). 런처는 `--config` 로 이 파일을 그대로 사용하므로 전역 설정을 건드리지 않습니다.

| 프로필 | Resolution Scale | 내부 렌더 | AF | WCB | Strict | MTRSX | 출력 스케일링 | 용도 |
|---|---|---|---|---|---|---|---|---|| CLEAN | 100 % | 1280x720 | Auto | Off | Off | Off | Bilinear | 기준선(전역 설정에도 적용됨) |
| DC_SAFE | 100 % | 1280x720 | Auto | Off | Off | Off | Bilinear | 최우선 호환성 |
| DC_SAFE_WCB | 100 % | 1280x720 | Auto | **On** | Off | Off | Bilinear | 그래픽 이상 시 1차 대응 |
| DC_4K_ULTRA | 300 % | 3840x2160 | Auto | Off | Off | Off | Bilinear | 일상 4K (게임 커스텀 설정에도 적용) |
| DC_4K_ULTRA_AF16 | 300 % | 3840x2160 | **16x** | Off | Off | Off | Bilinear | AF A/B |
| DC_5K_SSAA | 400 % | 5120x2880 | Auto | Off | Off | Off | Bilinear | 4K로 다운샘플 |
| DC_5K_SSAA_AF16 | 400 % | 5120x2880 | 16x | Off | Off | Off | Bilinear | AF A/B |
| DC_6K_TEST | 500 % | 6400x3600 | Auto | Off | Off | Off | Bilinear | 실험 |
| DC_8K_SCREENSHOT | 600 % | 7680x4320 | Auto | Off | Off | Off | Bilinear | 스크린샷 실험 |
| DC_4K_WCB_ON | 300 % | 3840x2160 | Auto | On | Off | Off | Bilinear | WCB A/B |
| DC_4K_STRICT | 300 % | 3840x2160 | Auto | Off | **On** | Off | Bilinear | Strict A/B |
| DC_4K_MTRSX | 300 % | 3840x2160 | Auto | Off | Off | **On** | Bilinear | MTRSX 실험 |
| DC_4K_CAS0 / CAS15 / CAS25 | 300 % | 3840x2160 | Auto | Off | Off | Off | **FSR** | CAS 선명도 A/B (FSR 선택 시에만 동작) |
| DC_TEST_WINDOWED | 100 % | 1280x720 | Auto | Off | Off | Off | Bilinear | 자동 검증용(창모드) |
| DC_4K_TEST_WINDOWED | 300 % | 3840x2160 | Auto | Off | Off | Off | Bilinear | 자동 검증용(창모드) |
| **DC_RPCN_NETPLAY** | 300 % | 3840x2160 | Auto | Off | Off | Off | Bilinear | **RPCN 넷플레이 검증용** — Net: Internet Connected / PSN status **RPCN** / UPNP Off / Clans Off, ReShade 차단(런처) |

* Resolution Scale ≠ 100 % 이면 “Resolution” 드롭다운은 무시되고 **기준 해상도는 항상 1280x720** 입니다 (RPCS3 소스 tooltip: “The base resolution is always 1280x720”). 따라서 300 % = 정확히 3840x2160, 400 % = 5120x2880, 600 % = 7680x4320.
* 게임별 커스텀 설정: `E:\PS3\RPCS3\config\custom_configs\BCAS20298.yml` = **DC_4K_ULTRA**. GUI에서 게임을 실행하면 자동 적용됩니다.

## 5. 런처 (E:\PS3\Launchers)

| 파일 | 동작 |
|---|---|
| `RPCS3_CLEAN.cmd` | RPCS3 GUI만 실행(전역=CLEAN). `DISABLE_VK_LAYER_reshade_1=1` 로 ReShade 차단 |
| `RPCS3_DRAGONS_CROWN.cmd` | DC_4K_ULTRA 로 게임 즉시 부팅 (GUI 유지) |
| `RPCS3_DRAGONS_CROWN_5K.cmd` | DC_5K_SSAA 로 부팅 |
| `RPCS3_DRAGONS_CROWN_SAFE.cmd` | DC_SAFE 로 부팅 |
| `RPCS3_DRAGONS_CROWN_CLEAN.cmd` | CLEAN(100 %)로 부팅 (A/B 기준) |
| `RPCS3_DRAGONS_CROWN_4K_NOGUI.cmd` | `--no-gui --fullscreen` + DC_4K_ULTRA (콘솔형 실행) |
| `RPCS3_DRAGONS_CROWN_8K.cmd` | DC_8K_SCREENSHOT 로 부팅 (실험) |
| `RPCS3_DRAGONS_CROWN_POSTFX.cmd` | DC_4K_ULTRA + ReShade (레이어 활성 시 자동 적용) |
| `RPCS3_DRAGONS_CROWN_RPCN.cmd` | **DC_RPCN_NETPLAY** 로 부팅 (RPCN 넷플레이 검증 기준, ReShade 차단) |

모든 런처는 **읽기 전용**(세이브/캐시/설정을 수정·삭제하지 않음), 관리자 권한 불필요, 다른 프로필에 영향 없음.

## 6. 공식 패치 DB

* RPCS3 공식 patch DB를 받아 `E:\PS3\RPCS3\patches\patch.yml` 로 배치했습니다.
  * 출처: `https://rpcs3.net/compatibility?patch&api=v1&v=1.2` (patch engine v1.2)
  * sha256: `ee9becef3285e6af644ea7eea85c748e48296c472345840def0e2582aad8810e`
  * 원본 JSON/추출본 사본: `E:\PS3\Mods_Patches\RPCS3_Patches\`
* **Dragon's Crown(BCAS20298 / BLES01950 / BLUS30767)용 패치는 DB에 존재하지 않습니다.** → 60 FPS 패치 등 일절 적용되지 않았고, 적용할 것도 없습니다(§18 요구사항 충족).
* No-Intro PSN 업데이트 DB(`psn_update.dat`)도 함께 보관: `E:\PS3\Mods_Patches\RPCS3_Patches\psn_update.dat`

## 7. 한글화

| 구분 | 상태 |
|---|---|
| RPCS3 GUI | 공식 한국어 번역 파일이 **없음**(리포지토리에 번역 파일 자체가 없음, 공식 빌드에도 `rpcs3_*.qm` 미포함). 로컬 번역 구축: `qt6\translations\rpcs3_ko.qm` |
| 번역 범위 | 272개 문자열 (메뉴/컨텍스트 메뉴/설정 항목/로그/게임 목록 열 등). Qt 자체 문자열(OK/취소/예/아니오 등)은 `qt_ko.qm` 제공 |
| 방식 | RPCS3 master 소스를 `pyside6-lupdate` 로 스캔해 **실제 컨텍스트**(클래스명)를 얻은 뒤 번역 병합 → `lrelease` 컴파일. 컨텍스트 불일치 시 번역이 적용되지 않는 Qt 6 동작을 검증했고, 실제 컨텍스트 기준 272/281 조회 성공 |
| GUI 언어 설정 | `GuiConfigs\CurrentSettings.ini` → `language=ko` (로그: `Current language changed to Korean (ko)`) |
| PS3 시스템 언어 | `config.yml` → `System: Language: Korean` (원래부터 설정되어 있었음) + GUI 언어 ko는 에뮬레이트 콘솔 언어 ID도 Korean으로 매핑 |
| 게임 한글 출력 | 덤프가 **Asia (Zh,Ko) = BCAS20298** 이므로 한국어 리소스 포함 가능성이 높음. 단 GameTDB는 BCAS20298 언어를 `ZHTW`(중국어 번체)로 표기하므로 **게임 내 실제 한국어 텍스트 출력은 실제 플레이로 확인 필요**(v1.09 changelog에는 한국어본이 존재) |
| 확장 방법 | `E:\PS3\Mods_Patches\RPCS3_Translation\rpcs3_all.ts` (3,149 문자열 전체) 를 편집 → `build_korean_translation_ctx.py` 재실행 |

## 8. ReShade (선택형, 되돌리기 가능)

| 항목 | 값 |
|---|---|
| 버전/파일 | ReShade **6.8.0.2155** (Addon), 공식 배포 `ReShade_Setup_6.8.0_Addon.exe` (SHA256 `AFE4C8F1…6445`) |
| 설치 방식 | `--headless --api vulkan "E:\PS3\RPCS3\rpcs3.exe"` (공식 설치 프로그램 CLI) |
| 모듈 | `C:\ProgramData\ReShade\ReShade64.dll` (+json, ReShadeApps.ini) |
| Vulkan 레이어 | `HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers` → `C:\ProgramData\ReShade\ReShade64.json` (값 1=사용/0=사용 안 함) |
| 적용 범위 | `ReShadeApps.ini` = `Apps=E:\PS3\RPCS3\rpcs3.exe` → **rpcs3.exe 전용** |
| 설정 | `E:\PS3\RPCS3\ReShade.ini` (Effect/Texture 검색 경로, PresetPath, 오버레이 키 HOME) |
| 셰이더 | 공식 소스만 사용: `Deband.fx`(slim), `CAS.fx`·`LumaSharpen.fx`·`SMAA.fx`(SweetFX, 공식 패키지 목록 #01) → `E:\PS3\Mods_Patches\ReShade\Shaders` |
| 프리셋 | `DC_POSTFX_MINIMAL.ini` (Deband + CAS 0.30), `DC_POSTFX_MINIMAL_SMAA.ini` (+SMAA) |
| 검증 | `ReShade.log`: 6.8.0.2155 로드, 스왑체인 3840x2160, 4개 이펙트 모두 컴파일 성공, 4K 300 % + ReShade 90초 무중단 |
| 프리셋 자동 로드 | ReShade 소스 확인: `[GENERAL] PresetPath` 는 시작 시 로드됨(`source/runtime.cpp` 1023/1146행). `PerformanceMode=1` 은 셰이더 코드 생성 최적화 옵션일 뿐 이펙트를 끄지 않음(동 소스 1942행) |
| CLEAN 분리 | `RPCS3_CLEAN.cmd` 는 `DISABLE_VK_LAYER_reshade_1=1` 로 강제 차단. 전역 차단은 `ReShade_Disable.cmd` |
| 제거 | `ReShade_Uninstall.cmd` (공식 `--state uninstall`) |
| 비용 | 동일 장면에서 GPU 사용률 10 %→30 % 수준(여유 충분), VRAM 변화 미미 |

## 9. NVIDIA / 디스플레이 설정 원칙

* NVIDIA 제어판/앱에서 **드라이버 강제 오버라이드(AA/AF/VSync)를 적용하지 않았습니다.** RPCS3 Vulkan 설정이 우선입니다.
* DSR/DLDSR 미사용 — RPCS3 내부 해상도 배율과 중복되므로 사용하지 않습니다.
* 디스플레이는 4K@60Hz 고정(VRR 없음) → RPCS3 VSync 기본(Disabled) + Frame limit Auto 유지. 화면 찢어짐이 보이면 VSync를 “Full”로 바꾸는 것이 첫 대응입니다.
* NVIDIA 오버레이(GeForce Experience)는 이번 검증에서 사용하지 않았습니다. 문제 발생 시 `Alt+Z` 비활성 상태로 A/B 하십시오.

## 10. 검증 로그 (E:\PS3\Logs)

| 파일 | 내용 |
|---|---|
| `RUN1_CLEAN_coldboot_RPCS3.log` | CLEAN 최초 부팅(셰이더 105개 신규 컴파일) — 성공 |
| `RUN2_CLEAN_coldboot_nollvmcache_RPCS3.log` | **PPU/SPU LLVM 캐시 제거 후 진짜 콜드부트** — 성공(행 없음) |
| `RUN3_CLEAN_warmboot_RPCS3.log` | CLEAN 재부팅 — 성공 |
| `RUN4_DC_4K_300_RPCS3.log` | 4K 300 % — 성공 |
| `RUN5_DC_5K_400_RPCS3.log` | 5K 400 % — 성공 |
| `RUN6_4K_POSTFX_reshade_RPCS3.log` | 4K + ReShade — 성공 |
| `fps_by_scale.csv`, `RUN*_perf.csv` | FPS / CPU / GPU / VRAM 계측 |
| `rpcs3_exe_strings.txt` | 번역 검증용 exe 문자열 덤프 |

## 11. RPCN 넷플레이 (추가 항목)

* 프로필: `E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml` (300 %, ReShade Off, Net: PSN status **RPCN**)
* 런처: `E:\PS3\Launchers\RPCS3_DRAGONS_CROWN_RPCN.cmd`
* 계정 파일: `E:\PS3\RPCS3\config\rpcn.yml` (현재 **없음** → 계정 생성 필요)
* 기본 서버: `np.rpcs3.net:31313` (도달성 확인 완료)
* 상세 절차·워크어라운드·진단·대체 방식·최종 분류: **`E:\PS3\Docs\RPCN_NETPLAY_GUIDE.md`**
* 이 세션에서 실제 로그인/매치 검증은 불가(계정·상대 피어 없음) → 분류 **RPCN_PARTIAL**, 실패 시 REMOTE_PLAY 권장

## 12. 출처 (Sources)

| # | 자료 | URL | 시점/버전 |
|---|---|---|---|
| 1 | RPCS3 공식 최신 빌드/릴리스 | https://rpcs3.net/download · https://api.github.com/repos/RPCS3/rpcs3/releases/latest | v0.0.42 (2026-07-31) |
| 2 | RPCS3 master 커밋 (빌드 확인) | https://api.github.com/repos/RPCS3/rpcs3/commits/38eba804 · …/commits/master | 2026-09-24/25 |
| 3 | RPCS3 공식 호환성 DB | https://rpcs3.net/compatibility?api=v1&export (로컬 `GuiConfigs/compat_database.dat`) | BCAS20298 = Playable(2018-07-07), update 01.09 |
| 4 | RPCS3 공식 patch DB | https://rpcs3.net/compatibility?patch&api=v1&v=1.2 | engine 1.2, 2026-09-25 수신 |
| 5 | RPCS3 Wiki — Dragon's Crown | https://wiki.rpcs3.net/index.php?title=Dragon%27s_Crown | “No options that deviate from RPCS3's default settings are recommended for this title.” |
| 6 | RPCS3 Wiki — Default Settings | https://wiki.rpcs3.net/index.php?title=Help:Default_Settings | Disable SPU GETLLAR = Off(기본) |
| 7 | GitHub Issue #17200 (BLES01950 부팅 행) | https://github.com/RPCS3/rpcs3/issues/17200 | 2025-05-10 보고, #17207 로 수정·종료 |
| 8 | GitHub Issue #17502 (BLUS30767 부팅 불가) | https://github.com/RPCS3/rpcs3/issues/17502 | 2025-09-14 보고, 2025-09-25 종료 |
| 9 | RPCS3 소스 (설정 의미/컨텍스트/업데이트 경로) | https://github.com/RPCS3/rpcs3 (master, 2026-09-25) — `rpcs3qt/settings_dialog.cpp`, `tooltips.h`, `gui_application.cpp`, `game_compatibility.cpp`, `patch_manager_dialog.cpp`, `rpcs3.cpp` | v0.0.42-20053 기준 |
| 10 | Sony 공식 업데이트 manifest (BCAS20298) | `a0.ww.np.dl.playstation.net/tpl/np/BCAS20298/BCAS20298-ver.xml` | v01.09, sha1sum 06bf9f1e… |
| 11 | Sony 공식 업데이트 PKG | `b0.ww.np.dl.playstation.net/tppkg/np/BCAS20298/BCAS20298_T6/90985636f4377b1f/HP5017-BCAS20298_00-DRAGONSCROWNDL00-A0109-V0100-PE.pkg` | 47,301,728 B |
| 12 | No-Intro PSN 업데이트 DB (RPCS3 공식 API) | https://api.rpcs3.net/nointro/updates/?api=v1 | md5 32ce31d4…, sha1 d3c9a27b… (다운로드 파일과 일치) |
| 13 | ReShade 공식 배포 | https://reshade.me (ReShade_Setup_6.8.0_Addon.exe) | 6.8.0.2155 |
| 14 | ReShade 공식 셰이더 패키지 목록 | https://raw.githubusercontent.com/crosire/reshade-shaders/list/EffectPackages.ini | Standard effects / SweetFX |
| 15 | RPCS3 소스 (ReShade 설치 CLI 근거) | https://github.com/crosire/reshade/blob/main/setup/MainWindow.xaml.cs | `--headless --api vulkan` |
