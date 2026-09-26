# ZERO-BANNER ReShade — 구축/검증 보고서

작성일: 2026-09-26
목적: Dragon's Crown(RPCS3)에서 **게임 시작 시 ReShade 문구(splash/banner)를 표시하지 않는** ReShade 빌드를
공식 소스 기반으로 최소 패치하여 직접 빌드하고, 공식 빌드와 A/B 검증한다.

---

## 1. 현재 설치된 ReShade 버전 (확인)

| 항목 | 값 | 근거 |
|---|---|---|
| 버전 | **6.8.0.2155** | `E:\PS3\RPCS3\ReShade.log`: `Initializing crosire's ReShade version '6.8.0.2155'` |
| DLL | `C:\ProgramData\ReShade\ReShade64.dll` (5,592,064 B) | 파일 버전 리소스 = 6.8.0.2155 |
| DLL SHA256 | `0CEE63F9C9F13F3AC909C5B4903F4DBB4B719A7AB3B4F13B0DEAF83C814B94F7` | 백업 시 계산 |
| 설치 프로그램 | `ReShade_Setup_6.8.0_Addon.exe` (reshade.me 공식) | SHA256 `AFE4C8F13048306307983B8B3D41D5BF00A86820440B0E57DEA10950E1176445` |
| 적용 방식 | Vulkan implicit layer, `ReShadeApps.ini` = rpcs3.exe 전용 | `HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers` |

## 2. Upstream 기준

| 항목 | 값 |
|---|---|
| 저장소 | `https://github.com/crosire/reshade` |
| 태그/커밋 | **v6.8.0 = `18deaa52de0c425a78b329e9cb3c497281cd00ec`** (2026-08-02) |
| 클론 위치 | `E:\PS3\Mods_Patches\ReShade\ZeroBanner\src` (submodule 포함, `git describe --tags` = v6.8.0) |
| 서브모듈(고정) | d3d12 9e393d6 · fpng 9257965 · glad 27bed11 · imgui 3912b3d(v1.92.5-docking) · jxl_simple_lossless 8dc970f · minhook 8fda4f5 · openxr 288d3a7(release-1.0.34) · spirv 7845730(1.5.4) · stb 28d546d · utfcpp 819011b · vma 1076b34 |

## 3. 참고 프로젝트 조사 (구현 참고용)

| 항목 | 결과 |
|---|---|
| 저장소 | `fienestar/OverlayDisabledReShade` — 설명: **“Disable the ReShade Startup Message”** (crosire/reshade 포크, 기본브랜치 main, HEAD 2025-10-26) |
| diff 시도 | upstream(v6.8.0) 대비 `diverged / ahead 276 / behind 552` → **포크 이력이 재작성(rewrite)되어 파일 단위 깨끗한 diff 불가** |
| 확인된 변경 영역 | 변경 파일 249개 중 overlay/UI 계열: `include/reshade_overlay.hpp`, `include/reshade_api.hpp`, `include/reshade_events.hpp`, `res/lang_*.rc2`(다국어 문자열), `source/runtime_gui.cpp` 계열 → **splash/overlay UI 영역과 일치** |
| 결론 | 포크는 **참고용으로만** 사용. 실제 이식은 **공식 v6.8.0 소스에서 splash 구현부를 직접 특정**하여 최소 패치 |

## 4. Splash/banner 구현 위치 (공식 소스 분석)

`source/runtime_gui.cpp` (v6.8.0):

```cpp
// line 805
const bool show_splash_window = _show_splash && (is_loading() ||
    (_reload_count <= 1 && (_last_present_time - _last_reload_time) < std::chrono::seconds(5)) ||
    (!_show_overlay && _tutorial_index == 0 && _input != nullptr));

// line 1047~  "Splash Window" 창이 그리는 내용:
//   - "ReShade 6.8.0" (VERSION_STRING_PRODUCT)          ← 시작 배너 문구
//   - 업데이트 알림 / "Visit https://reshade.me ..."
//   - "ReShade is now installed successfully! Press 'Home' to start the tutorial."  ← 첫 설치 안내
//   - 효과 컴파일 진행률(ProgressBar) + "Compiling (N effects remaining) ..."
//   - "No keyboard or mouse input available." 경고
```

