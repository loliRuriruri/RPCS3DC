# LAUNCHER / BORDERLESS 4K — Dragon's Crown (RPCS3)

작성일: 2026-09-26
목적: Dragon's Crown GUI 런처에 **Borderless 4K** 를 기본 디스플레이 모드로 추가하고,
RPCS3 자체는 Vulkan **Windowed** 상태로 유지한 채 **게임 렌더 창만** 외부 Win32 창 스타일 제어로
테두리 없는 모니터 전체(4K) 창으로 전환한다.

---

## 1. 실행 방법

| 방법 | 파일 |
|---|---|
| **GUI 런처** (권장) | `E:\PS3\Launchers\Dragon_Crown_Launcher.cmd` |
| Borderless 4K 바로 실행 (CLI) | `E:\PS3\Launchers\Dragon_Crown_Borderless_4K.cmd` [프로필] [모드] |
| 기존 4K 런처(창모드/기존 방식) | `Dragon_Crown_4K_ULTRA.cmd`, `Dragon_Crown_5K_SSAA.cmd` 등 그대로 유지 |

GUI 런처 구성:

```
[디스플레이 모드]
  (●) Borderless 4K  (기본) - RPCS3는 Vulkan Windowed 유지, 게임 창만 테두리 없이 모니터 전체(3840x2160)
  ( ) Fullscreen     (RPCS3 자체 전체화면 - fallback)
  ( ) Windowed       (창모드)
[프로필]  DC_4K_ULTRA / DC_5K_SSAA / DC_4K_ULTRA_AF16 / DC_SAFE / CLEAN / DC_8K_SCREENSHOT /
          DC_CHEAT_OFFLINE / DC_NETPLAY_SAFE
[환경]    모니터 자동 탐지(물리 해상도/작업 영역), 대상 모니터(자동), 게임/ReShade 상태
[버튼]    실행 / 게임 종료 / 창 스타일 복원 / 로그 열기 / 닫기
```

## 2. 동작 원리 (요구사항 대응)

| 요구사항 | 구현 |
|---|---|
| RPCS3는 Vulkan Windowed 유지 | 런처가 프로필을 복사해 `Start games in fullscreen mode: false` 로 실행 (RPCS3 자체 전체화면을 쓰지 않음) |
| 게임 렌더 창만 정확히 식별 | RPCS3 프로세스들의 최상위 창 중 **제목이 RPCS3 "Window Title Format"**(`FPS: … \| Vulkan \| …`)인 창만 선택. GUI 메인 창(`RPCS3 0.0.42-…`)·더미 창(`Temp Window`, `__wglDummyWindowFodder`)·`_q_titlebar` 는 제외 |
| 모니터/작업영역 자동 탐지 | `EnumDisplayMonitors`+`GetMonitorInfo` (DPI aware) → **3840x2160 / 작업영역 3840x2088** 자동 인식 |
| 기본 대상 모니터 | 게임 창이 있는 모니터(`MonitorFromWindow`) 자동 선택. GUI에서 0번 등으로 강제 가능 |
| 테두리 없이 모니터 전체 | `WS_CAPTION·WS_THICKFRAME·WS_MINIMIZEBOX·WS_MAXIMIZEBOX·WS_SYSMENU` 제거 + `SetWindowPos` 로 모니터 전체 rect(0,0,3840,2160) |
| Alt+Enter 미사용 | 키 입력 주입 없음(순수 Win32 창 속성/크기 제어) |
| 종료 시 원래 창 스타일 복원 | 적용 전 `style/exstyle/rect` 를 `E:\PS3\Logs\borderless_state.json` 에 저장 → 창이 사라지면 복원 시도, `창 스타일 복원` 버튼으로 수동 복원도 가능 |
| ReShade/RPCN/입력/해상도 영향 없음 | 창 크기/스타일만 변경. 프로필의 `Resolution Scale`(300 %), ReShade Vulkan layer, RPCN, 입력 설정은 **변경하지 않음** |

추가 안전장치:
* **드리프트 감시**: 2초마다 창이 테두리/크기를 벗어났는지 확인하고 자동 재적용.
* **원본 상태 보존**: 반복 적용해도 최초 원본 style/rect 를 덮어쓰지 않음(초기 버그 수정 완료).
* **커스텀 타이틀바**: `_q_titlebar` 가 보이는 경우에만 숨기고, 복원 시 다시 표시.
* **DPI**: 엔진 프로세스는 Per-Monitor V2 DPI aware 로 동작해 물리 픽셀 기준으로 정확히 배치.

