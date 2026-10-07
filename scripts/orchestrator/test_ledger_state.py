#!/usr/bin/env python3
"""Self-test for ledger_state.py: owed until the current stamp is marked published, owed again after an input-touching
commit, and NOT owed after a commit that touches no input (the stamp rule is gen_ledger's, never a copy); and PER ACCOUNT
(kb/Work PB2481): each account has its own artifact URL and stamp, the first mark needs --url, a legacy record anchors
no account.
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


def run(cmd, *extra, acct="account1", want=0):
    r = subprocess.run([sys.executable, str(HERE / "ledger_state.py"), cmd, "--repo", str(REPO), "--coord", str(COORD),
                        "--json", "--account", acct, *extra], capture_output=True, text=True)
    check(f"exit of {cmd} {' '.join(extra)} ({acct})", r.returncode, want)
    return json.loads(r.stdout.splitlines()[-1]) if cmd == "owed" and want == 0 else r.stdout + r.stderr


URL1, URL2 = "https://claude.ai/code/artifact/one", "https://claude.ai/artifact/two"
COORD.mkdir(parents=True)
(COORD / "ledger-published.json").write_text(json.dumps({"stamp": "legacy", "published_at": "2026-10-07"}), encoding="utf-8")


REPO.mkdir(parents=True)
git("init", "-q", "-b", "main")
input_file = REPO / "kb" / "Work" / "PB1.md"                     # kb/Work is one of gen_ledger.STAMP_PATHS
check("kb/Work is an input of the page", "kb/Work" in gen_ledger.STAMP_PATHS, True)
input_file.parent.mkdir(parents=True)
input_file.write_text("a", encoding="utf-8")
git("add", "."); git("commit", "-q", "-m", "input")

s = run("owed")
check("never published is owed (a legacy record anchors no account)", (s["state"], s["published_stamp"], s["url"]), ("owed", "", None))
check("the first mark without --url is refused", "--url" in run("mark-published", want=2), True)
check("url before any publish says to start one", "no ledger artifact yet for account1" in run("url"), True)
run("mark-published", "--url", URL1)
check("current after marking", run("owed")["state"], "current")
check("the url is recorded", run("owed")["url"], URL1)
check("url names it", URL1 in run("url"), True)
# account 2: the same stamp is still owed there, with no artifact; marking it leaves account 1's record alone
check("another account is still owed", (run("owed", acct="account2")["state"], run("owed", acct="account2")["url"]), ("owed", None))
run("mark-published", "--url", URL2, acct="account2")
check("account 2 current at its own url", (run("owed", acct="account2")["state"], run("owed", acct="account2")["url"]), ("current", URL2))
check("account 1's record kept", run("owed")["url"], URL1)
saved = json.loads((COORD / "ledger-published.json").read_text(encoding="utf-8"))
check("the file is keyed by account, the legacy record gone", sorted(saved), ["accounts"])

(REPO / "unrelated.txt").write_text("x", encoding="utf-8")        # touches no input
git("add", "."); git("commit", "-q", "-m", "unrelated")
check("an unrelated commit does not make it owed", run("owed")["state"], "current")

input_file.write_text("b", encoding="utf-8")                     # touches an input
git("add", "."); git("commit", "-q", "-m", "input again")
s = run("owed")
check("an input-touching commit makes it owed", s["state"], "owed")
check("the published stamp is the old one", s["published_stamp"] != s["stamp"], True)
run("mark-published")
check("a later mark keeps the recorded url", (run("owed")["state"], run("owed")["url"]), ("current", URL1))

if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("ledger_state self-test: all checks OK")
