# TROUBLESHOOTING — Dragon's Crown PRO Enhanced

작성일: 2026-09-26
로그: `E:\PS3\Logs\launcher.log`, `E:\PS3\Logs\borderless.log`, `E:\PS3\RPCS3\log\RPCS3.log`, `E:\PS3\RPCS3\ReShade.log`

---

## 1. 실행 / 부팅

| 증상 | 확인 | 조치 |
|---|---|---|
| PLAY 눌러도 실행 안 됨 | `Logs\launcher.log`, 런처 상태 패널 | RPCS3/게임/프로필 경로 확인. 게임은 RPCS3 게임 목록에 등록되어 있어야 함(games.yml) |
| 게임 경로 미탐지 | SETTINGS → GAME | RPCS3 GUI에서 Dragon's Crown 추가 후 다시 PLAY |
| 검은 화면에서 멈춤 | `RPCS3.log`에서 `PPU executable hash`, `SPU` | **KnownGood RPCS3로 폴백**(MAINTENANCE). 과거 SPU 회귀 사례(#17200)는 수정되었으나 캐시 문제 시 `Backups` 후 PPU 캐시 재생성 |
| SPU 캐시 생성 후 멈춤 | 콜드부트 3회 재현성 | KnownGood 사용. 필요 시 `Disable SPU GETLLAR Spin Optimization` **A/B 테스트만** |
| 게임이 스스로 종료 | 로그 `_sys_process_exit` | 타이틀 방치 시 게임이 종료하는 정상 동작(크래시 아님) |

## 2. 화면 / 창

| 증상 | 확인 | 조치 |
|---|---|---|
| Borderless가 적용 안 됨 | `Logs\launcher.log`의 `borderless: applied ...` | GRAPHICS에서 Display Mode = Borderless4K 확인. 게임 창 제목이 `FPS: … | Vulkan | …` 인지 확인 |
| 창이 테두리 없는 상태로 남음 | `Logs\borderless_state.json` | MAINTENANCE → **창 스타일 복원**, 또는 게임 종료 시 자동 복원 |
| 테두리 없어서 창 이동 불가 | — | Alt+Tab 사용(설계상 정상) |
| 화면이 늘어나 보임 | RPCS3 `Stretch To Display Area` | Off 유지(16:9). 프로필에서 변경하지 않음 |
| 검은 반투명 box / 배경 검은 선 | `GRAPHICS_REGRESSION.md` 장면 7종 | 알려진 alpha 이슈. WCB On / Read Color On 을 A/B 로만 시험, baseline 대비 회귀 감시 |

## 3. 성능 / tearing

| 증상 | 확인 | 조치 |
|---|---|---|
| 화면 찢어짐 | VSync 상태 | GRAPHICS에서 VSync ON(Full) 확인. NVIDIA 제어판에서 VSync 중복 강제 금지 |
| 60fps인데 미세하게 끊김 | 성능 오버레이 frame time | tearing이 아니라 frame pacing. ReShade를 잠시 Off 로 A/B, GPU 사용률/VRAM 확인 |
| 프레임 드랍 | GPU/CPU 사용률 | 400 %(PRO MAX) → 300 %(PLAY)로 낮춤 |
| 첫 진입 스터터 | 셰이더 컴파일 | 정상(첫 실행만). 이후 캐시됨 |

## 4. ReShade

| 증상 | 확인 | 조치 |
|---|---|---|
| 시작 시 ReShade 문구 보임 | `ReShade.log` 버전 | ZERO-BANNER 빌드 사용(`ReShade_Switch_ZeroBanner.cmd`) |
| 메뉴가 자동으로 열림 | `ReShade.ini` `[OVERLAY] TutorialProgress` | 4 로 설정(Silent). Home 키로만 열리게 |
| 효과가 안 보임 | `ReShade.log` `Successfully compiled` | 프리셋 경로/셰이더 경로 확인, ReShade On 확인 |
| Vulkan 오류/crash | ReShade.log, RPCS3.log | `ReShade_Disable.cmd` 로 즉시 차단 후 CLEAN 으로 플레이 |

## 5. NETPLAY

| 증상 | 확인 | 조치 |
|---|---|---|
| RPCN 로그인 실패 | RPCS3 → RPCN | 계정 생성/로그인, `config\rpcn.yml` 존재 확인 |
| 친구/방은 보이는데 접속 실패 | `RPCS3.log` | 알려진 이슈(#15283). 2대 PC + 유선 + VPN off + UPNP off 로 재시도, 실패 시 REMOTE_PLAY 대안 |
| 로비가 안 보임 | 양쪽 버전 | TITLE_ID(BCAS20298)와 APP_VER(01.09) 일치 확인, 같은 RPCS3 빌드 권장 |
| 치트/패치 경고 | 런처 NETPLAY READY | `config\cheats.yml`·`patch_config.yml` 에서 해당 항목 비활성화 |

## 6. 치트 / 세이브

| 증상 | 확인 | 조치 |
|---|---|---|
| Cheat Manager 가 없음 | 게임 우클릭 → Cheats | 현재 빌드에 존재. 없으면 게임 목록에 게임이 등록됐는지 확인 |
| 값이 안 잡힘 | — | 32비트 우선, 안 되면 16/8비트·Signed 재시도(게스트는 big-endian) |
| 세이브 손상 우려 | `Saves\Dragons_Crown\AutoBackup` | CHEAT OFFLINE 실행 시 자동 백업됨. SAVES → RESTORE 로 복원(복원 전 현재 세이브 재백업) |
| SP가 저장 후 초기화 | — | 과거 사례 보고(비영속 가능). `Persistence: runtime` 으로 기록 |

## 7. 원복

* 설정/프로필/업데이트/ReShade/캐시/세이브별 원복: `ROLLBACK.md`
* RPCS3 전체 초기화: 원본 `C:\Users\<user>\Downloads\rpcs3-…` 에서 재복제(원본은 항상 보존)

---

## 게임 경로를 찾지 못할 때 (다른 PC로 복사한 경우)

증상: RPCS3 목록에는 `Dragon's Crown / BCAS20298 / 01.09` 가 정상 표시되는데
런처에서 "Dragon's Crown [BCAS20298] 경로를 찾을 수 없습니다." 가 나오는 경우.

원인: RPCS3 `config\games.yml` 의 경로 형식이 PC마다 다를 수 있습니다.
(disc root / `...\PS3_GAME` / `...\PS3_GAME\.\` / EBOOT.BIN 경로 / USRDIR 경로)

해결 순서:

1. 런처의 오류 창에서 **[예] 게임 폴더 선택** → 게임의 **Disc root** 또는 **PS3_GAME** 폴더 선택
   (성공하면 `Launcher\game.txt` 에 저장되어 다음 실행부터 자동 사용)
2. 또는 RPCS3 에서 **Add Games** 로 다시 등록 → 런처의 **[RPCS3 상태 새로고침]** 클릭
   (런처 재실행 불필요 — 재탐지가 실행됩니다)
3. 게임을 `<ROOT>\Games\...` 아래에 두면 games.yml 경로가 없어도 자동 탐지됩니다
   (예: `<ROOT>\Games\Dragon's Crown\PS3_GAME\USRDIR\EBOOT.BIN`)
4. 진단: `DragonCrownProEnhanced.exe --game` (탐지 상태) / `--probe "<경로>"` (경로 해석 결과)
   로그: `Logs\launcher.log` 의 `[GameResolver]` 줄

참고: 런처는 게임 파일을 복사/이동/수정하지 않습니다. 다른 게임 폴더(BCAS20298 아님)는
검증에서 거부됩니다.
