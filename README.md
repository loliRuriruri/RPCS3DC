# Dragon's Crown PRO Enhanced for RPCS3

**Dragon's Crown PRO Enhanced for RPCS3**는 PS3판 《Dragon's Crown》을 RPCS3에서
PS4판 《Dragon's Crown Pro》의 장점을 **가능한 범위까지** 반영한 PC용 Enhanced Edition처럼
사용할 수 있도록 구성한 **RPCS3 전용 환경 구축 프로젝트**다.

단순히 옵션 숫자를 최고로 올리는 프로젝트가 아니라,
4K/5K 최고화질 · Borderless · 안정적인 60fps · tearing 방지 · ReShade Silent · RPCN Netplay ·
Offline Cheat · Save Backup · Known Good RPCS3 fallback · 단일 GUI Launcher · 공유용 패키지를
**하나의 환경으로 통합**하는 것을 목표로 한다.

> 나무위키 스타일 정리본: [`namuwiki/RPCS3DC.txt`](namuwiki/RPCS3DC.txt)

---

## 1. 개요

| 항목 | 내용 |
|---|---|
| 대상 게임 | Dragon's Crown (PS3) |
| 확인된 TITLE_ID | **BCAS20298** (Asia, Zh/Ko) |
| 게임 버전 | **APP_VER 01.09** (공식 업데이트 적용) |
| PPU executable hash | **`PPU-bc3ee27f265ee62d1d26ebe2323b69384db5708b`** |
| RPCS3 | v0.0.42-20053-38eba804 (Alpha \| master) |
| Renderer | Vulkan (NVIDIA RTX 5080, 드라이버 616.56) |
| 원본 RPCS3 | 보존(수정하지 않음) — 복제본에서만 작업 |
| 작업 루트 | 사용자 지정 루트(예: `E:\PS3`) — 런처가 자기 위치 기준으로 자동 탐지 |

게임 이름만 보고 지역판을 추측하지 않는다. `PARAM.SFO` · RPCS3 게임 목록 · `RPCS3.log`에서
실제 **TITLE_ID / VERSION / APP_VER / PPU hash**를 읽어 기준으로 삼는다.

---

## 2. GUI Launcher (단일 실행 파일)

