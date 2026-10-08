#!/usr/bin/env python3
"""PreToolUse hook — REFUSE to build or test while a subagent fleet is live IN THE CALLER'S OWN WORKING TREE.

⛔ WHY THIS EXISTS. On 2026-08-04 a ten-agent measurement fan-out was dispatched over the §15 result-type
population, and the fix for what it was measuring was then implemented WHILE IT RAN: ~6 `dotnet build`
invocations plus both test gates, over ~1.75 h and 60 agents. Its finders probed a binary that changed under
them and its refuters re-ran probes against a tree where the defects were already fixed, so not one of its
verdicts was usable. One build even failed outright with `MSB3027: cobol.exe locked by another process` — a live
agent — and that alarm was recorded as "transient" and the building continued.

The owner's correction: each repetition wasted tokens. Memory alone did not stop it, so the guard is mechanical.

⚖ THE RULE IT ENFORCES is the ORDER of two decisions, not just the concurrency:
  1. Measure INLINE first. A fan-out for work an inline probe already did is pure waste — PB15's entire
     population was derived and measured in three tool calls before the fleet was ever dispatched.
  2. Once a fleet IS running, THE TREE IT IS PROBING is FROZEN. Build/test only after the completion signal, or
     stop the fleet.

⚖ THE UNIT OF THE FREEZE IS THE WORKING TREE, NOT THE SESSION (2026-09-01). The guard originally denied a build
whenever ANY other agent of this session was live, which is the right harm model applied at the wrong scope: the
harm is "my build changes the binaries THAT agent is executing", and two agents in two `git worktree` checkouts
share no `bin/obj`, no `cobol.exe` and no source. Keying on the session therefore denied a build that could not
collide with anything, and with the owner's 2026-09-01 direction for MAXIMUM SUBAGENT PARALLELISM in the fix
lane that false denial became the binding constraint: N implementer agents in N worktrees serialized behind each
other's liveness. So the guard now denies iff some FOREIGN live agent is working in the SAME tree as the caller.

Both trees in that comparison are DERIVED, never listed — a hand-maintained map of agent→tree is exactly the
shape rule 5 forbids, and it would go stale the first time the host renamed a directory:
  • the CALLER's tree = the repository root of WHERE THE BUILD RUNS — the project a `dotnet` command names, else
    the directory the shell is in when it runs it, following every location change of either shell
    (`build_sites`, through the shared parser `shell_location.py`, kb/Work PB2599), else the payload's `cwd` —
    the nearest ancestor holding a `.git` entry, which is a DIRECTORY in the main checkout and a FILE in a
    worktree. Stopping at the FIRST one is what makes a worktree resolve to itself rather than to the repository
    it was cut from (`repo_root_of`). A directory outside every working tree (a scratch probe) stays UNKNOWN: a
    scratch project can `ProjectReference` a worktree's project, so the trees it writes are not its directory's.
  • the MAIN checkout = that root itself when its `.git` is a directory, else the `gitdir: <main>/.git/
    worktrees/<name>` target named by the worktree's `.git` FILE, parsed here rather than by shelling out to
    git — a PreToolUse hook runs on every `dotnet` call and must not pay a subprocess (`main_repo_root`).
  • a FOREIGN agent's tree = `<main>/.claude/worktrees/agent-<agentId>` WHEN THAT DIRECTORY EXISTS, else the
    main checkout. This is true BY CONSTRUCTION: `Agent(isolation="worktree")` creates exactly that path and
    starts the agent in it, so the directory's existence IS the launch mode (`agent_working_tree`).
Consequences, all three intended: a worktree agent may build its OWN worktree while main-tree fleets are live; a
MAIN-tree build is still denied while any main-tree agent is live (the original purpose, unchanged); and a
main-tree build is ALLOWED while only worktree agents are live, because their binaries are not the main tree's.

⚠ AND THE FALLBACK IS TOWARD THE OLD RULE, NEVER TOWARD ALLOWING. If the caller's tree cannot be located, or the
worktree's `.git` file cannot be read or parsed, the agent→tree map cannot be built at all — so the decision
reverts to the SESSION-WIDE rule and every foreign live agent denies. Fail-open (below) covers a BROKEN guard;
this covers an UNKNOWN tree, where allowing would be the one outcome that reproduces 2026-08-04.

Detection is by TRANSCRIPT MTIME, which is the one signal that cannot be faked by a stale process: a live agent
writes to its own `agent-*.jsonl` continuously, so a file touched within the window means an agent is thinking
right now. Keyed on this session's id, so another project's fleet never blocks this one.

⚠ THE CALLER IS NEVER A FLEET. The question the guard asks is "is any transcript OTHER THAN MINE live?", and the
caller's own transcript is always fresh at PreToolUse — it is being written by this very tool call. Getting that
exclusion wrong makes the guard fail CLOSED, which it has now done three separate ways — PB103's TWO modes (an
agent inside its own worktree, then a main-tree agent counting its own transcript) and PB185 (every subagent,
because `transcript_path` names the SESSION transcript). ⚠ Cite PB103, not PB101: PB101 is the COLLATION
subsystem note, and it appears in this history only because its agent was the one who HIT the worktree mode
(`kb/Work/PB103.md` records the find that way). The exclusion is therefore keyed on the caller's IDENTITY, not on
a path string: see `caller_identity` / `foreign_transcripts`.

FAIL-OPEN BY CONSTRUCTION. Any error, any missing path, any unreadable directory ⇒ exit 0 and allow the build. A
guard that blocks the build because IT broke would be worse than the defect it prevents.

Both branches are provable without a fleet: `python scripts/hooks/fleet_active_build.py --self-test` drives the
pure decision functions through DENY and ALLOW alike, over REAL temporary worktrees so the `.git`-file parse and
the `.claude/worktrees/agent-<id>` existence check are exercised rather than mocked
(feedback_prove_the_watchdog_fails — a monitor is a gate; fire its failure branch once before trusting its
silence).
"""
import json
import os
import pathlib
import sys
import tempfile
import time

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "orchestrator"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import account  # noqa: E402  (the one resolver of Claude's config dir, kb/Work PB2479)
import shell_location  # noqa: E402  (the one parser of a command's location changes, kb/Work PB2599)

