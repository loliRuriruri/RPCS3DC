# CHEAT GUIDE — Dragon's Crown / RPCS3 native Cheat Manager

작성일: 2026-09-26
대상: `BCAS20298` v1.09 / PPU `bc3ee27f265ee62d1d26ebe2323b69384db5708b`
원칙: **RPCS3 native 기능 우선** (Cheat Manager → Memory Viewer → Patch Manager → Artemis → 필요 시에만 Cheat Engine)

---

## 1. 이 빌드의 Cheat Manager 확인 결과

`E:\PS3\RPCS3\rpcs3.exe` 문자열/소스(`rpcs3qt/cheat_manager.cpp`) 기준으로 **모든 요구 기능이 존재**합니다.

| 기능 | 상태 | 위치/이름 |
|---|---|---|
| Cheat Manager | 있음 | 게임 목록 → 게임 우클릭 → **Cheats** |
| Cheat Search / New Search | 있음 | Cheat Manager 창의 **New Search** |
| Filter Results | 있음 | 검색 후 값 입력 → **Filter Results** |
| Current Value 표시 | 있음 | 목록의 **Current Value:** 열 |
| Apply | 있음 | cheat 선택 후 **Apply** |
| Add to cheat list | 있음 | 우클릭 → **Add to cheat list** (중복 시 “Cheat already exists. Do you want to overwrite…?”) |
| Memory Viewer | 있음 | 우클릭 → **Show in Memory Viewer** |
| Import Cheats / Export Cheats | 있음 | 창 하단 **Import Cheats** / **Export Cheats** |
| Reverse-Lookup Cheat | 있음 | 값 → cheat 역추적 |
| 저장 파일 | `E:\PS3\RPCS3\config\cheats.yml` | RPCS3가 직접 관리 |
| 데이터 타입 | Signed/Unsigned 8/16/32/64, Float 등 | **정수 우선**, Float는 §7 주의 |
| Unsafe 표시 | 있음 | 위험한 cheat에 **Unsafe** 라벨 |

> 별도 최신 빌드 테스트는 **불필요**합니다(기능 존재 + 현재 빌드가 최신). 기존 실행본은 그대로 유지합니다.

## 2. 실행 순서 (안전한 사용 흐름)

1. **`E:\PS3\Launchers\Dragon_Crown_CHEAT_OFFLINE.cmd`** 로 실행
   * 실행 전 **세이브 자동 백업**(검증 포함) → `E:\PS3\Saves\Dragons_Crown\AUTO_BACKUP\DC_SAVE_<시각>_cheat_offline`
   * 프로필 확인: `Internet enabled: Disconnected` + `PSN status: Disconnected` (온라인 차단)
   * ReShade 차단(`DISABLE_VK_LAYER_reshade_1=1`)
2. 게임에서 **치트 대상 값을 확인**(예: Gold = X)
3. RPCS3 GUI 게임 목록 → 게임 우클릭 → **Cheats**
4. 검색/필터/적용 (§4~§6)
5. 검증 후 **Add to cheat list** 로 저장 → `config\cheats.yml` 에 기록됨
6. 결과를 `E:\PS3\Cheats\Dragons_Crown\Custom\CHEAT_DB.yml` 에 metadata와 함께 기록
7. 세션이 끝나면 **치트를 모두 비활성**하고, 온라인 플레이는 **`Dragon_Crown_NETPLAY_SAFE.cmd`** 로만

## 3. 절대 규칙

