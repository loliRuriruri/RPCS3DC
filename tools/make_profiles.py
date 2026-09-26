"""RPCS3 profile generator for E:\\PS3 (Dragon's Crown project).

Reads the untouched baseline config (Backups/01_original/config/config.yml) and
writes full, self-contained config.yml variants into E:\\PS3\\Profiles\\<name>\\.
Only explicit key/value pairs are changed; everything else stays identical to
the original install, so a profile is always reproducible and reversible.
"""
import os
import re
import sys

BASE = r"E:\PS3\Backups\01_original\config\config.yml"
OUT_ROOT = r"E:\PS3\Profiles"

# ---- baseline values we always normalise (RPCS3 defaults / CLEAN baseline) ----
CLEAN = {
    # Video
    "Renderer": "Vulkan",
    "Resolution": "1280x720",
    "Resolution Scale": "100",
    "Aspect ratio": "16:9",
    "Anisotropic Filter Override": "0",
    "MSAA": "Auto",
    "Shader Precision": "High",
    "Shader Mode": "Async Recompiler with Shader Interpreter",
    "Frame limit": "Auto",
    "VSync Mode": "Disabled",
    "Vblank Rate": "60",
    "Write Color Buffers": "false",
    "Write Depth Buffer": "false",
    "Read Color Buffers": "false",
    "Read Depth Buffer": "false",
    "Strict Rendering Mode": "false",
    "Stretch To Display Area": "false",
    "Multithreaded RSX": "false",
    "Asynchronous Texture Streaming": "false",
    "Output Scaling Mode": "Bilinear",
    "FidelityFX CAS Sharpening Intensity": "50",
    "Texture LOD Bias Addend": "0",
    "Minimum Scalable Dimension": "385",
    "Use full RGB output range": "true",
    "Vulkan": None,  # section, handled separately
    # Core (must stay at defaults for Dragon's Crown)
    "SPU Block Size": "Safe",
    "Preferred SPU Threads": "0",
    "SPU loop detection": "false",
    "Disable SPU GETLLAR Spin Optimization": "false",
    "Accurate SPU Reservations": "true",
    "SPU Decoder": "Recompiler (LLVM)",
    "PPU Decoder": "Recompiler (LLVM)",
    # Misc
    "Start games in fullscreen mode": "true",
    "Exit RPCS3 when process finishes": "false",
}

VULKAN_SECTION = {
    "Adapter": "NVIDIA GeForce RTX 5080",
    "Asynchronous Queue Scheduler": "Safe",
    "Asynchronous Texture Streaming": "false",
    "Use Re-BAR for GPU uploads": "true",
    "VRAM allocation limit (MB)": "65536",
    "Exclusive Fullscreen Mode": "Automatic",
}

PROFILES = {
    "CLEAN": {},
    "DC_SAFE": {},
    "DC_SAFE_WCB": {"Write Color Buffers": "true"},
    "DC_4K_ULTRA": {"Resolution Scale": "300"},
    "DC_4K_ULTRA_AF16": {"Resolution Scale": "300", "Anisotropic Filter Override": "16"},
    "DC_5K_SSAA": {"Resolution Scale": "400"},
    "DC_5K_SSAA_AF16": {"Resolution Scale": "400", "Anisotropic Filter Override": "16"},
    "DC_6K_TEST": {"Resolution Scale": "500"},
    "DC_8K_SCREENSHOT": {"Resolution Scale": "600"},
    "DC_4K_WCB_ON": {"Resolution Scale": "300", "Write Color Buffers": "true"},
    "DC_4K_STRICT": {"Resolution Scale": "300", "Strict Rendering Mode": "true"},
    "DC_4K_MTRSX": {"Resolution Scale": "300", "Multithreaded RSX": "true"},
    "DC_4K_CAS0": {"Resolution Scale": "300", "Output Scaling Mode": "FidelityFX Super Resolution",
                   "FidelityFX CAS Sharpening Intensity": "0"},
    "DC_4K_CAS15": {"Resolution Scale": "300", "Output Scaling Mode": "FidelityFX Super Resolution",
                    "FidelityFX CAS Sharpening Intensity": "15"},
    "DC_4K_CAS25": {"Resolution Scale": "300", "Output Scaling Mode": "FidelityFX Super Resolution",
                    "FidelityFX CAS Sharpening Intensity": "25"},
    # automated-test variants: windowed so the desktop is not taken over
    "DC_TEST_WINDOWED": {"Start games in fullscreen mode": "false"},
    "DC_4K_TEST_WINDOWED": {"Resolution Scale": "300", "Start games in fullscreen mode": "false"},
}

VULKAN_KEYS = {
    "Adapter", "Asynchronous Queue Scheduler", "Asynchronous Texture Streaming",
    "Use Re-BAR for GPU uploads", "VRAM allocation limit (MB)", "Exclusive Fullscreen Mode",
}

