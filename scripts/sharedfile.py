#!/usr/bin/env python3
"""sharedfile.py — THE ONE WINDOWS SHARE-RETRY RULE for files several processes read and replace (kb/Work PB2564).

Windows refuses to replace, remove or open a file that another process is opening, reading or replacing at that
instant, because Python opens files without FILE_SHARE_DELETE: a reader can make a writer's `os.replace` fail with
WinError 5 and an `unlink` fail with WinError 32 (measured by wave 1037 group P, kb/Work PB2537, on the landing lease).
The refusal lasts milliseconds, so every read, replace and remove of a shared file retries a `PermissionError` briefly,
and a refusal that outlasts the retries is raised, never swallowed.

The callers: the orchestrator's coordination files (`scripts/orchestrator/coord.py`: `read_json`, `write_json`, the
short mutex's release; `landing_lease.py`'s release) and the gate settings every gate polls each second
(`scripts/gate_slot.py`, kb/Work PB2514). Both used to carry their own copy of this loop; one module now holds it, and
`--self-test` fails on a second `PermissionError` retry loop around a file operation anywhere under `scripts/`.

    python scripts/sharedfile.py --self-test
"""
from __future__ import annotations

import ast
import os
import subprocess
import sys
import tempfile
import threading
import time
from pathlib import Path
from typing import Callable, TypeVar

T = TypeVar("T")

#: How often, and how long apart, a refused operation is tried: 100 x 20 ms, two seconds at most.
RETRIES, PAUSE_S = 100, 0.02


def retry(fn: Callable[[], T]) -> T:
    """`fn()`, retried while Windows refuses it with a `PermissionError`; the last refusal is raised."""
    for attempt in range(RETRIES):
        try:
            return fn()
        except PermissionError:
            if attempt == RETRIES - 1:
                raise
            time.sleep(PAUSE_S)
    raise AssertionError("unreachable: the last attempt returns or raises")


def read_text(path: Path) -> str | None:
    """The file's text (UTF-8), or None when it does not exist."""
    try:
        return retry(lambda: path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return None


def replace_text(path: Path, text: str) -> None:
    """Write `text` to a sibling temp file and rename it over `path`, so a crash never leaves a half-written file and
    a reader sees the old text or the new one. A rename refused past the retries removes the temp file and raises.
    The temp file is named by process AND thread, so two threads of one process replacing the same file never write
    one temp file (the self-test runner's workers record passes from threads, kb/Work PB2914)."""
    tmp = path.with_name(f"{path.name}.{os.getpid()}.{threading.get_ident()}.tmp")
    tmp.write_text(text, encoding="utf-8")
    try:
        retry(lambda: os.replace(tmp, path))
    except BaseException:
        tmp.unlink(missing_ok=True)
        raise


def remove(path: Path, *, missing_ok: bool) -> None:
    """Delete `path`. A missing file is an error unless `missing_ok`."""
    try:
        retry(path.unlink)
    except FileNotFoundError:
        if not missing_ok:
            raise


# ── the drift check: no second copy of the loop ──────────────────────────────────────────────────────────────────

#: The file operations a share refusal hits: methods (`os.replace`, `Path.unlink`, …) and the builtin `open`. A loop
#: that retries one of them on `PermissionError` is a copy of `retry`. `os.open` is not one: an O_EXCL lock create
#: waits on its lock's own deadline (`coord.locked`, `gate_slot._Mutex`), a different rule with a different bound.
FILE_METHODS = frozenset({"replace", "rename", "remove", "unlink", "read_text", "write_text", "read_bytes",
                          "write_bytes"})
FILE_FUNCTIONS = frozenset({"open"})


def _catches_permission_error(handler: ast.ExceptHandler) -> bool:
    names = handler.type.elts if isinstance(handler.type, ast.Tuple) else [handler.type]
    return any(isinstance(n, ast.Name) and n.id == "PermissionError" for n in names)


def _calls_file_op(nodes: list[ast.stmt]) -> bool:
    return any(isinstance(n, ast.Call) and (
        (isinstance(n.func, ast.Attribute) and n.func.attr in FILE_METHODS)
        or (isinstance(n.func, ast.Name) and n.func.id in FILE_FUNCTIONS))
        for stmt in nodes for n in ast.walk(stmt))


def retry_loops(source: str) -> list[int]:
    """The line of every loop in `source` that retries a file operation on `PermissionError`: a `for`/`while` whose
    body holds a `try` that calls one of FILE_METHODS or FILE_FUNCTIONS and handles `PermissionError`."""
    try:
        tree = ast.parse(source)
    except SyntaxError:
        return []
    found = []
    for loop in ast.walk(tree):
        if not isinstance(loop, (ast.For, ast.While)):
            continue
        for node in ast.walk(loop):
            if isinstance(node, ast.Try) and _calls_file_op(node.body) and any(
                    _catches_permission_error(h) for h in node.handlers if h.type is not None):
                found.append(loop.lineno)
                break
    return found


def second_copies(repo: Path) -> list[str]:
    """`path:line` of every share-retry loop under scripts/ outside this module (tracked files only)."""
    out = subprocess.run(["git", "ls-files", "-z", "--", "scripts"], cwd=repo, check=True, capture_output=True).stdout
    me = Path(__file__).resolve()
    hits = []
    for rel in sorted(p for p in out.decode("utf-8").split("\0") if p.endswith(".py")):
        path = repo / rel
        if path.resolve() == me:
            continue
        try:
            source = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError) as e:
            hits.append(f"{rel}: unreadable, so unchecked ({e})")  # a file the scan cannot read is a finding
            continue
        hits += [f"{rel}:{line}" for line in retry_loops(source)]
    return hits


