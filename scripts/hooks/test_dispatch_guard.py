#!/usr/bin/env python3
"""Self-test for dispatch_guard.py — a fleet dispatch without the workstream skill, or an implementer/lander dispatch
without a checked brief, is blocked; a compliant dispatch and every other role pass.
Run: python scripts/hooks/test_dispatch_guard.py   (needs the tools/claude-skills submodule, like check_practices.py;
CI's fleet-practices step runs it after `git submodule update --init tools/claude-skills`.)"""
import json
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


def blocked(payload: dict) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps(payload), capture_output=True, text=True,
                       timeout=90)
    return r.returncode == 2


fails = [(n, e) for n, p, e in CASES if blocked(p) != e]
for n, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {n}")
print(f"dispatch guard self-test: {len(CASES) - len(fails)}/{len(CASES)} cases OK")
sys.exit(1 if fails else 0)
