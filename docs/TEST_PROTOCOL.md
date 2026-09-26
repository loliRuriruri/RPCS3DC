# TEST PROTOCOL — Dragon's Crown 4K A/B 및 안정성 검증 (사용자 수행)

자동 세션에서는 로그/성능 검증까지만 가능했습니다(화면 캡처 도구 없음).
아래 절차로 **시각 A/B와 30분 안정성**을 확정하면 최종 프로필이 결정됩니다.

---

## 0. 준비

| 항목 | 내용 |
|---|---|
| 실행 | `E:\PS3\Launchers\RPCS3_DRAGONS_CROWN.cmd` (4K 300 %) 등 원하는 프로필 런처 |
| 게임 내 설정 | 동일 세이브 사용. 게임 내 밝기/언어/카메라 옵션은 모든 캡처에서 동일하게 유지 |
| 캡처 | 게임 중 **F12** (RPCS3 스크린샷) → 저장 위치: RPCS3 → 게임 우클릭 → “폴더 열기 → 스크린샷 폴더”. 캡처 후 `E:\PS3\Screenshots_AB\Dragons_Crown` 로 복사 |
| 성능 오버레이 | RPCS3 GUI → 설정 → **Emulator → 성능 오버레이 → Enabled**, `Enable Frametime Graph` 켜기 → FPS / frametime / 1 % low 확인 |
| 프레임 제한 | 프로필 그대로(Frame limit = Auto, VSync = Disabled). 화면 찢어짐이 보이면 VSync만 “Full”로 A/B |

## 1. 장면 체크리스트 (동일 위치·동일 세이브)

- [ ] Title screen
- [ ] Character select
- [ ] Town (마을)
- [ ] Guild (모험자 길드)
- [ ] Stage entrance (스테이지 진입)
- [ ] 전투 인원이 많은 장면
- [ ] 마법 이펙트가 많은 장면
- [ ] 불 / 연기 / 반투명 효과
- [ ] 보스전
- [ ] HUD / 텍스트
- [ ] (가능하면) 후반 던전

## 2. A/B 매트릭스

| 비교 | 프로필 A | 프로필 B | 파일명 예 |
|---|---|---|---|
| 해상도 | `DC_4K_ULTRA` (300 %) | `DC_5K_SSAA` (400 %) | `DC_300_…`, `DC_400_…` |
| 해상도(실험) | `DC_5K_SSAA` | `DC_8K_SCREENSHOT` (600 %) | `DC_600_SCREENSHOT.png` |
| AF | `DC_4K_ULTRA` (Auto) | `DC_4K_ULTRA_AF16` | `DC_300_AF_auto.png`, `DC_300_AF16.png` |
| WCB | `DC_4K_ULTRA` (Off) | `DC_4K_WCB_ON` | `DC_400_WCB_ON.png` |
| Strict | `DC_4K_ULTRA` | `DC_4K_STRICT` | `DC_300_STRICT.png` |
| CAS | `DC_4K_CAS0` | `DC_4K_CAS15` / `DC_4K_CAS25` | `DC_300_CAS0.png`, `DC_300_CAS15.png` |
| 후처리 | `RPCS3_DRAGONS_CROWN.cmd` | `RPCS3_DRAGONS_CROWN_POSTFX.cmd` | `DC_400_RESHADE.png` |

파일명 규칙(권장): `DC_<스케일>_<옵션>.png` → `E:\PS3\Screenshots_AB\Dragons_Crown`

## 3. 판정 기준 (숫자가 아니라 순서)

1. 렌더링 오류 없음 (배경 누락 / 검은 효과 / 반투명 오류 / 스프라이트 오류 / 색상 깨짐 / post-processing 이상)
2. 정상 부팅
3. 안정적인 frame pacing (frametime 그래프가 평평한지, 1 % low가 60 fps 근처인지)
4. 원본 artwork 보존 (색/대비 변화 없음)
5. 눈으로 확인 가능한 화질 개선 (윤곽/텍스트/이펙트)
6. GPU 여유 (오버레이 GPU 사용률)

* 300 % vs 400 % 차이가 눈에 보이면 400 %, 거의 없으면 300 %.
* 600 %가 400 %와 거의 같으면 600 %는 스크린샷 전용.
* CAS는 **Output Scaling Mode = FSR일 때만** 동작합니다. 4K 1:1에서는 켜지 않는 것을 권장합니다.
* Strict Rendering은 화질 옵션이 아니며, 켜면 해상도 배율/AF가 비활성화됩니다.

## 4. 30분 안정성 테스트 (필수)

- [ ] 전투·보스 포함 30분 이상 연속 플레이
- [ ] 크래시 / 행 없음
- [ ] 오디오 끊김·깨짐 없음
- [ ] 컨트롤러 입력 정상(DualSense)
- [ ] 컷신 정상 재생 및 속도 정상
- [ ] 게임 속도(애니메이션/물리/입력/QTE) 정상
- [ ] 세이브 저장/로드 정상
- [ ] 프레임 pacing 유지(오버레이)

문제 발생 시: 그 프로필 이름과 증상, `E:\PS3\Logs` 에 남길 로그(RPCS3.log)를 함께 기록하십시오.

## 5. 결과 반영 방법

| 결정 | 방법 |
|---|---|
| Daily를 400 %로 승격 | `E:\PS3\Profiles\DC_5K_SSAA\config.yml` 을 `E:\PS3\RPCS3\config\custom_configs\BCAS20298.yml` 로 복사 |
| Daily를 300 %로 유지 | 그대로 (이미 300 % 적용됨) |
| AF 16x 채택 | `DC_4K_ULTRA_AF16` 을 커스텀 설정으로 복사 |
| ReShade 채택/해제 | 채택: POSTFX 런처 사용 / 해제: `ReShade_Disable.cmd` |
| 60 FPS 패치 | **해당 없음** — 공식 patch DB에 Dragon's Crown 패치가 존재하지 않으며, 게임은 이미 60 fps로 동작합니다. vblank 조작 금지 |

## 6. 보고용 기록 양식

```
[장면] Stage entrance / 보스전
[프로필] DC_4K_ULTRA vs DC_5K_SSAA
[차이] (예: 무기 외곽선과 HUD 텍스트가 400 %에서 더 깨끗함 / 차이 거의 없음)
[FPS] 평균 __ / 1% low __ / frametime 그래프 __
[GPU] 사용률 __ % / VRAM __ GB
[오류] 없음 / (증상 기술)
[결론] Daily = 300 % or 400 %
```
