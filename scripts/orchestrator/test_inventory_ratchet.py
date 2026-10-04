#!/usr/bin/env python3
"""Self-test for inventory_ratchet.py: a fabricated reopen and a fabricated GAP rise are RED, the marker excuses
exactly the rows it names, and the real repository against itself is GREEN.
Run: python scripts/orchestrator/test_inventory_ratchet.py   (no submodule, no build)."""
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import inventory_ratchet as r  # noqa: E402

fails, checked = [], []


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def row(rid, state):
    return {"rule-id": rid, "state": state}


BASE = [row("SR-1-1", "OK"), row("SR-1-2", "OK"), row("SR-1-3", "GAP"), row("GR-2-1", "OK")]

# 1. nothing changed: green
v = r.compare(BASE, BASE)
check("identical", (v["reopened"], v["new_gap"]), ([], []))

# 2. a fabricated reopen: SR-1-2 goes OK -> GAP (GAP rises 1 -> 2)
reopen = [row("SR-1-1", "OK"), row("SR-1-2", "GAP"), row("SR-1-3", "GAP"), row("GR-2-1", "OK")]
v = r.compare(BASE, reopen)
check("reopen detected", v["reopened"], ["SR-1-2"])
check("reopen is also new GAP", v["new_gap"], ["SR-1-2"])
check("gap counts", (v["gap_before"], v["gap_after"]), (1, 2))
check("unexcused reopen", r.unexcused(v["reopened"], []), ["SR-1-2"])

# 3. a closed row that VANISHES is a shrink of the closed set
v = r.compare(BASE, [row("SR-1-1", "OK"), row("SR-1-3", "GAP"), row("GR-2-1", "OK")])
check("vanished closed row", v["reopened"], ["SR-1-2"])

# 4. a swap (one row closes, another reopens, GAP flat) is still a reopen, but adds no new-GAP offenders
v = r.compare(BASE, [row("SR-1-1", "OK"), row("SR-1-2", "GAP"), row("SR-1-3", "OK"), row("GR-2-1", "OK")])
check("swap: reopen", v["reopened"], ["SR-1-2"])
check("swap: GAP did not rise", v["new_gap"], [])

# 5. a GAP rise from newly catalogued rows
v = r.compare(BASE, BASE + [row("SR-9-1", "GAP"), row("SR-9-2", "GAP")])
check("new rows raise GAP", v["new_gap"], ["SR-9-1", "SR-9-2"])
check("new rows are not reopens", v["reopened"], [])

# 6. the marker
marks = r.excuses([
    "Some DEVLOG prose. reopens-rows: SR-1-2 — the rule was re-adjudicated DIVERGES (PB2000)",
    "reopens-rows: SR-9-* - 9.x harvested from the catalog",
    "reopens-rows: GR-2-1 —   ",          # no reason: not an excuse
    "reopens rows: GR-2-1 — misspelled marker",
])
check("marker patterns", [p for p, _ in marks], ["SR-1-2", "SR-9-*"])
check("marker excuses named row", r.unexcused(["SR-1-2"], marks), [])
check("marker glob excuses harvest", r.unexcused(["SR-9-1", "SR-9-2"], marks), [])
check("marker does not excuse others", r.unexcused(["GR-2-1", "SR-1-1"], marks), ["GR-2-1", "SR-1-1"])
check("glob does not overmatch", r.unexcused(["SR-91-1"], marks), ["SR-91-1"])
check("multi-id marker", [p for p, _ in r.excuses(["reopens-rows: A-1, B-2 , C-3: reason here"])], ["A-1", "B-2", "C-3"])

# 7. end to end on the real repository: HEAD against itself is green
p = subprocess.run([sys.executable, str(HERE / "inventory_ratchet.py"), "--base", "HEAD", "--head", "HEAD"],
                   capture_output=True, text=True, encoding="utf-8")
check("real repo HEAD..HEAD exit", p.returncode, 0)
check("real repo verdict line", "GREEN ===" in p.stdout, True)

for f in fails:
    print("FAIL:", f)
print(f"inventory ratchet self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
