"""A/B: does a hook that ENFORCES "rewrite STATUS.md after every commit" beat the written rule alone?

    python run_ab.py <n per arm> <concurrency> <out dir>

Each run is a fresh headless `claude -p` session (Opus) in its own sandbox git repo, copied from template/. Both arms
get the identical brief, which states the rule. GUARD additionally gets status_guard.py registered through
--settings (PreToolUse/PostToolUse on git commit, Stop). Headless sessions are used because hooks load when a
session starts: in-session subagents cannot be split into arms by hook configuration.

Measured per run at exit: whether STATUS.md's stamp equals HEAD (the outcome), commit count, whether the tests pass,
turns, cost and tokens from the CLI's JSON result.
"""
import json, pathlib, shutil, subprocess, sys, time
from concurrent.futures import ThreadPoolExecutor

HERE = pathlib.Path(__file__).resolve().parent
GUARD = HERE / "status_guard.py"
TEMPLATE = "template2"
BRIEF = """You are a software engineer in a fleet of agents working on the git repository in the current directory.

Read PROTOCOL.md first and follow it throughout; it is mandatory.

Task: make the whole test suite pass (`python -m unittest -q`) and then make the gate GREEN (`python gate.py`).
The failures are real bugs in inventory.py, pricing.py and report.py.
"""
HOOK_MARKS = {"denied_commits": "Commit refused:", "post_reminders": "the last commit is not in the handoff",
              "unstamped_reminders": "has no valid `STATUS-AT:` stamp", "stop_blocks": "Before you finish:"}


def interventions(session_id):
    """Count the guard's messages in the session transcript (0 for the RULE arm by construction)."""
    root = pathlib.Path.home() / ".claude" / "projects"
    hits = list(root.glob(f"*/{session_id}.jsonl")) if session_id else []
    if not hits:
        return {"transcript_found": False}
    text = hits[0].read_text(encoding="utf-8", errors="replace")
    return {"transcript_found": True, **{k: text.count(v) for k, v in HOOK_MARKS.items()}}
ALLOWED = ["Read", "Edit", "Write", "Glob", "Grep", "Bash(git:*)", "Bash(python:*)", "Bash(python3:*)", "Bash(ls:*)", "Bash(cat:*)"]


ARMS = {"RULE": ("template2", False), "GUARD": ("template2", True)}


def sandbox(root, template):
    shutil.copytree(HERE / template, root)
    (root / ".gitignore").write_text("STATUS.md\n__pycache__/\n", encoding="utf-8")
    run = lambda *a: subprocess.run(["git", *a], cwd=root, capture_output=True, check=True)
    run("init", "-q"); run("config", "user.email", "agent@example.com"); run("config", "user.name", "agent")
    run("add", "-A"); run("commit", "-qm", "initial")


def settings_file(out):
    cmd = f'python "{GUARD.as_posix()}"'
    h = [{"type": "command", "command": cmd, "timeout": 30}]
    s = {"hooks": {
        "PreToolUse": [{"matcher": "Bash", "hooks": h}],
        "PostToolUse": [{"matcher": "Bash", "hooks": h}],
        "Stop": [{"hooks": h}], "SubagentStop": [{"hooks": h}]}}
    p = out / "guard-settings.json"
    p.write_text(json.dumps(s, indent=1), encoding="utf-8")
    return p


def one(job):
    arm, i, out, gs = job
    root = out / f"{arm}-{i}"
    template, guarded = ARMS[arm]
    sandbox(root, template)
    args = ["claude", "-p", BRIEF, "--output-format", "json", "--model", "opus", "--allowedTools", *ALLOWED]
    if guarded:
        args += ["--settings", str(gs)]
    t0 = time.time()
    r = subprocess.run(args, cwd=root, capture_output=True, text=True, encoding="utf-8", timeout=3600)
    wall = time.time() - t0
    try:
        res = json.loads(r.stdout)
    except Exception:
        res = {"parse_error": r.stdout[-2000:], "stderr": r.stderr[-2000:]}
    git = lambda *a: subprocess.run(["git", *a], cwd=root, capture_output=True, text=True).stdout.strip()
    head = git("rev-parse", "HEAD")
    st = (root / "STATUS.md").read_text(encoding="utf-8", errors="replace") if (root / "STATUS.md").exists() else ""
    import re
    m = re.search(r"^\s*STATUS-AT:\s*([0-9a-fA-F]{7,40})", st, re.M)
    stamp = git("rev-parse", "--verify", "--quiet", m.group(1) + "^{commit}") if m else ""
    tests = subprocess.run([sys.executable, "-m", "unittest", "-q"], cwd=root, capture_output=True, text=True)
    commits = int(git("rev-list", "--count", "HEAD")) - 1
    undescribed = int(git("rev-list", "--count", f"{stamp}..HEAD")) if stamp else commits
    row = {"arm": arm, "i": i, "stamp_is_head": bool(stamp) and stamp == head, "undescribed_commits": undescribed,
           "status_exists": bool(st), "commits": commits, "tests_pass": tests.returncode == 0, "wall_s": round(wall, 1),
           "num_turns": res.get("num_turns"), "cost_usd": res.get("total_cost_usd"), "usage": res.get("usage"),
           "is_error": res.get("is_error"), "result_tail": str(res.get("result", ""))[-400:], "session_id": res.get("session_id"),
           "log": git("log", "--format=%h %s"), "rc": r.returncode, **interventions(res.get("session_id")),
           "stdout_tail": r.stdout[-1500:] if "parse_error" in res else "", "stderr_tail": r.stderr[-1500:]}
    (root.parent / f"{arm}-{i}.json").write_text(json.dumps(row, indent=1), encoding="utf-8")
    print(f"{arm}-{i}: stamp_is_head={row['stamp_is_head']} undescribed={undescribed} commits={commits} tests={row['tests_pass']} turns={row['num_turns']}", flush=True)
    return row


def main():
    n, conc, out = int(sys.argv[1]), int(sys.argv[2]), pathlib.Path(sys.argv[3]).resolve()
    out.mkdir(parents=True, exist_ok=True)
    gs = settings_file(out)
    for spec in sys.argv[4:]:
        name, _, rest = spec.partition("=")
        tpl, _, g = rest.partition("+")
        ARMS[name] = (tpl, g == "guard")
    names = [s.partition("=")[0] for s in sys.argv[4:]] or ["RULE", "GUARD"]
    jobs = [(arm, i, out, gs) for i in range(1, n + 1) for arm in (names if i % 2 else list(reversed(names)))]
    with ThreadPoolExecutor(conc) as ex:
        rows = list(ex.map(one, jobs))
    (out / "results.json").write_text(json.dumps(rows, indent=1), encoding="utf-8")
    for arm in names:
        a = [r for r in rows if r["arm"] == arm]
        print(arm, f"stamp==HEAD at exit {sum(r['stamp_is_head'] for r in a)}/{len(a)}",
              f"tests pass {sum(r['tests_pass'] for r in a)}/{len(a)}")


if __name__ == "__main__":
    main()
