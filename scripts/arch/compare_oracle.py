#!/usr/bin/env python3
"""compare_oracle.py — the behavior-neutrality comparison every restructuring wave runs (kb/Work PB2116;
docs/rearchitecture/DESIGN-architecture-review.md §4).

Compares two oracle captures CASE BY CASE, never by totals: every case whose emitted C# or diagnostic stream
differs is printed with its id (which names the program, or construct, and the edition), and so is every case one
capture has and the other lacks. Exit 0 only when the two are identical; a wave's DEVLOG entry either shows the
IDENTICAL line or explains every difference (a renamed runtime helper is the only expected class).

THE BASELINE IS THE CHANGE'S OWN BASE COMMIT, CAPTURED (kb/Work PB3107). A capture is a pure function of its commit
(DESIGN-architecture-review.md §4.1: two captures of one commit are identical), so nothing is committed: the baseline
is the capture of the commit the change departs from, by default the merge base of HEAD and origin/main, made on
demand by that commit's OWN capture_oracle.py in a detached worktree of it and kept under
TestResults/arch-oracle/<commit12>/ (blobs and manifest), where a later comparison against the same base finds it.
A committed baseline could describe only the tree it was recorded on, and GitHub's merge queue lands each pull
request on a tree no branch ever recorded (its group is built on every group queued ahead of it): two input-changing
landings queued together left two baselines, or one behind the other's inputs, and bounced (PB3107).

USAGE
  python scripts/arch/compare_oracle.py [--base REV] [--diff] [--no-build] [--jobs N]
  python scripts/arch/compare_oracle.py <baseline-manifest> [<new-manifest>] [--diff] [--no-build] [--jobs N]

  --base REV           the commit to compare against (default: `git merge-base HEAD origin/main`), captured as above
  <baseline-manifest>  instead, an existing capture's TestResults/arch-oracle/<label>/manifest.json
  <new-manifest>       default: capture the current tree now (scripts/arch/capture_oracle.py, into TestResults/)
  --diff               print a unified diff of each differing case from the two captures' blob directories (the
                       directory each manifest sits in)

OUTPUT
  CSHARP <id>       the emitted C# differs
  DIAGNOSTICS <id>  the diagnostic stream differs
  ADDED <id>        only the new capture has the case (a golden the change adds)
  REMOVED <id>      only the baseline has it
  === ARCH-ORACLE: IDENTICAL — N cases ...   or   === ARCH-ORACLE: DIFFERENT — k of N cases ...
Exit status: 0 identical, 1 different, 2 usage error or a capture that could not be made (never "different").
"""
from __future__ import annotations

import argparse
import difflib
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import capture_oracle  # noqa: E402  (the sibling script: one capture mechanism, never a second)

DIFF_LINES_PER_CASE = 80
MAIN = "refs/remotes/origin/main"


