"""Generate E:\\PS3 launchers (.cmd) and install profile files into the RPCS3 copy."""
import os
import shutil

RPCS3_DIR = r"E:\PS3\RPCS3"
RPCS3 = r"E:\PS3\RPCS3\rpcs3.exe"
PROFILES = r"E:\PS3\Profiles"
LAUNCHERS = r"E:\PS3\Launchers"
GAME = r"C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"

HEADER = """@echo off
rem ============================================================
rem  RPCS3 {title}
rem  profile : {profile}
rem  game    : Dragon's Crown (BCAS20298)
rem  This script only *reads* files. It never modifies saves,
rem  never deletes caches and needs no administrator rights.
rem ============================================================
setlocal
set "RPCS3_DIR={rpcs3_dir}"
set "RPCS3={rpcs3}"
set "PROFILE={profile_path}"
set "GAME={game}"
if not exist "%RPCS3%" ( echo [ERROR] rpcs3.exe not found: "%RPCS3%" & pause & exit /b 1 )
if not exist "%PROFILE%" ( echo [ERROR] profile not found: "%PROFILE%" & pause & exit /b 1 )
if not exist "%GAME%" ( echo [ERROR] game dump not found: "%GAME%" & pause & exit /b 1 )
cd /d "%RPCS3_DIR%"
"""

FOOTER_GUI = """
start "" "%RPCS3%" --config "%PROFILE%" "%GAME%"
exit /b 0
"""

FOOTER_NOGUI = """
start "" "%RPCS3%" --no-gui --fullscreen --config "%PROFILE%" "%GAME%"
exit /b 0
"""

# launcher name -> (title, profile folder, no-gui)
LAUNCHER_SET = {
    "RPCS3_CLEAN.cmd":                    ("CLEAN baseline (no game auto-boot)", None, False),
    "RPCS3_DRAGONS_CROWN.cmd":            ("Dragon's Crown - daily 4K ULTRA (300%)", "DC_4K_ULTRA", False),
    "RPCS3_DRAGONS_CROWN_5K.cmd":         ("Dragon's Crown - 5K SSAA (400%)", "DC_5K_SSAA", False),
    "RPCS3_DRAGONS_CROWN_SAFE.cmd":       ("Dragon's Crown - SAFE (100%, compatibility first)", "DC_SAFE", False),
    "RPCS3_DRAGONS_CROWN_CLEAN.cmd":      ("Dragon's Crown - CLEAN A/B reference (100%)", "CLEAN", False),
    "RPCS3_DRAGONS_CROWN_4K_NOGUI.cmd":   ("Dragon's Crown - 4K ULTRA, no GUI / fullscreen", "DC_4K_ULTRA", True),
    "RPCS3_DRAGONS_CROWN_8K.cmd":         ("Dragon's Crown - 8K SCREENSHOT profile (600%, experimental)", "DC_8K_SCREENSHOT", False),
    "RPCS3_DRAGONS_CROWN_POSTFX.cmd":     ("Dragon's Crown - 4K ULTRA + ReShade (optional post-processing)", "DC_4K_ULTRA", False),
}

os.makedirs(LAUNCHERS, exist_ok=True)

# 1) plain GUI launcher for CLEAN (opens the RPCS3 GUI with the clean global config)
with open(os.path.join(LAUNCHERS, "RPCS3_CLEAN.cmd"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write(HEADER.format(title="CLEAN baseline (GUI, global CLEAN config)",
                          profile="global config.yml (CLEAN)",
                          rpcs3_dir=RPCS3_DIR, rpcs3=RPCS3, profile_path=os.path.join(RPCS3_DIR, "config", "config.yml"),
                          game=GAME))
    f.write('\nrem The global config of this copy is the CLEAN baseline. No game is booted automatically.\n')
    f.write('start "" "%RPCS3%"\nexit /b 0\n')

for name, (title, profile, nogui) in LAUNCHER_SET.items():
    if name == "RPCS3_CLEAN.cmd":
        continue
    profile_path = os.path.join(PROFILES, profile, "config.yml")
    body = HEADER.format(title=title, profile=f"{profile} (E:\\PS3\\Profiles\\{profile}\\config.yml)",
                         rpcs3_dir=RPCS3_DIR, rpcs3=RPCS3, profile_path=profile_path, game=GAME)
    body += FOOTER_NOGUI if nogui else FOOTER_GUI
    if name == "RPCS3_DRAGONS_CROWN_POSTFX.cmd":
        body = body.replace('start "" "%RPCS3%" --config',
                            'rem ReShade is enabled for this launcher only (Vulkan explicit layer, no registry change).\n'
                            'set "VK_INSTANCE_LAYERS=VK_LAYER_reshade"\n'
                            'set "VK_LAYER_PATH=E:\\PS3\\Mods_Patches\\ReShade"\n'
                            'start "" "%RPCS3%" --config')
    with open(os.path.join(LAUNCHERS, name), "w", encoding="utf-8", newline="\r\n") as f:
        f.write(body)

# 2) install profile files into the RPCS3 copy
#    a) global config = CLEAN baseline
shutil.copyfile(os.path.join(PROFILES, "CLEAN", "config.yml"),
                os.path.join(RPCS3_DIR, "config", "config.yml"))

#    b) Dragon's Crown custom config (used automatically by the GUI) = DC_4K_ULTRA
cc_dir = os.path.join(RPCS3_DIR, "config", "custom_configs")
os.makedirs(cc_dir, exist_ok=True)
shutil.copyfile(os.path.join(PROFILES, "DC_4K_ULTRA", "config.yml"),
                os.path.join(cc_dir, "BCAS20298.yml"))

#    c) official patch database into the standard patches folder
patches_dir = os.path.join(RPCS3_DIR, "patches")
os.makedirs(patches_dir, exist_ok=True)
shutil.copyfile(r"E:\PS3\Mods_Patches\RPCS3_Patches\patch.yml",
                os.path.join(patches_dir, "patch.yml"))

print("launchers:")
for f in sorted(os.listdir(LAUNCHERS)):
    print("  ", f)
print("global config  ->", os.path.join(RPCS3_DIR, "config", "config.yml"))
print("custom config  ->", os.path.join(cc_dir, "BCAS20298.yml"))
print("patch.yml      ->", os.path.join(patches_dir, "patch.yml"))
