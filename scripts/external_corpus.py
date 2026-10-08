#!/usr/bin/env python3
"""external_corpus.py — THE ONE RULE for obtaining the git-ignored GnuCOBOL corpus in a tree (kb/Work PB2611).

    python scripts/external_corpus.py ensure [--repo <tree>]   # fetch when absent; exit 1 on a failed fetch
    python scripts/external_corpus.py --self-test

⚖ The corpus is GPL-3.0, never committed, and lives per WORKTREE (and per clone) in `tests/external/gnucobol/`
(kb/Work PB209, PB277). `scripts/fetch-gnucobol-tests.ps1` is the fetch itself (pinned 3.2, sha256-verified,
extraction staged so a failed fetch never empties the tree, PB897); THIS module is the rule every caller applies
around it:

  1. the corpus is PRESENT when its marker `tests/external/gnucobol/tests/testsuite.src` exists — then nothing runs;
  2. otherwise the fetch script runs in that tree;
  3. a fetch that exits non-zero, or that exits 0 and still leaves no marker, is ONE named red,
     `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`: the population ExternalCorpusPopulationDriftTests and the
     GnuCOBOL differential measure is then unmeasured, and every red they show is attributable to it.

⛔ WHY ONE MODULE. Battery #89 (2026-10-08) read NOT GREEN at a green tree because `scripts/battery.sh` had no fetch at
all while the gate driver had one; the impact-map recorder, the Linux gate and the cloud session hook each carried a
fourth, fifth and sixth spelling of the rule. Every caller now imports `ensure` (python) or runs `ensure` (shell), and
`ExternalCorpusFetchTests` fails when any other script invokes the fetch script directly. CI's two fetch steps are the
one recorded exception: a fresh checkout never holds the corpus, and the step's own exit status is the red.

Exit codes of `ensure`: 0 present or fetched · 1 FETCH FAILED.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]

#: The corpus is present exactly when this exists (what the differential reads and the population drift test lists).
MARKER = Path("tests/external/gnucobol/tests/testsuite.src")
#: The fetch itself, run with cwd = the tree it fills (the script resolves its destination from its own location).
FETCH_SCRIPT = Path("scripts/fetch-gnucobol-tests.ps1")
#: THE red, named the same in every gate that applies the rule.
FETCH_FAILED = "EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED"

#: Runs the fetch command in the tree and returns its exit code. The default spawns it attached to this console; a
#: caller that logs or prioritises its children passes its own.
Runner = Callable[[list[str], Path], int]


def fetch_command(tree: Path) -> list[str]:
    return ["pwsh", "-NoProfile", "-File", str(tree / FETCH_SCRIPT)]


def _attached(cmd: list[str], cwd: Path) -> int:
    return subprocess.run(cmd, cwd=cwd).returncode


def present(tree: Path) -> bool:
    return (tree / MARKER).exists()


def ensure(tree: Path, say: Callable[[str], None] = print, run: Runner = _attached) -> list[str]:
    """Apply the rule to `tree`: fetch the corpus when it is absent. Returns the red reasons ([] or [FETCH_FAILED])."""
    if present(tree):
        return []
    say("=== EXTERNAL CORPUS: absent in this tree — fetching (GPL, git-ignored, never committed) ===")
    try:
        rc: int | str = run(fetch_command(tree), tree)
    except OSError as exc:  # pwsh itself missing: the fetch did not run, which is a failed fetch, never a crash
        rc = f"could not start: {exc}"
    if rc != 0 or not present(tree):
        cause = f"exit {rc}" if rc != 0 else f"exit 0 but no {MARKER.as_posix()}"
        say(f"=== EXTERNAL CORPUS: FETCH FAILED ({cause}; the FETCH FAILED line above names the cause) — the "
            "ExternalCorpusPopulationDriftTests reds and the GnuCOBOL differential's `corpus absent` are ATTRIBUTABLE "
            "TO IT, and the run is RED because it could not measure that population ===")
        return [FETCH_FAILED]
    say("=== EXTERNAL CORPUS: fetched ===")
    return []


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────


def self_test() -> int:
    with tempfile.TemporaryDirectory(prefix="external-corpus-selftest-") as tmp:
        return _self_test(Path(tmp))


def _self_test(root: Path) -> int:
    results: list[bool] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append(ok)
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    def planted(name: str, *, has: bool, rc: int | None = None, creates: bool = False, raises: bool = False):
        tree = root / name
        tree.mkdir()
        if has:
            (tree / MARKER).mkdir(parents=True)
        calls: list[tuple[list[str], Path]] = []
        said: list[str] = []

        def run(cmd: list[str], cwd: Path) -> int:
            calls.append((cmd, cwd))
            if raises:
                raise FileNotFoundError("planted: pwsh is not on PATH")
            if creates:
                (cwd / MARKER).mkdir(parents=True)
            return rc if rc is not None else 0

        return ensure(tree, said.append, run), calls, said, tree

    reds, calls, said, _ = planted("present", has=True)
    arm("a present corpus runs NO fetch and is not red", reds == [] and calls == [] and said == [], f"{reds} {calls}")

    reds, calls, said, tree = planted("fetched", has=False, rc=0, creates=True)
    arm("an absent corpus runs the fetch script once, in that tree, and is not red when it lands",
        reds == [] and len(calls) == 1 and calls[0][1] == tree and calls[0][0][-1] == str(tree / FETCH_SCRIPT),
        f"{reds} {calls}")

    reds, calls, said, _ = planted("exit1", has=False, rc=1)
    arm("a fetch that exits non-zero is the named FETCH FAILED red",
        reds == [FETCH_FAILED] and any("FETCH FAILED (exit 1" in s for s in said), f"{reds} {said}")

    reds, calls, said, _ = planted("exit0-empty", has=False, rc=0, creates=False)
    arm("a fetch that exits 0 but leaves no marker is the named FETCH FAILED red, never a green",
        reds == [FETCH_FAILED] and any("exit 0 but no" in s for s in said), f"{reds} {said}")

    reds, calls, said, _ = planted("no-pwsh", has=False, raises=True)
    arm("a fetch that cannot start (no pwsh) is the named FETCH FAILED red, never a crash",
        reds == [FETCH_FAILED] and any("could not start" in s for s in said), f"{reds} {said}")

    ok = all(results)
    print(f"external_corpus SELF-TEST: {'PASS' if ok else 'FAIL'} ({sum(results)}/{len(results)} arms)")
    return 0 if ok else 1


def main(argv: list[str] | None = None) -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    ap =argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    e = sub.add_parser("ensure", help="fetch the corpus into the tree when it is absent; exit 1 on a failed fetch")
    e.add_argument("--repo", type=Path, default=REPO, help="the tree to fill (default: this script's repository)")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.cmd != "ensure":
        ap.error("name a command: ensure, or --self-test")
    tree = a.repo.resolve()
    was_present = present(tree)
    reds = ensure(tree, lambda s: print(s, flush=True))
    if reds:
        print(reds[0])
        return 1
    how = "present" if was_present else "fetched, now present"
    print(f"=== EXTERNAL CORPUS: {how} at {(tree / MARKER).as_posix()} ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
