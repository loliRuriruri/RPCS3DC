# RESHADE GUIDE — Silent 모드 / ZERO-BANNER / 프리셋

작성일: 2026-09-26

---

## 1. 기본 정책

ReShade는 **선택적 후처리**다. Dragon's Crown은 2D artwork 비중이 높으므로
원본 그림을 훼손하지 않는 범위에서만 사용한다.

| 사용 | 사용하지 않음 |
|---|---|
| **Deband** (기본) | 강한 Bloom / MagicBloom |
| 약한 **CAS**(0.25~0.30) 또는 LumaSharpen | MXAO / RTGI / SSAO / Depth 기반 AO |
| 필요 시 Levels 소폭 | Depth of Field / Motion Blur / Chromatic Aberration |
| (선택) 아주 약한 SMAA | Film Grain / 과도한 saturation / Fake HDR |

* 프리셋: `E:\PS3\ReShade\Presets\DC_POSTFX_MINIMAL.ini` (Deband + CAS 0.30)
* 셰이더: `E:\PS3\Mods_Patches\ReShade\Shaders` (공식 소스만: slim + SweetFX)
* 백업: `E:\PS3\ReShade\Backup`

## 2. Silent 모드 (기본값)

목표: **효과는 적용, 시작 시 메뉴/튜토리얼/OSD는 표시하지 않음, Home으로 메뉴 호출**.

`E:\PS3\RPCS3\ReShade.ini`:

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
KeyOverlay=36,0,0,0        ; 36 = VK_HOME
```

* 키 이름은 ReShade 6.8.0 소스(`runtime_gui.cpp`의 `config_get("OVERLAY", ...)`)에서 확인한 실제 키다.
* 기대 동작: 게임 시작 → 메뉴 없음 → 효과 자동 적용 → **Home** → 메뉴 표시 → Home/Esc → 닫힘
* 메뉴 기능 자체는 삭제하지 않는다.

## 3. ZERO-BANNER (선택)

공식 ReShade가 시작할 때 잠깐 보여주는 splash/loading 문구까지 제거한 자체 빌드.

| 항목 | 값 |
|---|---|
| upstream | crosire/reshade **tag v6.8.0** = `18deaa52de0c425a78b329e9cb3c497281cd00ec` |
| 패치 | `source/runtime_gui.cpp` 1파일 — `show_splash_window = false` + 식별 로그 1줄 |
| 패치 파일 | `E:\PS3\Mods_Patches\ReShade\ZeroBanner\ZERO_BANNER.patch` |
| 빌드 | MSBuild x64 Release (VS2022) → `ZeroBanner\build\ReShade64.dll` (5,581,824 B) |
| 검증 | 이펙트 4/4 컴파일 · Vulkan hook 정상 · 40초 무크래시 · 마커 로그 확인 |
| 되돌리기 | `ReShade\ReShade_Switch_Official.cmd` (공식 6.8.0.2155 복원) |

유지되는 기능: effect runtime · preset · shader compile · Vulkan · Home menu · error logging.
제거되는 것: startup splash/banner UI만.

## 4. 켜고 끄기

| 목적 | 방법 |
|---|---|
| ReShade 끄기(레이어만) | 런처 GRAPHICS에서 `ReShade 사용` 해제 (런처가 `DISABLE_VK_LAYER_reshade_1=1` 로 실행) |
| ReShade 전역 끄기 | `E:\PS3\Mods_Patches\ReShade\ReShade_Disable.cmd` (관리자) |
| 공식 ↔ ZERO-BANNER 전환 | `ReShade_Switch_Official.cmd` / `ReShade_Switch_ZeroBanner.cmd` (관리자) |
| Silent 해제 | 런처 GRAPHICS에서 `ReShade Silent` 해제 (Show* 값 복원) |

## 5. 확인 방법

1. `E:\PS3\RPCS3\ReShade.log` — 버전, `ZERO-BANNER build: ...` 마커, `Successfully compiled ...`
2. 게임 시작 시 화면에 ReShade 문구/메뉴가 **보이지 않아야** 함
3. **Home** 키로 메뉴가 열리고, Home/Esc 로 닫혀야 함
4. 효과 적용 여부는 같은 장면 스크린샷을 ReShade On/Off 로 비교

## 6. 주의

* AdaptiveSharpen은 과거 Vulkan+ReShade 조합에서 crash 보고가 있어 사용하지 않는다.
* ReShade는 창 크기 변경 시 스왑체인을 재생성한다(정상).
* 첫 NETPLAY 연결 테스트에서는 ReShade를 끄는 것을 권장한다(변수 최소화).
* ReShade가 종료 crash/Vulkan 오류/shader compile 문제를 일으키면 즉시 `ReShade_Disable.cmd` 로 끄고
  CLEAN 환경으로 복귀한다.