WINDOW_SECONDS = 120

# ⛔ An agent inside a LONG TOOL CALL writes nothing to its transcript until the tool returns (kb/Work PB1702). The
# assistant line carrying the `tool_use` is flushed when the call STARTS; the `tool_result` line only when it ENDS. A
# gate that blocks on `timeout 580 … tail -f <log>` therefore leaves the transcript untouched for up to ten minutes
# while the agent is plainly live — measured 2026-09-28, wave 69: three finishers' transcripts went 20+ minutes
# without a write while their gates ran, and a watchdog keyed on mtime alone called them dead. So a transcript whose
# LAST record is an unanswered tool call is live for as long as a tool call can last: the shell tools' 600 s maximum
# plus a margin. The bound is what keeps a KILLED agent (which also leaves a dangling tool call) from denying forever.
TOOL_WINDOW_SECONDS = 660
TAIL_BYTES = 262144

# `dotnet clean/publish/run` rewrite or delete the same outputs a live agent is executing, so they are in scope
# too. `dotnet --version`, `dotnet tool`, `dotnet nuget` etc. are not.
BUILD_VERBS = ("build", "test", "clean", "publish", "run", "msbuild")

# The `.git` FILE of a `git worktree` checkout; the value is `<main>/.git/worktrees/<name>`, absolute by default
# and relative when the worktree was created with `--relative-paths`.
GITDIR_PREFIX = "gitdir:"

# A build writes the bin/obj of the PROJECT it builds: `dotnet build <x>.sln`, `dotnet run --project <x>.csproj`,
# `dotnet run walk.cs`. A positional argument with one of these suffixes, or a `--project` value, names it.
PROJECT_SUFFIXES = (".sln", ".slnx", ".csproj", ".fsproj", ".vbproj", ".proj", ".cs")


def bail() -> None:
    """Allow the tool call. Every failure path lands here."""
    sys.exit(0)


def is_build_command(cmd: str) -> bool:
    low = cmd.lower()
    if "dotnet" not in low:
        return False
    # Match `dotnet <verb>` allowing intervening flags is overkill; the real invocations in this repo are
    # `dotnet build ...` / `dotnet test ...`, plus `timeout NNN dotnet build ...`.
    return any(f"dotnet {verb}" in low for verb in BUILD_VERBS)


def caller_identity(data: dict) -> tuple[str, "pathlib.Path | None"]:
    """The two spellings of "this transcript is MINE", read off the hook payload.

    ⛔ The failure this replaces (PB185, the third self-block of this guard): the payload's `transcript_path`
    names the SESSION transcript — `~/.claude/projects/<proj>/<session-id>.jsonl` — for a subagent exactly as it
    does for the main session. A subagent's own file is `<session-id>/subagents/**/agent-<agent_id>.jsonl`, a
    different path entirely, so the raw string equality added for PB103 could never match and EVERY subagent
    denied its own build permanently (the caller rewrites its transcript on every tool call, so the 120 s window
    never clears). Measured 2026-08-31 from the live payload: `transcript_path` = `…/17d093ad-….jsonl` while the
    caller's own transcript was `…/17d093ad-…/subagents/agent-acc9d0d75b870eb9d.jsonl`.

    So identity comes from `agent_id`, which IS the caller's transcript identity by construction — the file is
    named for it, wherever in the `subagents/` tree the host puts it, so no path string has to match and a
    fourth spelling of the same question is not one comparison away. `transcript_path` is kept as a second,
    independent spelling for hosts that DO point it at the agent file, compared on a RESOLVED path rather than
    on raw text. A payload carrying neither (the main session) excludes nothing, which is correct: the main
    session's own transcript is not under `subagents/` and never enters the candidate set at all.
    """
    agent_id = (data.get("agent_id") or "").strip().lower()

    own: "pathlib.Path | None" = None
    raw = (data.get("transcript_path") or "").strip()
    if raw:
        try:
            own = pathlib.Path(raw).resolve()
        except OSError:
            own = None
    return agent_id, own