* **치트 활성 상태에서 RPCN/온라인 금지** (`DC_CHEAT_OFFLINE` 프로필은 네트워크 자체를 차단)
* 원본 세이브로 실험 금지 → `Saves\Dragons_Crown\CHEAT_TEST` 또는 백업 후 진행
* MAX 값을 먼저 쓰지 않는다 (+1, +5 부터)
* 검증되지 않은 외부 cheat는 import 금지 → `Tools\validate_cheat_import.py` 통과 후 `Cheats\...\Imported\` 에만
* 다른 지역판(BLUS30767/BLES01950) 주소를 현재 v1.09에 복사하지 않는다
* 게임 파일 자체를 영구 변조하지 않는다(RPCS3 runtime/patch 기능만 사용)

## 4. Tier A-1 : Gold

| 단계 | 조작 |
|---|---|
| 1 | 현재 Gold **X** 기록 (게임 UI) |
| 2 | Cheat Manager → **New Search** → 값 `X` (Unsigned 32 우선) |
| 3 | 게임에서 아이템 구매/판매 → Gold **Y** |
| 4 | **Filter Results** → `Y` |
| 5 | 다시 변화 → **Z** → Filter `Z` (후보가 1~3개 남을 때까지) |
| 6 | 후보 우클릭 → **Show in Memory Viewer** 로 주변 구조 확인(아이템 ID/카운트와 혼동 주의) |
| 7 | **Apply** 로 `X + 100` 적용 → 게임 UI에서 실제 증가 확인 |
| 8 | 게임 저장 후 재시작 → 값 유지/초기화 여부 확인(**Persistence** 기록) |
| 9 | 우클릭 → **Add to cheat list** → 이름 `Gold Editor` |
| 10 | `CHEAT_DB.yml` 에 metadata 기록 |

* 검증 성공 조건: **게임 UI와 실제 저장 결과가 일치**. 실패 시 값을 되돌리고 후보를 다시 좁힌다.
* Gold는 저장 데이터에 직렬화되므로 `Save effect: confirmed` 로 기록될 가능성이 높다.

## 5. Tier A-2 : Skill Points (SP)

* Gold와 동일한 절차(New Search → 값 변화 → Filter 반복 → Apply +1/+5).
* **중요 연구 노트**: 과거 커뮤니티 자료(FearlessRevolution CE table, BLUS30767 01.06)에서
  *“Skill Points are broken … Skills reset when changing areas or saving”* 라는 보고가 있습니다.
  즉 SP는 **런타임 전용(비영속)일 가능성**이 높습니다. 검증 시 반드시
  “영역 이동/저장 후 값 유지 여부”를 확인하고 `Persistence: runtime` 으로 기록하십시오.
* SP를 999 같은 값으로 만들지 말 것(스킬 트리/세이브 구조 검증 전까지).

## 6. Tier A-3 : Item Count / Score

* Item Count: 같은 아이템을 5개 → 검색 `5` → 1개 사용 → `4` → Filter → 1개 추가 → `5` → Filter.
  * 인벤토리 컨테이너 구조(아이템 ID + 개수 + 슬롯)와 단순 숫자를 혼동하지 말 것.
  * **아이템 복제/잘못된 ID 생성 기능은 만들지 않는다**(1차 버전 금지).
* Score: 스테이지 클리어 점수는 판정 직후에만 존재하는 임시값일 수 있음 → `Persistence: runtime` 예상.

## 7. Float 검색 주의

현재 RPCS3 Cheat Manager의 Float32 검색 관련 공개 이슈가 있으므로 **1차 버전은 정수만** 다룹니다.
Float가 필요하면 ① Cheat Manager ② Memory Viewer ③ (필요 시) 외부 debugger 로 **교차 검증**하고,
결과 하나만으로 patch를 확정하지 않습니다.

## 8. 메모리 특성 메모 (조사 결과)

* PS3(PPC)는 **big-endian** 게스트입니다. 커뮤니티 CE 자료에도 *“2byte big endian 으로 스캔”* 이라는
  조언이 있습니다(MP 계열). RPCS3 Cheat Manager는 **게스트 주소 공간**에서 동작하므로
  타입(Unsigned 16/32)을 바르게 고르면 엔디안은 RPCS3가 처리합니다. 값이 안 잡히면
  16비트/8비트 또는 Signed 로 재시도하십시오.
* 값이 여러 개 잡히면 “게임 UI 값이 바뀔 때 같이 바뀌는 후보”만 남깁니다(Filter Results).

## 9. Save 보호 / CHEAT_TEST 분리

```
E:\PS3\Saves\Dragons_Crown\
  ORIGINAL\      ← 최초 1회 복사한 원본 (절대 덮어쓰지 않음)
  AUTO_BACKUP\   ← DC_SAVE_<timestamp>_<label> (검증 manifest 포함, 최신 20개 유지)
  CHEAT_TEST\    ← 치트 실험 전용 세이브(게임 내에서 별도 슬롯으로 저장)
