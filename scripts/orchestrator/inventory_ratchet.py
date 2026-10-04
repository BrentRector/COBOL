#!/usr/bin/env python3
"""The closed-rows ratchet: no closed traceability-inventory row reopens, and GAP does not rise, without a stated reason.

    python scripts/orchestrator/inventory_ratchet.py                   # working tree vs merge-base(HEAD, origin/main)
    python scripts/orchestrator/inventory_ratchet.py --base REF --head REF

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 7 (kb/Work PB1981 item 1). The inventory
(`tests/version-matrix/traceability-inventory.json`) defines "done"; its GAP count is the project's progress metric.
`audit_witness_loss.py` guards a row's EVIDENCE; nothing guarded its STATE, so a landing could reopen a closed row
and every gate stayed green. Two rules:

  1. the CLOSED set (state OK) at `base` is a subset of the closed set at `head`;
  2. when the GAP count rises, every row that is GAP at `head` and was not GAP at `base` is an offender.

An offender is EXCUSED by a line ADDED in the range (DEVLOG.md or any kb/Work/*.md, committed or not) that reads

    reopens-rows: <rule-id-or-glob>[, <rule-id-or-glob>...] — <reason>

(` - ` or `: ` may stand for the dash; the reason must not be empty; globs are fnmatch patterns, so a catalog
harvest can write `reopens-rows: SR-10.7.3-* — 10.7.3 harvested`). Only ADDED lines count, so an old note can
never excuse a later regression. Exit 0 green, 1 with the offending row ids, 2 when `base` has no inventory.
"""
from __future__ import annotations

import argparse
import fnmatch
import pathlib
import re
import subprocess
import sys
from typing import Any, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "spec"))
from audit_witness_loss import default_base, rows_at  # noqa: E402  (one reader of the inventory at a ref)
from inventory_schema import REPO, load_inventory  # noqa: E402

WORKTREE = "WORKTREE"
CLOSED = "OK"
GAP = "GAP"
MARKER = re.compile(r"reopens-rows:\s*(?P<ids>[A-Za-z0-9.*?\[\]\-]+(?:\s*,\s*[A-Za-z0-9.*?\[\]\-]+)*)\s*"
                    r"(?:—|–|\s-\s|:)\s*(?P<reason>\S.*)$")
EXCUSE_PATHS = ("DEVLOG.md", "kb/Work")


def compare(before: list[dict[str, Any]], after: list[dict[str, Any]]) -> dict[str, Any]:
    """The ratchet's verdict on two inventories, before excuses: reopened rows, new GAP rows when GAP rose, counts."""
    state_before = {r["rule-id"]: r.get("state") for r in before}
    state_after = {r["rule-id"]: r.get("state") for r in after}
    reopened = sorted(rid for rid, s in state_before.items() if s == CLOSED and state_after.get(rid) != CLOSED)
    gap_before = sum(1 for s in state_before.values() if s == GAP)
    gap_after = sum(1 for s in state_after.values() if s == GAP)
    new_gap = sorted(rid for rid, s in state_after.items() if s == GAP and state_before.get(rid) != GAP) \
        if gap_after > gap_before else []
    return {"reopened": reopened, "new_gap": new_gap, "gap_before": gap_before, "gap_after": gap_after}


def excuses(added_lines: Iterable[str]) -> list[tuple[str, str]]:
    """(pattern, reason) for every marker in the added lines."""
    out = []
    for line in added_lines:
        m = MARKER.search(line)
        if m and m.group("reason").strip():
            out += [(p.strip(), m.group("reason").strip()) for p in m.group("ids").split(",") if p.strip()]
    return out


def unexcused(ids: Iterable[str], marks: list[tuple[str, str]]) -> list[str]:
    return [rid for rid in ids if not any(fnmatch.fnmatchcase(rid, pat) for pat, _ in marks)]


def _git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, check=True, capture_output=True, text=True,
                          encoding="utf-8", errors="replace").stdout


def added_lines(base: str, head: str) -> list[str]:
    """Every line added between base and head in DEVLOG.md and kb/Work (head WORKTREE adds untracked notes whole)."""
    rng = [base] if head == WORKTREE else [base, head]
    diff = _git("diff", "--unified=0", "--no-color", *rng, "--", *EXCUSE_PATHS)
    lines = [l[1:] for l in diff.splitlines() if l.startswith("+") and not l.startswith("+++")]
    if head == WORKTREE:
        for rel in _git("ls-files", "--others", "--exclude-standard", "--", "kb/Work").split():
            lines += (REPO / rel).read_text(encoding="utf-8", errors="replace").splitlines()
    return lines


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--base", help="the ref to compare against (default: merge-base of HEAD and origin/main)")
    ap.add_argument("--head", default=WORKTREE, help=f"a ref, or {WORKTREE} (default) for the working tree")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    base = a.base or default_base()
    before = rows_at(base)
    if before is None:
        print(f"⛔ the inventory does not exist at {base}: nothing to compare against")
        return 2
    after = load_inventory() if a.head == WORKTREE else rows_at(a.head)
    if after is None:
        print(f"⛔ the inventory does not exist at {a.head}")
        return 2
    v = compare(before, after)
    marks = excuses(added_lines(base, a.head))
    bad_reopen = unexcused(v["reopened"], marks)
    bad_gap = unexcused(v["new_gap"], marks)
    for rid in v["reopened"]:
        print(f"  {'⛔ REOPENED' if rid in bad_reopen else 'ⓘ reopened (excused)'} {rid}")
    for rid in v["new_gap"]:
        if rid not in v["reopened"]:
            print(f"  {'⛔ NEW GAP ' if rid in bad_gap else 'ⓘ new GAP (excused)'} {rid}")
    offenders = sorted(set(bad_reopen) | set(bad_gap))
    print(f"=== INVENTORY RATCHET {base[:12]}..{a.head[:12]}: GAP {v['gap_before']} -> {v['gap_after']}, "
          f"{len(v['reopened'])} reopened, {len(offenders)} unexcused — {'RED' if offenders else 'GREEN'} ===")
    if offenders:
        print("offending rows: " + " ".join(offenders))
        print("excuse a deliberate reopen with an added line `reopens-rows: <ids> — <reason>` in the DEVLOG entry "
              "or a kb/Work note")
    return 1 if offenders else 0


if __name__ == "__main__":
    sys.exit(main())