## 5. 이식한 최소 패치 (ZERO-BANNER)

| 항목 | 값 |
|---|---|
| 파일 | `source/runtime_gui.cpp` (1개 파일) |
| 변경 | `show_splash_window` 를 항상 `false` 로 고정 + 패치 식별용 **1회 로그 라인** 추가 |
| 패치 파일 | `E:\PS3\Mods_Patches\ReShade\ZeroBanner\ZERO_BANNER.patch` (SHA256 `2F9ED1A87F7B9146219FFA859A43081EC4A1F29DF530997FC7184B7F8CE848D7`) |
| 패치 적용 스크립트 | `E:\PS3\Tools\apply_zero_banner_patch.py` (idempotent, 대상 라인 검증 포함) |
| 패치된 소스 SHA256 | `4477A8F076FEE8B0B405FE7212A35E60D9072329B5A5AC98C157681B6A1F101F` |

핵심 diff:

```diff
-	const bool show_splash_window = _show_splash && (is_loading() || (_reload_count <= 1 && (_last_present_time - _last_reload_time) < std::chrono::seconds(5)) || (!_show_overlay && _tutorial_index == 0 && _input != nullptr));
+	// ===== ZERO-BANNER PATCH (E:\PS3) =====
+	static bool s_zero_banner_logged = false;
+	if (!s_zero_banner_logged)
+	{
+		s_zero_banner_logged = true;
+		log::message(log::level::info, "ZERO-BANNER build: startup splash/banner disabled (no ReShade text on startup).");
+	}
+	const bool show_splash_window = false;
+	// ===== end ZERO-BANNER PATCH =====
```

**제거되지 않은 것(요구사항 §7 준수)**: Vulkan hook, effect runtime, shader compile, preset, CAS/Deband,
Home overlay menu, error logging — 코드상 손대지 않았고 A/B 로그로 정상 동작 확인.

## 6. 빌드 정보

| 항목 | 값 |
|---|---|
| 방식 | MSBuild (Visual Studio 2022 Community 17.14, MSVC 14.44.35207, Windows SDK 10.0.26100) |
| 명령 | `msbuild ReShade.sln /p:Configuration=Release /p:Platform=64-bit /t:ReShade /m` |
| 선행 요구 | Python 3.14.3(PATH) → `deps/glad` 가 `pip install -r requirements.txt` 후 glad 2로 GL/Vulkan 로더 생성, `deps/khronos/*.xml`(gl/vk/wgl) 사용 |
| Vulkan SDK | **불필요** — ReShade는 glad가 생성한 Vulkan 1.4 헤더/로더를 사용 |
| 결과물 | `E:\PS3\Mods_Patches\ReShade\ZeroBanner\src\bin\x64\Release\ReShade64.dll` |
| DLL 크기 | **5,581,824 B** (공식 5,592,064 B) |
| DLL SHA256 | `FFCAB1B200DDE2EDDE7E52A49D67014082180CAD9A2D67835BCE0E00F356E181` |
| 파일 버전 | `6.8.0.1` / 제품 버전 `6.8.0 UNOFFICIAL` (로컬 빌드이므로 build 번호 1, UNOFFICIAL 표기) |
| 빌드 로그 | `E:\PS3\Logs\reshade_zerobanner_build.log` (33,614 B, 오류 0 / 경고는 C4530 계열뿐) |

## 7. 공식 binary 백업

