"""Apply the minimal ZERO-BANNER patch to the official ReShade v6.8.0 source.

Change scope (splash/banner UI only):
  * source/runtime_gui.cpp : force `show_splash_window` to false
    (the "Splash Window" draws the ReShade version banner, the "visit reshade.me"
     hint, the first-install tutorial prompt and the effect compile progress bar)
  * one one-time log line so the patched build is identifiable in ReShade.log

Nothing else is modified. Effect runtime, shader compilation, preset handling,
the overlay (Home) menu and error logging are untouched.
"""
import difflib
import os
import re
import sys

SRC = r"E:\PS3\Mods_Patches\ReShade\ZeroBanner\src\source\runtime_gui.cpp"
PATCH_OUT = r"E:\PS3\Mods_Patches\ReShade\ZeroBanner\ZERO_BANNER.patch"

ORIGINAL = "\tconst bool show_splash_window = _show_splash && (is_loading() || (_reload_count <= 1 && (_last_present_time - _last_reload_time) < std::chrono::seconds(5)) || (!_show_overlay && _tutorial_index == 0 && _input != nullptr));"

REPLACEMENT = """\t// ===== ZERO-BANNER PATCH (E:\\PS3) =========================================
\t// Upstream v6.8.0 draws a "Splash Window" for up to 5 seconds after injection:
\t// the "ReShade <version>" banner, the "visit reshade.me" hint, the first-install
\t// tutorial prompt and the effect compile progress bar. This build removes that
\t// startup splash/banner UI only. Nothing functional is touched:
\t// effect runtime, shader compilation, preset handling, the overlay (Home) menu
\t// and error logging all stay intact.
\tstatic bool s_zero_banner_logged = false;
\tif (!s_zero_banner_logged)
\t{
\t\ts_zero_banner_logged = true;
\t\tlog::message(log::level::info, "ZERO-BANNER build: startup splash/banner disabled (no ReShade text on startup).");
\t}
\tconst bool show_splash_window = false;
\t// ===== end ZERO-BANNER PATCH =============================================="""


def main():
    if not os.path.isfile(SRC):
        print(f"[ERROR] source not found: {SRC}")
        return 1

    text = open(SRC, encoding="utf-8").read()

    if "ZERO-BANNER PATCH" in text:
        print("[SKIP] patch already applied")
        return 0

    if ORIGINAL not in text:
        print("[ERROR] target line not found - source differs from v6.8.0?")
        for i, l in enumerate(text.splitlines()):
            if "show_splash_window" in l and "const bool" in l:
                print(f"  line {i+1}: {l[:160]}")
        return 1

    patched = text.replace(ORIGINAL, REPLACEMENT, 1)
    open(SRC, "w", encoding="utf-8", newline="").write(patched)

    # write a unified diff for documentation
    diff = difflib.unified_diff(
        text.splitlines(keepends=True),
        patched.splitlines(keepends=True),
        fromfile="source/runtime_gui.cpp (crosire/reshade v6.8.0)",
        tofile="source/runtime_gui.cpp (ZERO-BANNER)",
        n=3,
    )
    with open(PATCH_OUT, "w", encoding="utf-8", newline="") as f:
        f.writelines(diff)

    print("[OK] patch applied")
    print("  modified:", SRC)
    print("  diff    :", PATCH_OUT)
    # sanity: exactly one occurrence of the marker and no leftover original line
    check = open(SRC, encoding="utf-8").read()
    print("  markers :", check.count("ZERO-BANNER PATCH"))
    print("  original line remaining:", ORIGINAL in check)
    return 0


if __name__ == "__main__":
    sys.exit(main())
