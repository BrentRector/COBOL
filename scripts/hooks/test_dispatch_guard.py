#!/usr/bin/env python3
"""Self-test for dispatch_guard.py — a fleet dispatch without the workstream skill, or an implementer/finisher dispatch
without a checked brief, is blocked; a compliant dispatch and every other role pass.
Run: python scripts/hooks/test_dispatch_guard.py   (needs the tools/claude-skills submodule, like check_practices.py;
CI's fleet-practices step runs it after `git submodule update --init tools/claude-skills`.)"""
import json
import os
import pathlib
import subprocess
import sys
import tempfile

HOOK = pathlib.Path(__file__).with_name("dispatch_guard.py")
REPO = pathlib.Path(__file__).resolve().parents[2]
BRIEF = REPO / ".claude" / "skills" / "workstream" / "templates" / "fix-lane-implementer-brief.md"
FINISHER = REPO / ".claude" / "skills" / "workstream" / "templates" / "finisher-brief.md"
TMP = pathlib.Path(tempfile.mkdtemp())

WITH_SKILL = TMP / "with.jsonl"
WITH_SKILL.write_text(json.dumps({"type": "tool_use", "name": "Skill", "input": {"skill": "workstream"}}) + "\n",
                      encoding="utf-8", newline="\n")
WITHOUT_SKILL = TMP / "without.jsonl"
WITHOUT_SKILL.write_text(json.dumps({"type": "tool_use", "name": "Skill", "input": {"skill": "gate"}}) + "\n",
                         encoding="utf-8", newline="\n")
BAD_SPEC = TMP / "msg-w1-a.txt"
BAD_SPEC.write_text("a hand-written brief that drops every mandatory practice\n", encoding="utf-8", newline="\n")


def agent(role: str, prompt: str, transcript: pathlib.Path) -> dict:
    return {"tool_name": "Agent", "tool_input": {"subagent_type": role, "prompt": prompt},
            "transcript_path": str(transcript)}


CASES = [
    ("implementer, skill not loaded", agent("cobol-implementer", f"Follow {BRIEF}", WITHOUT_SKILL), True),
    ("implementer, inline brief", agent("cobol-implementer", "Fix PB1 and PB2 as follows ...", WITH_SKILL), True),
    ("implementer, unchecked rendered spec", agent("cobol-implementer", f"Follow {BAD_SPEC}", WITH_SKILL), True),
    ("implementer, template brief, skill loaded", agent("cobol-implementer", f"Follow {BRIEF}", WITH_SKILL), False),
    # kb/Work PB3023: the finisher is the judgment role that lands (the lander retired with the train)
    ("finisher, skill not loaded", agent("cobol-finisher", f"Follow {FINISHER}", WITHOUT_SKILL), True),
    ("finisher, inline brief", agent("cobol-finisher", "Resolve the conflict in x.cs and land it", WITH_SKILL), True),
    ("finisher, template brief, skill loaded", agent("cobol-finisher", f"Follow {FINISHER}", WITH_SKILL), False),
    ("workflow, skill not loaded", {"tool_name": "Workflow", "tool_input": {}, "transcript_path": str(WITHOUT_SKILL)}, True),
    ("workflow, skill loaded", {"tool_name": "Workflow", "tool_input": {}, "transcript_path": str(WITH_SKILL)}, False),
    ("read-only role needs no spec", agent("cobol-refuter", "Try to overturn this verdict", WITHOUT_SKILL), False),
    ("mechanical role needs no spec", agent("cobol-clerk", "File these notes", WITHOUT_SKILL), False),
    ("other tool is not guarded", {"tool_name": "Bash", "tool_input": {"command": "ls"}}, False),
    ("no transcript cannot be judged", {"tool_name": "Workflow", "tool_input": {}}, False),
]


COORD = TMP / "coord"   # rule 0 reads STOP-UNIT there: never the real coordination directory from a test
COORD.mkdir()
# The cases choose the session kind themselves (UNIT_ENV adds COBOL_LOOP_UNIT), so the caller's own kind never leaks in:
# a gate run inside a loop unit (an enqueue unit's finisher has COBOL_LOOP_UNIT=enqueue) made the "attended session" case a loop
# unit and turned this self-test red on a correct hook (kb/Work PB2702).
ENV = {k: v for k, v in os.environ.items() if k != "COBOL_LOOP_UNIT"} | {"COBOL_COORD_DIR": str(COORD)}


