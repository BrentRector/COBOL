#!/usr/bin/env python3
"""SessionStart hook — inject the mechanical live-state probe into the session.

Plan §0's bootstrap step ③ is "run session-probe.ps1". As a manual ritual it gets skipped; as a hook it cannot be.
Never fails the session: any error is reported as context, not raised.

In a claude.ai cloud session (CLAUDE_CODE_REMOTE=true) it first does the per-CLONE setup: the PUBLIC
`tools/claude-skills` submodule (the fleet scripts and the brent-tools base skills — cloud sessions do not receive the
project's plugin marketplace, so the overlays read the base SKILL.md files from it), then the private
`specs-private` submodule (a fresh clone has no submodules; it holds the licensed PDF that `render-spec-page.py` and
the figure audits read — `cite.py` and `specs/ISO_COBOL.md` live in the main repo and need no submodule — and it
clones only when BrentRector/COBOL-private is attached to the session) and the git-ignored GnuCOBOL corpus. The VM toolchain, and the user-level
shim that makes this hook fire when the session starts in /home/user rather than the repo, come from
scripts/cloud/setup-env.sh. Locally the hook stays read-only: a missing `tools/claude-skills` is reported with the
command that fetches it.
"""
import json
import os
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PROBE = REPO / "scripts" / "session-probe.ps1"


SKILLS = "tools/claude-skills"   # the PUBLIC brent-tools plugin: fleet scripts + base skills (kb/Work/PB1699)


def skills_missing() -> bool:
    return not (REPO / SKILLS / ".claude-plugin" / "plugin.json").exists()


def init_cloud_submodules() -> str:
    if os.environ.get("CLAUDE_CODE_REMOTE") != "true":
        return ""
    out = ""
    # The public submodule FIRST and on its own: the fleet scripts (orient.py, fix_clusters.py, status_delta.py)
    # and the base skills live there, and a failure of the private one below must not cost them.
    for args, fix in (
            ([SKILLS], "attach BrentRector/claude-skills to this session if the proxy refuses the public repository"),
            (["--depth", "1", "--recursive"], "the cloud GitHub proxy only serves repositories attached to the "
                                              "session — attach BrentRector/COBOL-private to this session (or the "
                                              "routine's sources)")):
        cmd = ["git", "submodule", "update", "--init", *args]
        try:
            r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace",
                               timeout=180, cwd=str(REPO))
            status = "ok" if r.returncode == 0 else (
                f"FAILED (exit {r.returncode}): {(r.stderr or r.stdout).strip()}\nFIX: {fix}, then re-run "
                f"`{' '.join(cmd)}`.")
        except Exception as exc:  # noqa: BLE001 - a hook must never break the session
            status = f"FAILED: {exc}"
        out += f"cloud session: {' '.join(cmd[1:])} → {status}\n"
    return out + fetch_cloud_corpus() + "\n"


def local_skills_hint() -> str:
    """Locally the hook stays read-only: a missing public submodule is reported, never fetched."""
    if os.environ.get("CLAUDE_CODE_REMOTE") == "true" or not skills_missing():
        return ""
    return (f"⚠ {SKILLS} is not checked out: the fleet scripts and the brent-tools base skills live there. "
            f"RUN: git submodule update --init {SKILLS}\n\n")


def fetch_cloud_corpus() -> str:
    """The git-ignored GnuCOBOL corpus (tests/external/) is per CLONE, so a cloud session starts without it and the
    population drift gate (ExternalCorpusPopulationDriftTests) is red by design (PB209/PB277) — cloud smoke #2,
    2026-09-24. setup-env.sh pre-caches the pinned tarball; copying it in first makes the fetch skip the download.
    The rule (fetch when absent, a failed fetch named) is scripts/external_corpus.py's, the one every gate applies
    (kb/Work PB2611)."""
    import shutil
    sys.path.insert(0, str(REPO / "scripts"))
    import external_corpus
    cached = pathlib.Path("/opt/cobolsharp-cache/gnucobol-3.2.tar.xz")
    target = REPO / "tests" / "external" / cached.name
    out: list[str] = []

    def captured(cmd: list[str], cwd: pathlib.Path) -> int:
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180,
                           cwd=str(cwd))
        out.append((r.stdout or "") + (r.stderr or ""))
        return r.returncode

    try:
        if cached.exists() and not target.exists():
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(cached, target)
        if external_corpus.present(REPO):
            status = "present"
        elif not external_corpus.ensure(REPO, lambda _: None, captured):
            status = "ok"
        else:
            text = "".join(out)
            status = (f"{external_corpus.FETCH_FAILED}: "
                      + next((l for l in text.splitlines() if l.startswith("FETCH FAILED")), text.strip()[-400:]))
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        status = f"FAILED: {exc}"
    return f"cloud session: GnuCOBOL corpus fetch → {status}\n"


