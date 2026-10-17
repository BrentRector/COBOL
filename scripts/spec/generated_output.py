#!/usr/bin/env python3
"""generated_output.py — the ONE way a generator clears the directory it writes into before writing it again.

    python scripts/spec/generated_output.py --self-test

A generator that takes `--out DIR` writes into a directory its USER named, so it may never delete that directory
wholesale: `--out .` would delete the working tree, and `--out` naming a directory of the user's own would delete
their files (kb/Work PB3176, branch review R1-1: `conformance-suite/harness/generate.py --out` reached an
unconditional `shutil.rmtree(out)`). `prepare(out, owned)` removes only the entries a previous run of the generator
wrote there, the ones `owned` accepts by name, and refuses a directory that holds anything else, deleting nothing.

Callers: `conformance-suite/harness/generate.py` (the suite manifests and the completeness report) and
`scripts/spec/gen_conformance_notes.py` (the vault's burn-down view).
"""
from __future__ import annotations

import argparse
import shutil
import sys
import tempfile
from pathlib import Path
from typing import Callable

#: How many of the foreign entries a refusal names before it counts the rest.
_SHOWN = 5


def prepare(out: Path, owned: Callable[[str], bool]) -> str | None:
    """Make `out` ready for a fresh write: remove each top-level entry whose name `owned` accepts (what a previous run
    wrote) and nothing else. When `out` is not a directory, or holds an entry `owned` rejects, nothing is removed and
    the reason is returned for the caller to refuse; None when `out` is ready (absent, empty, or cleared)."""
    if not out.exists():
        return None
    if not out.is_dir():
        return "it is not a directory"
    foreign = sorted(p.name for p in out.iterdir() if not owned(p.name))
    if foreign:
        shown = ", ".join(foreign[:_SHOWN]) + (f" and {len(foreign) - _SHOWN} more" if len(foreign) > _SHOWN else "")
        return f"it holds {shown}, which this generator does not write"
    for entry in out.iterdir():
        if entry.is_dir() and not entry.is_symlink():
            shutil.rmtree(entry)
        else:
            entry.unlink()
    return None


def self_test() -> int:
    rc = 0

    def check(name: str, ok: bool, got: object = None) -> None:
        nonlocal rc
        print(f"  {'ok' if ok else 'FAIL'}: {name}" + ("" if ok else f" — got {got!r}"))
        rc = rc or (0 if ok else 1)

    mine = {"a", "b.json"}.__contains__
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        check("arm: an absent directory is ready, and is not created", prepare(root / "new", mine) is None
              and not (root / "new").exists())
        (root / "empty").mkdir()
        check("arm: an empty directory is ready", prepare(root / "empty", mine) is None)
        prev = root / "prev"
        (prev / "a" / "deep").mkdir(parents=True)
        (prev / "a" / "deep" / "x.json").write_text("{}", encoding="utf-8")
        (prev / "b.json").write_text("{}", encoding="utf-8")
        check("arm: a previous run's output is removed whole", prepare(prev, mine) is None
              and list(prev.iterdir()) == [], list(prev.iterdir()))
        user = root / "user"
        (user / "a").mkdir(parents=True)
        (user / "precious").mkdir()
        (user / "precious" / "notes.txt").write_text("keep", encoding="utf-8")
        (user / "todo.txt").write_text("keep", encoding="utf-8")
        why = prepare(user, mine)
        check("arm: a directory holding anything else is refused, naming it, and nothing in it is deleted",
              why == "it holds precious, todo.txt, which this generator does not write"
              and sorted(p.name for p in user.iterdir()) == ["a", "precious", "todo.txt"]
              and (user / "precious" / "notes.txt").is_file(), why)
        check("arm: a path that is a file is refused and kept",
              prepare(user / "todo.txt", mine) == "it is not a directory" and (user / "todo.txt").is_file())
        many = root / "many"
        many.mkdir()
        for i in range(7):
            (many / f"f{i}").write_text("", encoding="utf-8")
        why = prepare(many, mine)
        check("arm: a long foreign list names five and counts the rest",
              why == "it holds f0, f1, f2, f3, f4 and 2 more, which this generator does not write", why)
    print("=== generated_output.py --self-test: " + ("ALL GREEN" if not rc else "FAILED") + " ===")
    return rc


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--self-test", action="store_true", required=True, help="prove every arm on planted directories")
    parser.parse_args()
    sys.exit(self_test())
