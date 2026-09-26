# Dragon's Crown PRO Enhanced for RPCS3 (총괄 문서)

작성일: 2026-09-26
대상: PS3판 《Dragon's Crown》 (BCAS20298, v1.09) / RPCS3 v0.0.42-20053

---

## 1. 목표

PS3판 Dragon's Crown을 RPCS3에서 **Dragon's Crown Pro의 장점을 가능한 범위까지 반영한 PC용 Enhanced Edition**처럼
사용할 수 있도록, 다음을 하나의 환경으로 통합한다.

4K/5K 최고화질 · Borderless · 안정적인 60fps · tearing 방지 · ReShade Silent ·
RPCN Netplay · Offline Cheat · Save Backup · Known Good RPCS3 fallback · 단일 GUI Launcher · 공유용 패키지

단순히 옵션 숫자를 최고로 올리는 프로젝트가 **아니다.** 공식 PS3 v1.09에 이미 반영된 QoL 개선
(Damage Display, 가방 이름 변경, 장비 효과 필터, 일괄 판매, Rune UI, 카메라/튜토리얼 개선, 스킬 리셋 개선 등)을
그대로 사용하고, 별도 개조는 하지 않는다.

---

## 2. 최종 구조

```
E:\\PS3\\
├─ RPCS3\\                  Current = 이 폴더 자체 (rpcs3.exe, config, dev_hdd0 …)
│   ├─ Current\\            (마커: Current = 상위 폴더임을 명시)
│   └─ KnownGood\\          검증 완료 fallback 스냅샷
├─ Games\\                  게임 덤프(사용자 보유) — 배포물에 포함하지 않음
├─ Profiles\\Dragons_Crown\\  PRO_4K / PRO_MAX_5K / NETPLAY / CHEAT_OFFLINE
├─ ReShade\\Presets|Backup
├─ Cheats\\Dragons_Crown\\   Verified / Experimental / Backup
├─ Saves\\Dragons_Crown\\    Original / AutoBackup / CheatTest
├─ Patches\\  Screenshots_AB\\  Logs\\  Backups\\
├─ Launcher\\               ★ DragonCrownProEnhanced.exe (단일 실행 파일)
│   ├─ Internal\\           (내부 스크립트)
│   └─ _Legacy\\            기존 .cmd 런처 모음(삭제하지 않음)
└─ Docs\\
```

## 3. 프로필 (사용자에게 보이는 4개)

| 버튼 | 프로필 | Resolution Scale | 내부 해상도 | 네트워크 | 비고 |
|---|---|---|---|---|---|
| PLAY | `DC_PRO_4K` | 300 % | 3840x2160 | Offline | 일상 기본 |
| PRO MAX | `DC_PRO_MAX_5K` | 400 % | 5120x2880 → 4K | Offline | 시각 차이 확인 시 |
| NETPLAY | `DC_NETPLAY` | 300 % | 3840x2160 | **RPCN** | 치트/실험패치 OFF |
| CHEAT OFFLINE | `DC_CHEAT_OFFLINE` | 300 % | 3840x2160 | Offline | Save 자동 백업 |

공통: Vulkan · 1280x720 · 16:9 · Shader High · Async Shader Mode · AA Auto · AF Auto ·
WCB/Depth/Strict Off · **VSync Full** · **VBlank 60** · **Frame Skip OFF** · Frame limit Auto ·
`Start games in fullscreen mode: false`(Borderless 처리용)

## 4. 단일 GUI 런처

* 파일: `E:\\PS3\\Launcher\\DragonCrownProEnhanced.exe`
* 빌드: **.NET 8 WPF · x64 · self-contained · single-file** (별도 Python/런타임 설치 불필요)
* 루트 경로를 하드코딩하지 않고 **exe 위치 기준**으로 탐지 (다른 드라이브에서도 동작)
* 게임 경로는 RPCS3 `config\\games.yml` 에서 자동 해석 (BCAS20298)
* 메인: PLAY / PRO MAX / NETPLAY / CHEAT OFFLINE + SAVES / GRAPHICS / SETTINGS / MAINTENANCE
* 상태 표시: GAME · VERSION · RPCS3 · GPU · DISPLAY · ReSHade · RPCN · CHEATS · VSYNC · PPU HASH
* PLAY는 사전 검사(RPCS3/게임/프로필/save 경로) 후 **v1.09 + Current + Vulkan + 4K 300 % + Borderless + VSync + VBlank 60 + ReShade Silent** 로 실행
* CLI(자동화/단축키용): `--launch PLAY|PROMAX|NETPLAY|CHEAT|KNOWNGOOD`, `--backup`, `--restore-window`, `--status <파일>`

