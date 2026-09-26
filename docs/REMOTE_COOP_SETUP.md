# REMOTE CO-OP SETUP — Dragon's Crown PC Edition

작성일: 2026-09-26 · 대상: Remote Co-op Helper v1 (Phase 1 RC 이후 추가 도구)

친구 PC의 **실제 패드** 입력을 원격으로 HOST PC의 **RPCS3 Player 2** 에 전달해
Dragon's Crown 의 **로컬 2인 협동** 을 그대로 즐기는 구성을 도와주는 도구입니다.

```text
GUEST PC (친구)
  실제 패드
      ↓
  Moonlight
      ↓  LAN / Internet
HOST PC (게임 실행)
  Sunshine
      ↓
  가상 XInput 패드 (Xbox 360)
      ↓
  RPCS3 Player 2
      ↓
  Dragon's Crown Local Co-op
```

> 게임 입장에서는 기존에 검증된 **Local 2P 와 동일**하게 보입니다 (P1=HOST 실제 패드, P2=가상 XInput).

---

## 1. 산출물

| 파일 | 설명 |
|---|---|
| `Launcher\DragonCrownRemoteCoopSetup.exe` | Remote Co-op Helper v1 (.NET 8 WPF x64, self-contained single-file) |
| `remote-coop\build_remote_coop_setup.cmd` | 빌드 스크립트 (`<ROOT>\Launcher` 로 출력) |
| `remote-coop\src\*` | 소스 (14개 모듈) |
| `Logs\remote_coop_helper.log` | Helper 로그 (민감정보 없음) |
| `Backups\RemoteCoop\<timestamp>\` | Sunshine config / RPCS3 input / Helper 설정 백업 |

## 2. 실행 방법

```text
DragonCrownRemoteCoopSetup.exe            GUI (HOST 기본)
                             --host       HOST 화면
                             --guest      GUEST 화면
                             --status [f] KEY=VALUE 상태 (런처 연동용)
                             --diag [f]   HOST+GUEST 진단 (분류 코드 포함)
                             --test-pad [f]  XInput 원격 패드 입력 테스트 (8초)
                             --resolve [sunshine|moonlight|vigem]
                             --download [sunshine|moonlight|vigem]
                             --restore [sunshine|rpcs3|all]
```

기존 런처의 `MULTIPLAYER → Remote Co-op` 화면에서도 상태 확인과 `SETUP HOST` / `TEST REMOTE PAD` /
`START REMOTE CO-OP` 를 실행할 수 있습니다.

## 3. HOST (게임 실행 PC) 설정

### 3.1 준비물

| 항목 | 필수 | 비고 |
|---|---|---|
| Sunshine | ✅ | 공식 MSI (Helper 가 최신 stable 릴리스를 자동 해석/다운로드/검증) |
| Gamepad backend | ✅ | **Virtual HID Driver**(유료 라이선스) 또는 **ViGEmBus**(무료 legacy/EOL) |
| RPCS3 + 게임 | ✅ | 기존 Phase 1 설치 그대로 |
| 컨트롤러 (P1) | ✅ | 기존 HOST 패드 — **절대 변경되지 않음** |

### 3.2 순서

```text
1) DragonCrownRemoteCoopSetup.exe --host
2) [INSTALL / REPAIR HOST]
   - Sunshine 최신 stable MSI 다운로드 (SHA256 + GitHub digest + Authenticode 검증)
   - 대화형 설치 (silent 아님) → 설치 후 exe/service/config 확인
   - Controller-only 정책 적용:  controller=enabled / gamepad=x360 / keyboard=disabled / mouse=disabled
   - Sunshine 실행 + Web UI(https://localhost:47990) 열기
3) Sunshine Web UI 에서 본인 계정 생성 (Helper 는 비밀번호를 저장/출력하지 않음)
4) [INSTALL FREE LEGACY DRIVER (ViGEmBus)]  ← Virtual HID Driver 가 없을 때만, 사용자가 직접 선택
   - 공식 nefarius/ViGEmBus release 에서만 다운로드, legacy/EOL 안내 표시
5) [SET P2 TO XINPUT]  ← Player 2 가 Null 일 때만. input config 자동 백업 후 적용
   - Player 1 은 어떤 경우에도 변경하지 않습니다.
6) 친구가 Moonlight 로 접속한 뒤 [TEST REMOTE PAD] → "REMOTE CONTROLLER READY" 확인
7) [START REMOTE CO-OP] → 메인 런처의 Remote Co-op 경로로 게임 실행 → 게임 내 2P Start
```

### 3.3 상태 표시

```text
Sunshine          [Not Installed / Installed / Running]  Service / WebUI
Gamepad Backend   [Missing / ViGEmBus / Virtual HID]
Controller Input  [ON/OFF]   Keyboard/Mouse [OFF=controller-only]
Virtual Pad       [WAITING FOR GUEST / READY]
RPCS3             [Detected]
Player 1 / 2      [DualSense / XInput]
Guest             [Waiting / Connected]
HOST READY        [READY / NOT READY]
```

## 4. GUEST (친구 PC) 설정

**RPCS3 / PS3 펌웨어 / 게임 파일이 전혀 필요 없습니다.** Moonlight + 패드 + 네트워크만 있으면 됩니다.

```text
1) DragonCrownRemoteCoopSetup.exe --guest
2) [INSTALL MOONLIGHT]  ← 공식 moonlight-qt installer 다운로드/검증 후 대화형 설치
3) [TEST CONTROLLER]    ← 패드 입력 확인 (문제 시 [OPEN WINDOWS GAME CONTROLLERS])
4) Host Address 입력 → [ADD / PAIR HOST]
   - Moonlight GUI 가 열리고 PIN 이 표시됩니다.
   - HOST 의 Sunshine Web UI 에서 그 PIN 을 승인합니다. (PIN 은 저장/로그하지 않음)
