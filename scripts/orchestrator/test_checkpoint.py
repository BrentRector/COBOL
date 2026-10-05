#!/usr/bin/env python3
"""Self-test for checkpoint.py: the checkpoint names every worktree's state, and a synthesized handoff is schema-valid.
Run: python scripts/orchestrator/test_checkpoint.py   (a fabricated repo with real worktrees in a temp directory)."""
import json
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
CP = HERE / "checkpoint.py"
SCHEMA = json.loads((HERE / "handoff.schema.json").read_text(encoding="utf-8"))
TMP = pathlib.Path(tempfile.mkdtemp(prefix="checkpoint-test-"))
REPO, COORD = TMP / "repo", TMP / "coord"
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def git(cwd, *a):
    subprocess.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", *a], cwd=cwd, check=True, capture_output=True)


def run(*a):
    r = subprocess.run([sys.executable, str(CP), *a, "--coord", str(COORD), "--repo", str(REPO)], capture_output=True, text=True)
    check(f"exit of {a[0]}", r.returncode, 0)
    return r


REPO.mkdir(parents=True)
git(REPO, "init", "-q", "-b", "main")
(REPO / "a.txt").write_text("a", encoding="utf-8")
git(REPO, "add", "."); git(REPO, "commit", "-q", "-m", "base")
git(REPO, "update-ref", "refs/remotes/origin/main", "HEAD")           # the base the worktrees are measured against
git(REPO, "worktree", "add", "-q", "-b", "committed", str(TMP / "wt-committed"))
git(REPO, "worktree", "add", "-q", "-b", "dirty", str(TMP / "wt-dirty"))
git(REPO, "worktree", "add", "-q", "-b", "clean", str(TMP / "wt-clean"))
(TMP / "wt-committed" / "b.txt").write_text("b", encoding="utf-8")
git(TMP / "wt-committed", "add", "."); git(TMP / "wt-committed", "commit", "-q", "-m", "wip")
(TMP / "wt-committed" / "STATUS.md").write_text("STATUS-AT: abc\nDONE: x\n", encoding="utf-8")
(TMP / "wt-dirty" / "c.txt").write_text("c", encoding="utf-8")                      # uncommitted
(TMP / "wt-dirty" / "STATUS.md").write_text("ignored in the dirty count", encoding="utf-8")
COORD.mkdir()
(COORD / "milestones.jsonl").write_text('{"at":"t1","what":"plan written"}\nnot json\n{"at":"t2","what":"train 1 landed"}\n',
                                        encoding="utf-8")

run("write", "--unit", "wave", "--session", "s1", "--started", "2026-10-04T18:00:00-07:00", "--calls", "40",
    "--context", "90000", "--cost", "3.5", "--bg-tasks", "1")
cp = json.loads((COORD / "checkpoint.json").read_text(encoding="utf-8"))
trees = {t["branch"]: t for t in cp["worktrees"]}
check("main checkout excluded", sorted(trees), ["clean", "committed", "dirty"])
check("committed: ahead 1, clean", (trees["committed"]["ahead_of_main"], trees["committed"]["uncommitted"]), (1, 0))
check("dirty: ahead 0, one uncommitted file (STATUS.md ignored)", (trees["dirty"]["ahead_of_main"], trees["dirty"]["uncommitted"]), (0, 1))
check("STATUS.md headline captured", trees["committed"]["status_md"], "STATUS-AT: abc")
check("counters recorded", (cp["calls"], cp["context_tokens"], cp["background_tasks"], cp["unit"]), (40, 90000, 1, "wave"))
check("milestones kept, a malformed line is not dropped", [m.get("what") for m in cp["milestones"]], ["plan written", "not json", "train 1 landed"])

out = TMP / "handoff.json"
run("synthesize", "--unit", "wave", "--out", str(out))
h = json.loads(out.read_text(encoding="utf-8"))
check("schema-required keys present", all(k in h for k in SCHEMA["required"]), True)
check("no key the schema forbids", sorted(set(h) - set(SCHEMA["properties"])), [])
check("outcome split, next unit resume", (h["outcome"], h["next_unit"], h["synthesized"]), ("split", "resume", True))
check("summary within the cap", len(h["summary"]) <= 900, True)
check("only worktrees holding work are pending", sorted(b["branch"] for b in h["branches_pending"]), ["committed", "dirty"])
check("summary quotes the last milestone", "train 1 landed" in h["summary"], True)
check("branch statuses are schema values", {b["status"] for b in h["branches_pending"]} <= {"DONE", "SPLIT", "DISCHARGED", "BLOCKED"}, True)

(COORD / "checkpoint.json").unlink()
(COORD / "milestones.jsonl").unlink()
run("synthesize", "--unit", "meter", "--out", str(out))
h = json.loads(out.read_text(encoding="utf-8"))
check("with no checkpoint and no milestones it still synthesizes", ("no checkpoint" in h["summary"], h["next_unit"]), (True, "resume"))

if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("checkpoint self-test: all checks OK")