def foreign_transcripts(data: dict, candidates: list) -> list:
    """The live transcripts that are NOT the caller's — the only ones that are evidence of a fleet."""
    agent_id, own = caller_identity(data)
    own_name = f"agent-{agent_id}.jsonl" if agent_id else None

    foreign = []
    for path in candidates:
        if own_name and path.name.lower() == own_name:
            continue
        if own is not None:
            try:
                if path.resolve() == own:
                    continue
            except OSError:
                pass
        foreign.append(path)
    return foreign


def same_path(a, b) -> bool:
    """Path equality that survives Windows' case-insensitive filesystem.

    The two sides are built from different strings — one from the payload's `cwd`, one from a `.git` file's
    `gitdir:` line — so `E:\\COBOL` and `e:\\COBOL` are both spellings that occur in practice and a
    raw `==` would silently answer "different tree" for the same tree.
    """
    return os.path.normcase(str(a)) == os.path.normcase(str(b))


def repo_root_of(start: str) -> "pathlib.Path | None":
    """The WORKING TREE containing `start`: its nearest ancestor holding a `.git` entry, resolved.

    `.git` is a DIRECTORY in the main checkout and a FILE in a `git worktree`; both count, and stopping at the
    FIRST one is precisely what makes a worktree resolve to ITSELF rather than to the repository it was cut
    from. `None` means "no tree found", which the caller must treat as UNKNOWN, never as "no conflict".
    """
    if not start:
        return None
    try:
        here = pathlib.Path(start).resolve()
    except OSError:
        return None
    for d in (here, *here.parents):
        try:
            if (d / ".git").exists():
                return d
        except OSError:
            continue
    return None


def main_repo_root(tree_root: pathlib.Path) -> "pathlib.Path | None":
    """The MAIN checkout owning `tree_root` — the tree under which `.claude/worktrees/` lives.

    ⛔ Parsed, not shelled out. This hook runs on EVERY `dotnet` invocation with a 30 s budget; spawning
    `git rev-parse` per call would put a process launch in front of every build, and on a broken/locked repo it
    is also the one call that can hang. The `.git` file's format (`gitdir: <path>`, optionally relative) is
    part of git's on-disk layout, so reading it is as stable as calling git and cannot block.

    Returns `None` when the file is missing, unreadable or does not carry a `gitdir:` line — the UNKNOWN answer,
    which `agents_sharing_tree` turns into the old session-wide rule rather than into an allow.
    """
    dot_git = tree_root / ".git"
    try:
        if dot_git.is_dir():
            return tree_root  # this IS the main checkout
        text = dot_git.read_text(encoding="utf-8", errors="replace")
    except (OSError, ValueError):
        return None

    gitdir = ""
    for line in text.splitlines():
        line = line.strip()
        if line.lower().startswith(GITDIR_PREFIX):
            gitdir = line[len(GITDIR_PREFIX):].strip()
            break
    if not gitdir:
        return None

    try:
        p = pathlib.Path(gitdir)
        if not p.is_absolute():
            p = tree_root / p  # `git worktree add --relative-paths` writes `../../.git/worktrees/<name>`
        p = p.resolve()
    except OSError:
        return None

    # `<main>/.git/worktrees/<name>` ⇒ the main root is the parent of the `.git` component. Walking up to the
    # component rather than slicing three levels keeps this correct if git ever nests the admin dir deeper.
    for d in (p, *p.parents):
        if d.name == ".git":
            return d.parent
    return None


def agent_id_of(transcript: pathlib.Path) -> str:
    """`…/agent-<agentId>.jsonl` ⇒ `<agentId>`; anything else ⇒ `""` (an agent with no derivable tree)."""
    name = transcript.name
    stem = name[:-len(".jsonl")] if name.lower().endswith(".jsonl") else name
    return stem[len("agent-"):] if stem.lower().startswith("agent-") else ""


