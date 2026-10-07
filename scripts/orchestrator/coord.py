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
import re
from typing import Any

DEFAULT = r"E:\COBOL-coord"
ENV = "COBOL_COORD_DIR"
REPO = pathlib.Path(__file__).resolve().parents[2]


RULES_PATH = pathlib.Path(__file__).resolve().with_name("model_rules.json")


def rules(path: pathlib.Path | None = None) -> dict[str, Any]:
    """model_rules.json: the routing, cost and quota constants budget.py and plan_wave.py share."""
    return json.loads((path or RULES_PATH).read_text(encoding="utf-8"))


def family(model: str) -> str:
    """A model id or alias ('claude-sonnet-5-5', 'sonnet', 'claude-opus-5-5[1m]', 'claude-mythos-5-1') -> 'sonnet' | 'opus' |
    'haiku' | 'fable' | 'mythos' | ''. Fable and Mythos are priced at their own rate (model_rules.json; kb/Work R69)."""
    m = (model or "").lower()
    return next((f for f in ("opus", "sonnet", "haiku", "fable", "mythos") if f in m), "")


def coord_path(override: str | None = None) -> pathlib.Path:
    """The coordination directory's path, WITHOUT creating it (a reader on a machine without one stays read-only)."""
    return pathlib.Path(override or os.environ.get(ENV) or DEFAULT)


def coord_dir(override: str | None = None) -> pathlib.Path:
    """The coordination directory, created when missing (the tools that write into it)."""
    d = coord_path(override)
    d.mkdir(parents=True, exist_ok=True)
    return d


# THE GRACEFUL-STOP FILES (kb/Work PB2483, design section 4.6). A stop is SCOPED: one session's stop must never abort
# another session's agents (2026-10-07 13:26, the loop's wind-down split the Mythos session's refuter).
#   global  <coord>\scratch\STOP           the OWNER's stop: every agent of every session and the loop obey it
#   fleet   <scratch>\STOP-<scope>          ONE fleet's stop: only the agents whose dispatch names it obey it
# Every dispatch names both (make_dispatch_specs.py, wf_rolling_wave.js args); the loop's fleet uses the scope `loop`.
STOP_SCOPE = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]*")


def global_stop(override: str | None = None) -> pathlib.Path:
    """The owner's global stop file. Never creates the directory (a check on a machine without one stays read-only)."""
    return coord_path(override) / "scratch" / "STOP"


def fleet_stop(scratch: pathlib.Path | str, scope: str) -> pathlib.Path:
    """One fleet's own stop file in its scratch directory: `STOP-<scope>` (a wave `w1033`, the loop's `loop`)."""
    if not STOP_SCOPE.fullmatch(scope):
        raise ValueError(f"stop scope {scope!r} is not a plain name ([A-Za-z0-9._-])")
    return pathlib.Path(scratch) / f"STOP-{scope}"


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
