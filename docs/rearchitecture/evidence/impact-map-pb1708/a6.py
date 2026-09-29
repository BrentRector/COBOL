"""(1) The METHOD-granularity FLOOR of every replayed cluster: only the changed method bodies (non-registry static
constructors still reach their type's users) and the data files — i.e. what a perfectly precise registry AND
declaration analysis could reach at best, since both only ADD tests on top of it.
(2) Train 68b X and Y, against the map at their own base 4b0f3e22a, with every registry contribution REMOVED (a lower
bound of the entry-level design, which adds the changed entries' readers back): are the tests that went red still
selected?"""
import pickle, statistics
from common import *

REG = {"src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs", "src/Cobol.Net.Frontend/Diagnostics/DiagnosticDescriptors.cs",
       "src/Cobol.Net.Editions/ConstructRegistry.g.cs", "src/Cobol.Net.Editions/Constructs.g.cs"}


def floor_mask(rows):
    mk = 0
    for s, p, mask, whole, notes, extra in rows:
        n0 = notes[0] if notes else ""
        if p in REG:
            continue
        if "changed method" in n0 and "file level" not in n0:
            mk |= mask
        elif not p.endswith(".cs"):
            mk |= mask
    return mk


m, ix = load()
res = pickle.load(open("a1.pkl", "rb"))
fl = []
for label, d in res.items():
    c = count(ix, floor_mask(d["rows"]))
    fl.append(c)
    print(f"{label}: floor {c}")
print(f"FLOOR over 20 clusters: min {min(fl)} median {statistics.median(fl)} max {max(fl)} of {len(ix.conf)}; "
      f">= 50%: {sum(1 for c in fl if c >= len(ix.conf) / 2)}")

m2, ix2 = load("4b0f3e22ae272ae6f5163b42b45b0bbd3ad1d707")
M2 = m2["commit"]
RED_X = ["DiagnosticPositionTests.Copy_MainLinesStayMain_CopiedLinesNameTheCopybook",
         "DiagnosticPositionTests.ParseErrorInsideCopybook_NamesTheCopybookLine"]
for label, b, h in json.loads((HERE / "cases-68b.json").read_text()):
    base = git("rev-parse", b).strip(); head = git("rev-parse", h).strip()
    rows = per_file(ix2, M2, base, head)
    full = 0; noreg = 0; regfiles = []
    whole = []
    for s, p, mask, w, notes, extra in rows:
        full |= mask
        whole += w
        if p in REG:
            regfiles.append(p)
        else:
            noreg |= mask
    sel_full = {ix2.tests[i][2] for i in testset(ix2, full)}
    sel_noreg = {ix2.tests[i][2] for i in testset(ix2, noreg)}
    print(f"{label}: registry files {regfiles}; selected today {len(sel_full)}; with every registry contribution "
          f"removed {len(sel_noreg)} of {len(ix2.conf)}; whole reasons {sorted({x.split(':')[0] for x in whole})[:4]}")
    if "X" in label:
        for r in RED_X:
            print(f"   red {r}: today {any(r in t for t in sel_full)}, registry removed {any(r in t for t in sel_noreg)}")
    else:
        f = "src/Cobol.Net.Compiler/Binding/DataBinder.Constants.cs"
        cover = {ix2.tests[i][2] for i in testset(ix2, ix2.file_ids.get(f, 0))}
        print(f"   tests executing {f}: {len(cover)}; selected with registry removed: {len(cover & sel_noreg)}")
        const_null = [t for t in cover if "CONSTANT" in t.upper() and "NULL" in t.upper()]
        print(f"   of them naming CONSTANT and NULL: {len(const_null)}; selected: {len(set(const_null) & sel_noreg)}")