```

* 백업: `powershell -File E:\PS3\Tools\save_backup.ps1 -Label "before_gold_test"`
* 복구: 해당 `AUTO_BACKUP\DC_SAVE_...` 폴더를 `dev_hdd0\home\00000001\savedata\` 로 복사
  (기존 폴더는 먼저 이름을 바꿔 보존)
* save corruption 발생 시 **즉시 이전 백업으로 복구**하고, 해당 cheat를 `Status: retired` 로 기록

## 10. Save Editor Research Mode (2단계, 아직 미구현)

* Runtime cheat(메모리)과 Save Editor(파일)는 **별도 구현**입니다.
* 1차는 Runtime(Cheat Manager) 완성. 이후 `CHEAT_TEST` 세이브로 Gold/SP 직렬화 구조를 조사합니다.
* Save Editor를 만들 경우 필수 기능: 자동 backup, magic/header 검사, version 검사, bounds 검사,
  checksum 존재 여부 확인, dry-run, diff preview, **원본 overwrite 금지**.
* 현재 상태: `SAVE0.DAT`(2,157,336 B) 구조 조사만 수행(아래 §11).

## 11. 세이브 파일 조사 (초기)

| 항목 | 값 |
|---|---|
| 파일 | `Saves\Dragons_Crown\ORIGINAL\BCAS20298-AUTO_0-\SAVE0.DAT` |
| 크기 | 2,157,336 바이트 |
| SHA256 | `B6FD91C468C0D49E651BAE13BB81F5332560A271445F7409A9C0422E5CF056CF` |
| PARAM.SFO | 세이브 메타(타이틀/아이콘) 포함 |
| 결론 | 1차 버전에서는 **런타임 치트만** 사용. 파일 구조 분석은 별도 단계(자동 backup/dry-run 전제) |

## 12. Artemis 조사 결과 (적용 금지 — 연구 자료)

| 항목 | 값 |
|---|---|
| 저장소 | `chidreams/ARTEMIS-RPCS3-Cheat-Manager`(관리 도구, imported_patch.yml 편집기) / `chidreams/Artemis-Patch-Collection-RPCS3`(치트 모음) |
| Dragon's Crown 항목 | **`Dragon's Crown [BLUS30767] v1.00`** (1개) |
| PPU hash | `PPU-60f7a7f5e0de894da8e5de235609228c40e70197` |
| 포함 cheat | Infinite HP / Max Gold After Buying Something / Score Multiplier 100x / Infinite Ammo / Infinite Items / 99999999 Score / Infinite MP / Infinite Skill Points / Max Gold On Gain |
| 작성자 | Hiei-YYH, games24.blog.fc2.com, Medo → ChIdReamS 포팅 |
| **현재 게임과 비교** | serial `BLUS30767` ≠ `BCAS20298`, version `1.00` ≠ `01.09`, PPU hash 불일치 → **적용 불가** |
| 검증기 결과 | `python Tools\validate_cheat_import.py "Cheats\Dragons_Crown\Artemis_Research\Dragon's Crown [BLUS30767] v1.00.yml"` → **REJECTED** (hash/serial/version 3건) |
| 보관 위치 | `E:\PS3\Cheats\Dragons_Crown\Artemis_Research\` |

> 과거 CE table(BLUS30767 01.06)에는 Infinite HP/MP/LP, Invincible, Durability, Item Quantity,
> Score Multiplier, camera/weapon/accessory 포인터 등이 있었습니다. 이는 **“수정 가능한 데이터 종류”** 의
> 참고 자료일 뿐이며, 주소는 현재 v1.09에서 **다시 찾아야** 합니다.

## 13. Cheat Import 방어 절차

1. 외부 text/YAML 을 **바로 import 하지 않는다**
2. `python E:\PS3\Tools\validate_cheat_import.py <file>` 실행
   * 검사: YAML 문법 → PPU hash → serial → version → 주소 범위/정렬 → 위험(코드 패치) 경고
   * `--allow-foreign` 은 **문법만** 검사(다른 지역판 자료를 “연구용”으로 검사할 때만)
3. 통과 시 백업 생성: `config\imported_patch.yml`, `config\cheats.yml`(또는 `patch_config.yml`)
4. RPCS3 Patch Manager → **Import**(드래그&드롭) 또는 Cheat Manager → **Import Cheats**
5. 클립보드 데이터를 그대로 Import 하지 않는다

## 14. Cheat Engine 정책

우선순위: **① RPCS3 Cheat Manager → ② Memory Viewer → ③ Patch Manager → ④ Artemis 관리도구 → ⑤ 필요 시에만 Cheat Engine**.
Cheat Engine 사용 시에도 RPCS3 프로세스 메모리 구조/엔디안을 정확히 확인하고,
**Windows 보안 기능을 끄라는 오래된 튜토리얼은 따르지 않습니다.**

## 15. NETPLAY / CHEAT 분리

| 구분 | 프로필 | 보장 |
|---|---|---|
| 치트(오프라인) | `DC_CHEAT_OFFLINE` + `Dragon_Crown_CHEAT_OFFLINE.cmd` | 네트워크 Disconnected, 세이브 자동 백업, ReShade 차단 |
| 넷플레이(치트 금지) | `DC_NETPLAY_SAFE` + `Dragon_Crown_NETPLAY_SAFE.cmd` | `cheats.yml`/`patch_config.yml` 에 BCAS20298 항목이 있으면 **경고 후 확인 요구** |
| 일상 4K | `DC_4K_ULTRA` + `Dragon_Crown_4K_ULTRA.cmd` | 치트/네트워크 미사용 |

* 같은 세이브를 치트로 수정한 뒤 RPCN으로 접속하는 행위는 금지합니다(상대방과 상태 불일치 → 동기화 오류/불공정).
* 넷플레이 전에는 반드시 `Dragon_Crown_NETPLAY_SAFE.cmd` 로 “cheat/패치 없음” 확인을 통과하십시오.

## 16. 고주사율 / Lossless Scaling (실험)

* 현재 PC 디스플레이는 **3840x2160 @ 60 Hz** 이므로 LSFG(프레임 생성)는 **해당 없음**.
* 120/144 Hz 환경이라면 `DC_EXPERIMENTAL_LSFG` 프로필(4K, 창모드)로만 실험하십시오.
* LSFG는 **에뮬레이션 정확도를 높이는 기능이 아니며**, 입력 지연/고스팅/HUD 아티팩트가 늘 수 있습니다.
  기본 4K ULTRA 프로필에는 적용하지 않습니다.

## 17. 60 FPS 관련

* Dragon's Crown PS3판은 원래 60 FPS 타이틀이며(실측 59.9), 공식 patch DB에 **패치가 존재하지 않습니다**.
* 따라서 60 FPS patch / VBlank hack / game speed patch를 **만들지도, 쓰지도 않습니다**.
