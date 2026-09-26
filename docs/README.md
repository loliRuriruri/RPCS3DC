# Dragon's Crown PRO Enhanced for RPCS3 — 문서

| 문서 | 내용 |
|---|---|
| `DRAGONS_CROWN_PRO_ENHANCED.md` | 프로젝트 총괄 (구조·프로필·런처·검증·완료 조건) |
| `GRAPHICS_PROFILES.md` | 그래픽/디스플레이 (4K/5K, Borderless, VSync, 해상도 배율) |
| `RPCN_NETPLAY.md` | RPCN 넷플레이 (설정 위치·버전 동기화·테스트 순서·진단) |
| `CHEAT_GUIDE.md` | 오프라인 치트 (Cheat Manager·세이브 보호·검증기) |
| `RESHade_GUIDE.md` | ReShade Silent / ZERO-BANNER / 프리셋 |
| `TROUBLESHOOTING.md` | 문제 해결 (부팅·화질·성능·넷플레이·치트) |
| `CHANGELOG.md` | 변경 이력 |
| `ROLLBACK.md` | 단계별 원복 절차 |
| `TEST_PROTOCOL.md` | 사용자 A/B 캡처 및 30분 안정성 검증 절차 |
| `DRAGONS_CROWN_IDENTITY.md` | TITLE_ID / 버전 / PPU hash 등 식별 정보 |
| `DRAGONS_CROWN_4K_REPORT.md` | 4K/5K 측정 보고서 |
| `RPCS3_SETUP_REPORT.md` | RPCS3 환경 구축 보고서 |
| `ZERO_BANNER_REPORT.md` | ZERO-BANNER ReShade 빌드/검증 보고서 |
| `GRAPHICS_REGRESSION.md` | black box / alpha 회귀 검사 |

## 빠른 시작

1. `Launcher\DragonCrownProEnhanced.exe` 실행
2. **PLAY** → 4K 300% · Borderless · VSync · ReShade Silent 로 바로 실행
3. 최고 화질은 **PRO MAX**, 온라인은 **NETPLAY**, 치트는 **CHEAT OFFLINE**
4. 문제 시 **MAINTENANCE → KnownGood RPCS3**

## 원칙

* 원본 RPCS3 설치본과 `rpcs3.exe` 바이너리는 수정하지 않는다.
* save / trophy / dev_hdd0 / config / RPCN data / patch data 는 삭제하지 않고, 변경 전 백업한다.
* 치트는 오프라인 전용이며 NETPLAY에서는 금지한다.
* 공유 패키지에 게임 파일·펌웨어·계정·세이브·스크린샷·로그를 포함하지 않는다.
