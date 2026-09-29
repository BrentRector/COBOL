"""Per-file attribution of every replayed cluster of trains 65-69 against the dbea12428 map."""
import pickle
from common import *

m, ix = load()
M = m["commit"]
cases = json.loads((HERE / "cases-trains.json").read_text())
res = {}
for label, b, h in cases:
    base = git("rev-parse", b).strip()
    head = git("rev-parse", h).strip()
    rows = per_file(ix, M, base, head)
    tot = 0
    for r in rows:
        tot |= r[2]
    print(f"== {label}: files {len(rows)} total map_selected {count(ix, tot)}", flush=True)
    for s, p, mask, whole, notes, extra in sorted(rows, key=lambda r: -count(ix, r[2])):
        n = count(ix, mask)
        if n > 300 or whole:
            why = ("WHOLE: " + whole[0][:110]) if whole else ""
            note = notes[0][len(p) + 2:][:130] if notes else ""
            print(f"   {n:5d} {p}  {why} | {note}")
    res[label] = dict(base=base, head=head, rows=rows)
pickle.dump(res, open("a1.pkl", "wb"))
