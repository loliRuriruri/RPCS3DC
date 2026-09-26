# RPCS3 CHANGELOG — E:\PS3 구축 작업

모든 변경은 **원본 C:\ 설치본을 건드리지 않고** `E:\PS3` 안에서만 수행했습니다.
각 항목의 되돌리기 방법은 `E:\PS3\Docs\ROLLBACK.md` 참조.

---

## 2026-09-25 — 조사 단계 (변경 없음)

| # | 작업 | 결과 |
|---|---|---|
| 1 | 원본 RPCS3 조사 (config.yml, GuiConfigs, dev_flash, dev_hdd0, cache, patches) | 전역 350 % + FSR, Korean/SCEK, 펌웨어 4.93, savedata/trophy 존재, custom_configs 없음 |
| 2 | 게임 덤프 PARAM.SFO 파싱 | BCAS20298 / DG / APP_VER 01.00 / PS3_SYSTEM_VER 04.4600 |
| 3 | RPCS3 빌드 최신 여부 확인 | 현재 빌드 = 공식 최신 rolling build (master는 CI 커밋 1개 앞) |
| 4 | 공식 호환성 DB 조회 | BCAS20298 = Playable, update 01.09, 패키지 sha1/size 확보 |
| 5 | 공식 Wiki / GitHub Issues 조사 | 기본 설정 권장, 과거 회귀 2건 모두 수정·종료 확인 |

