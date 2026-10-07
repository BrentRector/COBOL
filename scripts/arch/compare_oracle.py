#!/usr/bin/env python3
"""compare_oracle.py — the behavior-neutrality comparison every restructuring wave runs (kb/Work PB2116;
docs/rearchitecture/DESIGN-architecture-review.md §4).

Compares two oracle manifests CASE BY CASE, never by totals: every case whose emitted C# or diagnostic stream
differs is printed with its id (which names the program, or construct, and the edition), and so is every case one
manifest has and the other lacks. Exit 0 only when the two are identical; a wave's DEVLOG entry either shows the
IDENTICAL line or explains every difference (a renamed runtime helper is the only expected class).

USAGE
  python scripts/arch/compare_oracle.py [<baseline-manifest> [<new-manifest>]] [--diff] [--no-build] [--jobs N]

  <baseline-manifest>  default: THE recorded baseline, the one docs/rearchitecture/evidence/arch-oracle/*.manifest.json
  <new-manifest>       default: capture the current tree now (scripts/arch/capture_oracle.py, into TestResults/)
  --diff               print a unified diff of each differing case from the two captures' blob directories (the
                       directory a TestResults manifest sits in; for a recorded baseline,
                       TestResults/arch-oracle/<commit12>/ when that commit was captured on this machine)

OUTPUT
  CSHARP <id>       the emitted C# differs
  DIAGNOSTICS <id>  the diagnostic stream differs
  ADDED <id>        only the new manifest has the case (a golden landed since the baseline: re-record it)
  REMOVED <id>      only the baseline has it
  === ARCH-ORACLE: IDENTICAL — N cases ...   or   === ARCH-ORACLE: DIFFERENT — k of N cases ...
Exit status: 0 identical, 1 different, 2 usage error.
"""
from __future__ import annotations

import argparse
import difflib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import capture_oracle  # noqa: E402  (the sibling script: one capture mechanism, never a second)

DIFF_LINES_PER_CASE = 80


def load(path: Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schema") != capture_oracle.SCHEMA:
        print(f"compare_oracle: {path} is schema {data.get('schema')}, expected {capture_oracle.SCHEMA}",
              file=sys.stderr)
        sys.exit(2)   # a usage error, never "different"
    return data


def recorded_baseline() -> Path:
    found = sorted(capture_oracle.RECORDED.glob("*.manifest.json"))
    if len(found) != 1:
        print(f"compare_oracle: expected exactly one recorded baseline in {capture_oracle.RECORDED}, found {len(found)}",
              file=sys.stderr)
        sys.exit(2)
    return found[0]


def blob_dir(manifest_path: Path, data: dict) -> Path | None:
    if manifest_path.name == "manifest.json":
        return manifest_path.parent
    candidate = capture_oracle.RESULTS / data["commit"][:12]
    return candidate if candidate.is_dir() else None


def show_diff(case_id: str, suffix: str, old_dir: Path | None, new_dir: Path | None) -> None:
    if old_dir is None or new_dir is None:
        print("    (no local blobs for one side; capture that commit to diff it)")
        return

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
    old_dir, new_dir = (blob_dir(baseline_path, old), blob_dir(new_path, new)) if show else (None, None)
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
    parser.add_argument("baseline", nargs="?", type=Path, help="default: the recorded baseline")
    parser.add_argument("new", nargs="?", type=Path, help="default: capture the current tree now")
    parser.add_argument("--diff", action="store_true", help="print a unified diff of each differing case")
    parser.add_argument("--no-build", action="store_true", help="capture with the already-built host")
    parser.add_argument("--jobs", type=int, help="parallel compiles for the capture")
    args = parser.parse_args()
    baseline = args.baseline or recorded_baseline()
    new = args.new or capture_oracle.capture(build=not args.no_build, jobs=args.jobs)
    for p in (baseline, new):
        if not p.is_file():
            print(f"compare_oracle: no such manifest: {p}", file=sys.stderr)
            return 2
    return compare(baseline, new, args.diff)


if __name__ == "__main__":
    sys.exit(main())
