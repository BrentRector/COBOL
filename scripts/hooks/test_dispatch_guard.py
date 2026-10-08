#!/usr/bin/env python3
"""Self-test for dispatch_guard.py — a fleet dispatch without the workstream skill, or an implementer/lander dispatch
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
TMP = pathlib.Path(tempfile.mkdtemp())

WITH_SKILL = TMP / "with.jsonl"
WITH_SKILL.write_text(json.dumps({"type": "tool_use", "name": "Skill", "input": {"skill": "workstream"}}) + "\n",
                      encoding="utf-8")
WITHOUT_SKILL = TMP / "without.jsonl"
WITHOUT_SKILL.write_text(json.dumps({"type": "tool_use", "name": "Skill", "input": {"skill": "gate"}}) + "\n",
                         encoding="utf-8")
BAD_SPEC = TMP / "msg-w1-a.txt"
BAD_SPEC.write_text("a hand-written brief that drops every mandatory practice\n", encoding="utf-8")


def agent(role: str, prompt: str, transcript: pathlib.Path) -> dict:
    return {"tool_name": "Agent", "tool_input": {"subagent_type": role, "prompt": prompt},
            "transcript_path": str(transcript)}


CASES = [
    ("implementer, skill not loaded", agent("cobol-implementer", f"Follow {BRIEF}", WITHOUT_SKILL), True),
    ("implementer, inline brief", agent("cobol-implementer", "Fix PB1 and PB2 as follows ...", WITH_SKILL), True),
    ("implementer, unchecked rendered spec", agent("cobol-implementer", f"Follow {BAD_SPEC}", WITH_SKILL), True),
    ("implementer, template brief, skill loaded", agent("cobol-implementer", f"Follow {BRIEF}", WITH_SKILL), False),
    ("lander, skill not loaded", agent("cobol-lander", f"Follow {BRIEF}", WITHOUT_SKILL), True),
    ("workflow, skill not loaded", {"tool_name": "Workflow", "tool_input": {}, "transcript_path": str(WITHOUT_SKILL)}, True),
    ("workflow, skill loaded", {"tool_name": "Workflow", "tool_input": {}, "transcript_path": str(WITH_SKILL)}, False),
    ("read-only role needs no spec", agent("cobol-refuter", "Try to overturn this verdict", WITHOUT_SKILL), False),
    ("mechanical role needs no spec", agent("cobol-clerk", "File these notes", WITHOUT_SKILL), False),
    ("other tool is not guarded", {"tool_name": "Bash", "tool_input": {"command": "ls"}}, False),
    ("no transcript cannot be judged", {"tool_name": "Workflow", "tool_input": {}}, False),
]


COORD = TMP / "coord"   # rule 3 writes the dispatch ledger: never the real coordination directory from a test
COORD.mkdir()
ENV = dict(os.environ, COBOL_COORD_DIR=str(COORD))


def blocked(payload: dict) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps(payload), capture_output=True, text=True,
                       timeout=180, env=ENV)
    return r.returncode == 2


fails = [(n, e) for n, p, e in CASES if blocked(p) != e]
# rule 3: an admitted hand dispatch that can write is in the dispatch ledger with its open note; a read-only one is not
LEDGER = COORD / "inflight-groups.json"
LEDGER.unlink(missing_ok=True)
blocked(agent("cobol-refuter", "Refute PB2296", WITH_SKILL))
if LEDGER.exists() and "PB2296" in LEDGER.read_text(encoding="utf-8"):
    fails.append(("a read-only dispatch is not recorded", False))
blocked(agent("general-purpose", "Author PB2296 step 1 by hand", WITH_SKILL))
rec = json.loads(LEDGER.read_text(encoding="utf-8")) if LEDGER.exists() else {"groups": []}
if not any(g.get("hand") and g.get("notes") == ["PB2296"] and g.get("files") for g in rec["groups"]):
    fails.append(("a hand dispatch that can write is recorded with its note's file set", False))
# rule 3, Draft 9 (the eighth refuter's K2): an admitted Workflow is recorded from its args, and a dispatch whose
# prompt names only its brief file is recorded from the notes the brief names
def recorded(payload: dict) -> bool:
    LEDGER.unlink(missing_ok=True)
    blocked(payload)
    rec = json.loads(LEDGER.read_text(encoding="utf-8")) if LEDGER.exists() else {"groups": []}
    return any(g.get("hand") and "PB2296" in g.get("notes", []) and g.get("files") for g in rec["groups"])


if not recorded({"tool_name": "Workflow", "transcript_path": str(WITH_SKILL),
                 "tool_input": {"script": "wf_rolling_wave.js", "args": {"groups": [{"letter": "a", "notes": ["PB2296"]}]}}}):
    fails.append(("an admitted Workflow is recorded with the notes its args name", False))
HAND_BRIEF = TMP / "hand-brief.md"
HAND_BRIEF.write_text("# Brief\nAuthor kb/Work PB2296 step 1.\n", encoding="utf-8")
if not recorded(agent("general-purpose", f"Your brief is {HAND_BRIEF}. Read it first.", WITH_SKILL)):
    fails.append(("a dispatch naming only its brief file is recorded with the brief's notes", False))
for n, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {n}")
print(f"dispatch guard self-test: {len(CASES) + 4 - len(fails)}/{len(CASES) + 4} cases OK")
sys.exit(1 if fails else 0)