5) [START DESKTOP STREAM] 또는 Moonlight 에서 HOST 선택
6) 권장 스트림 설정: 1080p / 60 FPS / Codec Auto / low latency 우선
   (Remote Co-op 은 화질보다 지연이 중요합니다. 4K 강제하지 않습니다)
```

## 5. 네트워크 범위 (v1)

| 환경 | 지원 |
|---|---|
| 같은 LAN | ✅ Fully Supported |
| 외부 인터넷 | ⚠️ Guided / Manual (포트포워딩/UPnP/Tailscale/ZeroTier 자동 구성 안 함) |

## 6. 보안 원칙

* 제3자 바이너리(Sunshine MSI, Moonlight installer, ViGEmBus, 드라이버)를 저장소/패키지에 **포함하지 않습니다.**
  항상 공식 GitHub Releases 에서 다운로드합니다.
* 다운로드 검증: HTTPS + owner/repo URL 확인 + 에셋 allow-list + SHA256(+GitHub digest) + Authenticode.
  검증 실패 시 실행하지 않습니다.
* 비밀번호 / PIN / 토큰 / 자격증명은 **읽지도, 저장하지도, 로그에 남기지도 않습니다.**
* Sunshine 계정은 사용자가 Web UI 에서 직접 만듭니다.
* 게임 savedata / trophy 는 이 도구가 수정하지 않습니다.

## 7. 백업 / 복구

| 대상 | 백업 위치 | 복구 |
|---|---|---|
| Sunshine config (`sunshine.conf`, `apps.json`) | `Backups\RemoteCoop\<시각>\Sunshine\` | `[RESTORE SUNSHINE CONFIG]` 또는 `--restore sunshine` |
| RPCS3 input config | `Backups\RemoteCoop\<시각>\RPCS3\input_configs\` | `[RESTORE RPCS3 INPUT]` 또는 `--restore rpcs3` |
| Helper 설정 | `Backups\RemoteCoop\<시각>\helper\settings.json` | 수동 복사 |

* 모든 변경(정책 적용, P2 설정)은 **변경 전 자동 백업** 후 진행합니다.
* 복원 시에도 현재 상태를 먼저 백업합니다.

## 8. Failure Classification

| 코드 | 의미 | 조치 |
|---|---|---|
| `SUNSHINE_NOT_INSTALLED` | Sunshine 미설치 | `[INSTALL / REPAIR HOST]` |
| `SUNSHINE_SERVICE_STOPPED` | 서비스 중지 | `[START SUNSHINE]` / `sc start SunshineService` |
| `SUNSHINE_WEBUI_UNAVAILABLE` | Web UI(47990) 접속 불가 | Sunshine 실행 확인 |
| `CONTROLLER_INPUT_DISABLED` | `controller=disabled` | Controller-only 적용 (controller=enabled) |
| `GAMEPAD_BACKEND_MISSING` | 가상패드 백엔드 없음 | Virtual HID(유료) 또는 ViGEmBus(무료) |
| `VIGEMBUS_MISSING` | ViGEmBus 없음 | `[INSTALL FREE LEGACY DRIVER]` |
| `VIRTUAL_HID_UNAVAILABLE` | Virtual HID 없음/미인증 | Sunshine Web UI 에서 라이선스 확인 |
| `MOONLIGHT_NOT_INSTALLED` | Moonlight 미설치 | `[INSTALL MOONLIGHT]` |
| `GUEST_CONTROLLER_MISSING` | 패드 미검출 | 패드 연결 후 `[TEST CONTROLLER]` |
| `HOST_NOT_PAIRED` | HOST 주소 없음 | 주소 입력 + `[ADD / PAIR HOST]` |
| `GUEST_NOT_CONNECTED` | 스트림 미접속 | Moonlight 접속 |
| `VIRTUAL_PAD_NOT_CREATED` | 가상패드 미생성 | 게스트 연결 + Sunshine 로그 확인 |
| `RPCS3_P2_NOT_CONFIGURED` | P2 미설정 | `[SET P2 TO XINPUT]` |
| `RPCS3_P2_NO_INPUT` | P2 입력 없음 | `[TEST REMOTE PAD]` 로 실제 입력 확인 |

## 9. 알려진 제약 (v1)

* **Virtual HID Driver 는 유료 라이선스**(연 $14.99 / 평생 $49.99)가 필요하며, 라이선스 상태는 Sunshine Web UI 에서만 확인됩니다.
  Helper 는 설치 여부만 감지하고 **자동 활성화하지 않습니다.**
* **ViGEmBus 는 legacy/EOL**(저장소 archived 2023-11)이며 Xbox 360 / DS4 만 지원합니다. 무료 fallback 입니다.
* v1 은 **Desktop 스트림**을 사용합니다 (앱 직접 실행 항목을 자동 등록하지 않음).
* 외부 인터넷 자동 구성(포트포워딩/UPnP/Tailscale)은 하지 않습니다.
* Sunshine/Moonlight 제거는 공식 uninstaller 를 사용합니다 (드라이버 파일 임의 삭제 금지).

## 10. 관련 문서

* `Docs\ACCEPTANCE_TEST.md` — Phase 1 전체 승인 체크리스트
* `Docs\REMOTE_COOP_ACCEPTANCE.md` — Remote Co-op 전용 승인 체크리스트 (E2E)
* `Docs\MULTIPLAYER.md` — Local / Remote Co-op / RPCN 개요
* `Docs\TROUBLESHOOTING.md` — 일반 문제 해결
