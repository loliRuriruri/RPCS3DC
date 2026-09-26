"""Dragon's Crown PRO Enhanced — folder structure, 4 final profiles and ReShade Silent.

Creates the project layout requested by the PRO Enhanced spec, generates the four
user-facing profiles (DC_PRO_4K / DC_PRO_MAX_5K / DC_NETPLAY / DC_CHEAT_OFFLINE)
with VSync ON, VBlank 60, Frame Skip OFF, and applies the ReShade Silent overlay
settings (menu/OSD hidden, Home still opens the overlay).

Nothing is deleted: existing profiles/launchers stay where they are (legacy
launchers are moved by a separate step).
"""
import os
import re
import shutil

ROOT = r"E:\PS3"
PROFILES = os.path.join(ROOT, "Profiles")
DC = os.path.join(PROFILES, "Dragons_Crown")
RPCS3 = os.path.join(ROOT, "RPCS3")

# ---------------------------------------------------------------- 1) folders
FOLDERS = [
    os.path.join(ROOT, "Launcher"),
    os.path.join(ROOT, "Launcher", "Internal"),
    os.path.join(ROOT, "Launcher", "_Legacy"),
    os.path.join(RPCS3, "Current"),                    # marker folder (live install = RPCS3 root)
    os.path.join(DC, "PRO_4K"),
    os.path.join(DC, "PRO_MAX_5K"),
    os.path.join(DC, "NETPLAY"),
    os.path.join(DC, "CHEAT_OFFLINE"),
    os.path.join(ROOT, "ReShade", "Presets"),
    os.path.join(ROOT, "ReShade", "Backup"),
    os.path.join(ROOT, "Cheats", "Dragons_Crown", "Verified"),
    os.path.join(ROOT, "Cheats", "Dragons_Crown", "Experimental"),
    os.path.join(ROOT, "Cheats", "Dragons_Crown", "Backup"),
    os.path.join(ROOT, "Saves", "Dragons_Crown", "Original"),
    os.path.join(ROOT, "Saves", "Dragons_Crown", "AutoBackup"),
    os.path.join(ROOT, "Saves", "Dragons_Crown", "CheatTest"),
    os.path.join(ROOT, "Patches"),
    os.path.join(ROOT, "Screenshots_AB"),
    os.path.join(ROOT, "Logs"),
    os.path.join(ROOT, "Backups"),
    os.path.join(ROOT, "Docs"),
]
for d in FOLDERS:
    os.makedirs(d, exist_ok=True)

# marker so the Current/KnownGood split is explicit without moving the live install
with open(os.path.join(RPCS3, "Current", "README_CURRENT.txt"), "w", encoding="utf-8") as f:
    f.write(
        "RPCS3 Current = E:\\PS3\\RPCS3 (이 폴더의 상위 폴더)\n"
        "=================================================\n\n"
        "이 프로젝트에서 'Current'는 실제 사용 중인 RPCS3 설치본입니다.\n"
        "설치본 자체를 하위 폴더로 옮기면 모든 런처/프로필 경로가 깨지므로,\n"
        "Current는 RPCS3 루트(E:\\PS3\\RPCS3)를 그대로 사용하고\n"
        "검증 완료 fallback은 E:\\PS3\\RPCS3\\KnownGood 에 보관합니다.\n\n"
        "- Current   : E:\\PS3\\RPCS3            (rpcs3.exe, config, dev_hdd0 ...)\n"
        "- KnownGood : E:\\PS3\\RPCS3\\KnownGood  (검증 통과 스냅샷)\n\n"
        "업데이트 승인 기준: Cold Boot 3회 / Town / Stage / Boss / Save / Load /\n"
        "그래픽 이상 없음 / 30분 플레이 안정 — 모두 통과 후 Current 승인.\n"
        "승격 방법은 E:\\PS3\\RPCS3\\KnownGood\\README_KNOWNGOOD.md 참조.\n"
    )

# ---------------------------------------------------------------- 2) profiles
SECTION_OF = {
    "Resolution Scale": "Video", "Resolution": "Video", "Aspect ratio": "Video",
    "Anisotropic Filter Override": "Video", "MSAA": "Video", "Write Color Buffers": "Video",
    "Write Depth Buffer": "Video", "Read Color Buffers": "Video", "Read Depth Buffer": "Video",
    "Strict Rendering Mode": "Video", "Stretch To Display Area": "Video",
    "Multithreaded RSX": "Video", "Shader Precision": "Video", "Shader Mode": "Video",
    "Frame limit": "Video", "VSync Mode": "Video", "Vblank Rate": "Video",
    "Enable Frame Skip": "Video", "Output Scaling Mode": "Video",
    "FidelityFX CAS Sharpening Intensity": "Video",
    "Internet enabled": "Net", "PSN status": "Net", "UPNP Enabled": "Net",
    "Clans Enabled": "Net", "PSN Country": "Net", "Bind address": "Net",
    "DNS address": "Net", "Derive MAC from PSID": "Net",
    "Start games in fullscreen mode": "Miscellaneous", "Show RPCN popups": "Miscellaneous",
}