def probe() -> str:
    if not PROBE.exists():
        return f"session-probe.ps1 not found at {PROBE}"
    try:
        # -Command (not -File) so the console encoding can be set first: the probe emits '·' and '⚠',
        # which the default OEM code page turns into mojibake.
        r = subprocess.run(
            ["pwsh", "-NoProfile", "-Command",
             f"[Console]::OutputEncoding=[Text.Encoding]::UTF8; & '{PROBE.as_posix()}'"],
            capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=240, cwd=str(REPO),   # the branch survey (prune_worktrees.py, ~20-40 s) runs inside it; the hook allows 300
        )
        return ((r.stdout or "") + (r.stderr or "")).strip() or "session-probe produced no output"
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        return f"session-probe failed: {exc}"


def hook_payload() -> dict:
    """The SessionStart payload on stdin (session_id, transcript_path, source, …); {} when there is none (a hand run)."""
    try:
        if sys.stdin is None or sys.stdin.isatty():
            return {}
        data = json.loads(sys.stdin.read() or "{}")
        return data if isinstance(data, dict) else {}
    except (OSError, ValueError):
        return {}


def tooling(payload: dict) -> str:
    # R49: every adopted capability is checked at session start; what needs the owner becomes a question, not a skip.
    # The payload carries what only the session knows, e.g. its permission mode (kb/Work PB2601).
    try:
        sys.dont_write_bytecode = True   # no __pycache__ litter in the tree on every session start
        sys.path.insert(0, str(pathlib.Path(__file__).parent))
        import tooling_check
        return "\n\n" + tooling_check.check(payload)
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        return f"\n\nTOOLING check failed: {exc} — ASK-OWNER: run python scripts/hooks/tooling_check.py and report"


def session_lane() -> str:
    """'operator' | 'mythos' (the two attended lanes, by mailbox.py's ONE lane rule), 'loop:<unit type>' for a loop unit
    (the supervisor exports COBOL_LOOP_UNIT), 'solo' where no coordination directory exists (a cloud session, a clone
    elsewhere), 'unknown' when the rule cannot be read — never an exception (a hook must never break the session)."""
    try:
        sys.path.insert(0, str(REPO / "scripts" / "orchestrator"))
        import coord
        import mailbox
        if os.environ.get(coord.LOOP_UNIT_ENV):
            return f"loop:{os.environ[coord.LOOP_UNIT_ENV]}"
        cdir = coord.coord_path(None)
        return mailbox.lane(cdir) or "unknown" if cdir.is_dir() else "solo"
    except Exception:  # noqa: BLE001
        return "unknown"


# Owner 2026-10-08: "Be sure to setup all session start and resume processes to always use these skills, as
# appropriate". A resumed attended session (`claude --resume <id>`) gets no restart prompt, only this hook, so the hook
# is the one place that names the skills for every start, resume, clear and compaction, in every lane.
WORK_SKILLS = ("Then, as the work arises: `spec-lookup` before any COBOL semantics, syntax or output question · "
               "`land-a-fix` for a spec-derived fix · `gate` before every commit or merge · `kb-sync` after a landing · "
               "`review` for a review · `architecture-review` for R2/R3 work · `new-construct` for a construct, grammar "
               "rule or reserved word. A project skill that names a base (`brent-tools:…`) loads that base first.")
