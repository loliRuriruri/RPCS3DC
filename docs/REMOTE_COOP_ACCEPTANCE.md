# REMOTE CO-OP ACCEPTANCE — Dragon's Crown PC Edition

작성일: 2026-09-26 · 상태 표기: **PASS / FAIL / PENDING** (세 가지 외 사용 금지)

> **실제 2대 PC 테스트 전에는 E2E 항목을 PASS 로 쓰지 않습니다.**
> 이 문서는 Remote Co-op Helper v1 의 승인 기록 양식입니다. 결과는 복사해서 채웁니다.

---

## 0. 현재 상태 (코드 마감 시점, 1대 PC 자동 검증)

| 구분 | 항목 | 상태 |
|---|---|---|
| BUILD | .NET 8 빌드 / single-file exe (`Launcher\DragonCrownRemoteCoopSetup.exe`, 63.24 MB) | PASS |
| FREE | Sunshine/Moonlight/ViGEmBus = 무료 권장 경로, Virtual HID = 선택(premium) — UI/문서 반영 | PASS |
| FREE | Dragon's Crown Ready 판정 = ViGEmBus + `gamepad=x360` (무료 경로만으로 READY) | PASS |
| HOST | 릴리스 리졸버 (Sunshine v2026.914.233613 / Moonlight v6.1.0 / ViGEmBus v1.22.0) | PASS |
| HOST | 공식 MSI 다운로드 + SHA256 + GitHub digest 일치 + Authenticode valid | PASS |
| HOST | Sunshine 설치 감지 / 서비스 / Web UI / 가상패드 백엔드 감지 로직 | PASS (미설치 상태 감지) |
| HOST | Controller-only 정책 적용 코드 / RPCS3 P2 감지 / 백업·복구 | PASS (코드 + 상태 표시) |
| GUEST | Moonlight 감지 / 컨트롤러 감지 / HOST 주소 저장 / CLI | PASS |
| RPCS3 | 중복 인스턴스 금지 + 설정 바로가기 (UI Automation) — CASE A~E | PASS (아래 §G) |
| RPCS3 | 자동 업데이트 차단 실측 (20053, 2회 재실행) | PASS (아래 §H) |
| E2E | 2대 PC 실제 연결 | **PENDING** |

---

## A. HOST Acceptance (§42)

Clean Windows 기준. 이 PC 에서는 이미 RPCS3 환경이 있으므로 "Clean" 항목은 별도 VM/PC 에서 확인.

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| A1 | Helper 실행 | `DragonCrownRemoteCoopSetup.exe --host` | PASS |
| A2 | Sunshine 탐지 | 상태 패널 `Sunshine ✓/✗` | PASS (미설치 감지) |
| A3 | Sunshine 공식 MSI 다운로드 | `--download sunshine` | PASS (digest 일치) |
| A4 | 설치 성공 | `[INSTALL / REPAIR HOST]` (대화형 MSI) | PENDING |
| A5 | Service 감지 | 상태 `Service RUNNING/STOPPED` | PENDING (설치 후) |
| A6 | Gamepad backend 판별 | `Virtual HID / ViGEmBus / Missing` | PASS (Missing 판별) |
| A7 | controller input ON | `sunshine.conf` controller=enabled | PENDING (설치 후) |
| A8 | controller-only mode | keyboard/mouse=disabled 적용 | PENDING (설치 후) |
| A9 | RPCS3 탐지 | 상태 `RPCS3 ✓` | PASS |
| A10 | **P1 보존** | 적용 후에도 Player 1 = DualSense 유지 | PASS (코드상 P1 미변경) / 적용 후 재확인 |
| A11 | P2 XInput | `Player 2 = XInput` | PASS (이미 XInput) |
| A12 | config backup | `Backups\RemoteCoop\<시각>\Sunshine\` 생성 | PENDING (Sunshine 설치 후) |
| A13 | restore 동작 | `[RESTORE SUNSHINE CONFIG]` | PENDING (설치 후) |

## B. GUEST Acceptance (§43)

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| B1 | Moonlight 탐지 | 상태 `Moonlight ✓/✗` | PASS (미설치 감지) |
| B2 | 공식 installer 다운로드 | `[INSTALL MOONLIGHT]` / `--download moonlight` | PENDING (다운로드 검증은 로직 PASS) |
| B3 | 설치 | 대화형 설치 | PENDING |
| B4 | 패드 감지 | `[TEST CONTROLLER]` | PASS (DualSense non-XInput 감지) |
| B5 | HOST 탐색 | Moonlight GUI 자동 탐색 | PENDING |
| B6 | Pairing | PIN → Sunshine Web UI 승인 | PENDING |
| B7 | Desktop stream | `[START DESKTOP STREAM]` | PENDING |

## C. End-to-End Acceptance (§44) — 두 PC 실제 테스트

**준비물**: HOST PC(게임) + GUEST PC(친구) + 친구 패드 + 같은 LAN(권장)

절차:

```text
1) HOST: DragonCrownRemoteCoopSetup.exe --host
   → [INSTALL / REPAIR HOST] → Sunshine 설치/실행 + Controller-only + (필요 시) ViGEmBus + P2 XInput
2) GUEST: DragonCrownRemoteCoopSetup.exe --guest
   → [INSTALL MOONLIGHT] → [TEST CONTROLLER] → HOST 주소 입력 → [ADD / PAIR HOST] → PIN 승인
