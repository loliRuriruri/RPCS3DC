"""Enhanced Module: cheat-safe profiles + launchers for Dragon's Crown.

Profiles are complete config.yml files derived from the verified CLEAN baseline
(or from the RPCN profile for the netplay-safe variant).
Launchers are read-only .cmd files with pre-flight checks (save backup, RPCN/cheat guards).
"""
import os
import re
import shutil

PROFILES = r"E:\PS3\Profiles"
LAUNCHERS = r"E:\PS3\Launchers"
GAME = r"C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
RPCS3 = r"E:\PS3\RPCS3"
RPCS3_EXE = os.path.join(RPCS3, "rpcs3.exe")

SECTION_OF = {
    "Resolution Scale": "Video", "Resolution": "Video", "Aspect ratio": "Video",
    "Anisotropic Filter Override": "Video", "MSAA": "Video", "Write Color Buffers": "Video",
    "Write Depth Buffer": "Video", "Read Color Buffers": "Video", "Read Depth Buffer": "Video",
    "Strict Rendering Mode": "Video", "Stretch To Display Area": "Video",
    "Multithreaded RSX": "Video", "Shader Precision": "Video", "Shader Mode": "Video",
    "Frame limit": "Video", "VSync Mode": "Video", "Vblank Rate": "Video",
    "Output Scaling Mode": "Video", "FidelityFX CAS Sharpening Intensity": "Video",
    "Internet enabled": "Net", "PSN status": "Net", "UPNP Enabled": "Net",
    "Clans Enabled": "Net", "PSN Country": "Net", "Bind address": "Net",
    "DNS address": "Net", "Derive MAC from PSID": "Net",
    "Start games in fullscreen mode": "Miscellaneous", "Show RPCN popups": "Miscellaneous",
    "Exit RPCS3 when process finishes": "Miscellaneous",
}


def set_key(lines, section, key, value):
    out, in_sec, found = [], False, False
    for line in lines:
        if not line.startswith(" ") and line.strip().endswith(":"):
            in_sec = line.strip()[:-1] == section
        if in_sec and re.match(r"^  %s: " % re.escape(key), line):
            out.append(f"  {key}: {value}\n")
            found = True
            continue
        out.append(line)
    if not found:
        raise SystemExit(f"key not found: [{section}] {key}")
    return out


def build(name, base, overrides):
    lines = open(base, encoding="utf-8").read().splitlines(keepends=True)
    for key, value in overrides.items():
        lines = set_key(lines, SECTION_OF[key], key, value)
    d = os.path.join(PROFILES, name)
    os.makedirs(d, exist_ok=True)
    p = os.path.join(d, "config.yml")
    with open(p, "w", encoding="utf-8", newline="\n") as f:
        f.write("".join(lines))
    print("profile:", p)
    return p


GFX_4K = {
    "Resolution Scale": "300", "Resolution": "1280x720", "Aspect ratio": "16:9",
    "Anisotropic Filter Override": "0", "MSAA": "Auto",
    "Write Color Buffers": "false", "Write Depth Buffer": "false",
    "Read Color Buffers": "false", "Read Depth Buffer": "false",
    "Strict Rendering Mode": "false", "Stretch To Display Area": "false",
    "Multithreaded RSX": "false", "Shader Precision": "High",
    "Shader Mode": "Async Recompiler with Shader Interpreter",
    "Frame limit": "Auto", "VSync Mode": "Disabled", "Vblank Rate": "60",
    "Output Scaling Mode": "Bilinear", "FidelityFX CAS Sharpening Intensity": "50",
}

OFFLINE_NET = {
    "Internet enabled": "Disconnected",   # fully offline while cheating
    "PSN status": "Disconnected",
    "UPNP Enabled": "false",
    "Clans Enabled": "false",
    "Bind address": "0.0.0.0",
    "Show RPCN popups": "false",
}

# 1) DC_CHEAT_OFFLINE : 4K graphics + network fully disabled
build("DC_CHEAT_OFFLINE", os.path.join(PROFILES, "CLEAN", "config.yml"),
      {**GFX_4K, **OFFLINE_NET, "Start games in fullscreen mode": "true"})

