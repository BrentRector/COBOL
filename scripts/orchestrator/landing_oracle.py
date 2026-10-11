#!/usr/bin/env python3
"""landing_oracle.py — A LANDING CARRIES AN ARCH-ORACLE BASELINE THAT DESCRIBES ITS OWN TREE, checked where every
landing passes (scripts/push-main.sh, exit 7, before a CI run is spent; kb/Work PB2885).

    python scripts/orchestrator/landing_oracle.py [--rev HEAD]
    python scripts/orchestrator/landing_oracle.py --self-test

WHY. The behavior-neutrality oracle (scripts/arch/capture_oracle.py, DESIGN-architecture-review.md §4) is compared
against ONE committed baseline, docs/rearchitecture/evidence/arch-oracle/<commit12>.manifest.json. Re-recording it
after a train was a prose step (lander-train-brief 3c, MANDATORY-PRACTICES L11), and train 1049, an operator
hand-landing, skipped it: six new conformance cases reached main with the baseline still at train 1047's commit, and
train 1050's compare_oracle.py reported DIFFERENT for cases no change of its own touched. This makes the step a
refusal at the one place every landing passes, the way landing_devlog.py made the DEVLOG entry one (kb/Work PB2605).

THE RULE, on the landing's own trees (`git show <rev>:…`, never a working tree or an index):
  1. <rev> carries exactly ONE recorded baseline, of a clean tree, that records its `inputs` (a manifest recorded
     before PB2885 has none and must be re-recorded);
  2. the commit C it names is an ANCESTOR of <rev> (or <rev> itself): a baseline recorded and then rebased away names
     a commit that never reaches main, and a later `compare_oracle.py --diff` could not capture it;
  3. no path under any of its `inputs` differs between C and <rev>. `inputs` is DERIVED when the baseline is recorded
     (capture_oracle.py: the oracle host's MSBuild closure plus the data roots its row sources read), so the set of
     "paths that change a compiled program" is never a hand list here.
A landing that changes no input passes with the baseline it inherited (a docs-only operator landing records nothing);
one that changes an input must record a baseline at a commit after its last input change.

Exit 0: the baseline describes the landing. Exit 1: REFUSED (each reason printed, with the commands to run).
Exit 2: the range or the manifest could not be read (NOT RUN, never a pass).
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import shutil
import subprocess
import sys
import tempfile

RECORDED = "docs/rearchitecture/evidence/arch-oracle"
SHOW_PATHS = 12
RECORD = ("python scripts/arch/compare_oracle.py   (explain every difference by cluster in the DEVLOG entry), then "
          "python scripts/arch/capture_oracle.py --record   on the committed train, commit the new manifest, and run "
          "push-main.sh again (lander-train-brief step 3c, MANDATORY-PRACTICES L11)")


class RangeError(Exception):
    """The landing range or its manifest could not be read: the check could not run (never a pass)."""


def _git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace")


def _commit(repo: pathlib.Path, rev: str) -> str:
    r = _git(repo, "rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}")
    if r.returncode != 0:
        raise RangeError(f"{rev} is not a commit")
    return r.stdout.strip()


def baseline_at(repo: pathlib.Path, rev: str) -> tuple[list[str], dict | None]:
    """(the recorded manifest paths in <rev>'s tree, the parsed manifest when there is exactly one)."""
    r = _git(repo, "ls-tree", "--name-only", f"{rev}:{RECORDED}")
    names = [n for n in r.stdout.split() if n.endswith(".manifest.json")] if r.returncode == 0 else []
    if len(names) != 1:
        return names, None
    shown = _git(repo, "show", f"{rev}:{RECORDED}/{names[0]}")
    if shown.returncode != 0:
        raise RangeError(f"could not read {RECORDED}/{names[0]} at {rev}")
    try:
        return names, json.loads(shown.stdout)
    except json.JSONDecodeError as e:
        raise RangeError(f"{RECORDED}/{names[0]} at {rev} is not JSON: {e}") from e


def under(path: str, inputs: list[str]) -> bool:
    return any(path == i or path.startswith(i + "/") for i in inputs)


def check(repo: pathlib.Path, rev: str) -> list[str]:
    """The refusal reasons for a landing whose head is <rev> (empty: it passes). Raises RangeError when unreadable."""
    head = _commit(repo, rev)
    names, manifest = baseline_at(repo, head)
    if manifest is None:
        return [f"the landing's tree must carry exactly ONE recorded arch-oracle baseline under {RECORDED}/, and it "
                f"carries {len(names)} ({', '.join(names) or 'none'}): {RECORD}"]
    name = names[0]
    commit, inputs = manifest.get("commit"), manifest.get("inputs")
    if not isinstance(commit, str) or name != f"{commit[:12]}.manifest.json":
        return [f"{name} does not name the commit it records ({commit!r}); re-record it: {RECORD}"]
    if manifest.get("dirty") is not False:
        return [f"{name} was recorded from a dirty tree (no commit reproduces it): {RECORD}"]
    if not isinstance(inputs, list) or not inputs or not all(isinstance(i, str) and i for i in inputs):
        return [f"{name} records no `inputs` (it predates kb/Work PB2885), so nothing can say whether it describes "
                f"this landing: {RECORD}"]
    if _git(repo, "cat-file", "-e", f"{commit}^{{commit}}").returncode != 0 or \
            _git(repo, "merge-base", "--is-ancestor", commit, head).returncode != 0:
        return [f"{name} names {commit[:12]}, which is not in this landing's history (recorded before a rebase?); "
                f"re-record it on the final train: {RECORD}"]
    diff = _git(repo, "diff", "--name-only", "--no-renames", commit, head)
    if diff.returncode != 0:
        raise RangeError(f"git diff {commit[:12]} {head[:12]} failed: {diff.stderr.strip()}")
    stale = [p for p in diff.stdout.splitlines() if p and under(p, inputs)]
    if not stale:
        return []
    shown = "\n      ".join(stale[:SHOW_PATHS]) + (f"\n      … and {len(stale) - SHOW_PATHS} more"
                                                   if len(stale) > SHOW_PATHS else "")
    return [f"the arch-oracle baseline {name} is BEHIND the landing: {len(stale)} path(s) under its inputs changed "
            f"after {commit[:12]}:\n      {shown}\n    run: {RECORD}"]


def run(repo: pathlib.Path, rev: str) -> int:
    try:
        reasons = check(repo, rev)
    except RangeError as e:
        print(f"⛔ landing oracle: {e}")
        print("=== LANDING ORACLE: NOT RUN (the range or its baseline could not be read) ===")
        return 2
    for r in reasons:
        print(f"⛔ landing oracle: {r}")
    if reasons:
        print(f"=== LANDING ORACLE: REFUSED ({len(reasons)} reason(s)) ===")
        return 1
    _, manifest = baseline_at(repo, _commit(repo, rev))
    print(f"=== LANDING ORACLE: PASS (baseline {manifest['commit'][:12]}; no input changed since) ===")
    return 0


# ── self-test: real scratch repositories, real ranges ───────────────────────────────────────────────────────────
def self_test() -> int:
    for name in subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                               text=True).stdout.split():
        os.environ.pop(name, None)       # hermetic: nothing reaches the caller's repository
    root = pathlib.Path(tempfile.mkdtemp(prefix="landing-oracle-"))
    repo = root / "repo"
    repo.mkdir()
    env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t",
               GIT_COMMITTER_EMAIL="t@t")
    results: list[tuple[str, bool, str]] = []

    def sh(*args: str) -> str:
        return subprocess.run(["git", "-C", str(repo), *args], check=True, capture_output=True, text=True,
                              env=env).stdout.strip()

    def write(rel: str, text: str) -> None:
        (repo / rel).parent.mkdir(parents=True, exist_ok=True)
        (repo / rel).write_text(text, encoding="utf-8")

    def commit(edits: dict[str, str | None], msg: str) -> str:
        for rel, text in edits.items():
            if text is None:
                sh("rm", "-q", rel)
            else:
                write(rel, text)
        sh("add", "-A")
        sh("commit", "-qm", msg)
        return sh("rev-parse", "HEAD")

    def manifest(at: str, *, inputs: list[str] | None = None, dirty: bool = False, drop: bool = True) -> dict:
        """A recorded baseline naming `at`, replacing every other one (capture_oracle.record's rule)."""
        edits: dict[str, str | None] = {}
        if drop:
            for old in (repo / RECORDED).glob("*.manifest.json") if (repo / RECORDED).is_dir() else []:
                edits[f"{RECORDED}/{old.name}"] = None
        body = {"schema": 1, "commit": at, "dirty": dirty, "platform": "t", "cases": {}}
        if inputs is not None:
            body["inputs"] = inputs
        edits[f"{RECORDED}/{at[:12]}.manifest.json"] = json.dumps(body)
        return edits

    def expect(label: str, want: int, *, needle: str = "") -> None:
        try:
            reasons = check(repo, "HEAD")
            got, text = (1 if reasons else 0), "; ".join(reasons)
        except RangeError as e:
            got, text = 2, str(e)
        ok = got == want and (not needle or needle in text)
        results.append((label, ok, f"exit {got}: {text or 'PASS'}"))

    inputs = ["src/Compiler", "tests/conformance"]
    sh("init", "-q", "-b", "main")
    c0 = commit({"src/Compiler/a.cs": "1", "tests/conformance/x.cob": "1", "docs/d.md": "1", "src/CompilerX/b.cs": "1"},
                "base")
    base = commit(manifest(c0, inputs=inputs), "record the baseline")
    expect("the baseline as recorded -> PASS", 0)
    commit({"docs/d.md": "2", "DEVLOG.md": "entry"}, "a docs-only landing")
    expect("a landing that changes no input keeps its inherited baseline -> PASS", 0)
    commit({"src/CompilerX/b.cs": "2"}, "a sibling directory whose name only STARTS with an input's")
    expect("a path that merely shares an input's prefix (src/CompilerX vs src/Compiler) is not an input -> PASS", 0)
    commit({"tests/conformance/y.cob": "new golden"}, "the train-1049 shape: a new golden, no re-record")
    expect("an input changed after the baseline's commit (PB2885's train 1049) -> REFUSED, naming the path", 1,
           needle="tests/conformance/y.cob")
    expect("... and the refusal prints the commands to run", 1, needle="capture_oracle.py --record")
    fresh = sh("rev-parse", "HEAD")
    commit(manifest(fresh, inputs=inputs), "re-record on the train")
    expect("re-recorded at a commit after the last input change -> PASS", 0)
    commit({"src/Compiler/a.cs": "3"}, "a change after the record")
    expect("an input changed AFTER the re-record -> REFUSED", 1, needle="src/Compiler/a.cs")
    head = sh("rev-parse", "HEAD")
    sh("checkout", "-q", "--detach", base)
    side = commit({"src/Compiler/a.cs": "3"}, "the same change on another line of history")
    sh("checkout", "-q", "-B", "main", head)
    commit(manifest(side, inputs=inputs), "a baseline naming a commit outside this history")
    expect("a baseline naming a commit that is not an ancestor (recorded, then rebased away) -> REFUSED", 1,
           needle="not in this landing's history")
    now = sh("rev-parse", "HEAD")
    commit(manifest(now, inputs=None), "a baseline without inputs")
    expect("a baseline that records no inputs (pre-PB2885) -> REFUSED", 1, needle="records no `inputs`")
    now = sh("rev-parse", "HEAD")
    commit(manifest(now, inputs=inputs, dirty=True), "a dirty baseline")
    expect("a baseline recorded from a dirty tree -> REFUSED", 1, needle="dirty")
    now = sh("rev-parse", "HEAD")
    commit(manifest(now, inputs=inputs) | manifest(c0, inputs=inputs, drop=False), "two baselines")
    expect("two recorded baselines -> REFUSED", 1, needle="exactly ONE")
    commit({f"{RECORDED}/{p.name}": None for p in (repo / RECORDED).glob("*.manifest.json")}, "no baseline")
    expect("no recorded baseline -> REFUSED", 1, needle="carries 0")
    try:
        check(repo, "no-such-rev")
        results.append(("an unreadable revision -> NOT RUN (exit 2), never a pass", False, "no RangeError"))
    except RangeError:
        results.append(("an unreadable revision -> NOT RUN (exit 2), never a pass", True, ""))

    shutil.rmtree(root, ignore_errors=True)
    bad = [r for r in results if not r[1]]
    for label, ok, got in results:
        print(f"  {'ok ' if ok else 'BAD'} {label}" + ("" if ok else f": {got}"))
    print(f"=== LANDING ORACLE SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--rev", default="HEAD", help="the landing's head (the check reads the baseline's own commit, so "
                    "it needs no base)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    top = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    if top.returncode != 0:
        print("=== LANDING ORACLE: NOT RUN (not inside a git work tree) ===")
        return 2
    return run(pathlib.Path(top.stdout.strip()), a.rev)


if __name__ == "__main__":
    sys.exit(main())
