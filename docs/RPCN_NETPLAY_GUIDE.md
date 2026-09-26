# RPCN NETPLAY GUIDE — Dragon's Crown (BCAS20298)

작성일: 2026-09-26
대상 프로필: `E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml`
런처: `E:\PS3\Launchers\RPCS3_DRAGONS_CROWN_RPCN.cmd`

> **최종 분류: `RPCN_PARTIAL`** — 로그인/친구/로비까지는 공식·커뮤니티 근거상 가능성이 높지만,
> **RPCN 상태: PARTIAL (RPCN_PARTIAL)** — 연결 기능은 존재하고 친구 기반 로비/합류 사례가 있으나, 실제 Match 는 부분 지원이며 환경에 따라 “Connecting”에서 멈추는 사례가 있습니다. “RPCN Guaranteed” 로 표기하지 않습니다.
> 이 세션에서는 계정/상대 피어가 없어 실제 매치를 검증할 수 없었습니다(아래 §10 검증 한계).
> 실패 시 권장 순서: ① RPCN 재시도(§5 워크어라운드) → ② local RPCN(진단) → ③ **REMOTE_PLAY**.

---

## 1. 이 빌드에서의 정확한 설정 위치와 이름

### 1.1 GUI (RPCS3 → 설정 → **Network** 탭)