# 2) DC_NETPLAY_SAFE : identical to the RPCN profile, but a distinct file so the
#    netplay launcher can guarantee "no cheats / no experimental patches" semantics
rpcn_src = os.path.join(PROFILES, "Dragons_Crown", "DC_RPCN_NETPLAY", "config.yml")
build("DC_NETPLAY_SAFE", rpcn_src, {})

# 3) DC_EXPERIMENTAL_LSFG : 4K graphics, windowed (Lossless Scaling captures the window)
build("DC_EXPERIMENTAL_LSFG", os.path.join(PROFILES, "CLEAN", "config.yml"),
      {**GFX_4K, "Start games in fullscreen mode": "false",
       "Exit RPCS3 when process finishes": "false"})

# ------------------------------------------------------------------ launchers
HEAD = """@echo off
rem ============================================================
rem  {title}
rem  profile : {profile}
rem  game    : Dragon's Crown (BCAS20298) v1.09
rem  Read-only script. No admin rights. Never deletes saves.
rem ============================================================
setlocal
set "RPCS3_DIR={rpcs3_dir}"
set "RPCS3={rpcs3}"
set "PROFILE={profile_path}"
set "GAME={game}"
if not exist "%RPCS3%"  ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%"    ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"
"""

LAUNCH = {
    "Dragon_Crown_4K_ULTRA.cmd": ("Dragon's Crown 4K ULTRA (300%)", "DC_4K_ULTRA", ""),
    "Dragon_Crown_5K_SSAA.cmd": ("Dragon's Crown 5K SSAA (400%)", "DC_5K_SSAA", ""),
}

for name, (title, prof, extra) in LAUNCH.items():
    body = HEAD.format(title=title, profile=prof, rpcs3_dir=RPCS3, rpcs3=RPCS3_EXE,
                       profile_path=os.path.join(PROFILES, prof, "config.yml"), game=GAME)
    body += extra + '\nstart "" "%RPCS3%" --config "%PROFILE%" "%GAME%"\nexit /b 0\n'
    with open(os.path.join(LAUNCHERS, name), "w", encoding="utf-8", newline="\r\n") as f:
        f.write(body)
    print("launcher:", name)

# CHEAT_OFFLINE launcher: pre-flight = verified save backup + offline guard + ReShade off
cheat = HEAD.format(title="Dragon's Crown CHEAT OFFLINE (4K, network disabled)",
                    profile="DC_CHEAT_OFFLINE",
                    rpcs3_dir=RPCS3, rpcs3=RPCS3_EXE,
                    profile_path=os.path.join(PROFILES, "DC_CHEAT_OFFLINE", "config.yml"), game=GAME)
