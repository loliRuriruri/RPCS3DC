"""Build a context-correct Korean .ts for RPCS3 by running lupdate over the source tree
and merging our curated translations, then compile it to rpcs3_ko.qm."""
import os
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

SRC_ZIP = r"E:\PS3\Tools\rpcs3-src.zip"
SRC_DIR = r"E:\PS3\Tools\rpcs3-src"
OUT_DIR = r"E:\PS3\Mods_Patches\RPCS3_Translation"
TS_ALL = os.path.join(OUT_DIR, "rpcs3_all.ts")
TS_KO = os.path.join(OUT_DIR, "rpcs3_ko.ts")
QM_KO = os.path.join(OUT_DIR, "rpcs3_ko.qm")
QM_DEST = r"E:\PS3\RPCS3\qt6\translations\rpcs3_ko.qm"
LUPDATE = os.path.join(os.path.dirname(sys.executable), "Scripts", "pyside6-lupdate.exe")
LRELEASE = os.path.join(os.path.dirname(sys.executable), "Scripts", "pyside6-lrelease.exe")

# import the curated dictionary from the previous script
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_korean_translation import KO  # noqa: E402

os.makedirs(OUT_DIR, exist_ok=True)

# 1) extract source
if not os.path.isdir(SRC_DIR):
    os.makedirs(SRC_DIR, exist_ok=True)
    shutil.unpack_archive(SRC_ZIP, SRC_DIR)
roots = [d for d in os.listdir(SRC_DIR) if d.startswith("rpcs3-")]
assert roots, "source tree not found"
root = os.path.join(SRC_DIR, roots[0])
qt_dir = os.path.join(root, "rpcs3", "rpcs3qt")
print("source root:", root)

# 2) run lupdate over the Qt UI sources
cmd = [LUPDATE, qt_dir, "-ts", TS_ALL, "-no-obsolete", "-locations", "none"]
print("lupdate:", " ".join(cmd[:2]), "...")
r = subprocess.run(cmd, capture_output=True, text=True)
print("lupdate rc:", r.returncode)
print((r.stdout or "")[-600:])
print((r.stderr or "")[-600:])

# 3) merge curated translations into the context-correct file
tree = ET.parse(TS_ALL)
ts = tree.getroot()
merged = 0
missing = []
for ctx in ts.findall("context"):
    for msg in ctx.findall("message"):
        src_el = msg.find("source")
        tr_el = msg.find("translation")
        if src_el is None or tr_el is None:
            continue
        src = src_el.text or ""
        if src in KO:
            tr_el.text = KO[src]
            if "type" in tr_el.attrib:
                del tr_el.attrib["type"]
            merged += 1
        elif ("&" + src) in KO:
            tr_el.text = KO["&" + src]
            if "type" in tr_el.attrib:
                del tr_el.attrib["type"]
            merged += 1

# report curated strings that never appeared in the source scan
all_sources = {(m.find("source").text or "") for c in ts.findall("context") for m in c.findall("message")}
for k in KO:
    if k not in all_sources and ("&" + k) not in all_sources:
        missing.append(k)

ts.set("language", "ko_KR")
ts.set("sourcelanguage", "en_US")
tree.write(TS_KO, encoding="utf-8", xml_declaration=True)
print("total messages in ts:", len(all_sources))
print("merged translations:", merged)
print("curated entries not found in source:", len(missing), missing[:10])

# 4) compile
r = subprocess.run([LRELEASE, TS_KO, "-qm", QM_KO], capture_output=True, text=True)
print("lrelease rc:", r.returncode, (r.stdout or "").strip()[-300:])
if os.path.exists(QM_KO):
    shutil.copyfile(QM_KO, QM_DEST)
    print("installed ->", QM_DEST, os.path.getsize(QM_DEST), "bytes")