def blocked(payload: dict) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps(payload), capture_output=True, text=True,
                       timeout=180, env=ENV)
    return r.returncode == 2


fails = [(n, e) for n, p, e in CASES if blocked(p) != e]
# kb/Work PB3186: the hook writes NO dispatch ledger. Its retired rule 3 recorded every note id an admitted dispatch's
# prompt (and the files it named) mentioned, with the union of their file sets; the ledger is the planner's and the spec
# renderer's alone (plan_wave.record_dispatch, through make_dispatch_specs.py).
LEDGER = COORD / "inflight-groups.json"
HAND_BRIEF = TMP / "hand-brief.md"
HAND_BRIEF.write_text("# Brief\nAuthor kb/Work PB2296 step 1.\n", encoding="utf-8", newline="\n")
WRITES_NONE = [
    ("a write-capable hand dispatch", agent("general-purpose", "Author PB2296 step 1 by hand", WITH_SKILL)),
    ("a dispatch naming only its brief file", agent("general-purpose", f"Your brief is {HAND_BRIEF}. Read it first.",
                                                     WITH_SKILL)),
    ("an admitted implementer", agent("cobol-implementer", f"Follow {BRIEF} for PB2296", WITH_SKILL)),
    ("an admitted Workflow", {"tool_name": "Workflow", "transcript_path": str(WITH_SKILL),
                              "tool_input": {"script": "wf_rolling_wave.js",
                                             "args": {"groups": [{"letter": "a", "notes": ["PB2296"]}]}}}),
]
LEDGER.unlink(missing_ok=True)
for n, p in WRITES_NONE:
    blocked(p)
    if LEDGER.exists():
        fails.append((f"{n} writes no dispatch ledger (PB3186)", False))
        LEDGER.unlink()
# rule 0 (kb/Work PB2597): in a LOOP UNIT (COBOL_LOOP_UNIT set by the supervisor) with STOP-UNIT present, no fleet
# launch: a Workflow and an implementer or finisher are refused even with the skill loaded and a checked brief; a read-only
# role still passes; an attended session (no COBOL_LOOP_UNIT) sharing the coordination directory is never refused for it
WF_OK = {"tool_name": "Workflow", "tool_input": {}, "transcript_path": str(WITH_SKILL)}


def blocked_env(payload: dict, env: dict) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps(payload), capture_output=True, text=True,
                       timeout=180, env=env)
    return r.returncode == 2


UNIT_ENV = dict(ENV, COBOL_LOOP_UNIT="wave")
STOP_CASES = [
    ("loop unit after STOP-UNIT: Workflow refused", WF_OK, UNIT_ENV, True),
    ("loop unit after STOP-UNIT: implementer refused", agent("cobol-implementer", f"Follow {BRIEF}", WITH_SKILL), UNIT_ENV, True),
    ("loop unit after STOP-UNIT: finisher refused", agent("cobol-finisher", f"Follow {FINISHER}", WITH_SKILL), UNIT_ENV, True),
    ("loop unit after STOP-UNIT: a read-only role passes", agent("cobol-refuter", "Refute this", WITH_SKILL), UNIT_ENV, False),
    ("attended session, the loop's STOP-UNIT present: Workflow passes", WF_OK, ENV, False),
]
STOP_CASES_AFTER = [("loop unit, no STOP-UNIT: Workflow passes", WF_OK, UNIT_ENV, False)]
(COORD / "STOP-UNIT").write_text("", encoding="utf-8", newline="\n")
fails += [(n, e) for n, p, env, e in STOP_CASES if blocked_env(p, env) != e]
(COORD / "STOP-UNIT").unlink()
fails += [(n, e) for n, p, env, e in STOP_CASES_AFTER if blocked_env(p, env) != e]
for n, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {n}")
TOTAL = len(CASES) + len(STOP_CASES) + len(STOP_CASES_AFTER) + len(WRITES_NONE)
print(f"dispatch guard self-test: {TOTAL - len(fails)}/{TOTAL} cases OK")
sys.exit(1 if fails else 0)
