"""Classify every changed file's contribution and compute counterfactual selection sizes per cluster."""
import pickle
from collections import Counter
from common import *

m, ix = load()
res = pickle.load(open("a1.pkl", "rb"))
REG = {"src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs", "src/Cobol.Net.Frontend/Diagnostics/DiagnosticDescriptors.cs",
       "src/Cobol.Net.Editions/ConstructRegistry.g.cs", "src/Cobol.Net.Editions/Constructs.g.cs"}


def kind(p, notes, whole):
    n = notes[0] if notes else ""
    if p in REG:
        return "REG"
    if whole:
        return "WHOLE"
    if "file level" in n:
        return "DECL"
    if ".cctor" in n:
        return "CCTOR"
    if "changed method" in n:
        return "METHOD"
    return "DATA"


pop = len(ix.by_assembly["Conformance"])
agg = Counter()
print(f"population {pop}")
print("label | total | -REG | -REG-DECL(method+cctor+data) | -REG-DECL-CCTOR | whole reasons (non-REG) | kinds")
for label, d in res.items():
    rows = d["rows"]
    masks = {}
    kinds = Counter()
    wr = []
    for s, p, mask, whole, notes, extra in rows:
        k = kind(p, notes, whole)
        kinds[k] += 1
        masks.setdefault(k, 0)
        masks[k] |= mask
        if whole and p not in REG:
            wr += [w.split(":")[0] if w.startswith(("src/", "tests/")) else w[:60] for w in whole]
    allm = 0
    for v in masks.values():
        allm |= v
    no_reg = 0
    for k, v in masks.items():
        if k != "REG":
            no_reg |= v
    meth = masks.get("METHOD", 0) | masks.get("CCTOR", 0) | masks.get("DATA", 0) | masks.get("WHOLE", 0)
    meth_nc = masks.get("METHOD", 0) | masks.get("DATA", 0)
    print(f"{label} | {count(ix, allm)} | {count(ix, no_reg)} | {count(ix, meth)} | {count(ix, meth_nc)} | "
          f"{sorted(set(wr))} | {dict(kinds)}")