## 5. Borderless

RPCS3는 **Vulkan Windowed** 로 실행하고, **게임 렌더 창만** Win32 창 스타일 제어로 테두리 없이
모니터 전체(자동 탐지 실제 해상도)로 전환한다. Alt+Enter는 사용하지 않으며, 게임 종료 시 원래
창 스타일/크기로 복원한다. GUI에서 Borderless / Fullscreen(fallback) / Windowed 를 선택한다.
상세: `GRAPHICS_PROFILES.md`

## 6. 60fps / tearing

VSync Full + Frame limit Auto + VBlank 60 + Frame Skip OFF. VBlank 120/240은 사용하지 않는다
(게임 속도/로직 영향). FPS 60인데 끊기면 tearing이 아니라 frame pacing 문제일 수 있으므로
RPCS3 성능 오버레이로 FPS/frame time/GPU/VRAM을 확인한다. NVIDIA 제어판과 RPCS3에서
limiter/VSync를 중복 강제하지 않는다.

## 7. ReShade

* 기본: **Silent 모드** — 효과는 적용하고 메뉴/OSD는 표시하지 않음, **Home** 키로 메뉴 호출
* 선택: **ZERO-BANNER** 자체 빌드(공식 v6.8.0 소스 + 최소 패치, splash까지 제거)
* 프리셋: Deband + 약한 CAS (`ReShade\\Presets\\DC_POSTFX_MINIMAL.ini`)
* 상세: `RESHade_GUIDE.md`

## 8. RPCN / Cheat / Save

* RPCN: RPCS3 자체 기능 사용. 런처는 상태 확인·설정 열기·Ready 검사·NETPLAY 프로필 실행만 담당.
  계정/토큰은 RPCS3가 관리하며 런처에 저장하지 않는다. 상세: `RPCN_NETPLAY.md`
* Cheat: 오프라인 전용, RPCS3 native Cheat Manager 우선. CHEAT OFFLINE 실행 시 RPCN OFF 확인 +
  Save 자동 백업. 상세: `CHEAT_GUIDE.md`
* Save: Original / AutoBackup(검증 manifest) / CheatTest 분리, Restore 전 현재 세이브 재백업.

## 9. 검증 상태

| 항목 | 결과 |
|---|---|
| PRO 4K 부팅/부팅 안정성 | 정상 (콜드부트 3회 포함, fatal 0) |
| 3840x2160 / Borderless | 실측 적용·복원 확인 |
| FPS / frame pacing | 59.5~60.1 fps, VSync Full |
| ReShade Silent / ZERO-BANNER | 이펙트 4/4 컴파일, splash 없음 |
| PRO MAX 5K | 400 % 정상, 시각 차이는 사용자 A/B 필요 |
| NETPLAY | 서버 도달성/프로필/가드 완료 (실제 매치 2대 PC 필요) |
| CHEAT | Cheat Manager 확인, Gold/SP 절차·검증기 완료 (주소 검색은 플레이 필요) |
| GUI 런처 | PLAY E2E 실측 (실행→Borderless 적용→게임 종료→자동 복원) |
| black box / alpha | 회귀 검사 절차·장면 7종 문서화 (시각 판정 필요) |

## 10. 완료 조건 체크 (요약)

- [x] RPCS3 Current 정상 / KnownGood 구성 / TITLE_ID·v1.09·PPU hash 확인
- [x] 4K·5K 프로필 / Borderless 기본 / VSync·VBlank·Frame Skip 설정
- [x] ReShade Silent (+ ZERO-BANNER 선택)
- [x] GUI 단일 exe 런처 + Legacy 정리 + 경로 독립
- [x] Save 자동 백업 / NETPLAY·CHEAT 분리 / KnownGood 폴백
- [ ] PRO MAX vs PRO 4K 시각 A/B (사용자 캡처)
- [ ] 30분 연속 플레이 (사용자)
- [ ] RPCN 실제 매치 (2대 PC)
- [ ] Gold/Skill Point 주소 검색 (플레이 필요)
