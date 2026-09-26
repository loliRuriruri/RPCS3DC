# ACCEPTANCE TEST — Dragon's Crown PC Edition (Phase 1)

작성일: 2026-09-26 · 상태 표기 규칙: **PASS / FAIL / PENDING** (세 가지 외 사용 금지)

이 문서는 Phase 1 최종 승인(Acceptance)을 위한 체크리스트다.
각 항목은 실제 실행 결과를 보고 판정하며, **PENDING 을 PASS 로 간주하지 않는다.**

결과 기록: `Docs\ACCEPTANCE_RESULTS.md` (복사해서 사용) · 증거: `Logs\`, 스크린샷, 캡처

---

## 0. 현재 상태 (코드 마감 시점)

| 구분 | 항목 | 상태 |
|---|---|---|
| 자동 검증 | Standard 300 / Pro Enhanced 300 / Standard 400 / Pro Enhanced 400 부팅 | **PASS** (타이틀 59.8~60.1fps) |
| 자동 검증 | Standard ReShade 미로드 / Pro 6종 로드 / Pro→Safe 강제 OFF | **PASS** |
| 자동 검증 | Borderless 적용·유지 / `--restore-window` 복원·유지 | **PASS** |
| 자동 검증 | Backup / Restore / Reset Graphics / Reset Multiplayer / KnownGood | **PASS** |
| 자동 검증 | RPCN 사전 검사 차단(미설정 시) · 진단 35항목 | **PASS** |
| 사용자 검증 | A~G 항목 | **PENDING** |

---

## A. Solo (혼자 플레이)

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| A1 | Standard 300% | 런처 → SETTINGS Graphics(Standard · 4K) → PLAY Standard | PENDING |
| A2 | Pro Enhanced 300% | PLAY Pro Enhanced (ReShade 문구 없어야 함) | PENDING |
| A3 | Standard 400% | SETTINGS → Resolution 5K → PLAY Standard | PENDING |
| A4 | Pro Enhanced 400% | PLAY Pro Enhanced (5K 선택 상태) | PENDING |

확인 포인트: 한국어 표시 · 저장/로드 · 컨트롤러 · 60fps · UI 깨짐 없음 · 글자 가독성

## B. Graphics

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| B1 | Standard 에서 ReShade 실제 미로드 | 실행 전후 `RPCS3\ReShade.log` 크기 비교(불변) | PENDING(자동 PASS, 육안 재확인) |
| B2 | Pro Enhanced 에서 로드 | `ReShade.log` 에 Deband/CAS/Levels/Vibrance/SMAA 컴파일 | PENDING(자동 PASS) |
| B3 | Standard → Pro 전환 | APPLY 후 재실행, 문구 없음 확인 | PENDING |
| B4 | Pro → Standard 전환 | APPLY 후 재실행, ReShade 미로드 | PENDING |
| B5 | Pro → RPCN Safe | Safe 실행 시 ReShade 강제 OFF + 종료 후 설정 유지 | PENDING(자동 PASS) |
| B6 | Home 키로 ReShade 메뉴 호출 | 게임 중 Home | PENDING |

## C. Display

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| C1 | Borderless 4K | PLAY (기본) → 테두리 없이 전체 화면 | PENDING(자동 PASS) |
| C2 | Fullscreen | SETTINGS → Display Fullscreen → PLAY | PENDING |
| C3 | Windowed | SETTINGS → Display Windowed → PLAY | PENDING |
| C4 | Borderless 종료 후 복원 | 게임 종료 → 창 테두리/크기 원복 | PENDING(자동 PASS) |
| C5 | `--restore-window` | `DragonCrownProEnhanced.exe --restore-window` → 즉시 복원, 재적용 없음 | PENDING(자동 PASS) |
| C6 | DPI 150% | Windows 배율 150% 에서 UI/게임 크기 정상 | PENDING |
| C7 | 마우스/입력 좌표 | Borderless 상태에서 메뉴 클릭 위치 일치 | PENDING |

## D. Local (같은 PC 2인)

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| D1 | 1P 정상 | 패드 1 로 플레이 | PENDING |
| D2 | 2P 정상 | 2P 패드 연결 → RPCS3 Player 2 설정 → 게임 내 Start 합류 | **PENDING (하드웨어 필요)** |

> 2P 패드가 없으면 `IMPLEMENTED / HARDWARE TEST PENDING` 으로 남긴다. "검증됨"으로 쓰지 않는다.

## E. Remote Co-op (Sunshine + Moonlight)

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| E1 | Sunshine host | HOST 에서 Sunshine 실행 + Moonlight 페어링(PIN) | **PENDING (Sunshine 설치 필요)** |
| E2 | Moonlight client | 친구 PC 에서 접속 | PENDING |
| E3 | 원격 패드 → Player 2 | 친구 패드 입력이 HOST 2P 로 인식 | PENDING |
| E4 | 게임 내 2P 합류 | 캐릭터 선택/Tavern 에서 2P Start | PENDING |

> Sunshine 이 없어도 실행은 가능하다(경고 후 계속). Parsec 등 대체 방법도 동일 절차로 검증 가능.

## F. RPCN (Native Online)

사전 조건 (두 PC 동일):

```text
PC A: BCAS20298 v1.09 · 같은 RPCS3 빌드 · RPCN 계정 A
PC B: BCAS20298 v1.09 · 같은 RPCS3 빌드 · RPCN 계정 B
첫 테스트: Standard + Netplay Safe(기본 ON) + ReShade OFF + 유선 LAN + VPN OFF
```

| # | 항목 | 방법 | 상태 |
|---|---|---|---|
| F1 | Login A/B | 각 PC 에서 RPCS3 → RPCN 로그인 | **PENDING (계정 필요)** |
| F2 | Friend registration | 서로 친구 등록 | PENDING |
| F3 | Host stage | A 가 방/스테이지 생성 | PENDING |
| F4 | Friend join | B 가 합류 | PENDING |
| F5 | **Actual multiplayer** | 같은 stage 에서 **두 캐릭터 동시 조작 + 최소 1회 전투** | PENDING |

> 성공 판정: 친구 목록 표시나 로비 표시만으로는 **성공이 아니다**.
> F5(동시 조작 + 전투)가 확인되어야 성공이다.

### RPCN 실패 시 분류 (§32)

실패하면 반드시 원인을 구분해서 기록한다:

```text
Launcher bug
RPCS3 bug
Dragon's Crown RPCN compatibility
NAT / firewall
network environment (VPN·이중 NIC·ISP)
```

보존할 로그:

```text
Logs\
  rpcn_client_A.log      (RPCS3.log 복사 + 메모)
  rpcn_client_B.log
  rpcn_test_notes.md     (시각, 증상, 시도한 설정, 분류)
