# Dragon's Crown PRO Enhanced for RPCS3

**Dragon's Crown PRO Enhanced for RPCS3**는 PS3판 《Dragon's Crown》을 RPCS3에서
PS4판 《Dragon's Crown Pro》의 장점을 **가능한 범위까지** 반영한 PC용 Enhanced Edition처럼
사용할 수 있도록 구성한 **RPCS3 전용 환경 구축 프로젝트**다.

단순히 옵션 숫자를 최고로 올리는 프로젝트가 아니라,
4K/5K 화질 · Borderless · 60fps · tearing 방지 · ReShade Silent · RPCN 넷플레이 ·
오프라인 치트 · 세이브 백업 · KnownGood 폴백 · 단일 GUI 런처를 **하나의 환경으로 통합**하는 것을
목표로 한다.

---

## 1. 개요

| 항목 | 내용 |
|---|---|
| 프로젝트명 | Dragon's Crown PRO Enhanced for RPCS3 |
| 대상 게임 | Dragon's Crown (PS3) — 실제 확인된 TITLE_ID 기준 |
| 확인된 TITLE_ID | **BCAS20298** (Asia, Zh/Ko) |
| 게임 버전 | **APP_VER 01.09** (공식 업데이트 적용) |
| PPU executable hash | **`PPU-bc3ee27f265ee62d1d26ebe2323b69384db5708b`** |
| RPCS3 | v0.0.42-20053-38eba804 (Alpha \| master) |
| Renderer | Vulkan (NVIDIA RTX 5080, 드라이버 616.56) |
| 원본 RPCS3 | 보존(수정하지 않음) — 별도 복제본에서만 작업 |
| 작업 루트 | 사용자가 지정한 루트(예: `E:\PS3`) — 런처가 자기 위치 기준으로 자동 탐지 |

> 게임 이름만 보고 지역판을 추측하지 않는다. `PARAM.SFO` · RPCS3 게임 목록 · `RPCS3.log`에서
> **실제 TITLE_ID / VERSION / APP_VER / PPU hash**를 읽어 기준으로 삼는다.

---

## 2. 특징

### 2.1 4K / 5K 최고화질

* 기본 해상도 1280x720 유지 + **Resolution Scale 300 %** → 내부 정확히 **3840x2160**
* 최고화질 프로필은 **400 %** → 내부 **5120x2880** 을 4K로 다운샘플(SSAA)
* 600 %(7680x4320)는 **스크린샷 전용** 프로필로만 취급하며 일상 플레이 기본값으로 쓰지 않는다
* Dragon's Crown은 2D artwork 비중이 높으므로 **원본 Vanillaware artwork를 손상하지 않는 것**을
  최우선으로 하고, sharpening은 최소한만 사용한다
* PS3 asset을 5K/8K로 렌더링해도 **PS4 Pro의 refined artwork와 동일해지지는 않는다**

### 2.2 Borderless 기본화

* 기본 Display Mode는 **Borderless**
* RPCS3는 **Vulkan Windowed** 상태로 실행하고, **게임 렌더 창만** 정확히 찾아
  Win32 창 스타일 제어로 테두리 없이 모니터 전체(자동 탐지된 실제 해상도)로 전환한다
* Alt+Enter를 강제로 보내는 방식은 사용하지 않는다
* 게임 종료 시 **원래 창 스타일/크기로 자동 복원**한다
* GUI에서 **Borderless / Fullscreen / Windowed** 3종을 제공하며 기본값은 Borderless
* Fullscreen(RPCS3 자체 전체화면)은 **fallback**으로 유지한다

### 2.3 60fps / tearing 방지

