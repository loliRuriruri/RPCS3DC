"""Prepare the public GitHub repository content for loliRuriruri/RPCS3DC.

Only project-owned material is copied: documentation, launchers, tooling and
generators. Third-party binaries (RPCS3, ReShade), game data, firmware, save data,
RPCS3 configs (contain the console PSID) and RPCN credentials are excluded.

Personal identifiers are scrubbed: the Windows user name is replaced with <user>.
"""
import os
import re
import shutil

SRC = r"E:\PS3"
DST = r"E:\PS3\GitHub\RPCS3DC"

INCLUDE_DIRS = {
    "Docs": ("docs", ("*.md",)),
    "Launchers": ("launcher", ("*.cmd", "*.ps1")),
    "Launcher\\src": ("launcher/src", ("*.cs", "*.xaml", "*.csproj", "*.ico", "*.png", "*.txt")),
    "remote-coop\\src": ("remote-coop/src", ("*.cs", "*.xaml", "*.csproj", "*.ico", "*.png", "*.txt")),
    "Tools": ("tools", ("*.py", "*.ps1", "*.cmd")),
}

# binary assets are copied byte-for-byte (never run through the text scrubber)
BINARY_EXTS = (".ico", ".png", ".jpg", ".jpeg", ".dll", ".exe")

# single files copied to a fixed repository path (scrubbed)
ROOT_FILES = {
    "README.md": "README.md",
    "remote-coop\\build_remote_coop_setup.cmd": "remote-coop/build_remote_coop_setup.cmd",
}

GITIGNORE = """\
# never publish third-party content / personal data
RPCS3/
Games/
Saves/
Cheats/
Backups/
Logs/
Screenshots_AB/
*.exe
*.dll
*.pdb
*.pkg
*.PUP
*.7z
*.zip
*.dat
*.iso
rpcn.yml
CurrentSettings.ini
*.png
*.jpg
"""

# files that must not be published (third-party content / personal data)
EXCLUDE_NAMES = {
    "rpcs3_exe_strings.txt",
    "window_inspect.log",
    "borderless_state.json",
}
EXCLUDE_PATTERNS = (
    r"rpcs3-src",          # vendored RPCS3 source (translation work)
    r"reshade-src",        # vendored ReShade source
    r"Artemis_Research",   # third-party cheat database copy
    r"psn_update",         # No-Intro database
    r"patch_official",     # RPCS3 patch database copy
)

SCRUB = [
    (r"<user>", "<user>"),
    (r"0x00000000000000000000000000000000", "0x00000000000000000000000000000000"),
    (r"\{587e3323-3e1e-49d5-b277-0b433f41398d\}", "{00000000-0000-0000-0000-000000000000}"),
]


def scrub(text: str) -> str:
    for pat, repl in SCRUB:
        text = re.sub(pat, repl, text)
    return text


def wanted(path: str) -> bool:
    low = path.lower()
    if any(p.lower() in low for p in EXCLUDE_PATTERNS):
        return False
    if os.path.basename(path) in EXCLUDE_NAMES:
        return False
    return True


def main():
    if os.path.isdir(DST):
        # keep the .git directory if it already exists
        for entry in os.listdir(DST):
            if entry == ".git":
                continue
            p = os.path.join(DST, entry)
            shutil.rmtree(p, ignore_errors=True) if os.path.isdir(p) else os.remove(p)
    os.makedirs(DST, exist_ok=True)

    copied = 0
    for src_dir, (dst_dir, patterns) in INCLUDE_DIRS.items():
        src_root = os.path.join(SRC, src_dir)
        if not os.path.isdir(src_root):
            continue
        for name in sorted(os.listdir(src_root)):
            full = os.path.join(src_root, name)
            if not os.path.isfile(full):
                continue
            if not any(name.endswith(pat.lstrip("*")) for pat in patterns):
                continue
            if not wanted(full):
                continue
            dst_path = os.path.join(DST, dst_dir, name)
            os.makedirs(os.path.dirname(dst_path), exist_ok=True)
            if name.lower().endswith(BINARY_EXTS):
                shutil.copyfile(full, dst_path)
                copied += 1
                continue
            with open(full, "r", encoding="utf-8", errors="replace") as f:
                text = f.read()
            with open(dst_path, "w", encoding="utf-8", newline="\n") as f:
                f.write(scrub(text))
            copied += 1

    print(f"copied {copied} files -> {DST}")

    # root-level files (README) + .gitignore
    for src_name, dst_name in ROOT_FILES.items():
        full = os.path.join(SRC, src_name)
        if not os.path.isfile(full) or not wanted(full):
            continue
        with open(full, "r", encoding="utf-8", errors="replace") as f:
            text = f.read()
        with open(os.path.join(DST, dst_name), "w", encoding="utf-8", newline="\n") as f:
            f.write(scrub(text))
        print(f"  root: {dst_name}")
    with open(os.path.join(DST, ".gitignore"), "w", encoding="utf-8", newline="\n") as f:
        f.write(GITIGNORE)

    for root, dirs, files in os.walk(DST):
        if ".git" in root:
            continue
        rel = os.path.relpath(root, DST)
        print(f"  {rel}: {len(files)} files")


if __name__ == "__main__":
    main()
