#!/usr/bin/env python3
"""PreToolUse hook (matcher Agent|Workflow) — REFUSE a fleet dispatch unless the `workstream` skill was loaded in this
session, and refuse an implementer or lander dispatch whose brief is not a checked, rendered spec or template.

Owner, 2026-09-30, after an orchestrator hand-wrote three implementer briefs from memory and skipped
.claude/skills/workstream/SKILL.md (MANDATORY-PRACTICES O9): "Reconfigure such that all future sessions cannot skip
learning this." A sentence in CLAUDE.md or memory was already there and was skipped, so the rule is structural:

  1. Agent (cobol-implementer or cobol-lander) and Workflow calls need a Skill call for `workstream` earlier in THIS
     session's transcript (or a /workstream command). The skill is what carries the mandatory practices.
  2. An Agent call for cobol-implementer or cobol-lander must name, in its prompt, a RENDERED spec
     (`msg-w<wave>-<letter>.txt`, from make_dispatch_specs.py) or a brief under workstream/templates, and
     check_practices.py must print `=== PRACTICES CHECK: GREEN ===` over it. A hand-written inline brief is refused.

Read-only and mechanical roles (refuter, adjudicator, clerk, locator, Explore, general-purpose) pass rule 2; rule 1
applies to Workflow always. Exit 2 blocks the call and returns stderr to the agent. Anything unparseable passes (a
hook must never wedge a session).

  3. Every Agent call it ADMITS whose role can write (any role but the read-only ones), and every Workflow call it
     admits, is recorded in the dispatch ledger with the open kb/Work notes it names and their computed file sets
     (`plan_wave.py --record-dispatch`), so the restructuring planner's file-set partition sees a HAND dispatch (an
     attended Fable or Mythos author, a direct Agent call, an attended Workflow) before its agent edits a file
     (kb/Work PB2118 Draft 8, the seventh refuter's J3). The notes are read from the prompt (a Workflow's whole input:
     script and args) AND from every brief, spec or args file it names, one level deep: a prompt that names only its
     brief carries its notes there (Draft 9, the eighth refuter's K2). Recording never blocks: a failure to record
     is silent, like every other hook failure. The landing check (landing_check.py) is the guarantee behind it.
"""
import json
import pathlib
import re
import subprocess
import sys

try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    sys.exit(0)

# the harness reads hook stderr as UTF-8; Windows would otherwise encode it in the console code page
sys.stderr.reconfigure(encoding="utf-8")

REPO = pathlib.Path(__file__).resolve().parents[2]
CHECK = REPO / ".claude" / "skills" / "workstream" / "check_practices.py"
PLAN_WAVE = REPO / "scripts" / "orchestrator" / "plan_wave.py"
READ_ONLY_ROLES = {"cobol-refuter", "cobol-adjudicator", "cobol-locator", "Explore", "Plan", "claude-code-guide",
                   "statusline-setup"}
TEMPLATES = REPO / ".claude" / "skills" / "workstream" / "templates"
JUDGMENT_ROLES = {"cobol-implementer", "cobol-lander"}
LOADED = re.compile(
    r'"skill"\s*:\s*"(?:[\w-]+:)?workstream"'                     # a Skill tool call
    r'|<command-name>/?workstream</command-name>'                  # the user typed /workstream
    r'|Base directory for this skill: [^"\\\n]*workstream')        # the skill's own expansion


def block(why: str) -> None:
    print("DISPATCH GUARD: " + why, file=sys.stderr)
    sys.exit(2)


PB = re.compile(r"\bPB\d+\b")
NAMED_FILE = re.compile(r"(?:[A-Za-z]:[\\/]|/)[^\s\"'`<>|,;]+?\.(?:txt|md|json)")