* Renderer Vulkan, **Frame limit Auto**, **VSync ON**, **VBlank 60**, Frame Skip OFF
* tearing 해결을 위해 VBlank를 120/240으로 올리는 방식을 쓰지 않는다
  (게임 속도/로직에 영향을 줄 수 있으므로 Dragon's Crown에서는 60 유지)
* FPS가 60인데 미세하게 끊기면 tearing이 아니라 **frame pacing** 문제일 수 있으므로
  RPCS3 성능 오버레이(FPS / frame time / GPU / VRAM)로 확인한다
* NVIDIA 제어판·NVIDIA App·RPCS3에서 FPS limiter/VSync를 **여러 겹으로 강제하지 않는다**

### 2.4 ReShade Silent (메뉴/OSD 숨김)

* ReShade 효과(Deband + 약한 CAS 등)는 계속 적용하되, 게임 시작 시
  **설정 메뉴/튜토리얼/OSD가 화면에 나타나지 않게** 한다
* `ReShade.ini`의 `[OVERLAY]` 값을 조정해 **TutorialProgress=4**, 각종 OSD 표시를 끈다
* **Home 키로 메뉴를 열 수 있어야** 하며 메뉴 기능 자체를 삭제하지 않는다
* 기대 동작: 게임 시작 → 메뉴 없음 → 효과 자동 적용 → Home → 메뉴 표시 → Home/Esc → 닫힘

### 2.5 ZERO-BANNER ReShade (선택)

* 공식 ReShade가 시작 시 잠깐 표시하는 startup/loading splash까지 제거한 **자체 빌드**
* 인터넷에서 오래된 DLL을 받아 교체하는 방식이 아니라,
  **동일 버전의 공식 crosire/reshade 소스**에 startup/splash 관련 **최소 변경만** 적용해
  x64 Release로 직접 빌드한다
* 유지: effect runtime · preset · shader compile · Vulkan hook · Home menu · error logging
* 제거: startup splash/banner UI만
* 공식 바이너리는 백업해 두고 스크립트 한 번으로 원복할 수 있게 한다

### 2.6 RPCN 넷플레이

* RPCN을 자체 구현하지 않고 **RPCS3 자체 RPCN 기능**을 사용한다
* 런처는 **설정 상태 확인 · 설정 화면 열기 · Netplay Ready 검사 · NETPLAY 프로필 실행**만 담당한다
* RPCN ID/password/token은 런처 DB에 저장하지 않고 **RPCS3가 관리**하게 한다
* NETPLAY 프로필에서는 **Cheats OFF / Artemis OFF / 메모리 수정 OFF / 실험 패치 OFF / Save Editor OFF**
* ReShade는 사용할 수 있으나 **첫 연결 테스트에서는 OFF**로 시작한다

### 2.7 오프라인 치트

* 치트는 **오프라인 전용**이며 우선순위는
  **① RPCS3 native Cheat Manager → ② Memory Viewer → ③ Patch Manager → ④ Artemis DB → ⑤ 필요 시에만 Cheat Engine**
* Cheat Engine을 기본 기능으로 사용하지 않는다
* Tier 1: Gold / Skill Point / Item Count / Score → Tier 2: EXP / Level / Durability / Attack / Defense
  → Tier 3: MP / 전투 중 runtime value 순으로 진행하고 **Tier 1을 먼저 완성**한다
* 과거 다른 지역판(BLUS30767 v1.00 등) 주소를 현재 버전에 그대로 사용하지 않는다
* 현재 실제 **Title ID / Version / PPU Hash**와 일치할 때만 `Verified`로 승격한다

### 2.8 세이브 보호

* 치트 사용 시 자동으로 **RPCN OFF 확인 → Save Backup → Cheat 프로필 적용**
* 백업은 timestamp 폴더로 생성하고 SHA256 검증 + manifest를 남긴다
* **원본 세이브로 직접 실험하지 않는다** (Original / AutoBackup / CheatTest 분리)
* Restore 전에 현재 세이브도 추가 백업하여 **한 번 더 되돌릴 수 있게** 한다

### 2.9 Current / KnownGood RPCS3

* `RPCS3\Current` = 현재 사용 버전, `RPCS3\KnownGood` = 검증 완료된 fallback
* RPCS3 업데이트 직후 KnownGood를 삭제하지 않는다
* 승인 기준: **Cold Boot 3회 · Town · Stage · Boss · Save · Load · 그래픽 이상 없음 · 30분 플레이 안정**
  을 모두 통과해야 Current를 승인한다

### 2.10 단일 GUI Launcher

* 일반 사용자는 **실행 파일 하나**만 사용한다
* 메인 화면: `PLAY (4K PRO)` / `PRO MAX (5K SSAA)` / `NETPLAY` / `CHEAT OFFLINE`
  + 하단 `SAVES` / `GRAPHICS` / `SETTINGS` / `MAINTENANCE`
* KnownGood 실행은 메인에 노출하지 않고 **MAINTENANCE** 안에 둔다
* PLAY는 **v1.09 확인 + Current RPCS3 + Vulkan + 4K 300 % + Borderless + VSync + VBlank 60 + ReShade Silent**
  를 자동 적용해 사용자가 매번 옵션을 고르지 않아도 되게 한다
* 루트 경로(`E:\PS3` 등)를 하드코딩하지 않고 **런처 위치 기준 상대 경로**를 사용한다
  (다른 사람의 `D:\PS3` 등에서도 동작)

---

## 3. 폴더 구조

```
<ROOT>\
├─ RPCS3\
│   ├─ Current\        현재 사용 버전
│   └─ KnownGood\      검증 완료 fallback
├─ Games\              게임 덤프(사용자 보유)
├─ Profiles\           프로필 (PRO_4K / PRO_MAX_5K / NETPLAY / CHEAT_OFFLINE)
├─ ReShade\            Presets / Backup
├─ Cheats\             Verified / Experimental / Backup
├─ Saves\              Original / AutoBackup / CheatTest
├─ Patches\
├─ Screenshots_AB\
├─ Logs\
├─ Backups\
├─ Launcher\           단일 GUI 런처 (+ Internal / _Legacy)
└─ Docs\
```

---

## 4. 프로필

사용자가 인식하는 프로필은 **4개**뿐이다.

| 버튼 | 프로필 | Resolution Scale | 내부 해상도 | 용도 |
|---|---|---|---|---|
| PLAY | `DC_PRO_4K` | 300 % | 3840x2160 | 일상 플레이 기본 |
| PRO MAX | `DC_PRO_MAX_5K` | 400 % | 5120x2880 → 4K | 최고 화질(차이가 확인될 때만) |
| NETPLAY | `DC_NETPLAY` | 300 % | 3840x2160 | RPCN, 치트/실험패치 OFF |
| CHEAT OFFLINE | `DC_CHEAT_OFFLINE` | 300 % | 3840x2160 | RPCN OFF + Save 자동 백업 |

공통: Vulkan · 1280x720 유지 · 16:9 · Shader Quality High · Async Shader Mode ·
AA Auto · AF Auto(개선 확인 시 16x) · WCB/Depth/Strict **Off 기본** · Frame Skip Off ·
Frame limit Auto · **VSync ON** · **VBlank 60** · Borderless · ReShade Silent

> 호환성 옵션(WCB, Read Color/Depth, Strict Rendering 등)은 **문제가 실제 발생할 때만** 켠다.

---

## 5. 사용법

1. `Launcher\DragonCrownProEnhanced.exe` 실행
2. 최초 1회 루트 경로 확인(자동 탐지 실패 시에만 지정)
3. **PLAY** → 게임 실행 (v1.09 확인 → Current RPCS3 → 4K 300 % → Borderless → VSync → ReShade Silent)
4. 최고 화질은 **PRO MAX**, 온라인은 **NETPLAY**, 치트는 **CHEAT OFFLINE**
5. 문제 발생 시 **MAINTENANCE → KnownGood** 로 폴백

---

## 6. 검증 결과 (요약)

| 항목 | 결과 |
|---|---|
| 부팅 | CLEAN/4K/5K/6K/8K/RPCN/ReShade 전 구성 정상, fatal·행 없음 |
| 콜드부트 | PPU/SPU LLVM 캐시 제거 콜드부트 포함 3회 통과 |
| FPS | 100/300/400/500/600 % 전 구간 **59.5~60.1 fps** |
| GPU | 400 %에서도 사용률 10 % 내외(여유 큼), ReShade 시 +20 %p |
| VRAM | 300 % ≈ +0.5 GB, 400 % ≈ +0.24 GB (게임 증가분) |
| Borderless | 게임 창 3840x2160 테두리 없음 적용/복원 실측 확인, FPS 영향 없음 |
| ReShade | 공식 6.8.0.2155 ↔ ZERO-BANNER 6.8.0.1 A/B 모두 이펙트 4/4 컴파일·무크래시 |
| RPCN | 서버 도달성 확인, 프로필/가드 구축(실제 매치 검증은 2대 PC 필요) |
| 치트 | Cheat Manager 존재 확인, Gold/SP 절차·기록 양식·검증기 구축(주소 검색은 게임 플레이 필요) |
| 그래픽 회귀 | black box/alpha 이슈 근거 조사 + 장면 7종·baseline 절차 문서화 |

---

## 7. 주의사항

* 원본 RPCS3 설치본을 **직접 개조하지 않는다**. `rpcs3.exe` 바이너리도 수정하지 않는다.
* save / trophy / dev_hdd0 / user data / game data / config / RPCN data / patch data 는
  **임의 삭제하지 않는다.** 변경 전 반드시 백업한다.
* PS4판 게임 데이터·음악·artwork를 인터넷에서 가져오지 않는다.
  (PS4 Pro 오케스트라 OST 등은 **사용자가 합법적으로 보유한 자료가 있을 때만** 연구 대상)
* 공유 패키지에는 게임 파일·펌웨어·DLC/라이선스·`rpcn.yml`·계정/토큰·savedata·trophy·
  스크린샷·로그·개인 치트 세이브를 **포함하지 않는다**.
* Dragon's Crown은 로컬 4인 협동을 지원하므로, RPCN이 불안정하면
  Parsec / Steam Remote Play / Moonlight+Sunshine 같은 **원격 플레이 방식**을 대안으로 쓸 수 있다
  (이것은 RPCN이 아니라 로컬 협동 + 원격 컨트롤러 전달이다).

---

## 8. 여담

* Vanillaware 특유의 타일 기반 렌더링 + alpha 처리 때문에 **검은 반투명 박스/배경 선** 이슈가
  오래 보고되어 왔다(RPCS3 Wiki Known Issues, GitHub #4133 / #9150 계열). 이 프로젝트는 이를
  "고칠 수 있는 설정"으로 가정하지 않고 **회귀 감시 대상**으로 관리한다.
* Dragon's Crown PS3판은 원래 **60 FPS 타이틀**이므로 60 FPS 패치를 만들지 않는다.
  공식 patch DB에도 해당 타이틀의 패치가 존재하지 않는다.
* 공식 Wiki는 이 타이틀에 대해 **기본 설정에서 벗어난 옵션을 권장하지 않는다**고 명시하고 있다.

---

## 9. 출처

* RPCS3 공식: <https://rpcs3.net> · <https://rpcs3.net/download> · <https://rpcs3.net/compatibility>
* RPCS3 GitHub: <https://github.com/RPCS3/rpcs3> (릴리스 태그 v0.0.42, 커밋 단위로 최신 확인)
* RPCS3 Wiki: Dragon's Crown / Default Settings / RPCN Compatibility List
* RPCS3 Issues: #17200 (BLES01950 SPU GETLLAR 부팅 회귀, 수정됨) · #17502 (BLUS30767 부팅 불가, 종료) ·
  #4133 / #9150 (alpha/블랙박스) · #15283 (RPCN 접속 실패, UDP 3658 이슈)
* ReShade 공식: <https://reshade.me> · 소스 <https://github.com/crosire/reshade> (tag v6.8.0)
* Sony 공식 업데이트 서버(titlepatch XML / PKG) 및 No-Intro PSN 업데이트 DB(RPCS3 공식 API)

---

## 10. 라이선스 / 고지

이 저장소에는 **프로젝트 자체 문서·스크립트·설정 템플릿만** 포함된다.
RPCS3, ReShade 등 제3자 프로그램의 바이너리와 게임 데이터, PS3 펌웨어는 포함하지 않으며
각 권리자의 라이선스를 따른다. 게임 데이터는 사용자가 직접 덤프한 것만 사용한다.
