"""Phase 1: Graphics A/B presets, AF 16x, ReShade Pro Enhanced preset, Netplay Safe.

* Updates the four user-facing profiles to AF 16x (Graphics A requirement).
* Creates the ReShade "Pro Enhanced" preset (restrained: Deband + CAS + Levels + Vibrance + SMAA).
* Writes a Netplay Safe profile variant (all experimental/network-affecting options off).
Nothing touches game files, saves or EBOOT.
"""
import os
import re
import shutil

ROOT = r"E:\PS3"
DC = os.path.join(ROOT, "Profiles", "Dragons_Crown")
RS_PRESETS = os.path.join(ROOT, "ReShade", "Presets")
RS_SHADERS = os.path.join(ROOT, "Mods_Patches", "ReShade", "Shaders")

# ---------------------------------------------------------------- 1) AF 16x
PROFILES = ["DC_PRO_4K", "DC_PRO_MAX_5K", "DC_NETPLAY", "DC_CHEAT_OFFLINE"]
for name in PROFILES:
    p = os.path.join(DC, name, "config.yml")
    if not os.path.isfile(p):
        print(f"[WARN] missing profile: {p}")
        continue
    text = open(p, encoding="utf-8").read()
    text = re.sub(r"(?m)^(\s*Anisotropic Filter Override:\s*).*$", r"\g<1>16", text)
    text = re.sub(r"(?m)^(\s*MSAA:\s*).*$", r"\g<1>Auto", text)
    text = re.sub(r"(?m)^(\s*Stretch To Display Area:\s*).*$", r"\g<1>false", text)
    text = re.sub(r"(?m)^(\s*Aspect ratio:\s*).*$", r"\g<1>16:9", text)
    open(p, "w", encoding="utf-8", newline="\n").write(text)
    vals = {}
    for k in ("Resolution Scale", "Anisotropic Filter Override", "MSAA", "VSync Mode",
              "Vblank Rate", "Enable Frame Skip", "Stretch To Display Area", "Aspect ratio"):
        m = re.search(r"(?m)^\s*%s:\s*(\S.*)$" % re.escape(k), text)
        vals[k] = m.group(1).strip() if m else "?"
    print(f"{name:18} AF={vals['Anisotropic Filter Override']:>3}  scale={vals['Resolution Scale']:>3}  "
          f"vsync={vals['VSync Mode']}  stretch={vals['Stretch To Display Area']}")

# ---------------------------------------------------------------- 2) Netplay Safe
src = os.path.join(DC, "DC_NETPLAY", "config.yml")
if os.path.isfile(src):
    text = open(src, encoding="utf-8").read()
    # Netplay Safe = same as DC_NETPLAY but with every experimental/network-affecting
    # option pinned to the conservative value (used to isolate netplay problems).
    safe = [
        (r"(?m)^(\s*Resolution Scale:\s*).*$", r"\g<1>300"),
        (r"(?m)^(\s*Anisotropic Filter Override:\s*).*$", r"\g<1>16"),
        (r"(?m)^(\s*Write Color Buffers:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Write Depth Buffer:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Read Color Buffers:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Read Depth Buffer:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Strict Rendering Mode:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Multithreaded RSX:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Shader Mode:\s*).*$", r"\g<1>Async Recompiler with Shader Interpreter"),
        (r"(?m)^(\s*Output Scaling Mode:\s*).*$", r"\g<1>Bilinear"),
        (r"(?m)^(\s*Frame limit:\s*).*$", r"\g<1>Auto"),
        (r"(?m)^(\s*VSync Mode:\s*).*$", r"\g<1>Full"),
        (r"(?m)^(\s*Vblank Rate:\s*).*$", r"\g<1>60"),
        (r"(?m)^(\s*Enable Frame Skip:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Internet enabled:\s*).*$", r"\g<1>Connected"),
        (r"(?m)^(\s*PSN status:\s*).*$", r"\g<1>RPCN"),
        (r"(?m)^(\s*UPNP Enabled:\s*).*$", r"\g<1>false"),
        (r"(?m)^(\s*Clans Enabled:\s*).*$", r"\g<1>false"),
    ]
    for pat, rep in safe:
        text = re.sub(pat, rep, text)
    dst_dir = os.path.join(DC, "NETPLAY_SAFE")
    os.makedirs(dst_dir, exist_ok=True)
    open(os.path.join(dst_dir, "config.yml"), "w", encoding="utf-8", newline="\n").write(text)
    print("NETPLAY_SAFE 프로필 생성:", os.path.join(dst_dir, "config.yml"))