| GUI 항목 | 값 | config.yml 키 |
|---|---|---|
| Network Status | **Connected** | `Net: Internet enabled` |
| PSN Status | **RPCN** | `Net: PSN status` (`Disconnected` / `Simulated` / `RPCN`) |
| Enable UPNP | **해제** (Dragon's Crown 넷플레이 워크어라운드) | `Net: UPNP Enabled` |
| (고급, 커스텀 설정 전용) | 필요 시 활성 NIC IP 지정 | `Net: Bind address` (기본 `0.0.0.0`) |

### 1.2 RPCN 계정 파일 (정확한 경로)

* 파일: **`E:\PS3\RPCS3\config\rpcn.yml`** (portable 모드의 config 폴더)
* 키: `version`, `host`, `hosts`, `npid`, `password`, `token`, `ipv6_support`
* 기본 서버: `Official RPCN Server` → **`np.rpcs3.net`**, 기본 포트 **31313**
* 현재 상태: **파일 없음 = RPCN 계정 미설정** (로그: `RPCN config missing. Using default settings.`)

### 1.3 RPCN 다이얼로그 (계정/친구/메시지)

RPCS3 GUI 우측 상단의 **RPCN** 아이콘/메뉴 → `RPCN Account`, `Create Account`, `Log In`,
`Friends`, `Messages`, `Blocked Users`, `Server Settings`(hosts 목록 편집).

---

## 2. 계정 준비 (1회, 각 플레이어)

1. RPCS3 실행 → **RPCN** → `Create Account`
2. `Username`(npid), `Password`, `Online Name`, `Email` 입력 → 생성
   * npid는 영문/숫자 3~16자(대문자/소문자 구분), 온라인 이름은 게임 내 표시 이름
   * 이메일은 토큰 재발급/비밀번호 재설정용
3. 생성 후 `Log In` → 상단에 로그인 상태 표시
4. `Friends` → 상대 npid로 **친구 추가** (양쪽 모두 서로 추가해야 함)
5. (선택) `Server Settings` 에서 서버 목록 확인 — 기본은 Official RPCN Server 하나

> **주의**: 실제 PSN 계정/PSN 접속은 사용하지 않습니다. RPCN은 RPCS3 전용 서버입니다.

---

## 3. 버전 동기화 체크리스트 (양쪽 동일해야 함)

| 항목 | 확인 방법 | 기준값 |
|---|---|---|
| TITLE_ID | RPCS3 게임 목록 / 창 제목 | **BCAS20298** (지역판 혼용 금지: BLES01950·BLUS30767과 섞지 않음) |
| APP_VER | `dev_hdd0/game/BCAS20298/PARAM.SFO` (또는 게임 내 표기) | **01.09** |
| 업데이트 PKG | 동일 버전 사용 | `HP5017-…-A0109-V0100-PE.pkg` |
| RPCS3 build | 창 제목 | 동일 빌드 권장(예: `0.0.42-20053-38eba804`) |
| RPCN 설정 | §1 | Internet Connected / PSN RPCN / UPNP Off |
| 패치 | 패치 매니저 | **Dragon's Crown 패치 없음**(60 FPS·VBlank·game speed 패치 사용 금지) |
| 그래픽 | 프로필 | 300 % / AF Auto / AA Auto / WCB Off / Strict Off / ReShade Off (1차 검증) |

**온라인 해금 선행 조건**: Dragon's Crown은 기본 9개 스테이지를 완료해야 온라인(Stables/Gates)이 해금됩니다.
현재 세이브는 플레이타임 약 28분이므로 **온라인 해금 여부를 먼저 확인**해야 합니다(미해금이면 넷플레이 테스트 불가).

---

## 4. 테스트 순서 (성공 판정 기준 포함)

| 단계 | 내용 | 성공 판정 |
|---|---|---|
| 1 | 두 RPCS3 모두 RPCN 로그인 | 상단/다이얼로그에 로그인 상태 표시, 로그에 RPCN 연결/인증 성공 |
| 2 | 서로 친구 추가 | 양쪽 친구 목록에 상대 표시 |
| 3 | 동일 게임 버전 확인 | §3 체크리스트 전부 일치 |
| 4 | Host가 Dragon's Crown 실행 | 정상 부팅(로그 `Updates found at /dev_hdd0/game/BCAS20298/`) |
| 5 | Host가 실제 stage 진입 | 던전 내부에서 대기(마을/스테이블이 아니라 **던전 안**) |
| 6 | Guest가 Host에 합류 시도 | Stables/Gates → “Join a Friend” → Host 방 선택 |
| 7 | **실제 게임 플레이** | **두 캐릭터가 같은 화면에서 동시에 움직임** ← 이것만이 성공 |

* 친구 목록/방 목록이 보이는 것만으로는 성공이 아닙니다.
* Random Matchmaking 은 동작을 가정하지 않습니다 (공식 분류 PARTIAL).
* 실패 시 화면에 `Connecting.` 이 계속 표시되면 §5·§7을 확인하십시오.

---

## 5. 알려진 문제와 워크어라운드 (공식/커뮤니티 근거)

| 근거 | 내용 |
|---|---|
| RPCS3 Wiki — RPCN Compatibility List | Dragon's Crown 1.09 = **RPCN_PARTIAL** (연결 기능 존재 · 친구 기반 로비/합류 사례 · 실제 Match 부분 지원 · Random matchmaking 신뢰 안 함 · Connecting 멈춤 사례) · custom servers 불필요 |
| GitHub Issue **#15283** (2024-03-07, Dragon's Crown RPCN multiplayers failed to connect) | 친구 추가·방 목록 표시는 되지만 **join 시 `Connecting.` 에서 무한 대기**. **local RPCN + LAN에서도 동일 증상** 보고. 한 사용자는 **UDP 3658 포트 충돌**을 원인으로 지목하고 custom config에 `UDP Port: 3659`(한쪽)/`3658`(다른쪽) + `UPNP Enabled=false` 로 해결했다고 보고 |
| RPCS3 소스 (0.0.42-20053) | **`Net` 섹션에 `UDP Port` 키가 존재하지 않음** → 위 워크어라운드는 이 빌드에 그대로 적용 불가. RPCS3는 P2P 소켓용으로 **UDP 3658을 예약**(`lv2_socket_native.cpp`: “we don't support binding 3658 udp because we use it for the p2ps main socket”, `SCE_NP_PORT = 3658`) |

**실질적 결론**

1. RPCS3 **한 대에서 인스턴스 2개**로 Dragon's Crown 넷플레이를 검증하는 것은 부적합합니다(두 인스턴스가 같은 UDP 3658/P2P 자원을 두고 충돌).
2. **2대의 PC(각각 1개 인스턴스)** + 서로 다른 RPCN 계정으로 테스트하십시오.
3. 양쪽 모두 **UPNP Off**(이 프로필 기본값)로 시작하고, 필요 시 공유기에서 **UDP 3658 인바운드 포워딩**을 각 PC로 지정해 보십시오(포트 충돌 없는 환경에서의 대안).
4. 그래도 `Connecting.` 에서 멈추면 **local RPCN(§6)** 으로 “서버 문제 vs 게임/peer 문제”를 분리한 뒤, 실패 시 **REMOTE_PLAY(§7)** 로 전환하십시오.

---

## 6. 로컬 RPCN 서버 (진단 단계 전용)

* 프로젝트: `RipleyTom/rpcn` (RPCS3의 RPCN 서버 구현)
* 용도: 공식 서버 문제인지, 게임/peer 연결 문제인지 **분리 진단**
* 방법: 서버 실행 → RPCS3 `RPCN → Server Settings` 에 `내 서버|127.0.0.1:31313` 형식으로 host 추가 → 선택 후 로그인
* 한계(근거): Issue #15283 은 **local RPCN + LAN에서도** 방 목록은 보이지만 실제 접속이 실패했다고 보고합니다.
  즉 로컬 서버가 Dragon's Crown의 **peer(P2P) 연결 문제를 자동 해결하지 않습니다.**
* 따라서 local RPCN은 기본 구성이 아니라 **진단용**으로만 사용합니다(공식 서버 실패 시에만).

---

## 7. 대체 온라인 방식 (RPCN 아님)

Dragon's Crown PS3판은 **로컬(같은 콘솔) 4인 협동**을 지원합니다(스플릿스크린 없음, 추가 패드로 Start).
따라서 아래 방식은 “한 PC에서 로컬 멀티를 실행하고 화면/컨트롤러를 원격 전달”하는 접근입니다.

| 방식 | 장점 | 단점/주의 |
|---|---|---|
| **Parsec** | 지연 낮음, 게임패드 포워딩 지원, 설정 간단 | 계정 필요, 인터넷 업로드 대역폭 사용 |
| **Steam Remote Play Together** | Steam 친구 초대만으로 가능 | Steam 오버레이/입력 매핑 이슈, 지연은 Parsec보다 큼 |
| **Moonlight + Sunshine** | 매우 낮은 지연(NVIDIA 인코더), 컨트롤러 포워딩 | 호스트 설정 필요, RPCS3 창 캡처 설정 주의 |

* 로컬 2P는 **게임 시작 시 캐릭터 선택 화면** 또는 퀘스트 사이 **Tavern** 에서 2P 패드 Start 로 시작됩니다.
* 이 방식은 RPCN/온라인 기능이 아니라 **로컬 협동 + 원격 전달**임을 명확히 구분합니다.
* 로컬 2P에서는 트로피/세이브가 Player 1 기준으로만 기록됩니다(게임 설계).

---

## 8. 이 PC의 네트워크 진단 결과 (2026-09-26)

| 항목 | 측정/확인 결과 | 조치 |
|---|---|---|
| RPCN 서버 도달성 | `np.rpcs3.net` = 167.235.60.94, **TCP 31313 연결 성공(303 ms)** | 로그인 경로 정상 |
| 공인 IP / ISP | 27.232.201.103 / Korea Telecom(성남) — **현재 VPN 미사용** | 유지 |
| VPN 소프트웨어 | **NordVPN 설치됨**(NordLynx, OpenVPN DCO, TAP 어댑터, `nordvpn-service` 실행 중). 터널은 **미연결** | **테스트 전 NordVPN 완전 종료 + split tunneling 해제** (2025년 VPN split tunneling ↔ RPCN 충돌 사례) |
| 네트워크 어댑터 | **Wi-Fi(192.168.0.53) + 유선 Realtek 2.5GbE(192.168.0.52) 동시 연결(동일 서브넷)**, Hyper-V vEthernet(172.27.80.1) | **유선만 사용** 권장(Wi-Fi 비활성). 이중 NIC는 소스 인터페이스 혼선을 유발 |
| 기본 게이트웨이 | 192.168.0.1 (Wi-Fi/유선 metric 동일) | 위와 동일 |
| Windows 방화벽 | Domain/Private/Public **모두 Enabled**, rpcs3.exe 인바운드 규칙 없음 → **추가 완료(2026-09-26, 사용자 승인)** `RPCS3 (Private)` + `RPCS3 (Public)` 인바운드 Any 허용. 현재 네트워크가 **Public으로 분류**되어 있어 두 프로필 모두 등록 | 제거: `E:\PS3\Tools\firewall_rpcs3_remove.cmd` (관리자) / 재추가: `firewall_rpcs3_allow.cmd` |
| UPnP | `SSDPSRV` 실행 / `upnphost` 중지, RPCS3 `UPNP Enabled=false` | 공유기 UPnP 사용 여부와 무관하게 프로필은 Off |
| UDP 3658 (P2P) | 로컬에서 **미사용(충돌 없음)** | 한 PC에서 인스턴스 2개 테스트 시에만 충돌 |

**방화벽 규칙 스크립트(선택, 관리자 권한)**

```powershell
# 추가 (Private 프로필, rpcs3.exe 인바운드 TCP+UDP 허용)
New-NetFirewallRule -DisplayName "RPCS3 (Private)" -Direction Inbound -Program "E:\PS3\RPCS3\rpcs3.exe" -Protocol Any -Profile Private -Action Allow
# 제거
Remove-NetFirewallRule -DisplayName "RPCS3 (Private)"
```

---

## 9. 실측 로그 근거 (RUN8: RPCN 프로필 부팅 검증, 2026-09-26)

`E:\PS3\Logs\RUN8_RPCN_profile_no_account_RPCS3.log` 에서 확인된 사실:

```
SYS: Applied config override: E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml
Net:  PSN status: RPCN
sys_net: P2P port 3658 was bound!                                  ← 게임이 P2P(UDP 3658)를 실제로 바인딩
rpcn: RPCN config missing. Using default settings. Path: E:/PS3/RPCS3/config/rpcn.yml
cellSysmodule: load_module(): path="external/libsysutil_np_tus.sprx"
```

| 관찰 | 의미 |
|---|---|
| `PSN status: RPCN` 적용 | 프로필의 Net 설정이 정상 반영됨(YAML 오류 0) |
| **`P2P port 3658 was bound!`** | 게임이 부팅 시 NP/P2P 초기화를 수행. **동시에 두 인스턴스를 같은 PC에서 실행하면 이 포트가 충돌**함을 실측으로 확인 |
| `RPCN config missing` | 계정 파일 미존재(예상). GUI에서 계정 생성 후 이 로그가 사라져야 함 |
| NP 모듈(libsysutil_np_tus 등) 로드 | 네트워크 기능 초기화 경로 정상 |
| 45초 실행: FPS 60 유지, GPU 17.7 %, 오류 0 | RPCN 프로필 자체는 안정적 |

## 10. 검증 한계 (정직한 기록)

* 이 세션에서는 **RPCN 계정이 없고(파일 미존재), 상대 피어(2번째 플레이어/PC)도 없어** 로그인·친구·로비·매치를 실제로 검증할 수 없었습니다.
* 자동 세션에서 확인한 것: 설정 위치/키 이름, 프로필 생성, 서버 도달성, NAT/방화벽/VPN/이중 NIC 진단, 공식·커뮤니티 근거 수집.
* 확인하지 못한 것: RPCN 로그인 성공, 친구 등록, 로비 생성/참가, **실제 stage 동시 플레이**.

---

## 11. 최종 보고용 체크리스트 (사용자 테스트 후 채우기)
| 항목 | 결과 |
|---|---|
| RPCN login | (미검증 / 성공 / 실패) |
| Friend registration | (미검증) |
| Lobby creation | (미검증) |
| Lobby join | (미검증) |
| Stage join | (미검증) |
| Actual multiplayer | (미검증 — 성공 판정: 같은 stage에서 두 캐릭터 동시 이동) |
| Random matchmaking | 동작 가정 안 함(공식 분류 PARTIAL) |
| Direct friend join | 시도 순서 §4 |
| RPCN errors | 실패 시 `E:\PS3\Logs` 에 RPCS3.log 보관 |
| NAT/firewall findings | §8 (이중 NIC, 방화벽 규칙 없음, VPN 미연결 확인) |
| **Recommended netplay method** | ① 2대 PC + 유선 + VPN off + 방화벽 규칙 + RPCN(UPNP off) → ② 실패 시 local RPCN 진단 → ③ 실패 시 **REMOTE_PLAY(Parsec/Steam/Moonlight)** |

## 12. 출처

| 자료 | URL | 확인 내용 |
|---|---|---|
| RPCS3 Wiki — RPCN Compatibility List | https://wiki.rpcs3.net/index.php?title=RPCN_Compatibility_List | Dragon's Crown 1.09 = **RPCN_PARTIAL**, custom servers 불필요 |
| RPCS3 Wiki — Dragon's Crown | https://wiki.rpcs3.net/index.php?title=Dragon%27s_Crown | RPCN 표 포함 |
| GitHub Issue #15283 | https://github.com/RPCS3/rpcs3/issues/15283 | 로비 표시 O / join 실패, local RPCN·LAN에서도 동일, UDP 3658 포트 이슈 |
| RPCS3 소스 (0.0.42-20053) | `Emu/system_config.h`(Net 키), `Emu/NP/rpcn_config.cpp`(config 경로·기본 서버), `Emu/Cell/lv2/sys_net/lv2_socket_native.cpp`(3658 예약) | 설정 이름/위치, 서버 주소 |
| RPCS3 Wiki — Help:Netplay | https://wiki.rpcs3.net/index.php?title=Help:Netplay | RPCN 일반 절차 |
| Co-Optimus / IGN / GameFAQs | 로컬 4인 협동, 온라인 해금 조건(9 스테이지), 스테이블/게이트 join 절차 | 대체 방식 근거 |
| RipleyTom/rpcn | https://github.com/RipleyTom/rpcn | local RPCN 서버(진단용) |
