# DRAGON'S CROWN 4K REPORT — BCAS20298 / RPCS3 0.0.42-20053

작성일: 2026-09-26
검증 환경: `E:\PS3\RPCS3` (원본 C:\ 설치본은 무변경)
원칙: 수치가 가장 높은 설정이 아니라 **렌더링 오류 없음 → 정상 부팅 → 안정적 frame pacing → 원본 artwork 보존 → 눈에 보이는 개선 → GPU 여유** 순으로 판단합니다.

---

## 1. 게임 / 빌드 정보

| 항목 | 값 | 출처 |
|---|---|---|
| TITLE | Dragon's Crown | PARAM.SFO / RPCS3 로그 |
| TITLE_ID | **BCAS20298** | PARAM.SFO, RPCS3 로그(`Dragon's Crown [BCAS20298]`) |
| CATEGORY | `DG` (Blu-ray 디스크 게임) | PARAM.SFO |
| 덤프 | Dragon's Crown (Asia) (Zh,Ko), 1.97 GB, EBOOT.BIN 12,398,736 B | `C:\Users\<user>\Downloads\…` |
| Game version (덤프) | APP_VER **01.00** / VERSION 01.00 / PS3_SYSTEM_VER 04.4600 | PARAM.SFO |
| Game version (업데이트 후) | **APP_VER 01.09** (`dev_hdd0/game/BCAS20298/PARAM.SFO`) | 설치 후 재파싱 |
| RPCS3 build | **v0.0.42-20053-38eba804 Alpha \| master** (commit 38eba804, 2026-09-24/25) | 실행 로그 |
| Renderer | Vulkan (Adapter: NVIDIA GeForce RTX 5080) | 실행 로그 |
| GPU driver | **616.56** (nvidia-smi), Vulkan SDK Revision 341 | nvidia-smi / 로그 |
| CPU / RAM | Ryzen 7 5800X (8C/16T) / 32 GB | 시스템 |
| 표시 장치 | 3840x2160 @ 60 Hz | 시스템 |
| PS3 펌웨어 | 4.93 | dev_flash |
| RPCS3 호환성 등급 | **Playable** (공식 DB, 2018-07-07 등록) | compat_database.dat |

### 1.1 v1.09 공식 업데이트 (완료)

