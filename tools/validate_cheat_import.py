#!/usr/bin/env python
"""Validate external RPCS3 cheat/patch text BEFORE importing it.

Usage:
    python validate_cheat_import.py <file> [--expect-serial BCAS20298] [--expect-version 01.09]
                                          [--expect-ppu bc3ee27f265ee62d1d26ebe2323b69384db5708b]
                                          [--allow-foreign]

Checks performed (all must pass unless --allow-foreign):
  1. syntax            : the file is valid YAML and uses the RPCS3 patch keys
  2. title validation  : every "Games:" block contains the expected serial
  3. version validation: every app version list contains the expected version
  4. hash validation   : every top level key is the expected PPU-<hash>
  5. address width     : each patch entry has a valid type and a plausible address
  6. safety            : warns about code patches (be32 on .text) vs data patches

Exit codes: 0 = safe to import, 1 = rejected, 2 = usage error.
Nothing is written or imported by this script.
"""
import argparse
import re
import sys

# console safety: never crash on non-CP949 output
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

try:
    import yaml
except ImportError:
    print("PyYAML required: python -m pip install pyyaml")
    sys.exit(2)

VALID_TYPES = {
    "byte", "le16", "le32", "le64", "lef32", "lef64",
    "be16", "be32", "be64", "bd32", "bd64", "bef32", "bef64",
    "utf8", "c_utf8", "load", "alloc", "calloc", "code_alloc",
    "jump", "jump_link", "jump_func", "move_file", "hide_file",
    "bp_exec",
}
MAX_ADDR = 0x20000000          # PPU code/data lives below 512 MB on PS3
KNOWN_KEYS = {
    "Games", "Author", "Notes", "Patch Version", "Patch", "Group",
    "Configurable Values", "Anchors", "Enabled",
}


