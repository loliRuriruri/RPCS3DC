# PHASE 1 CLOSEOUT REPORT — Dragon's Crown PC Edition

작성일: 2026-09-26 · 태그: `v1.0.0-rc1` (코드 마감 + 자동 검증 완료)
런처 바이너리(RC2): `DragonCrownProEnhanced.exe` (63.24 MB) SHA256 `E904E350067E046580519CC9BF77B2A3774456E7555318A218C2A30BEA4EBD94`
후속 수정(RC2): 런처 ReShade 경로 자동 수정(다른 PC/폴더 지원) · RPCS3 자동 업데이트(20053→20058) 감지 후 기준 빌드 복원 + checkUpdateStart=false · 공유 패키지 재정비

```text
=== DRAGON'S CROWN PC EDITION — PHASE 1 CLOSEOUT ===

START SHA: de1281516e542ef59abd5c27251e9242da6daeab
END SHA:   tag `v1.0.0-rc1` 기준 커밋 (`git log --oneline -1`)

P0
[PASS] 300/400 GUI profile switching
[PASS] Netplay Safe ReShade hard-disable
[PASS] BCAS20298 / v1.09 preflight
[PASS] RPCN documentation corrected (RPCN_PARTIAL)

P1
[PASS] Diagnostics count/value fixes (런타임 카운트 · 프로필 실제값)
[PASS] Cheat/Patch enabled-state detection (존재↔활성, UNKNOWN 허용)
[PASS] Firewall dynamic path
[PASS] --restore-window (즉시 복원 + watcher 양보)
[PASS] Borderless launchPid targeting
[PASS] Root README (복원, 간결)

REGRESSION
[PASS] Standard
[PASS] Pro Enhanced
[PASS] 400%
[PASS] Borderless
[PASS] Backup
[PASS] Restore
[PASS] Reset
[PASS] KnownGood

USER ACCEPTANCE
[PENDING] 30min gameplay
[PENDING] Local 2P
[PENDING] Remote Co-op
[PENDING] RPCN 2-PC

PHASE STATUS:
Implementation Complete / Acceptance Pending

PS4 PHASE 2:
NOT STARTED
```

---

## P0 상세

### P0-1. 300% / 400% GUI 실제 연결

* 문제: GUI 에 4K/5K 선택이 있었지만 `resBox` 값이 APPLY/실행에 사용되지 않았다.
* 수정:
  * `ResolutionProfile=4K|5K` 저장(`settings.json`, 재실행 유지)
  * APPLY → 선택 즉시 반영, 상태창/진단에 실제값 표시
  * PLAY(Standard/Pro Enhanced) · Local · Remote · KnownGood 모두 선택 프로필 사용
  * CLI `--resolution 4K|5K` 추가 (기존 `--launch PROMAX` 동작 유지)
* 실측: 5K 선택 → 런타임 config `Resolution Scale: 400` → 창 제목 `FPS: 59.83~60.02`
* 진단: 4K 선택 시 `Resolution Scale (DC_PRO_4K) 300%`, 5K 선택 시 `(DC_PRO_MAX_5K) 400%`

### P0-2. Netplay Safe 실제 ReShade OFF 강제

* 문제: Safe 는 다른 config.yml 을 쓸 뿐, 사용자가 Pro Enhanced(ReShade ON) 상태면 Vulkan layer 가 남을 수 있었다.
* 수정: Safe 실행 시 `DISABLE_VK_LAYER_reshade_1=1` 로 **프로세스 runtime 차단** + `NETPLAY_SAFE` 프로필 + Safe 체크 기본 ON.
* 실측: 사용자 설정 `ReShade=1 (Pro Enhanced)` 상태에서 Safe 실행 → `ReShade.log` 크기 10134 → 10134 (레이어 미로드).

### P0-3. Safe 이후 사용자 그래픽 설정 보존

* 구현: Safe 는 **temporary runtime override** — `GraphicsPreset`/`ResolutionProfile`/`ReShade.ini` 를 변경하지 않는다.
* 실측: Safe 실행 전후 `settings.json` 동일(`GraphicsPreset=ProEnhanced`, `ResolutionProfile=5K`), 게임 종료 후에도 동일.

### P0-4. BCAS20298 / v1.09 엄격 검사