## 2026-09-25 — 구축 단계

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 6 | E:\PS3 디렉터리 구조 생성 | `E:\PS3\…` (RPCS3, Games, Updates_DLC, Mods_Patches, Profiles, Screenshots_AB, Logs, Backups, Launchers, Docs, Tools) | |
| 7 | RPCS3 전체 복제 (1,740 파일 / 378 MB) | `C:\…\rpcs3-v0.0.42-20053-…` → `E:\PS3\RPCS3` | robocopy, SHA256 검증, 원본 무변경 |
| 8 | 원본 상태 백업 | `E:\PS3\Backups\01_original\` | config.yml, games.yml, uuid, recording.yml, input_configs, CurrentSettings.ini, persistent_settings.dat, compat/config DB, **savedata(BCAS20298-AUTO_0-)**, **trophy(NPWR03852_00)**, localusername |
| 9 | 공식 patch DB 배치 | `E:\PS3\RPCS3\patches\patch.yml` (+사본 `Mods_Patches\RPCS3_Patches`) | v1.2, sha256 ee9becef…, Dragon's Crown 패치 없음 |
| 10 | No-Intro PSN 업데이트 DB 보관 | `E:\PS3\Mods_Patches\RPCS3_Patches\psn_update.dat` | PKG 해시 검증용 |
| 11 | 공식 v1.09 업데이트 PKG 다운로드·검증 | `E:\PS3\Updates_DLC\BCAS20298_v1.09\` | MD5/SHA1/CRC32/크기 = No-Intro DB 일치 |
| 12 | **v1.09 설치** (E:\PS3 복사본에만) | `E:\PS3\RPCS3\dev_hdd0\game\BCAS20298\` | APP_VER 01.09 확인, 원본 C:\ 설치본은 01.00 그대로 |
| 13 | 17개 프로필 생성 | `E:\PS3\Profiles\<이름>\config.yml` | 원본 config 기반 전체 config, 해당 키만 변경 |
| 14 | 전역 설정 = CLEAN 으로 설정 | `E:\PS3\RPCS3\config\config.yml` | 100 %, 720p, Bilinear, WCB/Strict Off, AF Auto |
| 15 | 게임 커스텀 설정 생성 | `E:\PS3\RPCS3\config\custom_configs\BCAS20298.yml` | DC_4K_ULTRA (300 %) |
| 16 | 런처 8종 생성 | `E:\PS3\Launchers\*.cmd` | 읽기 전용, 관리자 권한 불필요, `--config` 로 프로필 분리 |
| 17 | 자동 검증 스크립트 작성 | `E:\PS3\Tools\boot_test.ps1`, `fps_scale_test.ps1`, `make_profiles.py`, `install_update.ps1` 등 | 재현 가능 |

## 2026-09-25 — 검증 단계 (게임 실행)

| # | 작업 | 결과 |
|---|---|---|
| 18 | CLEAN 최초 부팅 (RUN1) | 성공, 셰이더 105개 컴파일 |
| 19 | PPU/SPU LLVM 캐시 백업 후 제거 → **콜드부트 (RUN2)** | 성공. 백업: `Backups\02_before_coldboot_test\` (144개 모듈 캐시, 복원 가능) |
| 20 | CLEAN 워밍 부팅 (RUN3) | 성공 |
| 21 | 4K 300 % 부팅 (RUN4) | 성공 |
| 22 | 5K 400 % 부팅 (RUN5) | 성공 |
| 23 | 스케일별 FPS 측정 (100/300/400/500/600 %) | 전 구간 59.5~60.1 fps |
| 24 | 성능 계측 저장 | `E:\PS3\Logs\RUN*_perf.csv`, `fps_by_scale.csv` |

## 2026-09-25 — 후처리(ReShade) 단계

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 25 | ReShade 6.8.0 Addon 설치 (Vulkan, rpcs3.exe 대상) | `C:\ProgramData\ReShade\`, `E:\PS3\RPCS3\ReShade.ini` | 공식 CLI `--headless --api vulkan` |
| 26 | Vulkan 암시적 레이어 등록 | `HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers` → ReShade64.json | 값 1=사용/0=중지, `ReShadeApps.ini` 로 rpcs3.exe 전용 |
| 27 | 공식 셰이더만 설치 | `E:\PS3\Mods_Patches\ReShade\Shaders` (Deband, CAS, LumaSharpen, SMAA + includes) | slim(공식) + SweetFX(공식 패키지 목록) |
| 28 | 프리셋 2종 + 스크립트 3종 | `E:\PS3\Mods_Patches\ReShade\Presets`, `ReShade_Enable/Disable/Uninstall.cmd` | |
| 29 | 4K + ReShade 90초 실행 (RUN6) | 성공 (4개 이펙트 컴파일, 무중단) | |
| 30 | CLEAN 런처에 ReShade 차단 환경변수 추가 | `E:\PS3\Launchers\RPCS3_CLEAN.cmd` | `DISABLE_VK_LAYER_reshade_1=1` |

## 2026-09-25/26 — 한글화 단계

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 31 | RPCS3 소스 확보(번역 컨텍스트 추출용) | `E:\PS3\Tools\rpcs3-src\` | master zip 8 MB |
| 32 | `pyside6-lupdate` 로 전체 문자열/컨텍스트 추출 | `Mods_Patches\RPCS3_Translation\rpcs3_all.ts` (3,149 문자열) | Qt 6은 컨텍스트가 일치해야 번역 적용됨을 실측으로 확인 |
| 33 | 한국어 272개 문자열 병합·컴파일 | `rpcs3_ko.ts` → `rpcs3_ko.qm` (18,446 B) | 컨텍스트 기준 272/281 조회 성공 |
| 34 | 번역 파일 설치 | `E:\PS3\RPCS3\qt6\translations\rpcs3_ko.qm` | 로그 `Found language 'ko'`, `Installing translation`, `Current language changed to Korean (ko)` |
| 35 | GUI 언어를 ko 로 설정 | `E:\PS3\RPCS3\GuiConfigs\CurrentSettings.ini` | 백업: `Backups\01_original\GuiConfigs\CurrentSettings.ini.bak_before_ko` |

## 2026-09-26 — RPCN 넷플레이 프로필 단계

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 41 | RPCN 넷플레이 프로필 생성 | `E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml` | 300 % / AF Auto / MSAA Auto / WCB Off / Strict Off / ReShade Off + Net: Internet Connected, **PSN status RPCN**, UPNP Off, Clans Off |
| 42 | RPCN 런처 추가 | `E:\PS3\Launchers\RPCS3_DRAGONS_CROWN_RPCN.cmd` | ReShade 차단(`DISABLE_VK_LAYER_reshade_1=1`) + 넷플레이 체크리스트 출력 |
| 43 | RPCN 설정 위치/이름 확정 | `Docs\RPCN_NETPLAY_GUIDE.md` | config 키 10종, `config\rpcn.yml`, 기본 서버 `np.rpcs3.net:31313` |
| 44 | 네트워크 진단 | 동 문서 §8 | 서버 TCP 31313 도달 성공, VPN 미연결(단 NordVPN 설치·서비스 실행 중), **Wi-Fi+유선 이중 NIC**, 방화벽 인바운드 규칙 없음, UDP 3658 미사용 |
| 45 | 공식/커뮤니티 근거 정리 | 동 문서 §5 | 공식 RPCN 목록 = Untested, Issue #15283(join 실패·UDP 3658 이슈·local RPCN에서도 실패) |
| 46 | 최종 분류 | 동 문서 §9·§10 | **RPCN_PARTIAL** (실제 매치 미검증) + 실패 시 REMOTE_PLAY 권장 |
| 47 | RPCN 프로필 부팅 실측 | `E:\PS3\Logs\RUN8_RPCN_profile_no_account_RPCS3.log` | `PSN status: RPCN` 적용, **`sys_net: P2P port 3658 was bound!`**, `RPCN config missing … config/rpcn.yml` 확인 |
| 48 | Windows 방화벽 인바운드 규칙 추가(사용자 승인) | `RPCS3 (Private)`, `RPCS3 (Public)` → `E:\PS3\RPCS3\rpcs3.exe` Any 허용 | 현재 네트워크가 Public 분류라 두 프로필 모두 등록. 제거/재추가 스크립트: `Tools\firewall_rpcs3_remove.cmd`, `firewall_rpcs3_allow.cmd` |
| 49 | 30분 연속 실행 검증(RUN7, 4K+ReShade) | `E:\PS3\Logs\RUN7_4K_POSTFX_30min_*` | **11분 3초에 게임이 스스로 종료**(`_sys_process_exit`, exit code 0, 크래시 아님) — 타이틀 방치 시 게임 자체 종료. 30분 연속 *플레이* 검증은 사용자 수행 필요 |

## 2026-09-26 — Phase 1: PC Edition (Graphics A/B · Multiplayer 3종 · 진단/백업/복구)

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 85 | 그래픽 A(Standard) 확정 | 4개 프로필 | **AF 16x** · MSAA Auto · Stretch Off · 16:9 · Scale 300/400% · VSync Full · VBlank 60 · Frame Skip Off |
| 86 | 그래픽 B(Pro Enhanced) 프리셋 | `ReShade\Presets\DC_PRO_ENHANCED.ini` | Deband + CAS(0.32) + Levels + Vibrance(0.10) + SMAA — 원본 느낌 유지, 과도한 효과 배제 |
| 87 | 셰이더 추가 | `Mods_Patches\ReShade\Shaders` | Levels.fx · Vibrance.fx (공식 SweetFX 소스) |
| 88 | ReShade.ini 프리셋 전환 | `RPCS3\ReShade.ini` | PresetPath 를 프리셋에 맞춰 자동 전환(Silent [OVERLAY] 유지) |
| 89 | Netplay Safe 프로필 | `Profiles\Dragons_Crown\NETPLAY_SAFE` | 실험/네트워크 영향 옵션 전부 보수값 고정 |
| 90 | 런처 UI 재구성 | `Launcher\src\MainWindow.xaml(.cs)` | PLAY(Standard/Pro Enhanced) · MULTIPLAYER(Local/Remote Co-op/RPCN) · SETTINGS(Graphics/Controller/Network/Advanced) · TOOLS(Diagnostics/Reset/Backup/KnownGood) |
| 91 | Diagnostics | 런처 TOOLS | RPCS3 · 게임 · 버전 · 펌웨어 · PPU · 세이브 · 1P/2P · 해상도 · ReShade · Sunshine · RPCN · 치트/패치 · 프로필 24항목 + 원인 안내 |
| 92 | 설정 백업/복구 | 런처 TOOLS + `Backups\<시각>_<라벨>` | config/custom config/ReShade.ini/CurrentSettings.ini/프로필/입력설정. **세이브는 대상 아님** |
| 93 | Reset | 런처 TOOLS | Reset Graphics / Reset Multiplayer (실행 전 자동 백업) |
| 94 | CLI 확장 | 런처 | `--preset`, `--diag`, `--backup-settings`, `--reset-graphics`, `--reset-multiplayer`, `--launch STANDARD\|PROENHANCED\|LOCAL\|REMOTE\|RPCN\|RPCNSAFE\|CHEAT\|KNOWNGOOD` |
| 95 | 멀티플레이 문서 | `Docs\MULTIPLAYER.md` | Local / Remote Co-op(Sunshine+Moonlight) / RPCN + Netplay Safe + 테스트 매트릭스 |
| 96 | Phase 1 검증 | 실측 | Standard: ReShade 미로드(로그 크기 불변) / Pro Enhanced: ReShade 6종 컴파일 + Silent / Reset: AF16·RPCN·UPNP Off 복원 확인 |
| 97 | PS4 Pro 이식 금지 명시 | 문서 | Phase 2 로 분리(텍스처/UI/음원/실행파일 분석 금지) |
## 2026-09-26 — Borderless 4K 런처 (GUI + Win32 창 제어)

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 75 | RPCS3 창 구조 조사 | `Tools\inspect_windows.ps1` (DPI aware) | 게임 렌더 창 = 제목 `FPS: … \| Vulkan \| …`, GUI 메인 창/더미 창과 구분 |
| 76 | Borderless 엔진 작성 | `Tools\borderless_4k.ps1` | `-Action Info/Apply/Restore/Watch`, Win32 style+SetWindowPos, Alt+Enter 미사용 |
| 77 | DPI 문제 해결 | 동 엔진 | Per-Monitor V2 DPI aware 로 물리 픽셀 기준 배치(3840x2160 정확) |
| 78 | 원본 상태 보존 버그 수정 | 동 엔진 | 반복 Apply 시 최초 원본 style/rect 유지(덮어쓰기 금지) |
| 79 | 드리프트 감시 추가 | 동 엔진 Watch | 2초 주기로 창 상태 확인 후 자동 재적용 |
| 80 | GUI 런처 작성 | `Launchers\DragonCrown_Launcher.ps1` + `Dragon_Crown_Launcher.cmd` | 모드 3종(**Borderless 4K 기본** / Fullscreen fallback / Windowed), 프로필 8종, 모니터 자동 탐지 표시, 실행/종료/복원/로그 |
| 81 | 공통 실행 로직 | `Tools\launch_with_mode.ps1` | 모드별 런타임 config 생성(`Profiles\_runtime\`), RPCS3 실행, 워처 시작 |
| 82 | CLI 런처 | `Launchers\Dragon_Crown_Borderless_4K.cmd` | `[프로필] [모드]` 인자 지원 |
| 83 | E2E 검증 | 실측 로그 | 적용: 3862x2110/0x97CF0000 → **3840x2160/0x97000000**, 복원: 원상 복귀, FPS 59.9 유지, ReShade 4종 컴파일 정상, 3개 모드 config 확인 |
| 84 | 문서 | `Docs\LAUNCHER_BORDERLESS.md` | 동작 원리/파일 구성/검증 결과/주의사항 |

## 2026-09-26 — ZERO-BANNER ReShade (자체 빌드)

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 65 | 설치 ReShade 버전 확인 | `ReShade.log` / DLL 리소스 | **6.8.0.2155**, SHA256 `0CEE63F9…` |
| 66 | upstream 소스 확보 | `Mods_Patches\ReShade\ZeroBanner\src` | **tag v6.8.0 = commit `18deaa52…`** + 서브모듈 11종 |
| 67 | 참고 포크 조사 | `fienestar/OverlayDisabledReShade` | “Disable the ReShade Startup Message”, 이력 재작성으로 clean diff 불가 → 참고만 |
| 68 | splash 구현부 특정 | `source/runtime_gui.cpp` line 805 / 1047~ | “Splash Window” = 버전 배너 + reshade.me 안내 + 튜토리얼 안내 + 컴파일 진행률 |
| 69 | 최소 패치 적용 | `source/runtime_gui.cpp` 1파일 | `show_splash_window = false` + 1회 식별 로그. 패치: `ZeroBanner\ZERO_BANNER.patch` (SHA256 `2F9ED1A8…`) |
| 70 | x64 Release 직접 빌드 | MSBuild(VS2022 17.14, MSVC 14.44) | `ReShade64.dll` 5,581,824 B, SHA256 `FFCAB1B2…`, 6.8.0.1 (UNOFFICIAL), 오류 0 |
| 71 | 공식 binary 백업 | `Mods_Patches\ReShade\Official_Backup\` | 6.8.0.2155 + json + ini, `BACKUP_INFO.md` |
| 72 | 전환 스크립트 | `ReShade_Switch_Official.cmd` / `ReShade_Switch_ZeroBanner.cmd` | 관리자, RPCS3 실행 중이면 거부 |
| 73 | A/B 검증 (40초 ×2) | `Logs\reshade_AB_*` | 공식: 4/4 이펙트 컴파일·무크래시 / ZERO-BANNER: 마커 로그 + 4/4 컴파일 + Vulkan 정상 + 무크래시 |
| 74 | 보고서 | `Docs\ZERO_BANNER_REPORT.md` | upstream/patch/build 해시 및 채택 체크리스트 |

## 2026-09-26 — Enhanced Module (Cheat / Save / KnownGood)

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 50 | Cheat Manager 존재 확인 | 이 빌드 exe/소스 | Cheat Search·New Search·Filter Results·Current Value·Apply·Add to cheat list·Memory Viewer·Import/Export Cheats 모두 존재(최신 빌드 교체 불필요) |
| 51 | 게임 식별 정보 문서화 | `Docs\DRAGONS_CROWN_IDENTITY.md` | TITLE/TITLE_ID/CATEGORY/APP_VER + **PPU hash `PPU-bc3ee27f…`**, SPU hash 2종 |
| 52 | KnownGood 스냅샷 생성 | `E:\PS3\RPCS3\KnownGood` (437.16 MB / 1,756 파일) | 현재 검증 통과 빌드 보존, 승격/롤백 절차 문서화 |
| 53 | 세이브 보호 구조 | `Saves\Dragons_Crown\{ORIGINAL,AUTO_BACKUP,CHEAT_TEST}` | ORIGINAL 1회 복사(SHA256 `B6FD91C4…`), AUTO_BACKUP 검증 manifest + 최신 20개 유지 |
| 54 | 세이브 자동 백업 스크립트 | `Tools\save_backup.ps1` | SHA256 검증 + manifest, 실패 시 exit 2 (런처가 중단) |
| 55 | Cheat 안전 프로필 | `Profiles\DC_CHEAT_OFFLINE` | 4K 300% + `Internet Disconnected` + `PSN Disconnected` + ReShade 차단 |
| 56 | 넷플레이 세이프 프로필 | `Profiles\DC_NETPLAY_SAFE` | RPCN On / 치트·패치 금지(런처가 검사) |
| 57 | LSFG 실험 프로필 | `Profiles\DC_EXPERIMENTAL_LSFG` | 4K 창모드(현재 60Hz 디스플레이 → 해당 없음) |
| 58 | 런처 5종 + 동기화 스크립트 | `Launchers\Dragon_Crown_*.cmd` | 4K_ULTRA / 5K_SSAA / CHEAT_OFFLINE(백업+오프라인 가드) / NETPLAY_SAFE(치트·패치 가드) / KNOWN_GOOD + `sync_saves_to_known_good.cmd` |
| 59 | Cheat DB 대장 + metadata 형식 | `Cheats\Dragons_Crown\Custom\{README.md,CHEAT_DB.yml}` | Name/TitleID/Version/PPU Hash/Type/Address/Original/Test/Persistence/Save effect/Author/Source/Date/Status |
| 60 | Cheat import 검증기 | `Tools\validate_cheat_import.py` | YAML 문법·PPU hash·serial·version·주소 범위/정렬·코드패치 경고. 양성 PASS / Artemis 파일 REJECTED 검증 완료 |
| 61 | Artemis 조사(적용 금지) | `Cheats\Dragons_Crown\Artemis_Research\` | **BLUS30767 v1.00 전용**(PPU 60f7a7f5…) → 현재 BCAS20298 v1.09와 불일치. cheat 종류: Max Gold, Score 100x, Infinite Items/MP/SP/Ammo/HP |
| 62 | 그래픽 regression 문서 | `Docs\GRAPHICS_REGRESSION.md` | black box/alpha 이슈 근거(#4133, #9150, Wiki, Reddit 2026-05) + 7개 장면 + A/B 절차 |
| 63 | Cheat 가이드 | `Docs\CHEAT_GUIDE.md` | Cheat Manager 정확한 UI 흐름, Tier A(Gold/SP/Item/Score) 절차, SP 비영속 보고, BE 메모 특성, import 방어, CE 정책, NETPLAY 분리 |
| 64 | CHEAT_OFFLINE 런처 실측 검증 | 로그 | 세이브 백업 자동 생성·검증(manifest), `Internet/PSN Disconnected` 적용, v1.09 부팅 확인 |

## 2026-09-26 — 문서화

| # | 변경 | 위치 |
|---|---|---|
| 36 | 구축 보고서 | `E:\PS3\Docs\RPCS3_SETUP_REPORT.md` |
| 37 | 게임 4K 보고서 | `E:\PS3\Docs\DRAGONS_CROWN_4K_REPORT.md` |
| 38 | 변경 이력(본 문서) | `E:\PS3\Docs\RPCS3_CHANGELOG.md` |
| 39 | 원복 절차 | `E:\PS3\Docs\ROLLBACK.md` |
| 40 | 사용자 검증 프로토콜 | `E:\PS3\Docs\TEST_PROTOCOL.md` |

---

## 2026-09-26 — Phase 1 Closeout (P0/P1 결함 수정 · RC 준비)

| # | 변경 | 위치 | 비고 |
|---|---|---|---|
| 98 | **P0-1 해상도 선택 연결** | 런처 SETTINGS → Graphics | 4K/300% ↔ 5K/400% 선택이 실제 프로필(`DC_PRO_4K`/`DC_PRO_MAX_5K`)에 연결. `ResolutionProfile` 로 저장(재실행 유지). PLAY/Local/Remote/KnownGood 모두 선택 해상도 사용 |
| 99 | CLI 확장 | 런처 | `--resolution 4K\|5K` 추가 |
| 100 | **P0-2 Netplay Safe 강제** | 런처 RPCN 화면 + CLI | Standard 강제 + ReShade **runtime 강제 차단**(`DISABLE_VK_LAYER_reshade_1=1`) + `NETPLAY_SAFE` 프로필. Safe 체크 기본 ON |
| 101 | **P0-3 사용자 설정 보존** | `Project.Launch(forceReShadeOff)` | Safe 는 temporary runtime override — 게임 종료 후 기존 설정(Pro Enhanced + 5K 등) 유지(실측 확인) |
| 102 | **P0-4 v1.09 엄격 검사** | Diagnostics + Preflight(GUI/CLI) | `APP_VER == 01.09` 가 아니면 RPCN/Netplay Safe 실행 차단 + 안내 문구. Solo 는 차단하지 않음 |
| 103 | **P0-5 RPCN 문서 정정** | MULTIPLAYER / RPCN_NETPLAY(_GUIDE) | "Untested(전 항목)" → **RPCN_PARTIAL** 분류, "RPCN Guaranteed" 표현 금지, fallback 순서 유지 |
| 104 | P1 진단 정확도 | `RunDiagnostics()` | 전체 항목(런타임 카운트, 현재 35): 프로필 실제값(Scale/Aspect/Stretch/AF/VSync/VBlank/FrameSkip) 검사, 치트/패치 **존재↔활성 구분**(UNKNOWN 허용), RPCN Configured↔Login 구분, 하드코딩 수치 제거 |
| 105 | P1 경로 하드코딩 제거 | 런처 Network 화면 | `E:\PS3\Tools\...` → `Path.Combine(Root, "Tools", ...)` |
| 106 | P1 `--restore-window` 수정 | CLI + TOOLS | watcher 시작이 아니라 **즉시 복원**(`RequestStopAndRestore`) + stop flag 로 watcher 재적용 방지 |
| 107 | P1 Borderless PID 타겟팅 | `BorderlessEngine` | launchPid 우선, 프로세스 종료 시에만 fallback, titlebar 도 대상 PID 한정 |
| 108 | P1 Backup/Restore 안전 | `RestoreSettingsBackup` | 프로젝트 Root 밖으로 나가는 경로 skip(방어). before_restore/before_reset 자동 백업 유지 |
| 109 | P1 세이브 문구 정확화 | 런처/문서 | "live savedata/trophy 는 Reset 대상 아님", "AutoBackup 최신 20세대 보존" 명시 |
| 110 | P1 문서 | 루트 README + Docs | 루트 README 복원(간결), `GRAPHICS_PROFILES.md` 재작성(중복 문서 정정), `CHANGELOG.md` 통합 안내, `ACCEPTANCE_TEST.md` 신규 |
| 111 | Closeout 실측 | Logs | 5K 400% 부팅 59.8~60.0fps / Standard ReShade 미로드 / Pro→Safe 강제 OFF + 설정 보존 / Borderless 물리 3840x2160·복원 유지 / Reset·Backup·CHEAT·KnownGood PASS |
| 112 | 상태 | Phase 1 | **Implementation Complete / Acceptance Pending** (tag `v1.0.0-rc1`) |
## 원본(C:\) 무변경 확인

| 항목 | 확인 |
|---|---|
| `C:\Users\<user>\Downloads\rpcs3-v0.0.42-20053-38eba804_win64_msvc` | 읽기만 수행. v1.09 설치·프로필·ReShade·한글화 모두 `E:\PS3` 에만 적용 |
| 원본 config.yml 해시 | `684CF5543F26BB194E213529A9F351249D6A3392D78BEB1C68CCE7EE7C4A7829` (백업본과 동일) |
| 원본 세이브 | 변경 없음(백업본 SHA256 `B6FD91C4…56CF` 와 동일해야 함 — 사용 후 원본 대조 권장) |