```

### RPCN fallback 순서

```text
1. RPCN + Netplay Safe
      ↓ 실패
2. 네트워크 환경 점검 (유선 / VPN OFF / 방화벽 / UDP 3658)
      ↓ 실패
3. 필요 시 local RPCN 으로 진단
      ↓ 실패
4. Remote Co-op (Sunshine + Moonlight)
```

RPCN 이 게임/RPCS3 외부 호환성 문제로 실패하더라도,
**구현 결함 없음 + 재현 로그 확보 + Remote Co-op fallback 검증** 이면 Phase 1 완료를 검토할 수 있다.

## G. Stability (실제 플레이 30분)

| # | 항목 | 상태 |
|---|---|---|
| G1 | 최소 30분 연속 플레이 — 마을 / 던전 / 전투 / 다수 적 / 보스 / 아이템·메뉴 / 저장 포함 | **PENDING (사용자 플레이)** |

> 타이틀 화면 30분은 Acceptance 로 인정하지 않는다.
> 크래시/프리징/세이브 손상/입력 끊김/프레임 드랍 발생 시 시각과 상황을 기록한다.

---

## Phase 1 완료 조건 (§31)

다음이 모두 충족되어야 `PHASE 1 COMPLETE` 로 변경한다:

```text
[ ] P0 코드 결함 모두 수정 (300/400 연결 · Netplay Safe 강제 · v1.09 검사 · RPCN 문서)
[ ] P1 주요 결함 수정 (진단 정확도 · 경로 · restore-window · PID 타겟팅 · README)
[ ] Standard / Pro Enhanced 전환 정상
[ ] Borderless / 복원 정상
[ ] Backup / Restore / Reset 정상
[ ] 실제 30분 플레이
[ ] Local 2P 실제 확인
[ ] Remote Co-op 실제 확인
[ ] RPCN 실제 2-PC 시도 (+ 실패 시 분류·로그)
```

## 증거 기록 방법

| 증거 | 위치 |
|---|---|
| 진단 결과 | 런처 TOOLS → Diagnostics → 저장 (`Logs\diagnostics.txt`) |
| 실행 로그 | `Logs\launcher.log`, `Logs\borderless.log`, `RPCS3\log\RPCS3.log` |
| ReShade 로드 여부 | `RPCS3\ReShade.log` (크기/내용) |
| 창 상태 | `Logs\borderless_state.json` (적용 중) |
| 스크린샷 | F12 (RPCS3) — `RPCS3\screenshots\` (공유 패키지에는 미포함) |
| RPCN | `Logs\rpcn_client_A.log`, `rpcn_client_B.log`, `rpcn_test_notes.md` |

## 결과 요약 템플릿

```text
Standard 300       PASS / FAIL / PENDING
Pro 300            PASS / FAIL / PENDING
Standard 400       PASS / FAIL / PENDING
Pro 400            PASS / FAIL / PENDING
Borderless         PASS / FAIL / PENDING
Windowed           PASS / FAIL / PENDING
Fullscreen         PASS / FAIL / PENDING
Local 2P           PASS / FAIL / PENDING
Remote Co-op       PASS / FAIL / PENDING
RPCN Match         PASS / FAIL / PENDING
30min gameplay     PASS / FAIL / PENDING
```