COMMON = {
    "Resolution Scale": "300",
    "Resolution": "1280x720",
    "Aspect ratio": "16:9",
    "Anisotropic Filter Override": "0",
    "MSAA": "Auto",
    "Write Color Buffers": "false",
    "Write Depth Buffer": "false",
    "Read Color Buffers": "false",
    "Read Depth Buffer": "false",
    "Strict Rendering Mode": "false",
    "Stretch To Display Area": "false",
    "Multithreaded RSX": "false",
    "Shader Precision": "High",
    "Shader Mode": "Async Recompiler with Shader Interpreter",
    "Frame limit": "Auto",
    "VSync Mode": "Full",          # VSync ON
    "Vblank Rate": "60",
    "Enable Frame Skip": "false",  # Frame Skip OFF
    "Output Scaling Mode": "Bilinear",
    "FidelityFX CAS Sharpening Intensity": "50",
    "Start games in fullscreen mode": "false",   # Borderless launcher handles the window
}

NET_OFFLINE = {
    "Internet enabled": "Disconnected",
    "PSN status": "Disconnected",
    "UPNP Enabled": "false",
    "Clans Enabled": "false",
    "Show RPCN popups": "false",
}
NET_RPCN = {
    "Internet enabled": "Connected",
    "PSN status": "RPCN",
    "UPNP Enabled": "false",
    "Clans Enabled": "false",
    "PSN Country": "us",
    "Bind address": "0.0.0.0",
    "Show RPCN popups": "true",
}

PROFILE_SPECS = {
    "DC_PRO_4K":        ("DC_4K_ULTRA",       {**COMMON, **NET_OFFLINE}),
    "DC_PRO_MAX_5K":    ("DC_4K_ULTRA",       {**COMMON, **NET_OFFLINE, "Resolution Scale": "400"}),
    "DC_NETPLAY":       ("Dragons_Crown/DC_RPCN_NETPLAY", {**COMMON, **NET_RPCN}),
    "DC_CHEAT_OFFLINE": ("DC_CHEAT_OFFLINE",  {**COMMON, **NET_OFFLINE}),
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


for name, (base_rel, overrides) in PROFILE_SPECS.items():
    base = os.path.join(PROFILES, base_rel.replace("/", os.sep), "config.yml")
    if not os.path.isfile(base):
        print(f"[WARN] base profile missing: {base} -> skipped")
        continue
    lines = open(base, encoding="utf-8").read().splitlines(keepends=True)
    for key, value in overrides.items():
        lines = set_key(lines, SECTION_OF[key], key, value)
    dst_dir = os.path.join(DC, name)
    os.makedirs(dst_dir, exist_ok=True)
    dst = os.path.join(dst_dir, "config.yml")
    with open(dst, "w", encoding="utf-8", newline="\n") as f:
        f.write("".join(lines))
    # verify the important keys
    text = open(dst, encoding="utf-8").read()
    vals = {}
    for key in ("Resolution Scale", "VSync Mode", "Vblank Rate", "Enable Frame Skip",
                "PSN status", "Internet enabled", "Frame limit"):
        m = re.search(r"^  %s: (\S.*)$" % re.escape(key), text, re.M)
        vals[key] = m.group(1).strip() if m else "??"
    print(f"profile {name:18} -> {dst}")
    print("        " + "  ".join(f"{k}={v}" for k, v in vals.items()))

# ---------------------------------------------------------------- 3) ReShade Silent
ini_path = os.path.join(RPCS3, "ReShade.ini")
if os.path.isfile(ini_path):
    backup = os.path.join(ROOT, "ReShade", "Backup", "ReShade.ini.before_silent")
    shutil.copyfile(ini_path, backup)
    text = open(ini_path, encoding="utf-8", errors="replace").read()

    def set_ini_section(text, section, pairs):
        header = f"[{section}]"
        block = header + "\n" + "".join(f"{k}={v}\n" for k, v in pairs.items())
        if re.search(rf"(?m)^{re.escape(header)}\s*$", text):
            return re.sub(rf"(?ms)^{re.escape(header)}.*?(?=^\[|\Z)", block, text)
        if not text.endswith("\n"):
            text += "\n"
        return text + "\n" + block

    text = set_ini_section(text, "OVERLAY", {
        "TutorialProgress": 4,
        "ShowClock": 0,
        "ShowFPS": 0,
        "ShowFrameTime": 0,
        "ShowPresetName": 0,
        "ShowScreenshotMessage": 0,
        "ShowPresetTransitionMessage": 0,
        "ShowForceLoadEffectsButton": 0,
    })
    text = set_ini_section(text, "INPUT", {
        "KeyOverlay": "36,0,0,0",
    })
    open(ini_path, "w", encoding="utf-8", newline="\r\n").write(text)
    print("ReShade Silent 적용 ->", ini_path, "(백업:", backup + ")")
    for line in open(ini_path, encoding="utf-8").read().splitlines():
        if line.startswith("[") or re.match(r"^(TutorialProgress|Show|KeyOverlay|PresetPath|EffectSearchPaths)", line):
            print("   ", line)

print("\n폴더/프로필/ReShade Silent 작업 완료")