def agent_working_tree(agent_id: str, main_root: pathlib.Path) -> pathlib.Path:
    """Where a live agent is working — decided by CONSTRUCTION, never by a maintained list.

    `Agent(isolation="worktree")` creates `<main>/.claude/worktrees/agent-<agentId>` and starts the agent with
    its cwd there, so the DIRECTORY'S EXISTENCE IS THE LAUNCH MODE. No directory ⇒ the agent was launched
    without isolation (a read-only fleet, a main-tree implementer) and is working in the main checkout. This is
    the whole reason nothing here needs hand-maintaining: the next isolation mode the host adds shows up as a
    directory or does not, and the answer stays right either way.
    """
    if agent_id:
        wt = main_root / ".claude" / "worktrees" / f"agent-{agent_id}"
        try:
            if wt.is_dir():
                return wt.resolve()
        except OSError:
            pass
    return main_root


def agents_sharing_tree(caller_cwd: str, foreign: list) -> tuple[list, "pathlib.Path | None"]:
    """The live foreign agents working in the CALLER'S tree — the only builds that can collide with this one.

    Returns `(sharing, caller_tree)`. When the caller's tree or its main checkout cannot be determined, EVERY
    foreign agent is returned: the guard reverts to the session-wide rule it had before 2026-09-01 rather than
    guess, because the failure it exists to prevent is a build that went ahead.
    """
    caller_tree = repo_root_of(caller_cwd)
    if caller_tree is None:
        return list(foreign), None
    main_root = main_repo_root(caller_tree)
    if main_root is None:
        return list(foreign), caller_tree
    sharing = [
        p for p in foreign
        if same_path(agent_working_tree(agent_id_of(p), main_root), caller_tree)
    ]
    return sharing, caller_tree


def build_sites(data: dict) -> list:
    """Every place the tool call builds: for each `dotnet <verb>` command, the project it names, else the directory it
    runs in. `None` is a place the command line does not determine (an UNKNOWN tree, so the session-wide rule).

    The directory each command runs in comes from `shell_location.steps`, the one parser of the two shells' location
    changes (kb/Work PB2599): a PowerShell `Set-Location <worktree>; dotnet build`, a Bash `git worktree add … && cd
    <worktree> && dotnet build`, a `pushd`, a `( … )` subshell or `pwsh -WorkingDirectory` each build where the shell
    does, not in the payload's cwd. `cwd` is a documented PreToolUse payload field (since PB103); `os.getcwd()` stands
    in for a host that omits it, because the hook is launched from the caller's directory.
    """
    cwd = (data.get("cwd") or "").strip()
    if not cwd:
        try:
            cwd = os.getcwd()
        except OSError:
            cwd = ""
    command = (data.get("tool_input") or {}).get("command") or ""
    sites = []
    for step in shell_location.steps(command, cwd or None, shell_location.shell_of(data.get("tool_name"))):
        if not is_build_command(step.text):
            continue
        words = list(step.words)
        named = [words[i + 1] for i, w in enumerate(words[:-1]) if w in ("--project", "--solution")]
        named += [w.split("=", 1)[1] for w in words if w.startswith(("--project=", "--solution="))]
        named += [w for w in words if w.lower().endswith(PROJECT_SUFFIXES) and not w.startswith("-")]
        sites += [shell_location.resolve(n, step.cwd) for n in named] or [step.cwd]
    return sites


def agents_sharing_any(sites: list, foreign: list) -> tuple[list, "pathlib.Path | None"]:
    """`agents_sharing_tree` over every build site of the call: the live agents sharing ANY of them (an UNKNOWN site
    reverts to the session-wide rule), and the tree the first sharing site resolved to (None when unknown)."""
    sharing, tree = [], None
    for site in sites:
        live, caller_tree = agents_sharing_tree(site or "", foreign)
        if live and not sharing:
            tree = caller_tree
        sharing += [p for p in live if p not in sharing]
    return sharing, tree


def live_agent_transcripts(session_id: str) -> list:
    projects = account.config_dir() / "projects"
    if not projects.is_dir():
        return []

    now = time.time()
    live = []
    # ~/.claude/projects/<sanitized-cwd>/<session-id>/subagents/**/agent-*.jsonl
    # Globbing on the session id rather than deriving the sanitized cwd keeps this correct whatever the
    # sanitization rule is, and scopes the guard to THIS session.
    for path in projects.glob(f"*/{session_id}/subagents/**/agent-*.jsonl"):
        try:
            if is_live(path, now):
                live.append(path)
        except OSError:
            continue
    return live


def is_live(path: pathlib.Path, now: float) -> bool:
    """Written within WINDOW_SECONDS, or ending on an unanswered tool call written within TOOL_WINDOW_SECONDS."""
    age = now - path.stat().st_mtime
    if age <= WINDOW_SECONDS:
        return True
    return age <= TOOL_WINDOW_SECONDS and pending_tool_call(path)


