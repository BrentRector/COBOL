"""The orchestrator's coordination directory: one place that names it, and the atomic JSON write every tool uses.

The directory lives OUTSIDE every git worktree (docs/rearchitecture/DESIGN-orchestrator-loop.md section 2), so no
branch switch, worktree removal or `git clean` can lose a reservation or a meter reading. Default `E:\\COBOL-coord`;
the environment variable `COBOL_COORD_DIR` overrides it (the supervisor exports it, and the tests point it at a temp
directory).
"""
from __future__ import annotations

import json
import os
import pathlib
from typing import Any

DEFAULT = r"E:\COBOL-coord"
ENV = "COBOL_COORD_DIR"
REPO = pathlib.Path(__file__).resolve().parents[2]


RULES_PATH = pathlib.Path(__file__).resolve().with_name("model_rules.json")


def rules(path: pathlib.Path | None = None) -> dict[str, Any]:
    """model_rules.json: the routing, cost and quota constants budget.py and plan_wave.py share."""
    return json.loads((path or RULES_PATH).read_text(encoding="utf-8"))


def family(model: str) -> str:
    """A model id or alias ('claude-sonnet-5-5', 'sonnet', 'claude-opus-5-5[1m]') -> 'sonnet' | 'opus' | 'haiku' | ''."""
    m = (model or "").lower()
    return next((f for f in ("opus", "sonnet", "haiku", "fable") if f in m), "")


def coord_dir(override: str | None = None) -> pathlib.Path:
    d = pathlib.Path(override or os.environ.get(ENV) or DEFAULT)
    d.mkdir(parents=True, exist_ok=True)
    return d


def read_json(path: pathlib.Path, default: Any) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return default


def write_json(path: pathlib.Path, value: Any) -> None:
    """Write to a sibling temp file and rename over the target, so a crash never leaves a half-written file."""
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    tmp.write_text(json.dumps(value, indent=1) + "\n", encoding="utf-8")
    os.replace(tmp, path)
