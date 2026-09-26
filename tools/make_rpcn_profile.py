"""Create the Dragon's Crown RPCN netplay profile.

Base: E:\\PS3\\Profiles\\CLEAN\\config.yml (100% baseline)
Changes: graphics kept at the *validated* netplay baseline (300%, AF Auto, MSAA Auto,
WCB Off, Strict Off, no FSR/CAS) + Net section switched to RPCN.

Deliberately NOT changed: SPU/PPU advanced options, frame limit, vblank, any patch,
ReShade (the launcher disables the ReShade Vulkan layer for this profile).
"""
import os
import re

BASE = r"E:\PS3\Profiles\CLEAN\config.yml"
DST_DIR = r"E:\PS3\Profiles\Dragons_Crown\DC_RPCN_NETPLAY"
DST = os.path.join(DST_DIR, "config.yml")

OVERRIDES = {
    # ---- graphics (netplay validation baseline, §그래픽 설정) ----
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
    "VSync Mode": "Disabled",
    "Vblank Rate": "60",
    "Output Scaling Mode": "Bilinear",
    "FidelityFX CAS Sharpening Intensity": "50",
    # ---- network (RPCN) ----
    "Internet enabled": "Connected",
    "PSN status": "RPCN",
    "UPNP Enabled": "false",          # documented Dragon's Crown RPCN workaround + avoids router-dependent behaviour
    "Clans Enabled": "false",          # Dragon's Crown does not use the Clans feature
    "PSN Country": "us",
    "Bind address": "0.0.0.0",         # set to the active NIC IP if the machine is dual-homed (see RPCN guide)
    "DNS address": "8.8.8.8",
    "Derive MAC from PSID": "false",
    # ---- misc ----
    "Start games in fullscreen mode": "true",
    "Show RPCN popups": "true",
}

SECTION_OF = {
    "Resolution Scale": "Video", "Resolution": "Video", "Aspect ratio": "Video",
    "Anisotropic Filter Override": "Video", "MSAA": "Video", "Write Color Buffers": "Video",
    "Write Depth Buffer": "Video", "Read Color Buffers": "Video", "Read Depth Buffer": "Video",
    "Strict Rendering Mode": "Video", "Stretch To Display Area": "Video",
    "Multithreaded RSX": "Video", "Asynchronous Texture Streaming": "Video",
    "Shader Precision": "Video", "Shader Mode": "Video", "Frame limit": "Video",
    "VSync Mode": "Video", "Vblank Rate": "Video", "Output Scaling Mode": "Video",
    "FidelityFX CAS Sharpening Intensity": "Video",
    "Internet enabled": "Net", "PSN status": "Net", "UPNP Enabled": "Net",
    "Clans Enabled": "Net", "PSN Country": "Net", "Bind address": "Net",
    "DNS address": "Net", "Derive MAC from PSID": "Net",
    "Start games in fullscreen mode": "Miscellaneous", "Show RPCN popups": "Miscellaneous",
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


lines = open(BASE, encoding="utf-8").read().splitlines(keepends=True)
for key, value in OVERRIDES.items():
    lines = set_key(lines, SECTION_OF[key], key, value)

os.makedirs(DST_DIR, exist_ok=True)
with open(DST, "w", encoding="utf-8", newline="\n") as f:
    f.write("".join(lines))

text = open(DST, encoding="utf-8").read()
print("written:", DST)
for key in ("Resolution Scale", "Write Color Buffers", "Strict Rendering Mode", "MSAA",
            "Anisotropic Filter Override", "Internet enabled", "PSN status", "UPNP Enabled",
            "Clans Enabled", "Bind address", "Frame limit", "Vblank Rate"):
    m = re.search(r"^  %s: (\S.*)$" % re.escape(key), text, re.M)
    print(f"  {key:32} = {m.group(1).strip() if m else '??'}")