| 항목 | 내용 |
|---|---|
| 실행 파일 | `<ROOT>\Launcher\DragonCrownProEnhanced.exe` |
| 빌드 | **.NET 8 WPF · x64 · self-contained · single-file** (Python/런타임 별도 설치 불필요) |
| 소스 | [`launcher/src/`](launcher/src) |
| 빌드 명령 | `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o <ROOT>\Launcher` |
| 메인 화면 | **PLAY**(4K 300%) · **PRO MAX**(5K 400%) · **NETPLAY**(RPCN) · **CHEAT OFFLINE** + SAVES / GRAPHICS / SETTINGS / MAINTENANCE |
| 경로 독립 | exe 위치 기준 ROOT 자동 탐지(다른 드라이브에서도 동작), 게임 경로는 RPCS3 `games.yml` 에서 해석 |
| CLI | `--launch PLAY\|PROMAX\|NETPLAY\|CHEAT\|KNOWNGOOD`, `--backup`, `--restore-window`, `--status <파일>` |
| Legacy | 기존 개별 `.cmd` 런처는 삭제하지 않고 `Launcher\_Legacy\` 로 이동 |

> 저장소에는 소스만 두고 **63 MB 실행 파일은 포함하지 않는다**(git 저장소 비대화 방지).
> `launcher/src` 에서 위 명령으로 빌드하거나, 릴리스 자산으로 첨부해 배포한다.

### 프로필 (사용자에게 보이는 4개)

| 버튼 | 프로필 | Resolution Scale | 내부 해상도 | 네트워크 |
|---|---|---|---|---|
| PLAY | `DC_PRO_4K` | 300 % | 3840x2160 | Offline |
| PRO MAX | `DC_PRO_MAX_5K` | 400 % | 5120x2880 → 4K | Offline |
| NETPLAY | `DC_NETPLAY` | 300 % | 3840x2160 | RPCN |
| CHEAT OFFLINE | `DC_CHEAT_OFFLINE` | 300 % | 3840x2160 | Offline + Save 자동 백업 |

공통: Vulkan · 1280x720 · 16:9 · Shader High · Async Shader Mode · AA Auto · AF Auto ·
WCB/Depth/Strict Off · **VSync Full** · **VBlank 60** · **Frame Skip OFF** · Frame limit Auto ·
Borderless(RPCS3는 Windowed 유지, 게임 렌더 창만 Win32 스타일 제어) · ReShade Silent

---

## 3. 주요 특징

### 3.1 4K / 5K

* 기본 1280x720 + **Resolution Scale 300 %** → 내부 정확히 **3840x2160**
* PRO MAX = **400 %** → **5120x2880** 을 4K로 다운샘플(SSAA)
* 600 %(7680x4320)는 **스크린샷 전용**으로만 취급, 일상 기본값으로 쓰지 않는다
* 2D artwork 비중이 높으므로 **원본 Vanillaware artwork를 손상하지 않는 것**이 최우선,
  sharpening은 최소한만. PS3 asset을 5K/8K로 렌더링해도 PS4 Pro의 refined artwork가 되지는 않는다

### 3.2 Borderless 기본

* RPCS3는 **Vulkan Windowed**, **게임 렌더 창만** Win32 창 스타일 제어로 테두리 없이 모니터 전체
* Alt+Enter 미사용, 게임 종료 시 원래 창 스타일/크기로 복원
* GUI에서 Borderless / Fullscreen(fallback) / Windowed 선택, 기본값 Borderless

### 3.3 60fps / tearing

* VSync **Full** + Frame limit Auto + **VBlank 60** + Frame Skip OFF
* VBlank 120/240은 사용하지 않는다(게임 속도/로직 영향)
* FPS 60인데 끊기면 tearing이 아니라 frame pacing → RPCS3 성능 오버레이로 확인
* NVIDIA 제어판과 RPCS3에서 limiter/VSync를 중복 강제하지 않는다

### 3.4 ReShade Silent (+ ZERO-BANNER 선택)

```ini
[OVERLAY]
TutorialProgress=4
ShowClock=0
ShowFPS=0
ShowFrameTime=0
ShowPresetName=0
ShowScreenshotMessage=0
ShowPresetTransitionMessage=0
ShowForceLoadEffectsButton=0
[INPUT]
KeyOverlay=36,0,0,0
```

효과는 계속 적용되고 시작 시 메뉴/OSD는 표시되지 않으며 **Home** 키로 메뉴를 연다.
공식 splash까지 제거한 **ZERO-BANNER** 자체 빌드는 공식 v6.8.0 소스 + 최소 패치로 만들고
(`docs/ZERO_BANNER_REPORT.md`), 공식 바이너리는 백업해 한 번에 원복할 수 있게 한다.

### 3.5 RPCN Netplay

* RPCS3 자체 RPCN 기능 사용. 런처는 **상태 확인 · 설정 열기 · Netplay Ready 검사 · 프로필 실행**만 담당
* 계정/토큰은 런처에 저장하지 않고 RPCS3가 관리
* NETPLAY 프로필: Cheats OFF / Artemis OFF / 메모리 수정 OFF / 실험 패치 OFF / Save Editor OFF
* 첫 연결 테스트는 ReShade OFF 권장, 2대 PC + 각자 계정 필요

### 3.6 오프라인 치트

* 우선순위: **RPCS3 native Cheat Manager → Memory Viewer → Patch Manager → Artemis DB → 필요 시에만 Cheat Engine**
* Tier 1(Gold / Skill Point / Item Count / Score) 우선, 다른 지역판 주소 재사용 금지
* 현재 **Title ID / Version / PPU Hash** 와 일치할 때만 `Verified` 로 승격
* CHEAT OFFLINE 실행 시 **RPCN OFF 확인 + Save 자동 백업**(SHA256 manifest)

### 3.7 세이브 보호 / KnownGood

* `Saves\Dragons_Crown\{Original, AutoBackup, CheatTest}` 분리, Restore 전 현재 세이브 재백업
* `RPCS3\Current`(실사용) ↔ `RPCS3\KnownGood`(검증 완료 fallback), 업데이트 후 즉시 삭제하지 않음
* 승인 기준: Cold Boot 3회 · Town · Stage · Boss · Save · Load · 그래픽 이상 없음 · 30분 플레이 안정

---

## 4. 폴더 구조

```
<ROOT>\
├─ RPCS3\        Current(이 폴더) + KnownGood(스냅샷)
├─ Games\        게임 덤프(사용자 보유) — 배포물에 포함하지 않음
├─ Profiles\     Dragons_Crown\{PRO_4K, PRO_MAX_5K, NETPLAY, CHEAT_OFFLINE}
├─ ReShade\      Presets / Backup
├─ Cheats\       Dragons_Crown\{Verified, Experimental, Backup}
├─ Saves\        Dragons_Crown\{Original, AutoBackup, CheatTest}
├─ Patches\  Screenshots_AB\  Logs\  Backups\
├─ Launcher\     DragonCrownProEnhanced.exe (+ Internal, _Legacy)
└─ Docs\
```

---

## 5. 문서

| 문서 | 내용 |
|---|---|
| [`docs/DRAGONS_CROWN_PRO_ENHANCED.md`](docs/DRAGONS_CROWN_PRO_ENHANCED.md) | 프로젝트 총괄 |
| [`docs/GRAPHICS_PROFILES.md`](docs/GRAPHICS_PROFILES.md) | 그래픽/디스플레이·Borderless |
| [`docs/RPCN_NETPLAY.md`](docs/RPCN_NETPLAY.md) | RPCN 넷플레이 |
| [`docs/CHEAT_GUIDE.md`](docs/CHEAT_GUIDE.md) | 오프라인 치트 |
| [`docs/RESHade_GUIDE.md`](docs/RESHade_GUIDE.md) | ReShade Silent / ZERO-BANNER |
| [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md) | 문제 해결 |
| [`docs/CHANGELOG.md`](docs/CHANGELOG.md) | 변경 이력 |
| [`docs/ROLLBACK.md`](docs/ROLLBACK.md) | 원복 절차 |
| [`docs/TEST_PROTOCOL.md`](docs/TEST_PROTOCOL.md) | A/B 캡처·30분 검증 |
| [`docs/DRAGONS_CROWN_IDENTITY.md`](docs/DRAGONS_CROWN_IDENTITY.md) | TITLE_ID·버전·PPU hash |
| [`docs/DRAGONS_CROWN_4K_REPORT.md`](docs/DRAGONS_CROWN_4K_REPORT.md) | 4K/5K 측정 보고서 |
| [`docs/GRAPHICS_REGRESSION.md`](docs/GRAPHICS_REGRESSION.md) | black box / alpha 회귀 검사 |

---

## 6. 공유 패키지

`tools/build_share_package.ps1` → `DragonCrown_PRO_Enhanced_Setup.zip`

* 포함: 런처, 프로필 템플릿(PSID/사용자명 살균), ReShade 프리셋, 문서, 설치 가이드
* 제외: 게임 파일, PS3 펌웨어, DLC/라이선스, `rpcn.yml`, 계정/토큰, savedata, trophy,
  스크린샷, 로그, 개인 치트 세이브

---

## 7. 주의사항

* 원본 RPCS3 설치본과 `rpcs3.exe` 바이너리는 수정하지 않는다.
* save / trophy / dev_hdd0 / user data / game data / config / RPCN data / patch data 는
  임의 삭제하지 않고 변경 전 백업한다.
* PS4판 게임 데이터·음악·artwork를 인터넷에서 가져오지 않는다.
* Dragon's Crown PS3판은 원래 60 FPS 타이틀이므로 60 FPS 패치를 만들지 않는다.
  공식 patch DB에도 해당 타이틀 패치가 없다.
* 공식 Wiki는 이 타이틀에 대해 **기본 설정에서 벗어난 옵션을 권장하지 않는다**고 명시한다.
* 검은 반투명 box/배경 선(alpha) 이슈는 오래된 알려진 문제이며(#4133 / #9150 계열),
  “고칠 수 있는 설정”으로 가정하지 않고 **회귀 감시 대상**으로 관리한다.

---

## 8. 출처

* RPCS3 공식: <https://rpcs3.net> · <https://rpcs3.net/download> · <https://rpcs3.net/compatibility>
* RPCS3 GitHub: <https://github.com/RPCS3/rpcs3>
* RPCS3 Wiki: Dragon's Crown / Default Settings / RPCN Compatibility List
* RPCS3 Issues: #17200 · #17502 · #4133 · #9150 · #15283
* ReShade 공식: <https://reshade.me> · 소스 <https://github.com/crosire/reshade> (tag v6.8.0)
* Sony 공식 업데이트 서버(titlepatch XML / PKG), No-Intro PSN 업데이트 DB(RPCS3 공식 API)

---

## 9. 라이선스 / 고지

이 저장소에는 **프로젝트 자체 문서·스크립트·설정 템플릿**만 포함된다.
RPCS3, ReShade 등 제3자 프로그램의 바이너리와 게임 데이터, PS3 펌웨어는 포함하지 않으며
각 권리자의 라이선스를 따른다. 게임 데이터는 사용자가 직접 덤프한 것만 사용한다.
