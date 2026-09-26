"""Functional test: does Qt 6.11.2 resolve our context-less Korean .qm through tr()?"""
import sys
from PySide6.QtCore import QTranslator, QCoreApplication, QLocale

app = QCoreApplication(sys.argv)
QM = r"E:\PS3\RPCS3\qt6\translations\rpcs3_ko.qm"
t = QTranslator()
ok = t.load(QM)
print("load:", ok)
if ok:
    app.installTranslator(t)
    samples = [
        ("MainWindow", "Boot"),
        ("game_list_frame", "&Manage Game"),
        ("settings_dialog", "Settings"),
        ("LogFrame", "Log"),
        ("PatchManagerDialog", "Download latest patches"),
        ("settings_dialog", "Write Color Buffers"),
    ]
    hits = 0
    for ctx, src in samples:
        out = t.translate(ctx, src)
        status = "OK " if out else "MISS"
        if out:
            hits += 1
        print(f"{status} ctx={ctx:18} '{src}' -> '{out}'")
    print(f"\nresolved {hits}/{len(samples)} (fallback to empty context works: {hits > 0})")
    # reverse check: an untranslated string must stay empty
    print("untranslated control:", repr(t.translate("MainWindow", "ThisStringIsNotInTheQm")))
