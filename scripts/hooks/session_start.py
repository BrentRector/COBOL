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
    2026-09-24. setup-env.sh pre-caches the pinned tarball; copying it in first makes the fetch skip the download."""
    import shutil
    cached = pathlib.Path("/opt/cobolsharp-cache/gnucobol-3.2.tar.xz")
    target = REPO / "tests" / "external" / cached.name
    try:
        if cached.exists() and not target.exists():
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(cached, target)
        r = subprocess.run(
            ["pwsh", "-NoProfile", "-File", str(REPO / "scripts" / "fetch-gnucobol-tests.ps1")],
            capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180, cwd=str(REPO),
        )
        out = (r.stdout or "") + (r.stderr or "")
        status = "ok" if r.returncode == 0 else (
            f"FAILED (exit {r.returncode}): "
            + next((l for l in out.splitlines() if l.startswith("FETCH FAILED")), out.strip()[-400:]))
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
            timeout=90, cwd=str(REPO),
        )
        return ((r.stdout or "") + (r.stderr or "")).strip() or "session-probe produced no output"
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        return f"session-probe failed: {exc}"


def tooling() -> str:
    # R49: every adopted capability is checked at session start; what needs the owner becomes a question, not a skip.
    try:
        sys.dont_write_bytecode = True   # no __pycache__ litter in the tree on every session start
        sys.path.insert(0, str(pathlib.Path(__file__).parent))
        import tooling_check
        return "\n\n" + tooling_check.check()
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        return f"\n\nTOOLING check failed: {exc} — ASK-OWNER: run python scripts/hooks/tooling_check.py and report"


text = init_cloud_submodules() + local_skills_hint() + (
    "Mechanical live state (scripts/session-probe.ps1). Plan §0 is the live-state SSOT; "
    "this is the computed half.\n\n" + probe()
) + tooling()
json.dump(
    {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}},
    sys.stdout,
)