# ── the self-test ─────────────────────────────────────────────────────────────────────────────────────────────────


def self_test() -> int:
    global RETRIES, PAUSE_S
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        print(f"  {'ok  ' if got == want else 'FAIL'} {name}" + ("" if got == want else f": got {got!r}, want {want!r}"))
        if got != want:
            fails.append(name)

    def refused(times: int, value: str = "done"):
        calls = {"n": 0}

        def fn():
            calls["n"] += 1
            if calls["n"] <= times:
                raise PermissionError(13, "planted share refusal")
            return value
        return fn, calls

    saved = RETRIES, PAUSE_S
    RETRIES, PAUSE_S = 5, 0.0
    try:
        fn, calls = refused(4)
        check("arm: a refusal shorter than the retries is retried until the call succeeds", (retry(fn), calls["n"]),
              ("done", 5))
        fn, calls = refused(5)
        try:
            retry(fn)
            check("arm: a refusal that outlasts the retries is raised", "returned", "raised")
        except PermissionError:
            check("arm: a refusal that outlasts the retries is raised", calls["n"], 5)
        boom = {"n": 0}

        def other():
            boom["n"] += 1
            raise FileExistsError("not a share refusal")
        try:
            retry(other)
        except FileExistsError:
            pass
        check("arm: any other error is raised at once, never retried", boom["n"], 1)

        with tempfile.TemporaryDirectory() as d:
            root = Path(d)
            target = root / "settings.json"
            check("arm: read_text of a missing file is None", read_text(target), None)
            replace_text(target, "one\n")
            replace_text(target, "two\n")
            check("arm: replace_text replaces the text and leaves no temp file",
                  (read_text(target), sorted(p.name for p in root.iterdir())), ("two\n", ["settings.json"]))
            real_replace = os.replace
            os.replace = lambda *a, **k: (_ for _ in ()).throw(PermissionError(5, "planted: held open"))
            try:
                replace_text(target, "three\n")
                check("arm: a replace refused past the retries raises and removes its temp file", "returned", "raised")
            except PermissionError:
                check("arm: a replace refused past the retries raises and removes its temp file",
                      (read_text(target), sorted(p.name for p in root.iterdir())), ("two\n", ["settings.json"]))
            finally:
                os.replace = real_replace
            remove(target, missing_ok=False)
            check("arm: remove deletes the file", target.exists(), False)
            remove(target, missing_ok=True)
            try:
                remove(target, missing_ok=False)
                check("arm: remove of a missing file raises unless missing_ok", "returned", "raised")
            except FileNotFoundError:
                check("arm: remove of a missing file raises unless missing_ok", True, True)
    finally:
        RETRIES, PAUSE_S = saved

    planted = ("import os, time\n"
               "def write(tmp, path):\n"
               "    for attempt in range(100):\n"
               "        try:\n"
               "            os.replace(tmp, path)\n"
               "            return\n"
               "        except PermissionError:\n"
               "            time.sleep(0.02)\n")
    check("arm: a planted second copy of the retry loop is found", retry_loops(planted), [3])
    lock_wait = ("import os, time\n"
                 "while True:\n"
                 "    try:\n"
                 "        fd = os.open('x', os.O_CREAT | os.O_EXCL)\n"
                 "        break\n"
                 "    except PermissionError:\n"
                 "        time.sleep(0.02)\n")
    check("arm: an O_EXCL lock-create wait (os.open) is the lock's own rule, not a copy", retry_loops(lock_wait), [])
    read_loop = ("for attempt in range(100):\n"
                 "    try:\n"
                 "        with open('x') as f:\n"
                 "            data = f.read()\n"
                 "        break\n"
                 "    except (OSError, PermissionError):\n"
                 "        pass\n")
    check("arm: a builtin open retried on PermissionError (in a tuple) is found", retry_loops(read_loop), [1])
    tolerated = ("def drop(path):\n"
                 "    try:\n"
                 "        path.unlink(missing_ok=True)\n"
                 "    except PermissionError:\n"
                 "        pass\n")
    check("arm: a single tolerated refusal outside a loop is not a retry loop", retry_loops(tolerated), [])
    hits = second_copies(Path(__file__).resolve().parents[1])
    check("arm: no script under scripts/ carries a second share-retry loop (use sharedfile.retry)", hits, [])

    print(f"sharedfile.py --self-test: {'ALL GREEN' if not fails else f'{len(fails)} FAILED'}")
    return 1 if fails else 0


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-test"]:
        if hasattr(sys.stdout, "reconfigure"):
            sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.exit(self_test())
    print(__doc__)
    sys.exit(2)
