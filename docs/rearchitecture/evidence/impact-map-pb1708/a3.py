"""What the DECLARATION changes (the file-level widening) actually are: the first line of each hunk the method
level could not bound, per cluster; and the ambient-orphan methods behind the 'outside every test' reason."""
import pickle
from collections import Counter
from common import *

m, ix = load()
M = m["commit"]
res = pickle.load(open("a1.pkl", "rb"))
shapes = Counter()
for label, d in res.items():
    base, head = d["base"], d["head"]
    it.HEAD = head
    for s, p, mask, whole, notes, extra in d["rows"]:
        n = notes[0] if notes else ""
        if "file level" not in n:
            continue
        if s != "M":
            shapes["new file (status A)"] += 1
            print(f"{label} | {p} | status {s}")
            continue
        for h in it.hunks(base, p):
            h2 = translate(WT, M, base, p, h)
            if it.method_ids(ix, p, h2) is None:
                lines = [x.strip() for x in (h[2] + h[3]) if x.strip() and not it.TRIVIAL_RE.match(x)]
                first = lines[0][:110] if lines else "(blank)"
                if re.search(r"\bconst\b", first):
                    shapes["const"] += 1
                elif re.search(r"static readonly", first):
                    shapes["static readonly field"] += 1
                elif re.search(r"\b(class|record|struct|interface|enum)\b", first):
                    shapes["type declaration"] += 1
                elif re.search(r"^\s*(public|private|internal|protected)[^=;]*\(", first):
                    shapes["member signature / new member"] += 1
                elif first.startswith("["):
                    shapes["attribute"] += 1
                elif first.startswith("using"):
                    shapes["using"] += 1
                else:
                    shapes["other"] += 1
                print(f"{label} | {p}:{h[0]} | {first}")
                break
print(shapes)
amb = ix.ambient & ~ix.reached
names = [ix.entries[i][0] for i in it.bit_ids(amb) if not all(f.startswith(it.TEST_PROJECTS) for f, _, _ in ix.entries[i][1])]
print(f"ambient-only product entries: {len(names)}")
for nme in names[:60]:
    print("   ", nme)