## 3. 파일 구성

| 파일 | 역할 |
|---|---|
| `Launchers\DragonCrown_Launcher.ps1` | WinForms GUI 런처 (모드 3종 + 프로필 + 상태/로그) |
| `Launchers\Dragon_Crown_Launcher.cmd` | GUI 런처 실행 래퍼 |
| `Launchers\Dragon_Crown_Borderless_4K.cmd` | CLI: `[프로필] [Borderless4K\|Fullscreen\|Windowed]` |
| `Tools\launch_with_mode.ps1` | 공통 실행 로직(런타임 config 생성 → RPCS3 실행 → 워처 시작) |
| `Tools\borderless_4k.ps1` | Win32 테두리 제거/복원 엔진 (`-Action Info\|Apply\|Restore\|Watch`) |
| `Tools\inspect_windows.ps1` | RPCS3 창 구조 진단(DPI aware, 물리 픽셀) |
| `Profiles\_runtime\*.yml` | 모드별 런타임 config(원본 프로필은 불변) |
| `Logs\launcher.log`, `Logs\borderless.log` | 실행/창 제어 로그 |
| `Logs\borderless_state.json` | 원본 창 상태(복원용, 게임 종료 시 자동 정리) |

## 4. 검증 결과 (2026-09-26 실측)

| 항목 | 결과 |
|---|---|
| 모니터 자동 탐지 | `\\.\DISPLAY1` 3840x2160 (작업영역 3840x2088), DPI aware |
| 대상 모니터 자동 선택 | 게임 창이 있는 DISPLAY1 자동 선택 |
| 적용 전 | `3862x2110 @ (-11,-11)`, style `0x97CF0000` (캡션/테두리 있음) |
| **적용 후** | **`3840x2160 @ (0,0)`, style `0x97000000` (테두리 없음)** |
| 창 식별 | 제목 `FPS: 59.85 \| Vulkan \| 0.0.42-20053 \| Dragon's Crown [BCAS20298]` (GUI 메인 창·더미 창 제외 확인) |
| 복원 | style `0x97CF0000`, rect `(-11,-11)-(3851,2099)` 로 정확히 복귀 |
| 반복 적용 | 원본 상태 보존 확인(로그: `original state preserved`) |
| 자동 복원(게임 종료) | `WATCH: game window gone -> restoring` → state 정리 → 워처 종료 |
| FPS | 적용 전/후 **59.85 ~ 59.91** (변화 없음) |
| ReShade | ZERO-BANNER 빌드 로드 + CAS/Deband/LumaSharpen/SMAA 컴파일 정상 (영향 없음) |
| Resolution Scale | 런타임 config 에서 `300` 유지(4K 내부 렌더 그대로) |
| 3개 모드 config | Borderless4K → fullscreen=false / Fullscreen → true / Windowed → false |

## 5. 사용 시나리오

1. `Dragon_Crown_Launcher.cmd` 실행 → 모드 **Borderless 4K** 선택(기본값) → 프로필 선택 → **실행**
2. 게임 창이 뜨면 약 5초 내 자동으로 테두리 없이 3840x2160 전체화면 적용(로그로 확인 가능)
3. 게임 종료 시 원래 창 스타일 복원(자동). 필요하면 GUI의 **창 스타일 복원** 버튼 사용
4. 화면이 이상하면 **Fullscreen** 또는 **Windowed** 로 전환해 fallback

## 6. 주의 / 한계

* Borderless 는 **창모드의 변형**이므로 RPCS3 내부적으로는 여전히 windowed 입니다. 따라서
  독점 전체화면(exclusive fullscreen)보다 입력 지연이 아주 약간 클 수 있고, Windows 전체화면
  최적화/VRR 동작은 독점 전체화면과 다를 수 있습니다(현 디스플레이는 60 Hz 고정).
* ReShade 는 창 크기 변경 시 스왑체인을 재생성합니다(정상 동작, 로그로 확인).
* 모니터를 강제 지정하려면: `borderless_4k.ps1 -Action Apply -MonitorIndex 1` (0부터 시작).
* 창이 테두리 없는 상태에서 다른 앱과 겹치면 `Alt+Tab` 으로 전환하십시오(테두리가 없어 드래그는 불가).
* RPCS3 설정의 `Stretch To Display Area` 는 건드리지 않았습니다(16:9 유지, 왜곡 없음).
