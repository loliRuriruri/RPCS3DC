# MULTIPLAYER — Local / Remote Co-op / RPCN Online

작성일: 2026-09-26 (Phase 1 Closeout) · 대상: Dragon's Crown PC Edition

런처 메인 화면의 **MULTIPLAYER** 섹션에서 세 가지 방식을 선택한다.

```text
MULTIPLAYER
 ├─ Local         : 1P / 2P 같은 PC
 ├─ Remote Co-op  : Sunshine + Moonlight (친구가 2P 패드를 원격 조작)
 └─ RPCN Online   : 각자 RPCS3 + 게임 + 캐릭터 + 세이브
```

---

## 1. Local Play (같은 PC 2인)

| 항목 | 내용 |
|---|---|
| 상태 | **IMPLEMENTED / HARDWARE TEST PENDING** (2P 패드 필요 — "검증됨"으로 표기하지 않음) |
| 필요한 것 | 2P 패드(또는 키보드/가상 패드), RPCS3 Player 2 핸들러 설정 |
| 게임 내 시작 | 캐릭터 선택 화면 또는 Tavern(주점)에서 2P 패드 **Start** |
| 세이브/트로피 | **Player 1 기준**으로만 기록(게임 설계) |
| 런처 지원 | MULTIPLAYER → Local → (2P 진단) → 선택된 해상도 프로필로 실행 |

* 2P 핸들러가 `Null`이면 런처가 경고한다. SETTINGS → Controller 에서 RPCS3 패드 설정을 연다.
* 로컬 협동은 스플릿스크린이 아니며 화면을 공유한다.

## 2. Remote Co-op (Sunshine + Moonlight)

```text
HOST PC
  Dragon's Crown → RPCS3
     ├─ Player 1 : Host Controller
     └─ Player 2 : Remote Virtual Controller  ←── Internet ── Friend Controller
```

| 항목 | 내용 |
|---|---|
| 상태 | **IMPLEMENTED / HARDWARE TEST PENDING** (Sunshine + 친구 PC 필요) |
| 장점 | RPCN/PSN 불필요 · 게임 온라인 기능 불필요 · 같은 세션 · 그래픽 MOD와 충돌 가능성 낮음 |
| 한계(문서 명시) | **친구가 자기 RPCS3 세이브/캐릭터를 쓰는 구조가 아니다.** 친구는 HOST 세션의 2P 입력만 담당한다 |
| 준비 | HOST: Sunshine 설치·실행 → Moonlight 페어링(PIN) → 2P 패드 설정 → 런처로 게임 실행 |
| 런처 동작 | Sunshine 이 실행 중이 아니면 **경고 후 계속 여부를 묻는다** (강제 차단하지 않음) |
| 대안 | Parsec, Steam Remote Play Together 도 동일한 개념으로 사용 가능 |

> Sunshine 은 **권장** 방법이며 절대적 종속성이 아니다. 다른 원격 입력 도구를 써도 된다.

## 3. RPCN Native Online

```text
Player A: RPCS3 + Game + 자기 캐릭터/세이브/화면
Player B: RPCS3 + Game + 자기 캐릭터/세이브/화면
        └────── RPCN (np.rpcs3.net:31313) ──────┘
```

| 항목 | 내용 |
|---|---|
| 상태 | **RPCN_PARTIAL — requires real 2-PC validation** |
| 의미 | RPCN 연결 기능 존재 · 친구 기반 로비/합류 가능 사례 존재 · 실제 Match 는 부분 지원 · Random matchmaking 은 신뢰하지 않음 · 환경에 따라 Connecting 에서 멈추는 사례 존재 |
| 금지 표현 | "RPCN Guaranteed" / "완전 지원" 같은 표현은 사용하지 않는다 |
| 계정 | 각자 별도 RPCN 계정. RPCS3 → RPCN → Create Account |
| 런처 역할 | 상태 확인 · 설정 화면 열기 · Ready 검사 · 프로필 실행만 (계정/토큰 저장 안 함) |
| 버전 동기화 | 양쪽 모두 **TITLE_ID BCAS20298** + **APP_VER 01.09** + 같은 RPCS3 빌드 권장 |
| 강제 검사 | 런처가 **v01.09 가 아니면 실행을 차단**한다 (RPCN / Netplay Safe 경로) |
| 금지 | 치트/Artemis/메모리 수정/실험 패치/Save Editor (런처가 활성 항목 발견 시 경고) |
| ReShade | 사용 가능하나 **첫 연결 테스트에서는 OFF** (Netplay Safe 기본 ON) |
| 순정 유지 | EBOOT/네트워크 코드/스크립트/게임 로직은 수정하지 않는다 |

