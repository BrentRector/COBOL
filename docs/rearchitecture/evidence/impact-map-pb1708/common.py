"""PB1708 measurement helpers: load a recorded impact map through impacted_tests.py and replay a commit per file.

Re-running the evidence (every path is repo-relative; nothing reads a session scratchpad):
1. Record the two maps the numbers were measured on, into the shared store `<git common dir>/cobol-impact/`:
       python scripts/spec/record_impact_map.py --commit dbea12428
       python scripts/spec/record_impact_map.py --commit 4b0f3e22a
   (each ~19 min at BelowNormal; the recorder's `--store DIR` redirects where it WRITES, and `--work DIR --keep`
   keeps the raw per-test contexts that `a11.py` reads). These maps are NOT in the repository (README.md).
   The scripts READ `<git common dir>/cobol-impact/`, or the directory the COBOLNET_IMPACT_STORE environment
   variable names — a variable only this file reads.
2. Run any `aN.py` from this directory: `python aN.py > aN.txt`.
The replayed commits are `cases-trains.json` (trains 65-69) and `cases-68b.json` (the two dropped 68b branches).
"""
import json, os, re, subprocess, sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
WT = HERE.parents[3]  # docs/rearchitecture/evidence/impact-map-pb1708 -> the repository root
sys.path.insert(0, str(WT / "scripts" / "spec"))
sys.path.insert(0, str(HERE))
import impacted_tests as it  # noqa
from replay_tr import translate  # noqa


def git(*a):
    return subprocess.run(["git", *a], cwd=WT, capture_output=True, text=True, encoding="utf-8").stdout


def store() -> Path:
    env = os.environ.get("COBOLNET_IMPACT_STORE")
    if env:
        return Path(env)
    common = Path(git("rev-parse", "--git-common-dir").strip())
    return (common if common.is_absolute() else (WT / common).resolve()) / "cobol-impact"


def load(sha="dbea1242895386b1ce3ae9ef75c621f470e6aec6"):
    m = it.load_map(store() / f"{sha}.json.gz")
    return m, it.MapIndex(m)


def per_file(ix, M, base, head):
    """[(status, path, mask, whole_reasons, notes, extra_terms)] — each changed file selected alone, hunks
    translated to M's line numbers (replay_tr.translate: over-approximates, never under)."""
    it.HEAD = head
    changes = it.changed_files(base)
    orig = it.hunks
    it.hunks = lambda b, p: [translate(WT, M, base, p, h) for h in orig(b, p)]
    out = []
    try:
        for s, p in changes:
            sel = it.Selection()
            it.select(ix, [(s, p)], base, sel)
            out.append((s, p, sel.mask, list(sel.whole), list(sel.notes), set(sel.extra_terms)))
    finally:
        it.hunks = orig
    return out


def testset(ix, mask):
    tests, classes = ix.selection(mask)
    return tests | {i for i in ix.conf if ix.tests[i][3] in classes}


def count(ix, mask):
    return len(testset(ix, mask))