def pending_tool_call(path: pathlib.Path) -> bool:
    """Does the transcript END on a tool call that has no result yet — i.e. is the agent inside a tool right now?

    Reads only the tail. The last record that is either an assistant message or a tool result decides it: an
    assistant message carrying `tool_use` ⇒ pending; a user record carrying `tool_result`, or an assistant message
    without `tool_use` (the turn ended) ⇒ not pending. Any parse problem ⇒ False, which leaves the plain mtime rule
    in force: this can only ADD liveness the old rule missed, never remove liveness it found.
    """
    try:
        with path.open("rb") as f:
            f.seek(0, os.SEEK_END)
            size = f.tell()
            f.seek(max(0, size - TAIL_BYTES))
            tail = f.read().decode("utf-8", errors="replace")
    except OSError:
        return False
    for line in reversed(tail.splitlines()):
        try:
            rec = json.loads(line)
        except ValueError:
            continue
        content = (rec.get("message") or {}).get("content")
        if not isinstance(content, list):
            continue
        kinds = {c.get("type") for c in content if isinstance(c, dict)}
        if rec.get("type") == "assistant":
            return "tool_use" in kinds
        if rec.get("type") == "user" and "tool_result" in kinds:
            return False
    return False


def deny_reason(live: list, tree: "pathlib.Path | None" = None) -> str:
    # ⛔ `p.stem`, not `p.parent.name`: the parent of `…/subagents/agent-bbb.jsonl` is `subagents`, so the
    # message used to name the CONTAINING DIRECTORY for every agent and printed "subagents" as the culprit.
    names = sorted({p.stem for p in live})
    detail = ", ".join(names[:4]) + (f" (+{len(names) - 4} more)" if len(names) > 4 else "")
    where = (
        f"the SAME working tree as this build ({tree})" if tree is not None
        else "a working tree that could not be determined, so the session-wide rule applies"
    )
    return (
        f"BLOCKED — {len(live)} subagent(s) are LIVE (transcript written in the last {WINDOW_SECONDS}s, or "
        f"inside a tool call started in the last {TOOL_WINDOW_SECONDS}s), so a fleet is running in {where}: "
        f"{detail}. Building or testing now changes the binary those agents are "
        f"probing, which is what made a 60-agent run unusable on 2026-08-04 (PB15). "
        f"Do ONE of: (a) wait for the completion notification, then build; "
        f"(b) stop the fleet with TaskStop if its results are no longer worth having; "
        f"(c) if you are about to fix what it measures, stop it now — you already have the answer, and a "
        f"fleet measuring a tree you are editing produces verdicts you will have to disclaim; "
        f"(d) re-dispatch the agent with `isolation: \"worktree\"` — an agent in its own worktree builds its "
        f"own binaries and neither blocks nor is blocked by this tree. "
        f"Meanwhile, doc / DEVLOG / design-doc / kb-note work is safe."
    )


def _build_fixture(root: pathlib.Path) -> dict:
    """A REAL main checkout with REAL worktrees on disk, for the self-test.

    Mocking the filesystem here would defeat the point: the two facts this guard now turns on are a `.git`
    FILE's parse and a `.claude/worktrees/agent-<id>` directory's EXISTENCE, and a fake for either is a probe
    that cannot fail for the reason production would.
    """
    main = root / "main"
    (main / ".git" / "worktrees").mkdir(parents=True)
    (main / "scripts" / "hooks").mkdir(parents=True)

    def worktree(name: str, gitdir_text: str) -> pathlib.Path:
        wt = main / ".claude" / "worktrees" / name
        wt.mkdir(parents=True)
        (wt / ".git").write_text(gitdir_text, encoding="utf-8")
        return wt.resolve()

    return {
        "main": main.resolve(),
        "nested": (main / "scripts" / "hooks").resolve(),
        # Absolute gitdir — what `git worktree add` writes by default.
        "wtA": worktree("agent-aaa", f"gitdir: {(main / '.git' / 'worktrees' / 'agent-aaa').as_posix()}\n"),
        "wtB": worktree("agent-bbb", f"gitdir: {(main / '.git' / 'worktrees' / 'agent-bbb').as_posix()}\n"),
        # Relative gitdir — what `git worktree add --relative-paths` writes.
        "wtRel": worktree("agent-rel", "gitdir: ../../../.git/worktrees/agent-rel\n"),
        # A `.git` file that exists but yields no gitdir — the UNKNOWN-tree branch.
        "wtBroken": worktree("agent-broken", "this file is not a gitdir pointer\n"),
        "outside": root.resolve(),
    }