def load(path: Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schema") != capture_oracle.SCHEMA:
        print(f"compare_oracle: {path} is schema {data.get('schema')}, expected {capture_oracle.SCHEMA}",
              file=sys.stderr)
        sys.exit(2)   # a usage error, never "different"
    return data


def resolve_base(rev: str | None) -> str:
    """The full sha of the commit to compare against: `rev`, or the merge base of HEAD and origin/main."""
    args = ["rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}"] if rev else ["merge-base", "HEAD", MAIN]
    r = subprocess.run(["git", "-C", str(capture_oracle.REPO), *args], capture_output=True, text=True)
    if r.returncode != 0 or not r.stdout.strip():
        print(f"compare_oracle: cannot resolve the base ({rev or f'git merge-base HEAD {MAIN}'}): "
              f"{r.stderr.strip() or 'no such commit'}; pass --base <commit>", file=sys.stderr)
        sys.exit(2)
    return r.stdout.strip()


def capture_commit(commit: str) -> Path:
    """The manifest of `commit`'s capture: the kept one under TestResults/arch-oracle/<commit12>/ when it is that
    commit's clean capture, else a new one, made by the commit's OWN capture_oracle.py in a detached worktree of it
    (its host, its population, its row sources) and kept there with its blobs. Exits 2 when no capture can be made."""
    target = capture_oracle.RESULTS / commit[:12]
    kept = target / "manifest.json"
    if kept.is_file():
        data = load(kept)
        if data.get("commit") == commit and data.get("dirty") is False:
            print(f"compare_oracle: the baseline is the kept capture of {commit[:12]} ({kept.parent.as_posix()})")
            return kept
    print(f"compare_oracle: capturing the baseline {commit[:12]} in a detached worktree ...", flush=True)
    with tempfile.TemporaryDirectory(prefix="arch-oracle-baseline-", ignore_cleanup_errors=True) as scratch:
        tree = Path(scratch) / "tree"
        capture_oracle.git("worktree", "add", "--detach", str(tree), commit)
        try:
            r = subprocess.run([sys.executable, str(tree / "scripts" / "arch" / "capture_oracle.py")], cwd=tree,
                               env=capture_oracle.oracle_environment())
            captured = tree / "TestResults" / "arch-oracle" / commit[:12]
            if r.returncode != 0 or not (captured / "manifest.json").is_file():
                print(f"compare_oracle: capturing the baseline {commit[:12]} failed (exit {r.returncode}); nothing to "
                      "compare against", file=sys.stderr)
                sys.exit(2)
            if target.exists():
                shutil.rmtree(target)
            shutil.copytree(captured, target)
        finally:
            # No --force: the tree holds only ignored build and capture outputs, so a refusal means something else
            # wrote into it, and that is reported (the scratch directory itself is still cleaned up below).
            if subprocess.run(["git", "-C", str(capture_oracle.REPO), "worktree", "remove", str(tree)]).returncode:
                print(f"    (git refused to remove the baseline worktree {tree}; run `git worktree prune`)")
    return kept


def show_diff(case_id: str, suffix: str, old_dir: Path, new_dir: Path) -> None:
    def read(d: Path) -> list[str]:
        f = d / (case_id + suffix)
        return f.read_text(encoding="utf-8").splitlines() if f.exists() else []

    diff = list(difflib.unified_diff(read(old_dir), read(new_dir), "baseline/" + case_id + suffix,
                                     "new/" + case_id + suffix, lineterm="", n=2))
    if not diff:
        print("    (the two blobs are identical: a blob directory is stale for its manifest)")
    for line in diff[:DIFF_LINES_PER_CASE]:
        print("    " + line)
    if len(diff) > DIFF_LINES_PER_CASE:
        print(f"    ... {len(diff) - DIFF_LINES_PER_CASE} more diff lines")


def compare(baseline_path: Path, new_path: Path, show: bool) -> int:
    old, new = load(baseline_path), load(new_path)
    old_cases, new_cases = old["cases"], new["cases"]
    if old.get("platform") != new.get("platform"):
        print(f"NOTE baseline captured on {old.get('platform')}, new on {new.get('platform')}: a diagnostic naming a COPY "
              "text found by a case-insensitive probe differs by platform (DESIGN-architecture-review.md §4.1); "
              "compare captures of one platform")
    old_dir, new_dir = baseline_path.parent, new_path.parent   # a capture's blobs sit beside its manifest
    differing = 0
    for case_id in sorted(old_cases.keys() | new_cases.keys()):
        if case_id not in new_cases:
            print(f"REMOVED {case_id}")
        elif case_id not in old_cases:
            print(f"ADDED {case_id}")
        else:
            (old_cs, old_diag), (new_cs, new_diag) = old_cases[case_id], new_cases[case_id]
            if old_cs == new_cs and old_diag == new_diag:
                continue
            if old_cs != new_cs:
                print(f"CSHARP {case_id}")
                if show:
                    show_diff(case_id, ".g.cs", old_dir, new_dir)
            if old_diag != new_diag:
                print(f"DIAGNOSTICS {case_id}")
                if show:
                    show_diff(case_id, ".diag.txt", old_dir, new_dir)
        differing += 1
    total = len(old_cases.keys() | new_cases.keys())
    sides = f"baseline {old['commit'][:12]}{'-dirty' if old['dirty'] else ''}, " \
            f"new {new['commit'][:12]}{'-dirty' if new['dirty'] else ''}"
    if differing:
        print(f"=== ARCH-ORACLE: DIFFERENT — {differing} of {total} cases differ ({sides}) ===")
        return 1
    print(f"=== ARCH-ORACLE: IDENTICAL — {total} cases ({sides}) ===")
    return 0


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")   # the verdict line carries an em dash
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("baseline", nargs="?", type=Path, help="an existing capture's manifest.json (instead of --base)")
    parser.add_argument("new", nargs="?", type=Path, help="default: capture the current tree now")
    parser.add_argument("--base", help="the commit to compare against (default: the merge base with origin/main)")
    parser.add_argument("--diff", action="store_true", help="print a unified diff of each differing case")
    parser.add_argument("--no-build", action="store_true", help="capture with the already-built host")
    parser.add_argument("--jobs", type=int, help="parallel compiles for the capture")
    args = parser.parse_args()
    if args.baseline and args.base:
        parser.error("name the baseline once: a capture's manifest or --base, not both")
    for p in (args.baseline, args.new):
        if p is not None and (p.name != "manifest.json" or not p.is_file()):
            print(f"compare_oracle: {p} is not a capture's manifest.json (its blobs sit beside it)", file=sys.stderr)
            return 2
    try:
        baseline = args.baseline or capture_commit(resolve_base(args.base))
        new = args.new or capture_oracle.capture(build=not args.no_build, jobs=args.jobs)
    except (SystemExit, subprocess.CalledProcessError) as e:
        # capture_oracle stops with sys.exit(<message>), whose status is 1, and 1 means DIFFERENT here: a capture that
        # could not be made is exit 2, never a verdict.
        if isinstance(e, SystemExit) and e.code in (0, 2, None):
            raise
        print(f"compare_oracle: no capture to compare ({e}); NOT RUN", file=sys.stderr)
        return 2
    return compare(baseline, new, args.diff)


if __name__ == "__main__":
    sys.exit(main())