| 항목 | 값 |
|---|---|
| 백업 위치 | `E:\PS3\Mods_Patches\ReShade\Official_Backup\` |
| 파일 | `ReShade64.dll`(6.8.0.2155), `ReShade64.json`, `ReShadeApps.ini`, `ReShade.ini.rpcs3`, `BACKUP_INFO.md` |
| 복원 | `E:\PS3\Mods_Patches\ReShade\ReShade_Switch_Official.cmd` (관리자, RPCS3 실행 중이면 거부) |
| 적용 | `E:\PS3\Mods_Patches\ReShade\ReShade_Switch_ZeroBanner.cmd` |

## 8. A/B 검증 (Dragon's Crown 실제 부팅)

조건: `DC_4K_TEST_WINDOWED`(300 %, 창모드), 40초 실행, ReShade.ini = `DC_POSTFX_MINIMAL.ini`(Deband+CAS),
동일 셰이더 경로(`E:\PS3\Mods_Patches\ReShade\Shaders`).

| 항목 | A: 공식 6.8.0.2155 | B: ZERO-BANNER 6.8.0.1 |
|---|---|---|
| 로그 로드 문구 | `version '6.8.0.2155'` | `version '6.8.0.1'` |
| **ZERO-BANNER 마커** | 없음 | **`ZERO-BANNER build: startup splash/banner disabled (no ReShade text on startup).`** |
| Vulkan hook (instance/device/swapchain/present) | 정상 | 정상 (3840x2054 swapchain) |
| effect runtime / shader compile | CAS·Deband·LumaSharpen·SMAA 4/4 컴파일 | **4/4 컴파일** |
| preset (ReShade.ini) | runtime 환경 재생성 + ini 로드 | 동일 |
| error logging | INFO/WARN 기록 | 동일 |
| RPCS3 안정성(40초) | 크래시 없음 | **크래시 없음** |
| RPCS3 fatal | 0 | 0 |
| 증거 로그 | `E:\PS3\Logs\reshade_AB_A_official_6.8.0.2155.log` | `E:\PS3\Logs\reshade_AB_B_zerobanner_full.log` |

* 참고: ReShade는 시작할 때 `ReShade.log` 를 **새로 생성(truncate)** 합니다. A/B 로그는 실행별로 분리 보관했습니다.

## 9. 채택 조건 체크리스트 (요구사항 §11)

| 조건 | 상태 | 근거/비고 |
|---|---|---|
| 게임 시작 시 ReShade 문구 없음 | **코드상 제거 + 실행 확인** | splash 창 비활성 + 마커 로그. *화면 확인은 사용자 육안 필요* |
| preset 자동 적용 | 확인 | `ReShade.ini`의 `PresetPath` 로드(runtime 환경 재생성 로그). 실제 효과 적용은 육안 확인 권장 |
| CAS/Deband 실제 적용 | 확인(컴파일 4/4) | 시각적 적용 여부는 육안 확인 권장 |
| Home 키 메뉴 정상 | 코드 미변경 | overlay/input 코드 무변경, user32·input hook 설치 로그 확인. **육안 확인 필요** |
| Vulkan 정상 | 확인 | instance/device/swapchain hook + 40초 무중단 |
| crash 없음 | 확인 | A/B 모두 0 |

> **채택 판정**: 자동 검증 항목은 **전부 통과**. 최종 채택은 사용자가 Home 메뉴/효과 적용을 육안 확인한 뒤 결정하십시오.
> 되돌리기: `ReShade_Switch_Official.cmd` (1회 실행).

## 10. 문서화 요약 (요구사항 §12)

| 항목 | 값 |
|---|---|
| upstream ReShade 커밋 | `18deaa52de0c425a78b329e9cb3c497281cd00ec` (tag v6.8.0, 2026-08-02) |
| patch | `ZERO_BANNER.patch` (SHA256 `2F9ED1A87F7B9146219FFA859A43081EC4A1F29DF530997FC7184B7F8CE848D7`) — 1파일 1조건 |
| build | `ReShade64.dll` SHA256 `FFCAB1B200DDE2EDDE7E52A49D67014082180CAD9A2D67835BCE0E00F356E181`, 5,581,824 B, 6.8.0.1 (UNOFFICIAL) |
| 공식 대조 | `ReShade64.dll` SHA256 `0CEE63F9C9F13F3AC909C5B4903F4DBB4B719A7AB3B4F13B0DEAF83C814B94F7` (6.8.0.2155) |

## 11. 출처

| 자료 | URL |
|---|---|
| ReShade 공식 소스/태그 | https://github.com/crosire/reshade (tag v6.8.0) |
| ReShade 공식 배포 | https://reshade.me |
| 참고 포크 | https://github.com/fienestar/OverlayDisabledReShade |
| Vulkan implicit layer 규격 | https://registry.khronos.org/vulkan/specs/latest/man/html/VK_LAYER_KHRONOS_validation.html (loader layer 동작) |