def dispatched_ids(text: str) -> list[str]:
    """The kb/Work ids a dispatch names: in its own text, and in every brief, spec or args file its text names (one
    level: a prompt that says only "follow msg-w1040-a.txt" carries its notes there; the eighth refuter's K2)."""
    ids = set(PB.findall(text))
    for p in dict.fromkeys(NAMED_FILE.findall(text)):
        try:
            f = pathlib.Path(p)
            if f.is_file() and f.stat().st_size < 2_000_000:
                ids |= set(PB.findall(f.read_text(encoding="utf-8", errors="replace")))
        except OSError:
            pass
    return sorted(ids, key=lambda s: int(s[2:]))


def record_hand_dispatch(args: dict, role: str, tool: str = "Agent") -> None:
    """Rule 3: the admitted write-capable dispatch's named notes into the dispatch ledger (never blocks). An Agent
    call's text is its prompt; a Workflow call's is its whole input (its script and args: groups, specs, briefs)."""
    if role in READ_ONLY_ROLES or role.startswith("brent-tools:"):
        return
    text = (args.get("prompt", "") or "") if tool == "Agent" else json.dumps(args, ensure_ascii=False)
    ids = dispatched_ids(text.replace("\\\\", "\\"))
    if not ids:
        return
    label = f"{tool if tool != 'Agent' else (role or 'general-purpose')}: {args.get('description', '') or ''}"[:120]
    try:
        subprocess.run([sys.executable, str(PLAN_WAVE), "--record-dispatch", ",".join(ids), "--label", label],
                       capture_output=True, text=True, timeout=90, encoding="utf-8", errors="replace")
    except (OSError, subprocess.SubprocessError):
        pass


def skill_loaded(transcript: str) -> bool:
    try:
        return bool(LOADED.search(pathlib.Path(transcript).read_text(encoding="utf-8", errors="replace")))
    except OSError:
        return True  # no readable transcript: cannot judge, never wedge


tool = data.get("tool_name", "")
args = data.get("tool_input") or {}
if tool not in ("Agent", "Workflow"):
    sys.exit(0)

role = args.get("subagent_type", "") if tool == "Agent" else ""
if tool == "Agent" and role not in JUDGMENT_ROLES:
    record_hand_dispatch(args, role)
    sys.exit(0)

transcript = data.get("transcript_path") or ""
if transcript and not skill_loaded(transcript):
    block("this session has not loaded the `workstream` skill. Invoke it with the Skill tool (skill: workstream) and "
          "follow it BEFORE dispatching any implementer, lander or fleet: it carries .claude/skills/workstream/"
          "templates/MANDATORY-PRACTICES.md, the rendered-spec rule (make_dispatch_specs.py, check_practices.py), the "
          "rolling wave, the meter read and the graceful STOP. Owner 2026-09-30: no session may skip it.")

if tool == "Agent":
    prompt = args.get("prompt", "")
    paths = [pathlib.Path(p) for p in re.findall(r"(?:[A-Za-z]:[\\/]|/)[^\s\"'`<>|]+?\.(?:txt|md)", prompt)]
    specs = [p for p in paths if re.fullmatch(r"msg-w[\w-]+\.txt", p.name) and p.is_file()]
    briefs = [p for p in paths if p.is_file() and TEMPLATES.resolve() in p.resolve().parents]
    if not specs and not briefs:
        block(f"a {role} brief must point at a RENDERED spec (`msg-w<wave>-<letter>.txt` written by "
              ".claude/skills/workstream/make_dispatch_specs.py) or at a brief under .claude/skills/workstream/"
              "templates, with the substitutions in the prompt. A hand-written inline brief is how the mandatory "
              "practices get dropped (MANDATORY-PRACTICES O1, O9).")
    cmd = [sys.executable, str(CHECK)] + ([str(p) for p in specs] if specs else [])
    try:
        out = subprocess.run(cmd, capture_output=True, text=True, timeout=60, encoding="utf-8", errors="replace")
    except (OSError, subprocess.SubprocessError):
        sys.exit(0)
    if "=== PRACTICES CHECK: GREEN ===" not in out.stdout:
        block("check_practices.py is not GREEN over this brief:\n" + (out.stdout + out.stderr)[-1500:])
    record_hand_dispatch(args, role)
else:
    record_hand_dispatch(args, "", "Workflow")

sys.exit(0)