3) GUEST: Moonlight 로 Desktop 스트림 접속 (1080p60)
4) HOST: [TEST REMOTE PAD] → "REMOTE CONTROLLER READY" 확인
5) HOST: [START REMOTE CO-OP] → 게임 실행 → 게임 내 2P Start
```

| # | 항목 | 확인 방법 | 상태 |
|---|---|---|---|
| C1 | Moonlight 연결 | Guest 화면에 스트림 표시 | PENDING |
| C2 | HOST에 가상 Xbox 패드 생성 | `[TEST REMOTE PAD]` XInput device count > 0 | PENDING |
| C3 | RPCS3 Player 2 버튼 반응 | RPCS3 게임패드 설정에서 P2 입력 확인 | PENDING |
| C4 | Dragon's Crown 실행 | 런처 Remote Co-op 경로 | PENDING |
| C5 | 2P Start | 주점/캐릭터 선택에서 2P Start | PENDING |
| C6 | 2P 캐릭터 등장 | 화면 확인 | PENDING |
| C7 | P1/P2 각각 독립 조작 | 두 패드로 각각 이동 | PENDING |
| C8 | 두 캐릭터 동시에 움직임 | 동시 입력 | PENDING |
| C9 | 실제 전투 | 최소 1회 전투 | PENDING |
| C10 | 15분 이상 플레이 | 연속 플레이 | PENDING |
| C11 | 연결 종료 후 HOST 입력 정상 | 스트림 종료 후 P1 정상 | PENDING |
| C12 | **Local 2P regression 없음** | 기존 Local 2P 경로 재실행 | PENDING |

## D. REGRESSION (Helper 추가 후 기존 기능)

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| D1 | Solo (Standard) | 런처 PLAY | PASS (헬퍼와 독립 실행 파일) |
| D2 | Local 2P | 런처 Local | PENDING (2P 패드 필요) |
| D3 | Standard / Pro Enhanced | 런처 프리셋 | PASS (런처 재빌드 후 스모크) |
| D4 | RPCN UI | 런처 RPCN 화면 | PASS (런처 스모크) |
| D5 | Backup / Restore | 런처 TOOLS | PASS (기존 기능 유지) |
| D6 | P1 보존 | input config 확인 | PASS (P1 미변경) |

## G. RPCS3 설정 / 중복 인스턴스 Acceptance (§21)

실측 (2026-09-26, 1대 PC):

| CASE | 시나리오 | 기대 | 결과 |
|---|---|---|---|
| A | RPCS3 종료 상태 → Launcher → Controller Settings | 프로세스 1개 + 설정창 | **PASS** (`--rpcs3 controller`: started single new instance, UIA `RESULT=ok|Pads`, `Gamepad Settings` 창 열림) |
| B | RPCS3 메인창 실행 중 → Controller Settings | 기존 프로세스 재사용, 개수 1 유지, duplicate 경고 없음 | **PASS** (log `reuse pid=2296`, 프로세스 1개 유지, `Gamepad Settings` 열림) |
| C | Dragon's Crown 실행 중 → RPCS3 Settings | 두 번째 인스턴스 없음 + 기존 창 foreground + 설정창 또는 안전 fallback | **PASS** (프로세스 1개 유지, `gameRunning=True`, 게임 중 툴바 숨김 → 자동 열기 실패 시 foreground + "게임 종료 후 Pads" 안내) |
| D | 버튼 10회 빠른 클릭 (burst) | RPCS3 최대 1개 | **PASS** (`--rpcs3 burst` → `processes=1`) |
| E | 다른 폴더 RPCS3(KnownGood) 실행 중 | 우리 `<ROOT>\RPCS3\rpcs3.exe` 와 구분 | **PASS** (`--rpcs3 status` → `○ not running`; `--rpcs3 open` → 우리 것만 실행, KnownGood pid 유지) |

추가: PLAY 시 RPCS3 GUI 가 이미 실행 중이면 **경고(기본 No)** 후 진행 여부를 묻고,
기존 RPCS3 를 kill/restart 하지 않습니다.

## H. RPCS3 자동 업데이트 차단 실측

| 항목 | 결과 |
|---|---|
| 설정 변경 | `GuiConfigs\CurrentSettings.ini` 의 `[Meta] checkUpdateStart=false` **1줄만** 추가 (백업 대비 diff = 1줄, BOM/CRLF 유지) |
| 기준 빌드 | `0.0.42-20053-38eba804` (C:\ 원본 = KnownGood = 현재, SHA256 F3C58A95…) |
| 재실행 1회차 | 창 제목 20053 · exe 해시 불변 · `update_history.log` 신규 없음 · 정상 종료 후에도 설정 키 잔존 |
| 재실행 2회차 | 동일 (20053 유지, 신규 업데이트 없음) |
| 결론 | **PASS** — 설정 파일 수정만이 아니라 **2회 재실행 실측**으로 확인 |

> 참고: 업데이트 사고(20053→20058 @18:48, 20053→20063 @21:45)는 차단 조치 이전에 발생했으며,
> 차단 후에는 신규 업데이트가 없습니다.

## E. 결과 요약 템플릿

```text
=== REMOTE CO-OP ACCEPTANCE ===
HOST  A1~A13   : PASS / FAIL / PENDING (각 항목)
GUEST B1~B7    : PASS / FAIL / PENDING
E2E   C1~C12   : PASS / FAIL / PENDING
REG   D1~D6    : PASS / FAIL / PENDING
RPCS3 settings A~E : PASS / FAIL / PENDING (각 CASE)
FREE PATH          : PASS / FAIL / PENDING
날짜 / 담당 :
비고 (실패 시 분류 코드: SUNSHINE_* / GAMEPAD_* / MOONLIGHT_* / GUEST_* / RPCS3_P2_*)
```

## F. 기록 시 주의

* 실패 시 §8 Failure Classification 코드로 원인을 구분해 기록합니다.
* Sunshine/Moonlight 버전과 스트림 설정(해상도/FPS)을 함께 기록합니다.
* PIN / 계정 / 토큰은 기록하지 않습니다.