SECTION_OF = {
    "Renderer": "Video", "Resolution": "Video", "Resolution Scale": "Video",
    "Aspect ratio": "Video", "Anisotropic Filter Override": "Video", "MSAA": "Video",
    "Shader Precision": "Video", "Shader Mode": "Video", "Frame limit": "Video",
    "VSync Mode": "Video", "Vblank Rate": "Video", "Write Color Buffers": "Video",
    "Write Depth Buffer": "Video", "Read Color Buffers": "Video", "Read Depth Buffer": "Video",
    "Strict Rendering Mode": "Video", "Stretch To Display Area": "Video",
    "Multithreaded RSX": "Video", "Asynchronous Texture Streaming": "Video",
    "Output Scaling Mode": "Video", "FidelityFX CAS Sharpening Intensity": "Video",
    "Texture LOD Bias Addend": "Video", "Minimum Scalable Dimension": "Video",
    "Use full RGB output range": "Video",
    "SPU Block Size": "Core", "Preferred SPU Threads": "Core", "SPU loop detection": "Core",
    "Disable SPU GETLLAR Spin Optimization": "Core", "Accurate SPU Reservations": "Core",
    "SPU Decoder": "Core", "PPU Decoder": "Core",
    "Start games in fullscreen mode": "Miscellaneous",
    "Exit RPCS3 when process finishes": "Miscellaneous",
    "Adapter": "Vulkan", "Asynchronous Queue Scheduler": "Vulkan",
    "Use Re-BAR for GPU uploads": "Vulkan", "VRAM allocation limit (MB)": "Vulkan",
    "Exclusive Fullscreen Mode": "Vulkan",
}


def set_key(lines, section, key, value):
    """Set '  <key>: <value>' inside '[section]' block of a config.yml text."""
    out, in_sec, found = [], False, False
    for line in lines:
        if line.startswith(" ") and line.rstrip().endswith(":") and not line.startswith("  "):
            pass
        if not line.startswith(" ") and line.strip().endswith(":") and not line.startswith(" "):
            in_sec = line.strip()[:-1] == section
        if in_sec and re.match(r"^  %s: " % re.escape(key), line):
            out.append(f"  {key}: {value}\n")
            found = True
            continue
        out.append(line)
    if not found:
        raise SystemExit(f"key not found: [{section}] {key}")
    return out


def build(name, overrides):
    text = open(BASE, "r", encoding="utf-8").read()
    lines = text.splitlines(keepends=True)

    values = dict(CLEAN)
    values.update(VULKAN_SECTION)
    values.update(overrides)
    values.pop("Vulkan", None)

    for key, value in values.items():
        if key in VULKAN_KEYS:
            # nested under Video -> Vulkan: two levels of indent
            section = "Vulkan"
            out, in_video, in_vk, found = [], False, False, False
            for line in lines:
                if not line.startswith(" ") and line.strip().endswith(":"):
                    in_video = line.strip()[:-1] == "Video"
                    in_vk = False
                if in_video and re.match(r"^  Vulkan:\s*$", line):
                    in_vk = True
                elif in_video and re.match(r"^  \S", line) and not re.match(r"^    ", line) and not re.match(r"^  Vulkan:", line):
                    in_vk = False
                if in_vk and re.match(r"^    %s: " % re.escape(key), line):
                    out.append(f"    {key}: {value}\n")
                    found = True
                    continue
                out.append(line)
            if not found:
                raise SystemExit(f"vulkan key not found: {key}")
            lines = out
        else:
            lines = set_key(lines, SECTION_OF[key], key, value)

    dst_dir = os.path.join(OUT_ROOT, name)
    os.makedirs(dst_dir, exist_ok=True)
    dst = os.path.join(dst_dir, "config.yml")
    with open(dst, "w", encoding="utf-8", newline="\n") as f:
        f.write("".join(lines))
    return dst


def main():
    for name, overrides in PROFILES.items():
        p = build(name, overrides)
        # verify the important keys are what we expect
        t = open(p, encoding="utf-8").read()
        checks = []
        for key in ("Resolution Scale", "Write Color Buffers", "Strict Rendering Mode",
                    "Output Scaling Mode", "Anisotropic Filter Override", "Multithreaded RSX",
                    "Start games in fullscreen mode", "Resolution", "Shader Mode",
                    "FidelityFX CAS Sharpening Intensity"):
            m = re.search(r"^  %s: (\S.*)$" % re.escape(key), t, re.M)
            checks.append(f"{key}={m.group(1).strip() if m else '??'}")
        print(f"{name:22} -> {p}")
        print("    " + "  ".join(checks))


if __name__ == "__main__":
    main()
