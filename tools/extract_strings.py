"""Extract printable strings from rpcs3.exe so we only translate strings that really exist."""
import re
import sys

exe = r"E:\PS3\RPCS3\rpcs3.exe"
data = open(exe, "rb").read()
# ASCII printable runs
runs = re.findall(rb"[\x20-\x7E]{3,120}", data)
strings = []
seen = set()
for r in runs:
    s = r.decode("ascii")
    if s in seen:
        continue
    seen.add(s)
    strings.append(s)

out = r"E:\PS3\Logs\rpcs3_exe_strings.txt"
with open(out, "w", encoding="utf-8") as f:
    f.write("\n".join(strings))
print("total unique strings:", len(strings))

# helper: check which candidate strings are present
candidates = sys.argv[1:]
if candidates:
    present = [c for c in candidates if c in seen]
    missing = [c for c in candidates if c not in seen]
    print("PRESENT:", len(present))
    print("MISSING:", missing)