LANE_SKILLS = {
    "operator": "Lane OPERATOR (the config dir operator-session.json names): invoke `workstream` before you supervise "
                "or steer the loop, land, or dispatch anything; act on the probe's `mailbox` line (arm this session's "
                "inbox watcher).",
    "mythos": "Lane MYTHOS: owner-assigned Mythos tasks only, each with its approval line; invoke `workstream` before any "
              "dispatch; act on the probe's `mailbox` line (arm this session's inbox watcher).",
    "solo": "No two-lane coordination directory here: invoke `workstream` before any dispatch.",
    "unknown": "Lane unknown (the lane rule could not be read): invoke `workstream` before any dispatch; check "
               "`python scripts/orchestrator/mailbox.py status`.",
}


def skills_block(source: str, lane: str) -> str:
    """The SKILLS block that heads the hook's context: what to load now, by how the session started and its lane."""
    head = "SKILLS (owner 2026-10-08: every session start and resume loads them)."
    if lane.startswith("loop:"):
        unit = lane.split(":", 1)[1]
        if unit == "meter":
            return f"{head} Loop unit `meter`: your unit prompt skips `session-start`; follow it.\n\n"
        return (f"{head} Loop unit `{unit}`: 1. invoke `session-start` first (your unit prompt says the same); 2. the "
                f"skills your unit prompt names (`workstream` for land and wave units). Arm no inbox watcher.\n\n")
    lane_line = LANE_SKILLS.get(lane, LANE_SKILLS["unknown"])
    if source == "compact":
        return (f"{head} Context was COMPACTED: the skills invoked before it stay in force (the harness lists them); "
                f"re-invoke one before applying it if its text is no longer in your context. {lane_line}\n\n")
    return f"{head}\n1. Invoke `session-start` NOW, before any other step.\n2. {lane_line}\n3. {WORK_SKILLS}\n\n"


def _self_test() -> int:
    fails: list[str] = []

    def check(what: str, ok: bool) -> None:
        if not ok:
            fails.append(what)

    op = skills_block("resume", "operator")
    check("a resumed operator invokes session-start first", "1. Invoke `session-start` NOW" in op)
    check("the operator lane loads workstream and arms its watcher", "`workstream`" in op and "watcher" in op)
    check("the work skills are named", all(s in op for s in ("`gate`", "`spec-lookup`", "`land-a-fix`", "`kb-sync`")))
    check("a startup and a clear read the same as a resume",
          skills_block("startup", "operator") == op == skills_block("clear", "operator"))
    my = skills_block("startup", "mythos")
    check("the Mythos lane names its approval rule and its watcher", "approval line" in my and "watcher" in my)
    cp = skills_block("compact", "operator")
    check("a compaction keeps the loaded skills instead of restarting", "stay in force" in cp and "NOW" not in cp)
    check("a meter unit skips session-start, as its prompt says", "skips `session-start`" in skills_block("startup", "loop:meter"))
    wave = skills_block("startup", "loop:wave")
    check("a wave unit loads session-start then workstream, and arms no watcher",
          "`session-start` first" in wave and "`workstream`" in wave and "Arm no inbox watcher" in wave)
    check("a solo session has no mailbox to watch", "watcher" not in skills_block("startup", "solo"))
    check("an unreadable lane falls back without a watcher claim", "mailbox.py status" in skills_block("resume", "bogus"))
    check("session_lane never raises", isinstance(session_lane(), str))
    for f in fails:
        print("FAIL:", f)
    print(f"=== SESSION-START HOOK SELF-TEST: {'GREEN' if not fails else f'RED ({len(fails)})'} ===")
    return 1 if fails else 0


def main() -> None:
    payload = hook_payload()   # read FIRST: the probe's child processes inherit stdin and could consume it
    text = skills_block(str(payload.get("source") or "startup"), session_lane()) + init_cloud_submodules() + \
        local_skills_hint() + (
            "Mechanical live state (scripts/session-probe.ps1). Plan §0 is the live-state SSOT; "
            "this is the computed half.\n\n" + probe()
        ) + tooling(payload)
    json.dump(
        {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}},
        sys.stdout,
    )


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(_self_test())
    main()
