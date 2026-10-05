#!/usr/bin/env python3
"""Self-test for ledger_state.py: owed until the current stamp is marked published, owed again after an input-touching
commit, and NOT owed after a commit that touches no input (the stamp rule is gen_ledger's, never a copy).
Run: python scripts/orchestrator/test_ledger_state.py   (a fabricated repo in a temp directory)."""
import json
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "spec"))
import gen_ledger  # noqa: E402

TMP = pathlib.Path(tempfile.mkdtemp(prefix="ledger-state-test-"))
REPO, COORD = TMP / "repo", TMP / "coord"
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def git(*a):
    subprocess.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", *a], cwd=REPO, check=True, capture_output=True)


def run(cmd):
    r = subprocess.run([sys.executable, str(HERE / "ledger_state.py"), cmd, "--repo", str(REPO), "--coord", str(COORD), "--json"],
                       capture_output=True, text=True)
    check(f"exit of {cmd}", r.returncode, 0)
    return json.loads(r.stdout.splitlines()[-1]) if cmd == "owed" else r.stdout


REPO.mkdir(parents=True)
git("init", "-q", "-b", "main")
input_file = REPO / "kb" / "Work" / "PB1.md"                     # kb/Work is one of gen_ledger.STAMP_PATHS
check("kb/Work is an input of the page", "kb/Work" in gen_ledger.STAMP_PATHS, True)
input_file.parent.mkdir(parents=True)
input_file.write_text("a", encoding="utf-8")
git("add", "."); git("commit", "-q", "-m", "input")

s = run("owed")
check("never published is owed", (s["state"], s["published_stamp"]), ("owed", ""))
run("mark-published")
check("current after marking", run("owed")["state"], "current")

(REPO / "unrelated.txt").write_text("x", encoding="utf-8")        # touches no input
git("add", "."); git("commit", "-q", "-m", "unrelated")
check("an unrelated commit does not make it owed", run("owed")["state"], "current")

input_file.write_text("b", encoding="utf-8")                     # touches an input
git("add", "."); git("commit", "-q", "-m", "input again")
s = run("owed")
check("an input-touching commit makes it owed", s["state"], "owed")
check("the published stamp is the old one", s["published_stamp"] != s["stamp"], True)

if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("ledger_state self-test: all checks OK")