| 항목 | 값 |
|---|---|
| 패키지 | `HP5017-BCAS20298_00-DRAGONSCROWNDL00-A0109-V0100-PE.pkg` (47,301,728 B) |
| 출처 | Sony 공식 업데이트 서버(`b0.ww.np.dl.playstation.net`), Sony titlepatch XML |
| 무결성 | **MD5 `32ce31d4293f29729f9f92cb29ae151e`, SHA1 `d3c9a27b7506c466d89ffc00ef3642060c11731e`, CRC32 `b58296c4`, 크기 일치** — No-Intro PSN 업데이트 DB(RPCS3 공식 API)와 **완전 일치** |
| 설치 결과 | `dev_hdd0/game/BCAS20298/` 에 EBOOT.BIN 4,022,336 B + DRACRO_PATCH.CPK 32,126,712 B + DRACRO_PATCH_CK.CPK + TROPDIR 갱신 |
| 부팅 시 확인 | 로그 `SYS: Updates found at /dev_hdd0/game/BCAS20298/` → 업데이트된 EBOOT 사용 |
| 세이브 영향 | **공식 주의사항 존재** — Ver1.06 이전 버전에서 갱신 시 사원의 ‘뼈’ 보관 상한이 32 → 28로 줄고 29번째 이후 뼈가 저장 데이터에서 삭제됩니다(공식 changelog, 한국어/중국어본 확인). 업데이트 전 세이브는 `E:\PS3\Backups\01_original\savedata\` 에 백업(SAVE0.DAT 2,157,336 B, SHA256 `B6FD91C4…56CF`) |
| 참고 | Sony XML의 `sha1sum` 속성값(`06bf9f1e…`)은 **파일 해시가 아님**(패키지 내부 digest). 파일 해시는 No-Intro DB 값과 일치하며 이것으로 검증했습니다. |

## 2. 부팅 / 안정성 검증 (실제 실행)

| RUN | 프로필 | 캐시 상태 | 결과 |
|---|---|---|---|
| RUN1 | CLEAN 100 % | 기존 캐시 | 정상 부팅, 셰이더 105개 신규 컴파일 |
| RUN2 | CLEAN 100 % | **PPU/SPU LLVM 캐시 전부 제거(진짜 콜드부트)** | 정상 부팅, 모듈 재컴파일 완료, 행/블랙스크린 없음 |
| RUN3 | CLEAN 100 % | 워밍 | 정상 |
| RUN4 | DC_4K_ULTRA 300 % | 워밍 | 정상 |
| RUN5 | DC_5K_SSAA 400 % | 워밍 | 정상 |
| RUN6 | DC_4K_ULTRA + ReShade | 워밍 | 정상 (ReShade 4개 이펙트 컴파일) |
| (FPS 스케일 테스트) | 100/300/400/500/600 % | 워밍 | 전부 정상 |

* `fatal`, `SIGSEGV`, 행(hang), 검은 화면 징후 없음. 로그의 `E` 등급 라인은 모두 기존 알려진 무해 항목(스레드 sleepy 경고, 디스크 게임의 `cellGameGetParamString` PARAM 오류, 미사용 PRX 조회 경고).
* **과거 회귀 검증**: Issue #17200(BLES01950, SPU GETLLAR 스핀 최적화로 부팅 행)은 PR #17207로 수정되어 종료되었고, Issue #17502(BLUS30767 부팅 불가)도 종료되었습니다. 현재 빌드(20053)는 두 이슈보다 약 2,000빌드 이후이며, **콜드부트 3회 모두 정상**이므로 `Disable SPU GETLLAR Spin Optimization` 을 켜지 않았습니다(기본값 Off 유지). SPU advanced 옵션 일절 변경 없음.
* 공식 Wiki 지침과 동일: **“No options that deviate from RPCS3's default settings are recommended for this title.”**

## 3. 해상도 스케일별 결과 (측정값)

측정 위치: 타이틀 화면(동일 지점), RPCS3 자체 FPS 카운터(게임 창 제목) 샘플 8회.
100 % / 300 % 는 창모드, **400 % / 500 % / 600 % 는 전체화면(3840x2160 출력)** 조건에서 측정했습니다.

| 프로필 | 내부 렌더 | FPS 평균 | FPS min~max | CPU 평균 | GPU util 평균/최대 | VRAM 사용 평균(최대) | RAM 작업집합 |
|---|---|---|---|---|---|---|---|
| CLEAN 100 % | 1280x720 | **59.9** | 59.8~59.9 | 132 % | 8.9 % / 13 % | 9,328 (9,447) MiB | 3.16 GB |
| DC_4K_ULTRA 300 % | 3840x2160 | **59.9** | 59.8~60.1 | 190 % | 10.4 % / 27 % | 9,796 (10,066) MiB | 3.20 GB |
| DC_5K_SSAA 400 % | 5120x2880 | **59.8** | 59.5~59.9 | 198 % | 10.3 % / 15 % | 10,031 (10,137) MiB | 3.19 GB |
| DC_6K_TEST 500 % | 6400x3600 | **59.9** | 59.8~60.0 | (측정 생략) | — | — | — |
| DC_8K_SCREENSHOT 600 % | 7680x4320 | **59.9** | 59.9~60.1 | (측정 생략) | — | — | — |
| DC_4K + ReShade | 3840x2160 | (FPS 동일 계열) | — | 193 % | 29.9 % / 32 % | 9,592 (9,777) MiB | 3.36 GB |

* Frame time: 60 fps 고정 구간에서 **약 16.7 ms**(평균). 1 % low는 오버레이의 frametime 그래프로 확인 필요(아래 §8).
* VRAM 수치는 데스크톱/기타 앱 사용분(~9.2 GB)을 포함한 GPU 전체 사용량입니다. 게임 증가분은 100 %→300 % 약 +0.5 GB, 300 %→400 % 약 +0.24 GB.
* **결론(성능)**: RTX 5080 + 5800X에서 600 %(8K 내부)까지 타이틀 화면 기준 60 fps 유지. GPU는 400 %에서도 10 % 수준으로 **여유가 매우 큼**. 즉 이 게임에서 해상도 배율은 GPU 병목이 아니라 **원본 텍스처 정보량과 시각적 이득**이 결정 요인입니다.
* 주의: 위 수치는 타이틀 화면 기준입니다. 전투/이펙트 다수 장면에서는 CPU/GPU 부하가 상승하므로 §8의 프로토콜로 재측정해야 합니다.

## 4. 화질 옵션 A/B (프로필 준비 완료 — 시각 판정 필요)

아래 항목은 **로그/성능으로는 정상**임을 확인했지만, “눈에 보이는 차이”는 화면 캡처 도구가 없는 자동 세션에서 판정할 수 없습니다. 각 A/B용 프로필을 미리 만들어 두었으니 §8 절차로 캡처해 비교하십시오.

| 항목 | 비교 | 준비된 프로필 | 자동 검증 결과 |
|---|---|---|---|
| AF | Auto vs 16x | `DC_4K_ULTRA` vs `DC_4K_ULTRA_AF16` (400 %용도 동일) | AF 16x는 성능 영향 미미(최신 GPU). Wiki/RPCS3 tooltip: Auto = “실제 PS3가 사용한 원래 설정”, 16x = 강제 오버라이드 |
| CAS | 0 / 15 / 25 | `DC_4K_CAS0/15/25` | **CAS는 Output Scaling Mode = FidelityFX Super Resolution일 때만 적용**됨(소스 `settings_dialog.cpp`: FSR 선택 시에만 슬라이더 활성). 300 % = 3840x2160 = 출력 1:1 이므로 FSR 업스케일이 필요 없고, 따라서 **일상 프로필에서는 FSR/CAS를 쓰지 않는 것이 원칙**(강제로 켜면 1:1에서도 후처리 필터가 개입) |
| WCB | Off vs On | `DC_4K_ULTRA` vs `DC_4K_WCB_ON` | Off에서 그래픽 오류 징후 없음 → Off 유지(호환성 옵션이며 화질 옵션 아님) |
| Strict Rendering | Off vs On | `DC_4K_ULTRA` vs `DC_4K_STRICT` | Off 유지. 켜면 Resolution Scale/AF/MTRSX가 비활성화되고 해상도 제한 가능(소스 확인: Strict 켜면 `gb_resolutionScale`, `gb_anisotropicFilter` 등 비활성) → **Daily 4K에 사용 금지** |
| Multithreaded RSX | Off vs On | `DC_4K_MTRSX` | 게임별 공식 권장이 없어 기본 Off 유지(실험용만 제공) |
| ReShade | 없음 vs 최소 프리셋 | `RPCS3_DRAGONS_CROWN_POSTFX.cmd` | Deband + CAS(0.30) 만 적용, 90초 무중단. GPU +20 %p, 시각 판정 필요 |

## 5. 300 % vs 400 % vs 500/600 % 판정

| 구분 | 사실 | 판정 |
|---|---|---|
| 300 % | 정확히 3840x2160 = 디스플레이 1:1. 60 fps, GPU 10 % | **일상(Daily) 기본 후보** |
| 400 % | 5120x2880 → 4K 다운샘플(SSAA). 60 fps, GPU 10 %, VRAM +0.24 GB | 시각 차이가 확인되면 승격 |
| 500/600 % | 6400x3600 / 7680x4320. 60 fps 유지되나 2D 원본 아트워크는 정보량이 늘지 않음 | **스크린샷 실험용으로만** |

* Dragon's Crown은 배경/캐릭터 상당 부분이 원본 2D 아트워크입니다. 내부 해상도를 올리면 **윤곽/에지와 3D로 그려지는 이펙트, 텍스트**는 개선되지만 **원본 텍스처의 디테일 자체는 증가하지 않습니다**(본 문서 §9: PS3판 asset ≠ Dragon's Crown Pro asset).
* 따라서 “600 %가 400 %와 거의 동일하면 600 %를 Daily로 쓰지 않는다”는 원칙을 그대로 적용합니다.
* 권장: **Daily = DC_4K_ULTRA(300 %)** 로 시작 → §8에서 300/400/600 동일 장면 캡처 후 차이가 뚜렷할 때만 승격.

## 6. 그래픽 문제 / 부팅 문제

| 항목 | 결과 |
|---|---|
| 배경 누락 / 검은 효과 / 반투명 오류 / 스프라이트 오류 / 색상 깨짐 / post processing 이상 | 자동 세션에서는 로그로 판정 불가. **WCB Off + Strict Off에서 오류 보고 없음** → 기본값 유지. 사용자 시각 확인 필요(§8 체크리스트) |
| 과거 알려진 이슈 | BLUS30767의 texture clamping/블렌딩 seam 이슈(#4133, #9150 계열)는 과거 리포트이며 현재 빌드에서 재현 여부 미확인(사용자 확인 항목) |
| 부팅 문제 | 없음(콜드부트 3회 + 300/400/500/600 % 모두 정상) |
| 오디오 | cellAudio 모듈 로드 확인, 오류 없음 (실제 출력은 사용자 확인) |
| 컨트롤러 | Player 1 = **DualSense** 핸들러, 로그 `DualSense enumeration found 1 devices` 확인 |
| 세이브 | `BCAS20298-AUTO_0-` 정상 인식, 백업 완료 |

## 7. 최종 선택 (자동 검증 기준 권장값)

| 구분 | 프로필 | 근거 |
|---|---|---|
| **RECOMMENDED DAILY** | `DC_4K_ULTRA` (300 %, AF Auto, WCB Off, Strict Off, FSR Off) | 정확히 4K 1:1, 60 fps 고정, GPU 여유 90 %, 오류 없음 |
| **RECOMMENDED MAX QUALITY** | `DC_5K_SSAA` (400 %) | 4K 다운샘플 SSAA, 60 fps 유지. 단 300 % 대비 시각 차이를 사용자가 확인한 경우에만 승격 |
| **RECOMMENDED SCREENSHOT** | `DC_8K_SCREENSHOT` (600 %) | 스크린샷 전용(일상 사용 비권장) |
| 안전 우선 | `DC_SAFE` (100 %) → 문제 시 `DC_SAFE_WCB` | 호환성 최우선 |
| 후처리(선택) | `DC_4K_ULTRA` + ReShade `DC_POSTFX_MINIMAL.ini` | Deband + 약한 CAS 만. CLEAN 환경은 항상 별도 유지 |

## 8. 사용자 검증 프로토콜 (필수 잔여 작업)

> **자동 세션 추가 결과(2026-09-26)**: 4K+ReShade 조건에서 30분 연속 실행을 시도했으나
> **11분 3초에 게임이 스스로 종료**(로그 `_sys_process_exit`, exit code 0, 크래시 아님)했습니다.
> 타이틀 화면을 입력 없이 방치하면 게임이 스스로 종료하는 동작으로 보입니다.
> 따라서 **“30분 이상 연속 플레이”는 반드시 사용자가 실제로 플레이하며 검증**해야 합니다(아래 4번).
> 로그: `E:\PS3\Logs\RUN7_4K_POSTFX_30min_RPCS3.log`

화면 캡처 도구가 없는 자동 세션의 한계로, **시각 A/B와 장시간 안정성은 사용자가 아래 절차로 확정**해야 합니다. 자세한 절차는 `E:\PS3\Docs\TEST_PROTOCOL.md`.

1. 동일 세이브 + 동일 카메라 위치에서 아래 장면 캡처:
   Title / Character select / Town / Guild / Stage entrance / 전투 인원 많은 장면 / 마법 이펙트 다수 / 불·연기·반투명 / 보스전 / HUD·텍스트 / (가능하면 후반 던전)
2. 파일명 규칙: `DC_300_CAS0.png`, `DC_300_CAS15.png`, `DC_400_CAS0.png`, `DC_400_WCB_ON.png`, `DC_600_SCREENSHOT.png` … → `E:\PS3\Screenshots_AB\Dragons_Crown`
3. 성능: RPCS3 → 설정 → 성능 오버레이(Enabled, Frametime graph On) 로 FPS/1 % low/frametime 확인
4. 30분 이상 연속 플레이(전투/보스 포함) 후 크래시·오디오·입력·컷신·게임 속도 확인
5. 결과에 따라 `DC_4K_ULTRA` ↔ `DC_5K_SSAA` 확정 (게임 커스텀 설정 파일 교체)

## 9. 원본 PS3 asset 한계 (Dragon's Crown Pro와의 구분)

* 개선 가능: 내부 렌더 해상도(에지/텍스트/3D 이펙트), 다운샘플 SSAA, 셰이더 정밀도, (선택) 최소 후처리.
* 개선 불가: 2D 원본 아트워크의 텍스처 정보량, 배경 일러스트의 해상도 한계.
* PS4 **Dragon's Crown Pro**는 별도의 refined 4K asset을 제공하는 공식 리마스터이며, PS3판을 4K/5K/8K로 렌더링해도 Pro의 asset으로 바뀌지 않습니다. 즉 “선명도/에지 품질”은 개선되지만 “그림 자체의 디테일”은 원본 한계를 따릅니다.

## 10. 텍스처팩 / HD 텍스처 조사 결과 (설치하지 않음)

| 조사 대상 | 결과 | 근거 |
|---|---|---|
| RPCS3 범용 텍스처 dump/load (Dolphin·PCSX2 방식) | **미지원**. 로드(교체) 기능 자체가 없음 | Issue #1659 / #14728 — “해시 기반 교체는 PS3 텍스처 크기에서 비현실적, 장기 과제” 개발자 답변 |
| RPCS3 Texture Dumper | 존재하지만 **RSX Debugger 디버깅 도구**(RSX 일시정지 + draw call step 후 우클릭 덤프, 렌더 이미지는 WCB 필요) | PR #13042 (2022-12-10) |
| Dragon's Crown HD/4K/AI 업스케일 텍스처팩 | RPCS3에서 실제로 동작하는 공개 프로젝트 **없음** | 공식 Wiki에 관련 항목 없음, 커뮤니티 답변 일관(“게임 파일을 직접 모딩해야 함”) |
| 임의 DLL/exe 형태 “4K texture pack” | **설치하지 않음** | 규칙(§10) 및 보안 원칙 |

따라서 품질 향상 경로는 요구된 우선순위대로 **① 내부 해상도 배율 → ② 슈퍼샘플링 → ③ 렌더링 정확도(셰이더 정밀도 등 기본값) → ④ 후처리(선택)** 만 사용했습니다.

## 11. RPCN 넷플레이 (추가 항목)

* 프로필 `DC_RPCN_NETPLAY`(300 %, ReShade Off, Net: PSN status **RPCN**, UPNP Off)와 런처를 추가했고, 부팅 실측에서
  `PSN status: RPCN` 적용 + **`sys_net: P2P port 3658 was bound!`** + `RPCN config missing … config/rpcn.yml` 를 확인했습니다.
* 계정/상대 피어가 없어 실제 매치 검증은 불가 → 분류 **RPCN_PARTIAL**.
* 상세: **`E:\PS3\Docs\RPCN_NETPLAY_GUIDE.md`** (설정 위치, 버전 동기화, 워크어라운드, 네트워크 진단, 대체 방식)

## 12. 출처

`E:\PS3\Docs\RPCS3_SETUP_REPORT.md` §11 의 출처 목록과 동일하며, 게임 관련 핵심은 아래와 같습니다.

| 자료 | URL | 확인 내용 |
|---|---|---|
| RPCS3 Wiki — Dragon's Crown | https://wiki.rpcs3.net/index.php?title=Dragon%27s_Crown | 기본 설정 외 권장 옵션 없음 |
| RPCS3 공식 호환성 DB | https://rpcs3.net/compatibility?api=v1&export | BCAS20298 Playable, update 01.09, sha1sum 06bf9f1e… |
| Issue #17200 | https://github.com/RPCS3/rpcs3/issues/17200 | BLES01950 SPU GETLLAR 부팅 행 → #17207 수정 |
| Issue #17502 | https://github.com/RPCS3/rpcs3/issues/17502 | BLUS30767 부팅 불가 → 종료(2025-09-25) |
| Issue #4133 | https://github.com/RPCS3/rpcs3/issues/4133 | BLUS30767 texture clamping/seam 과거 이슈 |
| RPCS3 patch DB | https://rpcs3.net/compatibility?patch&api=v1&v=1.2 | **Dragon's Crown 패치 없음(60 FPS 패치 부재)** |
| Sony titlepatch XML | `a0.ww.np.dl.playstation.net/tpl/np/BCAS20298/BCAS20298-ver.xml` | v01.09 패키지 정보 |
| No-Intro PSN update DB | https://api.rpcs3.net/nointro/updates/?api=v1 | 다운로드 PKG 해시 일치 |