# ---------------------------------------------------------------- 3) ReShade presets
os.makedirs(RS_PRESETS, exist_ok=True)

standard_note = """Standard 그래픽은 ReShade를 사용하지 않습니다.
RPCS3 자체 설정(Resolution Scale / AF 16x / MSAA Auto)만 사용합니다.
"""
open(os.path.join(RS_PRESETS, "README.txt"), "w", encoding="utf-8").write(
    "Dragon's Crown PC Edition — ReShade 프리셋\n"
    "=========================================\n\n"
    "* Standard        : ReShade 없음 (RPCS3 자체 화질)\n"
    "* Pro Enhanced    : DC_PRO_ENHANCED.ini (Deband + CAS + Levels + Vibrance + SMAA)\n"
    "* 최소 프리셋      : DC_POSTFX_MINIMAL.ini (Deband + CAS)\n"
    "* SMAA 추가        : DC_POSTFX_MINIMAL_SMAA.ini\n\n"
    + standard_note
)

pro = """PreprocessorDefinitions=RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0
Techniques=Deband@Deband.fx,ContrastAdaptiveSharpen@CAS.fx,Levels@Levels.fx,Vibrance@Vibrance.fx,SMAA@SMAA.fx
TechniqueSorting=Deband@Deband.fx,ContrastAdaptiveSharpen@CAS.fx,Levels@Levels.fx,Vibrance@Vibrance.fx,SMAA@SMAA.fx

[Deband.fx]
Deband_Radius=16.0
Deband_Threshold=0.008
Deband_Range=16.0

[CAS.fx]
Contrast=0.0
Sharpening=0.32

[Levels.fx]
BlackPoint=0.0
WhitePoint=1.0
Gamma=1.0

[Vibrance.fx]
Vibrance=0.10
Vibrance_RGB_balance=1.0,1.0,1.0

[SMAA.fx]
EdgeDetectionType=1
EdgeDetectionThreshold=0.06
MaxSearchSteps=32
MaxSearchStepsDiagonal=16
CornerRounding=25
PredicationEnabled=0
DebugOutput=0
"""
open(os.path.join(RS_PRESETS, "DC_PRO_ENHANCED.ini"), "w", encoding="utf-8", newline="\r\n").write(pro)
print("ReShade 프리셋:", os.path.join(RS_PRESETS, "DC_PRO_ENHANCED.ini"))

# ---------------------------------------------------------------- 4) ReShade.ini
ini = os.path.join(ROOT, "RPCS3", "ReShade.ini")
if os.path.isfile(ini):
    text = open(ini, encoding="utf-8", errors="replace").read()
    target = os.path.join(RS_PRESETS, "DC_PRO_ENHANCED.ini")
    if re.search(r"(?m)^PresetPath=", text):
        text = re.sub(r"(?m)^PresetPath=.*$", "PresetPath=" + target, text)
    else:
        text = text.replace("[GENERAL]", "[GENERAL]\r\nPresetPath=" + target, 1)
    open(ini, "w", encoding="utf-8", newline="\r\n").write(text)
    print("ReShade.ini PresetPath ->", target)
    for line in open(ini, encoding="utf-8").read().splitlines():
        if line.startswith(("PresetPath", "EffectSearchPaths", "TextureSearchPaths")):
            print("   ", line)

print("\nPhase 1 그래픽 프리셋 작업 완료")