def norm_ver(v):
    """Normalise a version scalar (YAML may give 1.09 as float for 01.09)."""
    parts = re.split(r"[.]", str(v).strip().strip('"').strip("'"))
    out = []
    for p in parts:
        p = p.strip()
        try:
            out.append(str(int(p)))
        except ValueError:
            out.append(p)
    return tuple(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("file")
    ap.add_argument("--expect-serial", default="BCAS20298")
    ap.add_argument("--expect-version", default="01.09")
    ap.add_argument("--expect-ppu", default="bc3ee27f265ee62d1d26ebe2323b69384db5708b")
    ap.add_argument("--allow-foreign", action="store_true",
                    help="only validate syntax; do not require serial/version/hash match")
    args = ap.parse_args()

    try:
        text = open(args.file, encoding="utf-8", errors="strict").read()
    except Exception as e:
        print(f"[REJECT] cannot read file: {e}")
        return 1

    try:
        doc = yaml.safe_load(text)
    except Exception as e:
        print(f"[REJECT] YAML syntax error: {e}")
        return 1
    if not isinstance(doc, dict):
        print("[REJECT] top level is not a mapping")
        return 1

    errors, warnings, entries = [], [], 0

    for top_key, body in doc.items():
        if top_key == "Anchors":
            continue
        if not re.fullmatch(r"PPU-[0-9a-f]{40}", str(top_key)):
            errors.append(f"top level key '{top_key}' is not a PPU-<40 hex> hash")
        elif not args.allow_foreign and top_key != f"PPU-{args.expect_ppu}":
            errors.append(f"PPU hash mismatch: file={top_key} expected=PPU-{args.expect_ppu}")

        if not isinstance(body, dict):
            errors.append(f"{top_key}: body is not a mapping")
            continue
        for patch_name, patch in body.items():
            if not isinstance(patch, dict):
                errors.append(f"{top_key}/{patch_name}: patch is not a mapping")
                continue
            unknown = set(patch.keys()) - KNOWN_KEYS
            if unknown:
                warnings.append(f"{top_key}/{patch_name}: unknown keys {sorted(unknown)}")

            games = patch.get("Games") or {}
            if not isinstance(games, dict) or not games:
                errors.append(f"{top_key}/{patch_name}: missing Games block")
            else:
                for title, serials in games.items():
                    if not isinstance(serials, dict):
                        errors.append(f"{top_key}/{patch_name}: Games/{title} malformed")
                        continue
                    for serial, versions in serials.items():
                        if not args.allow_foreign and serial != args.expect_serial:
                            errors.append(f"{top_key}/{patch_name}: serial {serial} != {args.expect_serial}")
                        vlist = versions if isinstance(versions, list) else [versions]
                        for v in vlist:
                            if not args.allow_foreign and norm_ver(v) != norm_ver(args.expect_version):
                                errors.append(f"{top_key}/{patch_name}: version {v} != {args.expect_version}")

            patch_data = patch.get("Patch")
            if not isinstance(patch_data, list) or not patch_data:
                errors.append(f"{top_key}/{patch_name}: missing Patch list")
                continue
            for item in patch_data:
                if isinstance(item, str) and item.startswith("*"):   # anchor reference
                    continue
                if not isinstance(item, list) or len(item) < 2:
                    errors.append(f"{top_key}/{patch_name}: malformed patch entry {item!r}")
                    continue
                ptype = str(item[0]).strip().lower()
                entries += 1
                if ptype not in VALID_TYPES:
                    errors.append(f"{top_key}/{patch_name}: unknown patch type '{ptype}'")
                    continue
                addr = item[1]
                if ptype in ("load", "calloc", "code_alloc") and isinstance(addr, str) and addr.startswith("*"):
                    continue
                if isinstance(addr, int):
                    a = addr
                    addr = f"0x{a:08X}"
                    if a > MAX_ADDR:
                        errors.append(f"{top_key}/{patch_name}: address {addr} above 0x{MAX_ADDR:X}")
                    if ptype == "be64" and a % 4:
                        warnings.append(f"{top_key}/{patch_name}: be64 at unaligned address {addr}")
                    if ptype in ("be32", "bd32", "bef32") and a % 4:
                        warnings.append(f"{top_key}/{patch_name}: 32-bit write at unaligned address {addr}")
                    if ptype in ("be32", "be64", "be16", "jump", "jump_link", "code_alloc"):
                        warnings.append(f"{top_key}/{patch_name}: code-level write ({ptype} @ {addr}) - version specific")
                    continue
                if isinstance(addr, str) and re.fullmatch(r"0x[0-9A-Fa-f]{1,8}", addr):
                    a = int(addr, 16)
                    if a > MAX_ADDR:
                        errors.append(f"{top_key}/{patch_name}: address {addr} above 0x{MAX_ADDR:X}")
                    if ptype == "be64" and a % 4:
                        warnings.append(f"{top_key}/{patch_name}: be64 at unaligned address {addr}")
                    if ptype in ("be32", "bd32", "bef32") and a % 4:
                        warnings.append(f"{top_key}/{patch_name}: 32-bit write at unaligned address {addr}")
                elif ptype not in ("load", "calloc", "code_alloc", "jump", "jump_link", "jump_func", "code_alloc"):
                    errors.append(f"{top_key}/{patch_name}: bad address {addr!r}")
                if ptype in ("be32", "be64", "be16", "jump", "jump_link", "code_alloc"):
                    warnings.append(f"{top_key}/{patch_name}: code-level write ({ptype} @ {addr}) - "
                                    "these are version specific and can crash the game")

    print(f"file        : {args.file}")
    print(f"patch entries: {entries}")
    print(f"expect      : serial={args.expect_serial} version={args.expect_version} ppu=PPU-{args.expect_ppu}"
          + ("  (foreign allowed)" if args.allow_foreign else ""))
    if warnings:
        print("\n[WARNINGS]")
        for w in warnings[:30]:
            print("  -", w)
    if errors:
        print("\n[REJECTED]")
        for e in errors[:30]:
            print("  -", e)
        print(f"\n=> NOT safe to import ({len(errors)} error(s)). Nothing was written.")
        return 1

    print("\n[PASS] syntax, title, version, hash and address checks passed.")
    print("=> Safe to import. Recommended: back up config\\imported_patch.yml first, then use")
    print("   RPCS3 Patch Manager -> Import (drag & drop) or Patch Manager -> Import Cheats.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
