#!/usr/bin/env python3
"""Drift test for the architectural-invariant rules.

Two independent checks, because a rule set can fail in two opposite directions:

  1. EVERY rule must still FIRE on scripts/semgrep/testdata/Violations.cs. A rule that silently stops matching
     reports zero findings and looks green — strictly worse than no rule. This has already happened once: an
     invented `pattern-not-inside` suppression construct disabled two rules while the scan still said "success".
     The path-scoped rules are re-run with their `paths:` filter stripped, since the testdata lives outside src/.

  2. The greenfield tree's finding count must not EXCEED the recorded baseline. New violations fail the check;
     fixing violations is expected and prints a reminder to lower the baseline.

Usage:  python scripts/semgrep/verify.py [--update-baseline]
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
CONFIG = HERE / "invariants.yml"
TESTDATA = HERE / "testdata"
BASELINE = HERE / "baseline.json"

GREENFIELD = [
    "src/Cobol.Net.Cli", "src/Cobol.Net.Compiler", "src/Cobol.Net.Editions",
    "src/Cobol.Net.Frontend", "src/Cobol.Net.Runtime",
]


def run(config: pathlib.Path, targets: list[str], no_git_ignore: bool = False) -> list[dict]:
    cmd = ["semgrep", "scan", "--config", str(config), "--metrics=off", "--json", "-q"]
    if no_git_ignore:
        cmd.append("--no-git-ignore")
    cmd += targets
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", cwd=str(REPO), timeout=900)
    if not proc.stdout.strip():
        sys.exit(f"semgrep produced no output.\n{proc.stderr[-2000:]}")
    return json.loads(proc.stdout)["results"]


def rule_ids(config: pathlib.Path) -> list[str]:
    import re
    return re.findall(r"^\s*-\s+id:\s*(\S+)", config.read_text(encoding="utf-8"), re.M)


def strip_paths(config: pathlib.Path, into: pathlib.Path) -> pathlib.Path:
    """Copy the config with every `paths:` block removed, so path-scoped rules can be fire-tested. The copy goes into
    the caller's own directory `into`, never a fixed name in the machine-wide temp directory (kb/Work PB2922)."""
    out, skipping, skip_indent = [], False, 0
    for line in config.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        indent = len(line) - len(line.lstrip())
        if skipping:
            if stripped and indent <= skip_indent:
                skipping = False
            else:
                continue
        if stripped.startswith("paths:"):
            skipping, skip_indent = True, indent
            continue
        out.append(line)
    copy = into / "invariants-nopaths.yml"
    copy.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    return copy


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--update-baseline", action="store_true")
    args = ap.parse_args()

    declared = rule_ids(CONFIG)
    print(f"{len(declared)} rules declared.\n")

    # ---- Check 1: every rule fires on the deliberate violations -------------------------------------------
    with tempfile.TemporaryDirectory(prefix="cobolnet-semgrep-") as tmp:
        fired = {r["check_id"].split(".")[-1]
                 for r in run(strip_paths(CONFIG, pathlib.Path(tmp)), [str(TESTDATA)], no_git_ignore=True)}
    dead = [r for r in declared if r not in fired]
    print("CHECK 1 — every rule fires on testdata/")
    for r in declared:
        print(f"   {'OK  ' if r in fired else 'DEAD'}  {r}")
    if dead:
        print(f"\nFAIL: {len(dead)} rule(s) matched nothing. A rule that never fires is worse than no rule —")
        print("      it reports green. Fix the pattern or remove the rule.")
        return 1

    # ---- Check 2: the greenfield tree has not regressed ---------------------------------------------------
    results = run(CONFIG, GREENFIELD)
    counts: dict[str, int] = {}
    for r in results:
        rid = r["check_id"].split(".")[-1]
        counts[rid] = counts.get(rid, 0) + 1

    if args.update_baseline:
        BASELINE.write_text(json.dumps(counts, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
        print(f"\nBaseline written: {counts}")
        return 0

    base = json.loads(BASELINE.read_text(encoding="utf-8")) if BASELINE.exists() else {}
    print("\nCHECK 2 — greenfield findings vs baseline")
    failed = False
    for rid in sorted(set(base) | set(counts)):
        now, was = counts.get(rid, 0), base.get(rid, 0)
        flag = "OK  "
        if now > was:
            flag, failed = "UP  ", True
        elif now < was:
            flag = "DOWN"
        print(f"   {flag}  {rid}: {was} -> {now}")
    if failed:
        print("\nFAIL: new violations introduced. Fix them, or record an explicit decision and re-baseline.")
        return 1
    if any(counts.get(r, 0) < base.get(r, 0) for r in base):
        print("\nViolations went DOWN — re-run with --update-baseline to lock the improvement in.")
    print("\nPASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
