# Dragon's Crown PC Edition for RPCS3

PS3판 《Dragon's Crown》(BCAS20298, v1.09)을 RPCS3에서 고품질 PC 환경으로 즐기기 위한
**설정·런처·문서 프로젝트**입니다. **Phase 1 RC** 상태입니다.

> 이 저장소에는 게임 파일, 업데이트 PKG, 펌웨어, DLC, 세이브, RPCN 계정 정보,
> Sony/Atlus 저작물이 **포함되지 않습니다.** 본인 소유의 덤프와 공식 업데이트만 사용하세요.

## Requirements

* Windows 10/11 x64
* RPCS3 (검증: `v0.0.42-20053-38eba804`)
* PS3 펌웨어 4.93 (사용자 설치)
* 본인 소유 Dragon's Crown (Asia) 덤프 + 공식 업데이트 01.09
* 선택: ReShade 6.8.0 (Vulkan), Sunshine/Moonlight(Remote Co-op), RPCN 계정

## Quick Start

1. `Launcher\DragonCrownProEnhanced.exe` 실행 (빌드: `Launcher\build_launcher.cmd`)
2. `SETTINGS → Graphics` 에서 프리셋(Standard / Pro Enhanced)과 해상도(4K 300% / 5K 400%) 선택
3. `PLAY` → Standard 또는 Pro Enhanced

```
Launcher\DragonCrownProEnhanced.exe --status            상태 요약
                                   --diag               진단(전체 항목)
                                   --backup-settings    설정 백업
                                   --restore-window     Borderless 즉시 복원
                                   --resolution 4K|5K   해상도 선택
                                   --launch STANDARD|PROENHANCED|RPCN|RPCNSAFE|CHEAT|KNOWNGOOD
```

## Graphics Modes

| 모드 | 내용 |
|---|---|
| **Standard** | RPCS3 자체 화질 — 4K/5K · AF 16x · MSAA Auto · Stretch Off · 16:9 · VSync Full · VBlank 60 · Frame Skip Off · **ReShade 없음** |
| **Pro Enhanced** | Standard + ReShade `DC_PRO_ENHANCED.ini` (Deband + CAS 0.32 + Levels + Vibrance 0.10 + SMAA) · **Silent** (시작 문구 없음, Home 키로 메뉴) |

* `Pro Enhanced` 는 프로젝트 자체 프리셋 이름이며 PS4 Pro 리소스와 무관합니다.
* 4K/300% = Recommended, 5K/400% = Super Sampling.

## Multiplayer Modes

| 모드 | 내용 | 상태 |
|---|---|---|
| **Local** | 같은 PC 1P/2P (2P 패드 필요) | 구현 완료 / 하드웨어 테스트 대기 |
| **Remote Co-op** | Sunshine + Moonlight (친구가 2P 입력) | 구현 완료 / 환경 테스트 대기 |
| **RPCN Online** | 각자 RPCS3 + 캐릭터 + 세이브 · **Netplay Safe** 기본 ON | `RPCN_PARTIAL` / 실제 2-PC 검증 대기 |

## Safety

* 원본 RPCS3 설치본과 게임 파일(EBOOT/스크립트/네트워크 코드)은 **수정하지 않습니다.**
* **Live savedata 와 trophy 는 Reset/복원/삭제 대상이 아닙니다.**
* 자동 생성된 세이브 백업 사본(AutoBackup)은 **최신 20세대만 보존**됩니다.
* 설정 변경 전에는 `Backups\<시각>_<라벨>` 로 자동 백업합니다.
* 치트는 오프라인 전용이며, NETPLAY(RPCN/Netplay Safe)에서는 활성 치트/패치가 있으면 차단합니다.

## Current Validation Status

| 항목 | 상태 |
|---|---|
| Standard / Pro Enhanced 부팅 (4K 300%, 5K 400%) | PASS (타이틀 59.8~60.1 fps) |
| Standard ReShade 미로드 / Pro 6종 로드 / Safe 강제 OFF | PASS |
| Borderless 적용·복원 / `--restore-window` | PASS |
| Backup / Restore / Reset / KnownGood | PASS |
| RPCN 사전 검사(버전·계정·치트) | PASS |
| 30분 플레이 / Local 2P / Remote Co-op / RPCN 실제 매치 | **PENDING** (사용자 검증) |

자세한 체크리스트: `docs/ACCEPTANCE_TEST.md`

## Docs

| 문서 | 내용 |
|---|---|
| `docs/GRAPHICS_PROFILES.md` | 그래픽 프리셋 정의·해상도 선택·실측 |
| `docs/MULTIPLAYER.md` | Local / Remote Co-op / RPCN + Netplay Safe |
| `docs/ACCEPTANCE_TEST.md` | 최종 승인 체크리스트 (PASS/FAIL/PENDING) |
| `docs/TROUBLESHOOTING.md` | 문제 해결 |
| `docs/ROLLBACK.md` | 원상 복구 |
| `docs/RPCS3_CHANGELOG.md` | 전체 변경 이력 |
| `docs/VERIFICATION_REQUEST.md` | 검증 요청서 (주장/증거/재현 명령) |

## Phase 2 (out of scope)

PS4 《Dragon's Crown Pro》 리소스 연구(고해상도 텍스처, UI, 아트워크, 오케스트라 음원,
리소스 매핑)는 **Phase 2** 로 분리되어 있으며 이 저장소의 현재 코드에는 포함되지 않습니다.