* 진단: `Game version (BCAS20298 v01.09)` 항목이 `== "01.09"` 로 판정.
* Preflight(GUI/CLI): RPCN/Netplay Safe 경로에서 버전 불일치 시 실행 차단 + 안내:
  `RPCN Online requires Dragon's Crown BCAS20298 v1.09. / Detected: ... / Apply the official v1.09 update before continuing.`
* 실측: 미설정 상태 `--launch RPCNSAFE` → `RPCN account is not configured` 로그, RPCS3 미실행(차단).

### P0-5. RPCN 문서 정정

* `RPCN_NETPLAY.md` / `RPCN_NETPLAY_GUIDE.md` / `MULTIPLAYER.md`:
  * "Untested(전 항목)" 표현 제거 → **`RPCN_PARTIAL`** 분류
  * 의미 명시: 연결 기능 존재 · 친구 기반 로비/합류 사례 · 실제 Match 부분 지원 · Random matchmaking 신뢰 안 함 · Connecting 멈춤 사례
  * "RPCN Guaranteed" 표현 금지, 성공 판정(같은 stage + 동시 조작 + 전투) 명시
  * fallback 순서 유지: Safe → 네트워크 점검 → local RPCN 진단 → Remote Co-op

## P1 상세

| 항목 | 결과 |
|---|---|
| Diagnostics | 하드코딩 숫자 제거(런타임 카운트, 현재 35항목). Resolution/Aspect/Stretch/AF/VSync/VBlank/FrameSkip 을 **선택된 프로필에서 실제로 읽어** 판정 |
| Cheat/Patch | `cheats.yml`/`patch_config.yml` 의 TitleId 블록에서 `Enabled: true/false` 파싱. 존재↔활성 분리, 판별 불가 시 **UNKNOWN**(잘못된 PASS 금지). Netplay 는 활성 감지 시 차단, UNKNOWN 은 경고 |
| RPCN 상태 | `Configured`(rpcn.yml NPID+token/password, 대소문자/빈 값 처리) 와 `Login`(로그에 기록 있을 때만, 아니면 `Not verified`) 분리 |
| Firewall 경로 | `Path.Combine(Root, "Tools", "firewall_rpcs3_allow.cmd")` — 프로젝트 이동 가능 |
| `--restore-window` | `RequestStopAndRestore()` — 즉시 복원 + stop flag 로 watcher 재적용 방지. 실측: 복원 후 8초 유지, 로그 `stop requested -> restoring and exiting watcher` |
| Borderless PID | launchPid 우선(프로세스 종료 시에만 fallback), titlebar 도 대상 PID 한정. 로그 `target pid=<launched pid>` |
| Backup/Restore | Root 밖 경로 skip 방어. before_restore / before_reset 자동 백업 유지 |
| 세이브 문구 | "live savedata/trophy 는 Reset 대상 아님", "AutoBackup 최신 20세대 보존" 으로 정확화 |
| README/문서 | 루트 README 복원(간결), `GRAPHICS_PROFILES.md` 재작성(중복 정정), `CHANGELOG.md` 통합 안내, `ACCEPTANCE_TEST.md` 신규 |

## 자동 검증 원자료

| 파일 | 내용 |
|---|---|
| `Logs\diag_closeout_4k.txt` | 4K 선택 진단 (35항목) |
| `Logs\diag_closeout_5k.txt` | 5K 선택 진단 (Scale 400 확인) |
| `Logs\launcher.log` | 실행/Safe/복원/PID 타겟팅 로그 |
| `Backups\20260926_163948_closeout_test` | 설정 백업 산출물 |
| `Saves\Dragons_Crown\AutoBackup\DC_SAVE_20260926_164013_cheat_offline` | CHEAT 실행 세이브 백업 |

## 남은 사용자 Acceptance

`Docs\ACCEPTANCE_TEST.md` 참조 — 30분 플레이 / Local 2P / Remote Co-op / RPCN 2-PC.
모두 완료되면 `PHASE 1 COMPLETE` 로 변경하고 최종 태그(`v1.0.0`)를 만든다.

## Phase 2

PS4 Dragon's Crown Pro 리소스 연구는 **시작하지 않았다** (텍스처/UI/음원/실행파일 분석 금지 유지).