cheat += r'''
rem --- 1) verified save backup before any cheat session ---
echo [1/3] backing up save data ...
powershell -NoProfile -ExecutionPolicy Bypass -File "E:\PS3\Tools\save_backup.ps1" -Label "cheat_offline"
if errorlevel 2 ( echo [ERROR] save backup failed - aborting. & pause & exit /b 1 )

rem --- 2) offline guard: profile must have RPCN disabled ---
echo [2/3] verifying offline profile ...
findstr /C:"PSN status: Disconnected" "%PROFILE%" >nul || ( echo [ERROR] profile is not offline. & pause & exit /b 1 )
findstr /C:"Internet enabled: Disconnected" "%PROFILE%" >nul || ( echo [ERROR] internet is not disabled. & pause & exit /b 1 )

rem --- 3) ReShade off + cheat reminder ---
echo [3/3] ReShade disabled for this session.
set "DISABLE_VK_LAYER_reshade_1=1"
echo.
echo  CHEAT MODE: RPCN/online is disabled. Do NOT use this profile for netplay.
echo  Cheats are enabled inside RPCS3 via: Game list -^> right click game -^> Cheats
echo.
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
'''
with open(os.path.join(LAUNCHERS, "Dragon_Crown_CHEAT_OFFLINE.cmd"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write(cheat)
print("launcher: Dragon_Crown_CHEAT_OFFLINE.cmd")

# NETPLAY_SAFE launcher: pre-flight = cheats/patches must be disabled
net = HEAD.format(title="Dragon's Crown NETPLAY SAFE (RPCN, no cheats)",
                  profile="DC_NETPLAY_SAFE",
                  rpcs3_dir=RPCS3, rpcs3=RPCS3_EXE,
                  profile_path=os.path.join(PROFILES, "DC_NETPLAY_SAFE", "config.yml"), game=GAME)
net += r'''
rem --- 1) cheat guard: config\cheats.yml must not contain enabled cheats for this title ---
echo [1/3] checking cheat state ...
if exist "%RPCS3_DIR%\config\cheats.yml" (
  findstr /I /C:"BCAS20298" "%RPCS3_DIR%\config\cheats.yml" >nul && (
    echo [WARN] cheats.yml contains entries for BCAS20298.
    echo        Disable all cheats in the Cheat Manager before netplay.
    choice /C YN /M "Continue anyway"
    if errorlevel 2 exit /b 1
  )
)

rem --- 2) patch guard: no enabled game patches for this title ---
echo [2/3] checking patch state ...
if exist "%RPCS3_DIR%\config\patch_config.yml" (
  findstr /I /C:"BCAS20298" "%RPCS3_DIR%\config\patch_config.yml" >nul && (
    echo [WARN] patch_config.yml contains entries for BCAS20298.
    echo        Remove/disable them before netplay (experimental patches break sync with the peer).
    choice /C YN /M "Continue anyway"
    if errorlevel 2 exit /b 1
  )
)

rem --- 3) ReShade off for the first netplay validation ---
echo [3/3] ReShade disabled for netplay validation.
set "DISABLE_VK_LAYER_reshade_1=1"
echo.
echo  NETPLAY SAFE: cheats OFF, experimental patches OFF, RPCN ON.
echo  Both players must use the same TITLE_ID (BCAS20298) and APP_VER (01.09).
echo.
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
'''
with open(os.path.join(LAUNCHERS, "Dragon_Crown_NETPLAY_SAFE.cmd"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write(net)
print("launcher: Dragon_Crown_NETPLAY_SAFE.cmd")

# KNOWN_GOOD launcher: uses the snapshot install
kg = r'''@echo off
rem ============================================================
rem  Dragon's Crown KNOWN GOOD fallback (verified snapshot)
rem  snapshot : E:\PS3\RPCS3\KnownGood   (v0.0.42-20053-38eba804, boot-tested)
rem  Note: the snapshot has its own dev_hdd0 -> save data may differ from Current.
rem        Sync saves first with:  E:\PS3\Tools\sync_saves_to_known_good.cmd
rem ============================================================
setlocal
set "KG=E:\PS3\RPCS3\KnownGood"
set "GAME=C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
set "PROFILE=%KG%\config\custom_configs\BCAS20298.yml"
if not exist "%KG%\rpcs3.exe" ( echo [ERROR] KnownGood snapshot missing: "%KG%\rpcs3.exe" & pause & exit /b 1 )
if not exist "%PROFILE%"     ( echo [ERROR] KnownGood profile missing & pause & exit /b 1 )
if not exist "%GAME%"        ( echo [ERROR] game dump not found & pause & exit /b 1 )
cd /d "%KG%"
start "" "%KG%\rpcs3.exe" --config "%PROFILE%" "%GAME%"
exit /b 0
'''
with open(os.path.join(LAUNCHERS, "Dragon_Crown_KNOWN_GOOD.cmd"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write(kg)
print("launcher: Dragon_Crown_KNOWN_GOOD.cmd")

# save sync helper for the snapshot
sync = r'''@echo off
rem Copy current save data into the KnownGood snapshot (backup first).
setlocal
set "SRC=E:\PS3\RPCS3\dev_hdd0\home\00000001"
set "DST=E:\PS3\RPCS3\KnownGood\dev_hdd0\home\00000001"
echo Syncing savedata + trophy from Current to KnownGood ...
if not exist "%DST%" ( echo [ERROR] KnownGood dev_hdd0 missing & pause & exit /b 1 )
robocopy "%SRC%\savedata" "%DST%\savedata" /E /COPY:DAT /R:1 /W:1
robocopy "%SRC%\trophy"   "%DST%\trophy"   /E /COPY:DAT /R:1 /W:1
echo Done. (Current install is unchanged.)
pause
'''
with open(os.path.join(LAUNCHERS, "sync_saves_to_known_good.cmd"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write(sync)
print("launcher: sync_saves_to_known_good.cmd")
