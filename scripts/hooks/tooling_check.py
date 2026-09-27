#!/usr/bin/env python3
"""The R49 tooling readiness check, run by session_start.py at EVERY session start (and runnable by hand).

Owner, 2026-09-25: "It should not require manual intervention to use these. If my explicit permission for something is
required, automatic use of such a feature should trigger a query to me, not silently failure to use it."

So each adopted capability is CHECKED here, never assumed, and every line is one of:
  OK        present and in use
  REPAIRED  was missing and needs no permission, so this check fixed it (e.g. started the telemetry sink)
  ASK-OWNER needs the owner's permission or an owner-only action — the session MUST ask (AskUserQuestion), not skip it
  N/A       does not apply in this environment (e.g. a cloud session has no local telemetry sink)

    python scripts/hooks/tooling_check.py      # prints the block
"""
import datetime
import json
import os
import pathlib
import shutil
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
HOME = pathlib.Path.home()
CLOUD = os.environ.get("CLAUDE_CODE_REMOTE") == "true"
ROLES = ["cobol-implementer", "cobol-lander", "cobol-refuter", "cobol-adjudicator", "cobol-clerk", "cobol-locator"]
SKILL_DOCTOR_STAMP = HOME / ".claude" / "cobolsharp-skill-doctor.stamp"
SKILL_DOCTOR_DAYS = 7


def load(p):
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except Exception:  # noqa: BLE001
        return {}


def check():
    out = []
    add = lambda status, what, detail="": out.append(f"  {status:9} {what}" + (f" — {detail}" if detail else ""))

    add("OK", "Workflow tool", "STANDING owner opt-in (R49): run fleets through Workflow without asking")

    missing = [r for r in ROLES if not (REPO / ".claude" / "agents" / f"{r}.md").exists()]
    add("OK" if not missing else "ASK-OWNER", "role agents (.claude/agents)",
        "workflows select them with agentType" if not missing else f"missing {missing} — restore from git before dispatching")

    proj = load(REPO / ".claude" / "settings.json")
    hooks = json.dumps(proj.get("hooks", {}))
    add("OK" if "forbidden_commands.py" in hooks else "ASK-OWNER", "guard hook (forbidden_commands.py)",
        "" if "forbidden_commands.py" in hooks else "not registered in .claude/settings.json — restore from git")

    if CLOUD:
        add("N/A", "telemetry sink", "cloud session")
    else:
        # Claude Code IGNORES telemetry-enabling variables in project settings files (they may only turn it off),
        # so the switch lives in the USER settings, ~/.claude/settings.json.
        env = load(HOME / ".claude" / "settings.json").get("env", {})
        if env.get("CLAUDE_CODE_ENABLE_TELEMETRY") != "1":
            add("ASK-OWNER", "telemetry", "not enabled in ~/.claude/settings.json on this machine — ask to enable "
                "(CLAUDE_CODE_ENABLE_TELEMETRY=1, OTLP http/json → 127.0.0.1:4318); effective from the next session")
        else:
            sink = REPO / "scripts" / "telemetry" / "otlp_sink.py"
            try:
                import socket
                with socket.socket() as s:
                    s.settimeout(0.5)
                    up = s.connect_ex(("127.0.0.1", 4318)) == 0
                if not up:
                    subprocess.run([sys.executable, str(sink), "--ensure"], timeout=20)
                add("OK" if up else "REPAIRED", "telemetry sink",
                    "usage: python scripts/telemetry/usage_report.py" + ("" if up else " (sink was down; started)"))
            except Exception as exc:  # noqa: BLE001
                add("ASK-OWNER", "telemetry sink", f"could not start: {exc}")

    user = load(HOME / ".claude" / "settings.json")
    lsp_plugin = any(k.startswith("csharp-lsp@") and v for k, v in (user.get("enabledPlugins") or {}).items())
    ls = shutil.which("csharp-ls") or next((str(p) for p in [HOME / ".dotnet" / "tools" / "csharp-ls.exe",
                                                             HOME / ".dotnet" / "tools" / "csharp-ls"] if p.exists()), None)
    if CLOUD:
        add("N/A", "C# LSP", "cloud session")
    elif lsp_plugin and ls:
        add("OK", "C# LSP (csharp-lsp + csharp-ls)", "use the LSP tool for C# definitions/references before grep (P13)")
    else:
        need = ([] if lsp_plugin else ["enable plugin csharp-lsp@claude-plugins-official"]) + \
               ([] if ls else ["install the server: dotnet tool install --global csharp-ls"])
        add("ASK-OWNER", "C# LSP", "; ".join(need) + " — ask before installing; effective from the next session")

    try:
        age = (datetime.datetime.now() - datetime.datetime.fromtimestamp(SKILL_DOCTOR_STAMP.stat().st_mtime)).days
    except OSError:
        age = None
    if CLOUD:
        add("N/A", "/skill-doctor", "cloud session")
    elif age is not None and age < SKILL_DOCTOR_DAYS:
        add("OK", "/skill-doctor", f"reviewed {age} day(s) ago")
    else:
        add("ASK-OWNER", "/skill-doctor", "owner-only interactive command, due (weekly): ask the owner to run "
            "`/skill-doctor`, prune what it flags, then touch " + SKILL_DOCTOR_STAMP.as_posix())

    asks = sum(1 for l in out if "ASK-OWNER" in l)
    head = ("TOOLING (kb/Work R49) — every line is checked, not assumed. "
            + (f"⛔ {asks} ASK-OWNER line(s): ask the owner about EACH at the start of this session "
               "(AskUserQuestion, one bare question each) — never skip one silently.\n" if asks else "all ready.\n"))
    return head + "\n".join(out) + "\n"


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    print(check())
