#!/usr/bin/env python3
"""sweep_s2.py — slice S2's citation sweep (kb/Work PB3177): rewrite every reference to a moved program.

    python conformance-suite/harness/sweep_s2.py <map.json> [--dry-run]

⛔ A ONE-TIME TOOL, deleted with convert_s2.py before the move lands. `<map.json>` is convert_s2.py's `--map` output
(old `<edition|negative>/<name>` -> new suite path). In every tracked text file outside the history (DEVLOG/,
adjudication/, a kb/Work note that is no longer open) and outside the suite itself and the converters, it rewrites
  tests/conformance/<dir>/<name>[.cob|.out|.err]  ->  conformance-suite/<tree>/<folder>/<id>[.cob|.out]
  conformance:<dir>/<name>                         ->  conformance:<tree>/<folder>/<id>
and prints every `tests/conformance` mention it could not map (a glob, a directory, a copybook), for a hand edit.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
PATH_REF = re.compile(r"tests/conformance/(85|2002|2014|2023|negative)/([A-Za-z0-9_-]+)(\.(?:cob|out|err))?(?![A-Za-z0-9_.-])")
INV_REF = re.compile(r"conformance:(85|2002|2014|2023|negative)/([A-Za-z0-9_-]+)(?![A-Za-z0-9_-])")
SKIP_PREFIXES = ("DEVLOG/", "adjudication/", "conformance-suite/", "tests/version-matrix/traceability-inventory.json",
                 "specs/", "evidence/")
OPEN = re.compile(r"(?m)^status:\s*(open|in-progress|blocked)\s*$")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("map", type=Path)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    mapping = json.loads(args.map.read_text(encoding="utf-8"))
    files = subprocess.run(["git", "ls-files"], cwd=REPO, capture_output=True, text=True, check=True).stdout.split("\n")
    changed = 0
    left: list[str] = []
    for rel in files:
        if not rel or rel.startswith(SKIP_PREFIXES):
            continue
        path = REPO / rel
        if path.suffix.lower() not in (".md", ".cs", ".py", ".ps1", ".sh", ".yml", ".yaml", ".json", ".txt", ".g4",
                                       ".cob", ".base", ".js"):
            continue
        try:
            data = path.read_bytes()
            text = data.decode("utf-8")
        except (UnicodeDecodeError, OSError):
            continue
        if rel.startswith("kb/") and (not rel.startswith("kb/Work/") or not OPEN.search(text)):
            continue

        def path_ref(m: re.Match) -> str:
            hit = mapping.get(f"{m.group(1)}/{m.group(2)}")
            if hit is None:
                return m.group(0)
            ext = m.group(3) or ""
            return "conformance-suite/" + hit["path"][:-len(".cob")] + (".out" if ext == ".out" else ext and ".cob")

        def inv_ref(m: re.Match) -> str:
            hit = mapping.get(f"{m.group(1)}/{m.group(2)}")
            return f"conformance:{hit['path'][:-len('.cob')]}" if hit else m.group(0)

        new = INV_REF.sub(inv_ref, PATH_REF.sub(path_ref, text))
        if new != text:
            changed += 1
            if not args.dry_run:
                path.write_bytes(new.encode("utf-8"))
        for n, line in enumerate(new.split("\n"), start=1):
            if "tests/conformance" in line or re.search(r"conformance:(85|2002|2014|2023|negative)/", line):
                left.append(f"{rel}:{n}: {line.strip()[:160]}")
    for line in left:
        print("  LEFT " + line)
    print(f"=== SWEEP S2: {changed} file(s) rewritten, {len(left)} mention(s) left for a hand edit"
          f"{' (dry run)' if args.dry_run else ''} ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
