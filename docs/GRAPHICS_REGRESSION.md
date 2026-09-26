# GRAPHICS REGRESSION — Dragon's Crown (black box / alpha 이슈)

작성일: 2026-09-26
목적: RPCS3 업데이트/설정 변경 후 **검은 반투명 box(alpha/블렌딩 아티팩트)** 및 기타 그래픽 회귀를
동일 장면 비교로 빠르게 확인한다.

---

## 1. 알려진 이슈 (조사 결과)

| 출처 | 내용 |
|---|---|
| RPCS3 Wiki — Dragon's Crown, Known Issues | **“Lines on backdrops — Issue 4133: The game has a problem with alpha channels, which is why black lines are visible on some backdrops.”** |
| GitHub Issue **#4133** (BLUS30767 texture clamping) | Vanillaware 렌더러가 화면을 **타일(texture atlas)로 쪼개 그리면서 1px 경계**가 생기고, 게임이 **S.src_alpha + D.(1-src_alpha)** 블렌딩을 쓰는데 PS3 출력 포맷이 alpha를 지워버리는 구조. RPCS3가 “alpha 폐기” 플래그를 완벽히 전파하지 못해 **이음새/검은 경계**가 보임. GL_CLAMP(레거시) 동작 차이도 원인 |
| GitHub Issue **#9150** (Odin Sphere Leifthrasir, 같은 엔진) | 동일 계열 투명/검은 box 문제가 **0.0.38-18404에서도 지속**, “March 2026에도 여전함” 보고 |
| Reddit r/DragonsCrown (2026-05-25) “RPCS3 Black boxes” | **어두운 지역에서 캐릭터/이펙트 뒤 검은 반투명 box**가 보인다는 현재 보고 |
| GitHub Issue **#3013** (과거 부팅/블랙스크린) | 과거 BCAS20298 v1.00용 **Sync Fix 패치**(PPU `5a321fc3…`)가 존재했으나 에뮬레이터 측 수정(#3158)으로 해결. 현재 v1.09 해시(`bc3ee27f…`)와 다르므로 **재사용 금지** |

**결론**: 이 이슈는 게임 asset/블렌딩 구조 + 에뮬레이터 alpha 처리의 조합이며,
특정 설정(WCB/Strict/AF)으로 완전히 사라지지 않을 수 있습니다. 따라서 “회귀 감시”가 목적입니다.

## 2. 베이스라인 캡처 규칙

* 저장 위치: `E:\PS3\Screenshots_AB\Dragons_Crown\REGRESSION_BASELINE\`
* 파일명: `DC_GRAPHICS_BASELINE_<장면>_<해상도>.png` (예: `DC_GRAPHICS_BASELINE_dungeon_dark_300.png`)
* 조건 고정: 동일 세이브, 동일 장면, 동일 프로필(`DC_4K_ULTRA`), ReShade OFF, Frame limit Auto
* 캡처 방법: 게임 중 **F12** (RPCS3 스크린샷) → 스크린샷 폴더에서 위 경로로 복사

## 3. 회귀 테스트 장면 (필수)

| # | 장면 | 확인 포인트 |
|---|---|---|
| 1 | **어두운 dungeon** | 캐릭터/이펙트 주변 검은 반투명 box, 배경 검은 선 |
| 2 | **강한 spell effect** | 이펙트 경계의 검은 사각형, alpha 깨짐 |
| 3 | **캐릭터 주변 alpha effect** | 반투명 sprite 주변 테두리 |
| 4 | **다수 캐릭터 전투** | 겹친 sprite 간 이음새 |
| 5 | **불/연기** | 반투명 연기 경계, 색 번짐 |
| 6 | **반투명 sprite (메뉴/초상화)** | 메뉴 컵/그림 등 정지 이미지의 검은 box(과거 사례) |
| 7 | **boss battle** | 큰 이펙트에서 프레임 드랍 + 아티팩트 동시 확인 |

## 4. 검사 절차 (RPCS3 업데이트 또는 설정 변경 후)

1. `Dragon_Crown_4K_ULTRA.cmd` 로 실행 (치트/ReShade/패치 없음)
2. §3 장면을 순서대로 방문하며 F12 캡처 → `Screenshots_AB\Dragons_Crown\REGRESSION_<날짜>\` 에 저장
3. `REGRESSION_BASELINE` 이미지와 나란히 비교 (Windows 사진 앱 또는 이미지 뷰어)
4. 판정:
   * **PASS**: 차이 없음 / 차이가 있어도 개선 방향
   * **FAIL**: 검은 box 증가, 배경 선 증가, sprite 깨짐 → **이전 빌드(KnownGood)로 롤백**
5. 결과를 `E:\PS3\Docs\RPCS3_CHANGELOG.md` 에 날짜/빌드와 함께 기록

## 5. 설정 A/B (회귀가 의심될 때)

| 시도 | 프로필 | 기대 |
|---|---|---|
| WCB On | `DC_4K_WCB_ON` | 렌더 타깃 메모리 초기화로 일부 alpha/깜빡임 완화 가능(성능 비용) |
| Strict Rendering On | `DC_4K_STRICT` | 드물게 누락 그래픽 완화, **해상도 배율 비활성** → Daily 사용 금지 |
| Read Color/Depth On | (config 수동) | alpha/깊이 초기화 이슈 완화 가능, 성능/아티팩트 주의 |
| 해상도 100 % | `CLEAN` | 업스케일 관련 아티팩트인지 판별(100 %에서도 보이면 게임/에뮬레이터 고유) |

> 위 A/B는 **회귀 판별용**이며, “검은 box가 사라지는 설정”을 찾으면 그 결과를 문서에 남기고
> Daily 프로필에 반영할지는 별도로 판단합니다(화질/성능 trade-off).

## 6. 자동화 여지

* 현재 세션에서는 화면 캡처 도구가 없어 **자동 비교 불가** → 위 절차를 수동으로 수행합니다.
* 향후 자동화하려면: RPCS3 스크린샷 폴더 감시 + 기준 이미지와 픽셀 diff(예: Pillow/OpenCV) 스크립트.
  스크립트 골격은 `E:\PS3\Tools\` 에 추가 가능(요청 시).
