"""Definitive check: does rpcs3_ko.qm resolve RPCS3 tr() calls with their real contexts?"""
import sys
import xml.etree.ElementTree as ET
from PySide6.QtCore import QTranslator, QCoreApplication

TS = r"E:\PS3\Mods_Patches\RPCS3_Translation\rpcs3_ko.ts"
QM = r"E:\PS3\RPCS3\qt6\translations\rpcs3_ko.qm"

tree = ET.parse(TS)
pairs = []          # (context, source, translation)
for ctx in tree.getroot().findall("context"):
    name = ctx.findtext("name") or ""
    for msg in ctx.findall("message"):
        src = msg.findtext("source") or ""
        tr = msg.findtext("translation") or ""
        if tr:
            pairs.append((name, src, tr))

print(f"translated messages in .ts: {len(pairs)}")
app = QCoreApplication(sys.argv)
t = QTranslator()
print("load qm:", t.load(QM))
app.installTranslator(t)

hits = 0
for ctx, src, tr in pairs:
    got = t.translate(ctx, src)
    if got == tr:
        hits += 1
print(f"context-correct lookups resolved: {hits}/{len(pairs)}")

print("\nsample lookups:")
for ctx, src, tr in pairs[:12]:
    print(f"  [{ctx:22}] {src[:42]:44} -> {t.translate(ctx, src)}")