### Netplay Safe (기본 ON 권장)

RPCN 문제 발생 시 원인 분리를 위해 **NETPLAY_SAFE** 를 사용한다. 런처 RPCN 화면에서 기본 체크되어 있다.

| 강제 항목 | 값 |
|---|---|
| Graphics preset | Standard (일시 적용) |
| ReShade | **runtime 강제 OFF** (`DISABLE_VK_LAYER_reshade_1=1` — 프리셋 변수만 바꾸는 것이 아님) |
| 프로필 | `NETPLAY_SAFE` (300% · AF 16 · VSync Full · VBlank 60 · Frame Skip Off) |
| 네트워크 | RPCN On · Internet Connected · UPNP Off · Clans Off · Bind 0.0.0.0 |
| 실험 옵션 | WCB/Depth/Read/Strict Off · MTRSX Off · Async Shader · Bilinear |
| 사용자 설정 | **변경하지 않음** (게임 종료 후 기존 Pro Enhanced/5K 등 그대로) |

### 네트워크 권장 사항

* 유선 LAN만 사용(Wi-Fi 비활성) — 이중 NIC 환경에서 소스 인터페이스 혼선 방지
* VPN(NordVPN 등) 완전 종료 — split tunneling 충돌 사례
* 방화벽: `RPCS3 (Private)/(Public)` 인바운드 허용 규칙(SETTINGS → Network 에서 실행)
* UPNP Off 유지, 필요 시 공유기에서 UDP 3658 포워딩
* 알려진 제약: 친구/방 목록은 보이나 실제 접속이 실패하는 사례가 보고됨(#15283)

### 성공 판정 기준

```text
친구 목록 표시     = 성공 아님
로비 표시          = 성공 아님

성공 = 두 플레이어가 실제 같은 stage 에 입장
      + 동시에 캐릭터 조작
      + 최소 한 전투 수행
```

### RPCN 실패 시 fallback 순서

```text
1. RPCN + Netplay Safe
      ↓ 실패
2. 네트워크 환경 점검 (유선 / VPN OFF / firewall / UDP 3658)
      ↓ 실패
3. 필요 시 local RPCN 으로 진단
      ↓ 실패
4. Remote Co-op (Sunshine + Moonlight)
```

실패 시 원인을 분류해 기록한다: `Launcher bug / RPCS3 bug / 게임 RPCN 호환성 / NAT·방화벽 / 네트워크 환경`
(로그: `Logs\rpcn_client_A.log`, `rpcn_client_B.log`, `rpcn_test_notes.md`)

## 4. 테스트 매트릭스 (Phase 1)

| 조합 | 상태 |
|---|---|
| Standard + Solo | ✅ 자동 검증(런처 실행·AF16·ReShade OFF) |
| Pro Enhanced + Solo | ✅ 자동 검증(ReShade 6종 컴파일·Silent) |
| Standard + Local 2P | ⏳ 2P 패드 필요 (진단에서 경고 표시) |
| Pro Enhanced + Local 2P | ⏳ 동일 |
| Standard + Remote Co-op | ⏳ Sunshine 설치 필요 |
| Pro Enhanced + Remote Co-op | ⏳ 동일 |
| Standard + RPCN | ⏳ RPCN 계정 + 2대 PC 필요 |
| Pro Enhanced + RPCN | ⏳ 동일 (권장: 첫 테스트는 Standard + Netplay Safe) |

## 5. 하지 않는 것 (Phase 1 범위 밖)

* Private RPCN 서버(Docker/Tailscale/ZeroTier), 자체 matchmaking server 구축 — 필요성 확인 전까지 보류
* PS4 Dragon's Crown Pro 리소스(텍스처/UI/음원) 이식 — **Phase 2** 에서만
* EBOOT/네트워크 코드/게임 로직 수정 — 금지

## 6. 참고

* 상세 절차/기록 양식: `Docs\ACCEPTANCE_TEST.md`
* RPCN 가이드: `Docs\RPCN_NETPLAY.md`, `Docs\RPCN_NETPLAY_GUIDE.md`
* 그래픽 프리셋: `Docs\GRAPHICS_PROFILES.md`