def self_test() -> int:
    """Fire BOTH branches of the guard without a fleet. `python scripts/hooks/fleet_active_build.py --self-test`.

    A guard is only trustworthy once its failure branch has been observed firing — a silent guard and a broken
    guard look identical (feedback_prove_the_watchdog_fails). Three of this hook's defects were the ALLOW branch
    failing CLOSED, which no amount of green silence would ever have revealed; the 2026-09-01 tree-scoping is the
    same shape one level up (the whole guard failing closed for every parallel worktree agent), so its ALLOW
    cases are asserted here as hard as its DENY cases.
    """
    failures = 0

    def check(name: str, got, expected) -> None:
        nonlocal failures
        ok = got == expected
        failures += 0 if ok else 1
        print(f"  [{'ok' if ok else 'FAIL'}] {name}: {got!r} expected={expected!r}")

    # ── 1. The caller-identity exclusion (PB103's two modes / PB185), unchanged ────────────────────────────
    sess = "S1"
    # Real `Path`s, not `PurePath`s: `foreign_transcripts` resolves candidates, and a self-test that could not
    # reach that line would be exactly the well-formed-but-worthless probe the guard exists to prevent.
    mine = pathlib.Path(f"/p/x/{sess}/subagents/agent-aaa.jsonl")
    other = pathlib.Path(f"/p/x/{sess}/subagents/agent-bbb.jsonl")
    nested = pathlib.Path(f"/p/x/{sess}/subagents/workflows/wf_1/agent-aaa.jsonl")
    session_transcript = f"/p/x/{sess}.jsonl"

    # The exact payload shape measured 2026-08-31: a subagent's `transcript_path` is the SESSION transcript.
    subagent = {"agent_id": "aaa", "transcript_path": session_transcript}
    main_payload = {"transcript_path": session_transcript}

    for name, payload, candidates, expected in [
        ("subagent alone (PB185) -> ALLOW", subagent, [mine], 0),
        ("subagent, own file nested in workflows/ -> ALLOW", subagent, [nested], 0),
        ("subagent inside a REAL fleet -> DENY", subagent, [mine, other], 1),
        ("main session + real fleet -> DENY", main_payload, [mine, other], 2),
        ("main session, no fleet -> ALLOW", main_payload, [], 0),
        ("agent id case-folded -> ALLOW", {"agent_id": "AAA"}, [mine], 0),
        ("no identity in payload -> DENY (the one fail-closed path)", {}, [mine], 1),
    ]:
        check(f"identity: {name}", len(foreign_transcripts(payload, candidates)), expected)

    # ── 2. Tree resolution, over REAL temp worktrees ──────────────────────────────────────────────────────
    with tempfile.TemporaryDirectory() as tmp:
        fx = _build_fixture(pathlib.Path(tmp))

        check("tree: main checkout resolves to itself", repo_root_of(str(fx["main"])), fx["main"])
        check("tree: a nested dir walks UP to the main checkout", repo_root_of(str(fx["nested"])), fx["main"])
        check("tree: a worktree resolves to ITSELF, not to main", repo_root_of(str(fx["wtA"])), fx["wtA"])
        check("tree: main's own main-root is itself", main_repo_root(fx["main"]), fx["main"])
        check("tree: worktree .git FILE parses to main", main_repo_root(fx["wtA"]), fx["main"])
        check("tree: RELATIVE gitdir parses to main", main_repo_root(fx["wtRel"]), fx["main"])
        check("tree: unparseable .git file -> UNKNOWN", main_repo_root(fx["wtBroken"]), None)
        check("tree: agent WITH a worktree dir", agent_working_tree("aaa", fx["main"]), fx["wtA"])
        check("tree: agent WITHOUT one falls to main", agent_working_tree("zzz", fx["main"]), fx["main"])

        def t(agent: str) -> pathlib.Path:
            """A live transcript for `agent`, wherever the host files it."""
            return pathlib.Path(f"/p/x/{sess}/subagents/agent-{agent}.jsonl")

        # name, caller cwd, foreign transcripts, expected sharing count
        tree_cases = [
            # THE CASE THIS CHANGE EXISTS FOR: N implementer agents, N worktrees, all building at once.
            ("worktree caller + main-tree fleet -> ALLOW", fx["wtA"], [t("main1"), t("main2")], 0),
            ("worktree caller + ANOTHER worktree's agent -> ALLOW", fx["wtA"], [t("bbb")], 0),
            ("worktree caller + BOTH -> ALLOW", fx["wtA"], [t("bbb"), t("main1")], 0),
            # A helper spawned inside agent-aaa's worktree: its cwd IS that worktree, and aaa is still live.
            ("worktree caller + live agent in the SAME worktree -> DENY", fx["wtA"], [t("aaa")], 1),
            ("worktree caller, same tree + a foreign tree -> DENY on the one", fx["wtA"], [t("aaa"), t("bbb")], 1),
            # The original purpose, unchanged.
            ("main caller + main-tree agent -> DENY", fx["main"], [t("main1")], 1),
            ("main caller (nested cwd) + main-tree agent -> DENY", fx["nested"], [t("main1")], 1),
            ("main caller + worktree-only agents -> ALLOW", fx["main"], [t("aaa"), t("bbb")], 0),
            ("main caller, no fleet -> ALLOW", fx["main"], [], 0),
            # Fallbacks: UNKNOWN tree reverts to the pre-2026-09-01 session-wide rule, never to an allow.
            ("unparseable .git -> DENY session-wide (old rule)", fx["wtBroken"], [t("aaa"), t("main1")], 2),
            ("cwd outside any repo -> DENY session-wide (old rule)", fx["outside"], [t("main1")], 1),
            ("empty cwd -> DENY session-wide (old rule)", "", [t("main1")], 1),
        ]
        for name, cwd, foreign, expected in tree_cases:
            sharing, _ = agents_sharing_tree(str(cwd), foreign)
            check(f"scope: {name}", len(sharing), expected)

        # A build is judged where it runs (kb/Work PB2599): every location change either shell accepts, and the project
        # a build names. The shapes are the operator's six false denials of 2026-10-07/08, plus the harmful direction.
        def sites(command: str, cwd, tool: str = "Bash") -> list:
            return build_sites({"cwd": str(cwd), "tool_name": tool, "tool_input": {"command": command}})

        def denies(command: str, cwd, tool: str = "Bash", fleet=("main1",)) -> int:
            return len(agents_sharing_any(sites(command, cwd, tool), [t(a) for a in fleet])[0])

        main, wt = fx["main"], fx["wtA"]
        for name, command, tool, expected in [
            ("`cd <main> && dotnet build` from a worktree -> DENY on the main fleet", f'cd "{main}" && dotnet build', "Bash", 1),
            ("no location change -> the payload cwd (main) -> DENY", "dotnet build Cobol.Net.sln", "Bash", 1),
            ("PowerShell `Set-Location <wt>; dotnet build` -> ALLOW", f"Set-Location {wt}; dotnet build -c Debug", "PowerShell", 0),
            ("PowerShell `Set-Location <wt>; (Measure-Command {{ dotnet publish }})` -> ALLOW",
             f"Set-Location {wt}; (Measure-Command {{ dotnet publish x.csproj -c Release }}).TotalSeconds", "PowerShell", 0),
            ("PowerShell `Push-Location -Path <wt>` -> ALLOW", f"Push-Location -Path '{wt}'; dotnet build; Pop-Location", "PowerShell", 0),
            ("PowerShell `pwsh -WorkingDirectory <wt> -Command` -> ALLOW",
             f'pwsh -NoProfile -WorkingDirectory "{wt}" -Command "dotnet build"', "PowerShell", 0),
            ("Bash `git worktree add … && cd <wt> && dotnet build` -> ALLOW",
             f"git worktree add -q --detach x abc && cd {wt.as_posix()} && dotnet build Cobol.Net.sln", "Bash", 0),
            ("Bash `mkdir -p … && cd <wt> && dotnet build` -> ALLOW", f"mkdir -p /tmp/q && cd {wt.as_posix()} && dotnet build", "Bash", 0),
            ("Bash `ls …; cd <wt> && dotnet build` -> ALLOW", f"ls STOP; cd {wt.as_posix()} && dotnet build", "Bash", 0),
            ("Bash `pushd <wt> && dotnet build` -> ALLOW", f"pushd {wt.as_posix()} && dotnet build", "Bash", 0),
            ("Bash `(cd <wt> && dotnet build) && dotnet test` -> DENY: the test runs in main",
             f"(cd {wt.as_posix()} && dotnet build) && dotnet test", "Bash", 1),
            ("Bash `cd <wt> & dotnet build` -> DENY: a background cd does not move the shell", f"cd {wt.as_posix()} & dotnet build", "Bash", 1),
            ("a target the line does not determine -> UNKNOWN -> DENY session-wide", "cd $NOWHERE && dotnet build", "Bash", 1),
            ("from main, `dotnet build <wt>/x.sln` builds the worktree -> ALLOW",
             f"dotnet build {wt.as_posix()}/Cobol.Net.sln", "Bash", 0),
        ]:
            check(f"scope: {name}", denies(command, main if "from a worktree" not in name else wt, tool), expected)
        # The harmful direction: from a worktree, a build of the MAIN checkout's solution must see the main fleet.
        check("scope: from a worktree, `dotnet build <main>/x.sln` -> DENY on the main fleet",
              denies(f"dotnet build {main.as_posix()}/Cobol.Net.sln", wt), 1)
        check("scope: from a worktree, `dotnet run --project <main>/x.csproj` -> DENY on the main fleet",
              denies(f"dotnet run --project {main.as_posix()}/x.csproj", wt), 1)
        check("scope: a scratch dir outside every tree stays UNKNOWN -> DENY session-wide (2026-10-07 21:41 refuter)",
              denies(f"cd {fx['outside'].as_posix()}; timeout 600 dotnet run walk.cs", main), 1)

        # PB474: a Git-Bash `cd /e/…` target must land on the worktree, not on `E:\e\…` and thence UNKNOWN.
        if os.name == "nt":
            drive, rest = os.path.splitdrive(str(wt))
            msys = "/" + drive[0].lower() + rest.replace("\\", "/")
            for spelled in (msys, "/cygdrive" + msys):
                got = sites(f"cd {spelled} && dotnet build", main)
                check(f"scope: MSYS `cd {spelled[:14]}…` resolves to the worktree (PB474)", same_path(got[0], wt), True)
                sharing, tree = agents_sharing_tree(got[0], [t("main1")])
                check("scope: …and a main-tree fleet then does NOT deny that worktree build", (len(sharing), tree), (0, wt))

        # The DENY message must name the agents AND the shared tree (it named `subagents` for every one).
        sharing, tree = agents_sharing_tree(str(fx["main"]), [t("main1"), t("main2")])
        msg = deny_reason(sharing, tree)
        check("message: names the live agents", "agent-main1" in msg and "agent-main2" in msg, True)
        check("message: names the shared tree", str(fx["main"]) in msg, True)

    # ── 2b. Liveness (PB1702): an agent INSIDE a long tool call writes nothing until the call returns ──────────
    with tempfile.TemporaryDirectory() as tmp:
        base = pathlib.Path(tmp)
        tool_use = {"type": "assistant", "message": {"content": [{"type": "tool_use", "id": "t1", "name": "Bash"}]}}
        tool_res = {"type": "user", "message": {"content": [{"type": "tool_result", "tool_use_id": "t1"}]}}
        ended = {"type": "assistant", "message": {"content": [{"type": "text", "text": "done"}]}}
        now = time.time()

        def transcript(name: str, records: list, age: float) -> pathlib.Path:
            p = base / f"agent-{name}.jsonl"
            p.write_text("\n".join(json.dumps(r) for r in records) + "\n", encoding="utf-8")
            os.utime(p, (now - age, now - age))
            return p

        for name, records, age, expected in [
            ("fresh write -> LIVE", [tool_use, tool_res], 30, True),
            # THE CASE THIS EXISTS FOR: wave 69's finishers, blocked 5+ minutes on `timeout 580 … tail -f gate.log`.
            ("in a tool call for 5 min -> LIVE", [tool_res, tool_use], 300, True),
            ("tool call answered, quiet 5 min -> not live", [tool_use, tool_res], 300, False),
            ("turn ended, quiet 5 min -> not live", [tool_use, tool_res, ended], 300, False),
            # A KILLED agent also leaves a dangling tool call: bounded, so it cannot deny forever.
            ("dangling tool call older than any tool can run -> not live", [tool_res, tool_use], 900, False),
            ("unparseable tail, quiet 5 min -> not live (old rule stands)", [], 300, False),
        ]:
            p = transcript(name.split()[0] + str(age), records, age)
            if not records:
                p.write_text("{not json\n", encoding="utf-8")
                os.utime(p, (now - age, now - age))
            check(f"liveness: {name}", is_live(p, now), expected)

    # ── 3. The verb matcher, both ways — `dotnet --version` must stay OUT of scope ─────────────────────────
    for cmd, expected in [
        ("dotnet build Cobol.Net.sln", True),
        ("timeout 900 dotnet test x.csproj", True),
        ("dotnet --version", False),
        ("dotnet tool list", False),
        ("git status", False),
    ]:
        check(f"verb `{cmd}`", is_build_command(cmd), expected)

    print("SELF-TEST " + ("PASS" if not failures else f"FAIL ({failures} case(s))"))
    return 1 if failures else 0


def main() -> None:
    try:
        data = json.load(sys.stdin)
    except Exception:  # noqa: BLE001
        bail()

    command = (data.get("tool_input") or {}).get("command") or ""
    if not command or not is_build_command(command):
        bail()

    session_id = data.get("session_id") or ""
    if not session_id:
        bail()

    try:
        foreign = foreign_transcripts(data, live_agent_transcripts(session_id))
        live, caller_tree = agents_sharing_any(build_sites(data), foreign)
    except Exception:  # noqa: BLE001
        bail()

    if not live:
        bail()

    json.dump(
        {
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "deny",
                "permissionDecisionReason": deny_reason(live, caller_tree),
            }
        },
        sys.stdout,
    )


if __name__ == "__main__":
    sys.exit(self_test()) if "--self-test" in sys.argv else main()
