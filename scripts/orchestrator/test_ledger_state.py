#!/usr/bin/env python3
"""Self-test for ledger_state.py: owed until the current stamp is marked published, owed again after an input-touching
commit, and NOT owed after a commit that touches no input (the stamp rule is gen_ledger's, never a copy); and PER ACCOUNT
(kb/Work PB2481): each account has its own artifact URL and stamp, the first mark needs --url, a legacy record anchors
no account; and against MAIN (kb/Work PB2987): a landing pushed from another clone makes the next check owed while the
checkout the supervisor passes is still behind, mark-published refuses a page rendered from that stale checkout, and a
fast-forward lets it mark.
Run: python scripts/orchestrator/test_ledger_state.py   (a fabricated repo and its bare origin in a temp directory)."""
import json
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "spec"))
import gen_ledger  # noqa: E402

TMP = pathlib.Path(tempfile.mkdtemp(prefix="ledger-state-test-"))
REPO, COORD, ORIGIN, LANDER = TMP / "repo", TMP / "coord", TMP / "origin.git", TMP / "lander"
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def git(*a, cwd=REPO):
    subprocess.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", *a], cwd=cwd, check=True, capture_output=True)


def land(msg, cwd=REPO):
    """Commit the working tree and push it to main, as a landing does."""
    git("add", ".", cwd=cwd)
    git("commit", "-q", "-m", msg, cwd=cwd)
    git("push", "-q", "origin", "main", cwd=cwd)


def run(cmd, *extra, acct="account1", want=0):
    r = subprocess.run([sys.executable, str(HERE / "ledger_state.py"), cmd, "--repo", str(REPO), "--coord", str(COORD),
                        "--json", "--account", acct, *extra], capture_output=True, text=True)
    check(f"exit of {cmd} {' '.join(extra)} ({acct})", r.returncode, want)
    return json.loads(r.stdout.splitlines()[-1]) if cmd == "owed" and want == 0 else r.stdout + r.stderr


URL1, URL2 = "https://claude.ai/code/artifact/one", "https://claude.ai/artifact/two"
COORD.mkdir(parents=True)
(COORD / "ledger-published.json").write_text(json.dumps({"stamp": "legacy", "published_at": "2026-10-07"}), encoding="utf-8")


REPO.mkdir(parents=True)
git("init", "-q", "--bare", "-b", "main", str(ORIGIN), cwd=TMP)
git("init", "-q", "-b", "main")
git("remote", "add", "origin", str(ORIGIN))
input_file = REPO / "kb" / "Work" / "PB1.md"                     # kb/Work is one of gen_ledger.STAMP_PATHS
check("kb/Work is an input of the page", "kb/Work" in gen_ledger.STAMP_PATHS, True)
input_file.parent.mkdir(parents=True)
input_file.write_text("a", encoding="utf-8")
land("input")

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
land("unrelated")
check("an unrelated commit does not make it owed", run("owed")["state"], "current")

input_file.write_text("b", encoding="utf-8")                     # touches an input
land("input again")
s = run("owed")
check("an input-touching commit makes it owed", s["state"], "owed")
check("the published stamp is the old one", s["published_stamp"] != s["stamp"], True)
run("mark-published")
check("a later mark keeps the recorded url", (run("owed")["state"], run("owed")["url"]), ("current", URL1))

# the fix lane's population is an input of the page's WHAT REMAINS block (kb/Work PB2912): changing it is owed
(REPO / ".agent-fleet.json").write_text('{"kind": "defect"}', encoding="utf-8")
land("the fix lane's population")
check("a commit to .agent-fleet.json makes it owed", run("owed")["state"], "owed")
run("mark-published")
# the held architecture review's evidence is no longer on the page: its folders are not inputs
(REPO / "docs" / "rearchitecture" / "evidence" / "arch-census").mkdir(parents=True)
(REPO / "docs" / "rearchitecture" / "evidence" / "arch-census" / "x.json").write_text("{}", encoding="utf-8")
land("a census record")
check("a census record no longer makes it owed", run("owed")["state"], "current")

# kb/Work PB2987: a landing pushed from ANOTHER clone (a lander's worktree) while the checkout stays behind
git("clone", "-q", str(ORIGIN), str(LANDER), cwd=TMP)
(LANDER / "kb" / "Work" / "PB2.md").write_text("landed elsewhere", encoding="utf-8")
land("a train landed from a lander's worktree", cwd=LANDER)
s = run("owed")
check("a landing pushed from another clone is owed (main's stamp, fetched)", s["state"], "owed")
check("the stale checkout is named", (s["checkout_current"], s["checkout_stamp"] == s["published_stamp"]), (False, True))
check("mark-published refuses a page rendered from the stale checkout", "merge --ff-only" in run("mark-published", want=2), True)
check("still owed after the refusal", run("owed")["state"], "owed")
git("merge", "-q", "--ff-only", "origin/main")
run("mark-published")
check("current once the checkout is fast-forwarded and marked", (run("owed")["state"], run("owed")["checkout_current"]),
      ("current", True))
check("a main ref that names nothing is refused, never read as current",
      "names no commit" in run("owed", "--main-ref", "origin/no-such-branch", want=2), True)

if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("ledger_state self-test: all checks OK")
