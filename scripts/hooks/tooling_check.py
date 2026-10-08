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
import json
import os
import pathlib
import shutil
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts" / "orchestrator"))
import account  # noqa: E402  (the one resolver of Claude's config dir, kb/Work PB2479)
import coord  # noqa: E402

HOME = pathlib.Path.home()
CONFIG = account.config_dir()   # THIS session's account: its user settings, skills and plugins live here
CLOUD = os.environ.get("CLAUDE_CODE_REMOTE") == "true"
ROLES = ["cobol-implementer", "cobol-lander", "cobol-refuter", "cobol-adjudicator", "cobol-clerk", "cobol-locator",
         "cobol-reviewer"]


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
        # so the switch lives in the account's USER settings, <config dir>/settings.json.
        env = load(CONFIG / "settings.json").get("env", {})
        if env.get("CLAUDE_CODE_ENABLE_TELEMETRY") != "1":
            add("ASK-OWNER", "telemetry", f"not enabled in {CONFIG / 'settings.json'} for this account — ask to enable "
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

    # PB2480: a named account's config dir carries its seed (model_rules.json accounts.seed: settings, plugins, skills,
    # the shared memory junction, the Claude in Chrome flags). Seeding rewrites its .claude.json, which the running
    # session also writes, so the check asks rather than repairs.
    rules = coord.rules()
    try:
        acct = account.current(rules)
    except account.UnknownAccount as exc:
        acct = None
        if not CLOUD:
            add("ASK-OWNER", "Claude account", f"{exc} — ask the owner which account this is")
    if CLOUD:
        add("N/A", "Claude account seed", "cloud session")
    elif acct is not None:
        missing = account.seed_problems(acct, rules, REPO)
        add("OK" if not missing else "ASK-OWNER", f"Claude account seed ({acct.name})",
            ("the default account, the source of every seed" if not acct.explicit else f"{CONFIG} is seeded")
            if not missing else f"{'; '.join(missing)} — ask the owner to run `pwsh scripts/account-profile.ps1 "
            f"-ConfigDir {CONFIG}` with no session of that account running; effective from the next session")

    user = load(CONFIG / "settings.json")
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

    # The weekly review covers a SKILL SET, so a review under any account whose skill set contains this account's
    # counts (account.skill_review, kb/Work PB2598): a freshly seeded account is not asked again for its source's review.
    if CLOUD:
        add("N/A", "/skill-doctor", "cloud session")
    elif acct is None:
        add("N/A", "/skill-doctor", "the account is unknown (the Claude account line above asks which it is)")
    elif (review := account.skill_review(acct, rules)) is not None:
        add("OK", "/skill-doctor", f"reviewed {review.age_days} day(s) ago"
            + ("" if review.by == acct.name else f" under {review.by}, whose skill set contains this account's"))
    else:
        add("ASK-OWNER", "/skill-doctor", f"owner-only interactive command, due (weekly; no review in "
            f"{account.SKILL_REVIEW_DAYS} days covers this account's skill set): ask the owner to run `/skill-doctor`, "
            f"prune what it flags, then touch {(acct.config_dir / account.SKILL_REVIEW_STAMP).as_posix()}")

    asks = sum(1 for l in out if "ASK-OWNER" in l)
    head = ("TOOLING (kb/Work R49) — every line is checked, not assumed. "
            + (f"⛔ {asks} ASK-OWNER line(s): ask the owner about EACH at the start of this session "
               "(AskUserQuestion, one bare question each) — never skip one silently.\n" if asks else "all ready.\n"))
    return head + "\n".join(out) + "\n"


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    print(check())
