# ROLLBACK — 단계별 원복 절차

모든 변경은 독립적으로 되돌릴 수 있습니다. **아래 명령은 관리자 권한이 필요한 항목만 표시했습니다.**
세이브/트로피는 어떤 절차에서도 삭제하지 않습니다.

---

## 0. 백업 목록

| 폴더 | 내용 | 복원 대상 |
|---|---|---|
| `E:\PS3\Backups\01_original\config\` | 원본 config.yml / games.yml / uuid / recording.yml / input_configs | `E:\PS3\RPCS3\config\` |
| `E:\PS3\Backups\01_original\GuiConfigs\` | CurrentSettings.ini(+`.bak_before_ko`), persistent_settings.dat, compat/config DB | `E:\PS3\RPCS3\GuiConfigs\` |
| `E:\PS3\Backups\01_original\savedata\BCAS20298-AUTO_0-\` | **세이브 데이터(SAVE0.DAT 2,157,336 B)** | `E:\PS3\RPCS3\dev_hdd0\home\00000001\savedata\` |
| `E:\PS3\Backups\01_original\trophy\NPWR03852_00\` | 트로피 데이터 | `E:\PS3\RPCS3\dev_hdd0\home\00000001\trophy\` |
| `E:\PS3\Backups\02_before_coldboot_test\` | 콜드부트 테스트 전 PPU/SPU LLVM 모듈 캐시 144개 | `E:\PS3\RPCS3\cache\` |
| `E:\PS3\Backups\03_before_reshade\` | ReShade 도입 시점의 config/custom config/CurrentSettings/ReShade.ini 스냅샷 | 참고용 |
| `E:\PS3\Updates_DLC\BCAS20298_v1.09\` | 공식 v1.09 PKG(재설치 가능) | — |
| 원본 설치본 | `C:\Users\<user>\Downloads\rpcs3-v0.0.42-20053-38eba804_win64_msvc` | 항상 그대로 유지(최종 안전판) |

---

## 1. 게임별 커스텀 설정만 원복 (전역은 그대로)

문제가 Dragon's Crown 커스텀 설정 때문이라고 의심될 때:

```powershell
# 커스텀 설정 제거 = GUI 부팅 시 전역(CLEAN) 설정 사용
Remove-Item "E:\PS3\RPCS3\config\custom_configs\BCAS20298.yml"
# (선택) 백업 스냅샷으로 되돌리기
Copy-Item "E:\PS3\Backups\03_before_reshade\custom_configs_BCAS20298.yml" `
          "E:\PS3\RPCS3\config\custom_configs\BCAS20298.yml"
```

## 2. 전역 설정 원복

```powershell
Copy-Item "E:\PS3\Backups\01_original\config\config.yml" "E:\PS3\RPCS3\config\config.yml" -Force
```

## 3. GUI 설정/언어 원복

```powershell
Copy-Item "E:\PS3\Backups\01_original\GuiConfigs\CurrentSettings.ini" "E:\PS3\RPCS3\GuiConfigs\CurrentSettings.ini" -Force
# 한국어 번역 파일 제거(선택)
Remove-Item "E:\PS3\RPCS3\qt6\translations\rpcs3_ko.qm"
```

## 4. v1.09 업데이트 원복 (01.00으로 되돌리기)

업데이트 데이터는 `dev_hdd0/game/BCAS20298/` 폴더 하나입니다. **세이브는 건드리지 않습니다.**

```powershell
# 1) 업데이트 폴더를 백업으로 이동(삭제하지 않음)
Move-Item "E:\PS3\RPCS3\dev_hdd0\game\BCAS20298" "E:\PS3\Backups\update_BCAS20298_v109_removed" -Force
# 2) 필요 시 재설치
#    rpcs3.exe --installpkg "E:\PS3\Updates_DLC\BCAS20298_v1.09\HP5017-…-A0109-V0100-PE.pkg"
```

주의: 업데이트를 되돌려도 **세이브 데이터는 01.09 기준으로 갱신되었을 수 있습니다.** 되돌리기 전 세이브를 먼저 백업하십시오.
(01.00 → 01.09 업데이트는 세이브를 확장하지만, 01.09 → 01.00 다운그레이드는 세이브 호환을 보장하지 않습니다.)

## 5. ReShade 원복 (3단계 중 선택)

```powershell
# (A) 일시 중지 — 레지스트리 값만 0으로 (가장 안전, 재사용 쉬움)
E:\PS3\Mods_Patches\ReShade\ReShade_Disable.cmd        # 관리자 권한
# (B) 완전 제거(공식 uninstall) — 레이어/모듈 제거, 셰이더·프리셋 파일은 유지
E:\PS3\Mods_Patches\ReShade\ReShade_Uninstall.cmd      # 관리자 권한
# (C) 흔적 없이 정리
Remove-Item "E:\PS3\RPCS3\ReShade.ini","E:\PS3\RPCS3\ReShade.log","E:\PS3\RPCS3\ReShadePreset.ini" -Force
Remove-Item "C:\ProgramData\ReShade" -Recurse -Force
reg delete "HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers" /v "C:\ProgramData\ReShade\ReShade64.json" /f
```

CLEAN 런처는 이미 `DISABLE_VK_LAYER_reshade_1=1` 로 ReShade를 차단하므로, 레이어가 켜져 있어도 CLEAN 플레이는 가능합니다.

## 6. 셰이더/모듈 캐시 원복

```powershell
# 콜드부트 테스트 전 캐시 되돌리기(선택 — 보통은 새로 생성되므로 불필요)
Get-ChildItem "E:\PS3\Backups\02_before_coldboot_test" -Directory | ForEach-Object {
  Copy-Item $_.FullName "E:\PS3\RPCS3\cache\" -Recurse -Force
}
```

캐시 삭제가 필요할 때는 **반드시 백업 후** 수행하고, 목적을 본 문서에 기록하십시오.

## 7. 세이브/트로피 복원

```powershell
Copy-Item "E:\PS3\Backups\01_original\savedata\BCAS20298-AUTO_0-" `
          "E:\PS3\RPCS3\dev_hdd0\home\00000001\savedata\" -Recurse -Force
Copy-Item "E:\PS3\Backups\01_original\trophy\NPWR03852_00" `
          "E:\PS3\RPCS3\dev_hdd0\home\00000001\trophy\" -Recurse -Force
```

## 8. 전체 초기화 (가장 확실한 원복)

```powershell
# E:\PS3\RPCS3 를 지우고 원본에서 다시 복제 (원본은 그대로이므로 항상 가능)
Remove-Item "E:\PS3\RPCS3" -Recurse -Force
robocopy "C:\Users\<user>\Downloads\rpcs3-v0.0.42-20053-38eba804_win64_msvc" "E:\PS3\RPCS3" /E /COPY:DAT /DCOPY:DAT
# 이후 필요하면 세이브/트로피만 백업에서 복원(§7)
```

## 9. 원복 후 확인 체크리스트

- [ ] RPCS3 실행 → 로그 첫 줄 `RPCS3 v0.0.42-20053-38eba804`
- [ ] 게임 목록에 `Dragon's Crown [BCAS20298]` 표시
- [ ] 세이브 데이터 인식(게임 내 로드 화면)
- [ ] `dev_hdd0/game/BCAS20298` 존재 여부(업데이트 상태)
- [ ] `patches/patch.yml` 존재(패치는 기본 비활성)
- [ ] `qt6/translations/rpcs3_ko.qm` 유무(GUI 언어)
- [ ] `HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers` 의 ReShade 값
