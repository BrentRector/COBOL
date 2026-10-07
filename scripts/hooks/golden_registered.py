#!/usr/bin/env python3
"""PostToolUse hook — a conformance golden that is not in its manifest is flagged immediately.

A positive golden must be listed in its edition directory's manifest.json ("enabled" or "pending"); a negative
golden must be listed in tests/conformance/negative/manifest.json. An unregistered golden never runs AND fails the
manifest-integrity test — but only at the COMPREHENSIVE gate, never at the per-commit wave-local run. That delay is
why it has bitten twice in one session.

Read-only. Silent when the golden is registered or the file is unrelated.
"""
import json
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
CORPUS = REPO / "tests" / "conformance"
GOLDEN_SUFFIXES = {".cob", ".out", ".err"}


def bail() -> None:
    sys.exit(0)


try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    bail()

ti = data.get("tool_input") or {}
tr = data.get("tool_response") or {}
raw = tr.get("filePath") or ti.get("file_path") or ""
if not raw:
    bail()

path = pathlib.Path(raw)
if path.suffix not in GOLDEN_SUFFIXES:
    bail()

try:
    rel = path.resolve().relative_to(CORPUS.resolve())
except Exception:  # noqa: BLE001 - not under tests/conformance
    bail()

if len(rel.parts) < 2:
    bail()

subdir = rel.parts[0]
manifest = CORPUS / subdir / "manifest.json"
if not manifest.exists():
    bail()

try:
    doc = json.loads(manifest.read_text(encoding="utf-8"))
except Exception:  # noqa: BLE001
    bail()

listed = set(doc.get("enabled") or []) | set(doc.get("pending") or [])
name = path.stem
if name in listed:
    bail()

where = "tests/conformance/negative/manifest.json" if subdir == "negative" else f"tests/conformance/{subdir}/manifest.json"
json.dump(
    {
        "hookSpecificOutput": {
            "hookEventName": "PostToolUse",
            "additionalContext": (
                f"Golden '{name}' is NOT listed in {where}. Register it in the SAME commit (add to \"enabled\", or "
                f"\"pending\" if its feature has not landed) — an unregistered golden never runs AND fails the "
                f"manifest-integrity test at the comprehensive gate, long after the wave-local gate said green. "
                f"Note: negative goldens use their own manifest, separate from the per-edition ones."
            ),
        }
    },
    sys.stdout,
)
