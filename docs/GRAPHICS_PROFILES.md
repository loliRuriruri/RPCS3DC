# GRAPHICS PROFILES — Standard / Pro Enhanced (4K·5K)

작성일: 2026-09-26 (Phase 1 Closeout) · 대상: Dragon's Crown PC Edition

이 문서는 **그래픽 프리셋 2종(Standard / Pro Enhanced)** 과
**해상도 프리셋 2종(4K 300% / 5K 400%)** 의 정의·전환 방법·실측 근거를 정리한다.

---

## 1. 프리셋 정의

### Standard (Graphics A)

RPCS3 자체 렌더링만 사용한다. ReShade 를 **사용하지 않는다.**

| 항목 | 값 |
|---|---|
| Resolution Scale | 300 % (4K) 또는 400 % (5K) — 사용자 선택 |
| Anisotropic Filter Override | **16x** |
| MSAA | Auto |
| Aspect ratio | 16:9 |
| Stretch To Display Area | false |
| VSync Mode | Full |
| Vblank Rate | 60 |
| Enable Frame Skip | false |
| Start games in fullscreen mode | false (Borderless 4K 기본) |
| ReShade | **OFF** (Vulkan layer runtime 차단: `DISABLE_VK_LAYER_reshade_1=1`) |

### Pro Enhanced (Graphics B)

Standard 와 동일한 RPCS3 설정에 **ReShade 프리셋 `DC_PRO_ENHANCED.ini`** 를 더한다.

| 이펙트 | 값 | 목적 |
|---|---|---|
| Deband | radius 16 / threshold 0.008 / range 16 | banding(계단 현상) 감소 |
| CAS | sharpening 0.32 / contrast 0 | 선명도 보완 (과하지 않게) |
| Levels | black 0.0 / white 1.0 / gamma 1.0 | 톤 기준점 유지(과도한 대비 금지) |
| Vibrance | 0.10 | 색감 보정 (채도 폭주 방지) |
| SMAA | edge 0.06 / search 32 | 가벼운 AA 보완 |

* Bloom / MXAO / RTGI / DOF / MotionBlur / ChromaticAberration / FilmGrain 은 **사용하지 않는다** (Vanillaware 일러스트 느낌 유지).
* ReShade 는 **Silent 모드**: 시작 시 문구/메뉴/튜토리얼이 뜨지 않으며, 필요할 때만 **Home** 키로 호출한다.
* ZERO-BANNER 빌드(자체 컴파일)를 사용해 시작 배너도 없다.

> `Pro Enhanced` 는 **이 프로젝트의 프리셋 이름**이다. PS4 Dragon's Crown Pro 리소스와는 무관하다 (Phase 2 범위).

---

## 2. 해상도 프리셋 (런처에서 선택)

| 표기 | 프로필 | 내부 렌더링 | 용도 |
|---|---|---|---|
| **4K / 300% — Recommended** | `DC_PRO_4K` | 3840x2160 | 기본 권장 (성능 여유, 전 구간 60fps 실측) |
| **5K / 400% — Super Sampling** | `DC_PRO_MAX_5K` | 5120x2880 → 4K 출력 | 슈퍼샘플링 (더 선명, 부하 증가) |

### 선택 방법

* 런처 → `SETTINGS → Graphics → Resolution Scale` 에서 선택 후 **APPLY**
* 선택값은 `settings.json` 의 `ResolutionProfile=4K|5K` 로 저장되어 **재실행 후에도 유지**
* CLI: `DragonCrownProEnhanced.exe --resolution 4K` 또는 `--resolution 5K`

### PLAY 연결 (Standard / Pro Enhanced 공통)

| 선택 | Standard | Pro Enhanced |
|---|---|---|
| 4K 300% | `DC_PRO_4K` + ReShade OFF | `DC_PRO_4K` + ReShade ON |
| 5K 400% | `DC_PRO_MAX_5K` + ReShade OFF | `DC_PRO_MAX_5K` + ReShade ON |

PLAY 버튼과 Local / Remote Co-op / KnownGood 실행도 모두 **선택된 해상도 프로필**을 사용한다.

---

## 3. Netplay Safe 와 그래픽

Netplay Safe 는 **일시적 runtime override** 이며 사용자의 그래픽 설정을 변경하지 않는다.

| 항목 | 동작 |
|---|---|
| Graphics preset | 실행 중 Standard 로 강제 (기존 설정 유지) |
| ReShade | `DISABLE_VK_LAYER_reshade_1=1` 로 **실제 Vulkan layer 차단** |
| 프로필 | `NETPLAY_SAFE` (300% · AF 16 · 보수적 네트워크 옵션) |
| 게임 종료 후 | 사용자의 기존 설정(예: Pro Enhanced + 5K) 그대로 |

실측 (2026-09-26): 사용자 설정 `Pro Enhanced + 5K` 상태에서 Safe 실행 →
`ReShade.log` 크기 불변(레이어 미로드), `settings.json` 값 변화 없음.

---

## 4. 실측 근거 (2026-09-26, RTX 5080 / 3840x2160@60)

| 항목 | 결과 | 근거 |
|---|---|---|
| 300% 타이틀 | 59.5 ~ 60.1 fps | `Logs\fps_by_scale.csv` |
| 400% 타이틀 부팅 | 59.83 ~ 60.02 fps (창 제목 카운터) | Closeout 테스트 로그 |
| 5K 내부 렌더링 | 5120x2880 (scale 400) | 프로필 + RPCS3 tooltip 기준 |
| Standard ReShade | 미로드 (로그 크기 불변) | `Logs\launcher.log`, ReShade.log |
| Pro Enhanced | 6종 셰이더 컴파일 + Silent | ReShade.log |
| Borderless | 물리 3840x2160 / style 0x96000000, 복원 시 3862x2186 / 0x96CF0000 | `Logs\launcher.log` |

> 전투/보스 등 부하 장면의 성능은 Acceptance Test 에서 사용자가 별도 확인한다 (§G).

---

## 5. 관련 파일

| 파일 | 설명 |
|---|---|
| `Profiles\Dragons_Crown\DC_PRO_4K\config.yml` | 4K 300% 프로필 |
| `Profiles\Dragons_Crown\DC_PRO_MAX_5K\config.yml` | 5K 400% 프로필 |
| `Profiles\Dragons_Crown\NETPLAY_SAFE\config.yml` | Netplay Safe 프로필 |
| `ReShade\Presets\DC_PRO_ENHANCED.ini` | Pro Enhanced 프리셋 |
| `ReShade\Presets\DC_POSTFX_MINIMAL.ini` | (참고) 최소 프리셋 |
| `RPCS3\ReShade.ini` | Silent `[OVERLAY]` + PresetPath |
| `Launcher\DragonCrownProEnhanced.exe` | 프리셋/해상도 전환 및 실행 |
