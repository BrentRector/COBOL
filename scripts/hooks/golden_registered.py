#!/usr/bin/env python3
"""PostToolUse hook — a test written under the removed `tests/conformance/` is flagged the moment it is written.

The corpus moved into the conformance suite (kb/Work PB3177, docs/rearchitecture/DESIGN-conformance-suite.md §13
S2): a test is a program in a clause folder of `conformance-suite/cases/` (or `wiseowl/cases/`) whose header the
generator reads, and there is no manifest to register it in. A file under `tests/conformance/` would never run, so the
generator refuses it (`conformance-suite/harness/generate.py --check`, a gate audit that runs before the build); this
hook says so at the write instead of at the gate, naming where the test belongs. The refusal is the generator's; this
is only its early echo.

Read-only. Silent for every other file.
"""
import json
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
REMOVED = REPO / "tests" / "conformance"


def main() -> int:
    try:
        data = json.load(sys.stdin)
    except Exception:  # noqa: BLE001 - not a hook payload: nothing to say
        return 0
    ti = data.get("tool_input") or {}
    tr = data.get("tool_response") or {}
    raw = tr.get("filePath") or ti.get("file_path") or ""
    if not raw:
        return 0
    try:
        rel = pathlib.Path(raw).resolve().relative_to(REMOVED.resolve())
    except ValueError:
        return 0
    json.dump({"hookSpecificOutput": {"hookEventName": "PostToolUse", "additionalContext": (
        f"tests/conformance/{rel.as_posix()} is under the REMOVED corpus tree (kb/Work PB3177): it never runs, and "
        f"`python conformance-suite/harness/generate.py --check` (a gate audit) refuses it. Move the test into "
        f"conformance-suite/cases/<ISO clause>-<Name>/ with its metadata header (conformance-suite/README.md, "
        f"'Adding a test'); a negative is a refusal entry in conformance-suite/wiseowl/diagnostics.json.")}},
        sys.stdout)
    return 0


if __name__ == "__main__":
    sys.exit(main())
