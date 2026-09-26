# DRAGON'S CROWN IDENTITY — 실제 게임/빌드 식별 정보

작성일: 2026-09-26
모든 값은 **실제 실행/파싱 결과**입니다(예상 ID 하드코딩 없음).

---

## 1. 게임 (덤프)

| 항목 | 값 | 확인 방법 |
|---|---|---|
| TITLE | **Dragon's Crown** | `PS3_GAME\PARAM.SFO` (Python SFO 파서) |
| TITLE_ID | **BCAS20298** | PARAM.SFO + RPCS3 로그 `Dragon's Crown [BCAS20298]` |
| CATEGORY | `DG` (Blu-ray 디스크 게임) | PARAM.SFO |
| VERSION (base) | `01.00` | PARAM.SFO |
| APP_VER (base) | `01.00` | PARAM.SFO |
| APP_VER (현재, 업데이트 적용 후) | **`01.09`** | `E:\PS3\RPCS3\dev_hdd0\game\BCAS20298\PARAM.SFO` |
| PS3_SYSTEM_VER (base) | `04.4600` | PARAM.SFO |
| 덤프 경로 | `C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\…\PS3_GAME\USRDIR\EBOOT.BIN` | games.yml / 런처 |
| 덤프 크기 | 1.97 GB (EBOOT.BIN 12,398,736 B) | 파일 시스템 |
| 세이브 폴더 | `dev_hdd0\home\00000001\savedata\BCAS20298-AUTO_0-` (SAVE0.DAT 2,157,336 B) | 파일 시스템 |
| 트로피 | `dev_hdd0\home\00000001\trophy\NPWR03852_00` | 파일 시스템 |

## 2. 실행 해시 (RPCS3 로그에서 추출)

| 항목 | 값 |
|---|---|
| **PPU executable hash** | **`PPU-bc3ee27f265ee62d1d26ebe2323b69384db5708b`** |
| SPU executable hash #1 | `SPU-682619389e289289e062d651c08907f98147df2a` |
| SPU executable hash #2 | `SPU-02d07d713e749feea4943b038c4dce11ea025fef` |
| PPU 캐시 폴더 | `E:\PS3\RPCS3\cache\BCAS20298\ppu-fUfKQehrwLz5ojg8vnYon0HUciMw-EBOOT.BIN\` |
| 로그 근거 | `E:\PS3\Logs\RUN8_RPCN_profile_no_account_RPCS3.log` → `ppu_loader: PPU executable hash: PPU-bc3ee27f…` |

> **주의**: 이 PPU hash는 업데이트된 v1.09 EBOOT 기준입니다. 게임을 01.00으로 되돌리면 해시가 달라집니다
> (`dev_hdd0/game/BCAS20298` 제거 시). cheat/patch의 hash 검증은 항상 현재 실행 중인 버전 기준으로 하십시오.

## 3. RPCS3 (Current / KnownGood)

| 항목 | 값 |
|---|---|
| Current | `E:\PS3\RPCS3` — **v0.0.42-20053-38eba804 Alpha \| master** (commit 38eba804, 2026-09-24/25) |
| KnownGood | `E:\PS3\RPCS3\KnownGood` — 동일 빌드 스냅샷 (1,756 파일 / 437.16 MB, 2026-09-26 생성) |
| 최신 여부 | master HEAD = `e447511a` (CI 변경 1건) → **현재 빌드가 최신**, 업데이트 불필요 |
| Qt | 6.11.2 (런타임/컴파일 동일) |
| 펌웨어 | PS3 4.93 (dev_flash) |

## 4. 호환성 / 참고 자료 (다른 지역판과의 관계)

| TITLE_ID | 지역/판 | 버전 | RPCS3 호환성 | PPU hash (참고) | 비고 |
|---|---|---|---|---|---|
| **BCAS20298** (현재 보유) | Asia (Zh,Ko) | 01.00 → **01.09** | Playable | **PPU-bc3ee27f…** (v1.09) | 업데이트 PKG 공식 서버 검증 완료 |
| BLUS30767 | US | 01.09 | Playable | (Artemis 자료: `PPU-60f7a7f5…` = v1.00 기준) | **다른 판 → cheat 주소 호환 안 됨** |
| BLES01950 | EU | 01.09 | Playable | — | 과거 SPU GETLLAR 부팅 회귀(#17200)는 수정됨 |

* Artemis Patch Collection의 Dragon's Crown 항목은 **BLUS30767 v1.00** 전용이며 현재 게임과
  serial·version·PPU hash가 모두 다릅니다 → **적용 금지**(연구 자료로만 보관:
  `E:\PS3\Cheats\Dragons_Crown\Artemis_Research\`).

## 5. 온라인/RPCN

| 항목 | 값 |
|---|---|
| RPCN 프로필 | `E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY\config.yml` (PSN status: RPCN) |
| 계정 파일 | `E:\PS3\RPCS3\config\rpcn.yml` (미생성 — GUI에서 생성 필요) |
| 서버 | `np.rpcs3.net:31313` (도달성 확인) |
| 실측 | 부팅 시 `sys_net: P2P port 3658 was bound!` (같은 PC에서 2인스턴스 금지) |

## 6. 문서/도구 위치

| 목적 | 경로 |
|---|---|
| 4K/5K 보고서 | `Docs\DRAGONS_CROWN_4K_REPORT.md` |
| RPCN 가이드 | `Docs\RPCN_NETPLAY_GUIDE.md` |
| Cheat 가이드 | `Docs\CHEAT_GUIDE.md` |
| 그래픽 regression | `Docs\GRAPHICS_REGRESSION.md` |
| 세이브 백업 | `Tools\save_backup.ps1`, `Saves\Dragons_Crown\` |
| Cheat 검증기 | `Tools\validate_cheat_import.py` |
