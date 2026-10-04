#!/usr/bin/env python3
"""The ONE locked allocator for DEVLOG entry numbers, kb/Work `PB` ids and `COBOLNET` diagnostic codes.

    python scripts/orchestrator/alloc.py devlog          # reserve the next DEVLOG entry number   -> 1866
    python scripts/orchestrator/alloc.py pb 5            # reserve five kb/Work ids                -> PB1982-PB1986
    python scripts/orchestrator/alloc.py code 3          # reserve three diagnostic codes          -> COBOLNET2799-COBOLNET2801
    python scripts/orchestrator/alloc.py peek code       # the next free value, reserving nothing
    python scripts/orchestrator/alloc.py peek code --probe   # the session-probe's "diag" line
    python scripts/orchestrator/alloc.py seed code 2798  # record values handed out before the allocator (never lowers)
    ... [--coord DIR] [--repo DIR]

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 6. The next value of a kind is
max(truth, reserved) + 1. TRUTH is read from the repository on every call, by the one rule per kind below
(`session-probe.ps1` calls `peek code --probe` instead of computing it a second time). RESERVED is the highest value
handed out so far, kept in `<coord>/alloc.json` OUTSIDE every worktree, so an id given to an in-flight implementer
whose note has not landed yet is never handed out again (five collisions in one day cost a renumbering pass,
workstream SKILL section 4). A reservation is never returned: an unused id is a harmless gap.

The lock is an atomic create of `<coord>/alloc.lock` (os.open with O_CREAT|O_EXCL). A lock older than STALE_S is
broken by an atomic rename to a unique name (so two breakers cannot both win); a waiter gives up after WAIT_S.
"""
from __future__ import annotations

import argparse
import contextlib
import os
import pathlib
import re
import sys
import time

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import coord  # noqa: E402

STALE_S = 60.0
WAIT_S = 30.0
KINDS = ("devlog", "pb", "code")
ENTRY = re.compile(r"^## Entry (\d+)\b")
PB_FILE = re.compile(r"^PB(\d+)\.md$")
CODE = re.compile(r"COBOLNET(\d{4})")
CATALOG_CODE = re.compile(r'"COBOLNET(\d{4})"')
CATALOG = pathlib.Path("src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs")


# ── truth: one rule per kind ─────────────────────────────────────────────────────────────────────────────────────
def devlog_top(repo: pathlib.Path) -> int:
    """The first line-start `## Entry NNN` of DEVLOG.md: the file is DESCENDING, so that is the newest entry."""
    with open(repo / "DEVLOG.md", encoding="utf-8", errors="replace") as f:
        for line in f:
            m = ENTRY.match(line)
            if m:
                return int(m.group(1))
    raise SystemExit(f"⛔ no '## Entry NNN' line in {repo / 'DEVLOG.md'}")


def pb_max(repo: pathlib.Path) -> int:
    ids = [int(m.group(1)) for p in (repo / "kb" / "Work").glob("PB*.md") if (m := PB_FILE.match(p.name))]
    return max(ids, default=0)


def code_scans(repo: pathlib.Path) -> tuple[int, int]:
    """(highest COBOLNET code anywhere in src/**/*.cs outside bin/obj, highest quoted code in the catalog).

    The next free code is the CEILING of BOTH (the COBOLNET1573/1518 collision lesson). No band is pinned: a
    band in this pattern capped out twice (at 1600 and at 2000) and reported a code the catalog already held.
    The compiler channel's raw codes are not catalog descriptors, so src >= catalog is the steady state; a
    catalog maximum ABOVE every emitted code is an orphan descriptor worth reconciling before allocating.
    """
    src_max = 0
    for p in (repo / "src").rglob("*.cs"):
        if {"bin", "obj"} & set(p.relative_to(repo).parts):
            continue
        for m in CODE.finditer(p.read_text(encoding="utf-8", errors="replace")):
            src_max = max(src_max, int(m.group(1)))
    cat = (repo / CATALOG).read_text(encoding="utf-8", errors="replace")
    cat_max = max((int(m.group(1)) for m in CATALOG_CODE.finditer(cat)), default=0)
    return src_max, cat_max


def truth(kind: str, repo: pathlib.Path) -> int:
    if kind == "devlog":
        return devlog_top(repo)
    if kind == "pb":
        return pb_max(repo)
    return max(code_scans(repo))


def render(kind: str, first: int, count: int) -> str:
    last = first + count - 1
    if kind == "devlog":
        return str(first) if count == 1 else f"{first}-{last}"
    if kind == "pb":
        return f"PB{first}" if count == 1 else f"PB{first}-PB{last}"
    return f"COBOLNET{first:04d}" if count == 1 else f"COBOLNET{first:04d}-COBOLNET{last:04d}"


# ── the lock ─────────────────────────────────────────────────────────────────────────────────────────────────────
@contextlib.contextmanager
def locked(cdir: pathlib.Path):
    lock = cdir / "alloc.lock"
    deadline = time.monotonic() + WAIT_S
    while True:
        try:
            fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            os.write(fd, f"{os.getpid()} {time.time():.3f}\n".encode())
            os.close(fd)
            break
        except FileExistsError:
            try:
                age = time.time() - lock.stat().st_mtime
            except FileNotFoundError:
                continue  # released between our create and our stat: try again at once
            if age > STALE_S:
                # Break a stale lock by renaming it to a name only this process uses: exactly one breaker's rename
                # succeeds, and a fresh lock created after it is never the one renamed away.
                # Between our stat and our rename another breaker may already have removed the stale lock and a live
                # allocator created a fresh one; if the file we moved is fresh, put it back with a link (which, unlike
                # a POSIX rename, never overwrites a lock created meanwhile).
                grave = lock.with_name(f"alloc.lock.stale.{os.getpid()}.{time.time_ns()}")
                with contextlib.suppress(OSError):
                    os.rename(lock, grave)
                    if time.time() - grave.stat().st_mtime <= STALE_S:
                        with contextlib.suppress(OSError):
                            os.link(grave, lock)
                    os.remove(grave)
                continue
            if time.monotonic() > deadline:
                raise SystemExit(f"⛔ alloc.lock held for more than {WAIT_S:.0f}s: {lock} (a live allocator never holds it "
                                 f"for more than milliseconds; if no allocator is running, delete it)")
            time.sleep(0.02)
        except PermissionError:
            # Windows: the file is being deleted by its holder at this instant.
            time.sleep(0.02)
    try:
        yield
    finally:
        with contextlib.suppress(FileNotFoundError):
            os.remove(lock)


# ── allocation ───────────────────────────────────────────────────────────────────────────────────────────────────
def allocate(kind: str, count: int, repo: pathlib.Path, cdir: pathlib.Path) -> tuple[int, int]:
    """Reserve `count` consecutive values of `kind`; returns (first, last)."""
    if kind not in KINDS or count < 1:
        raise ValueError(f"bad allocation {kind!r} x{count}")
    t = truth(kind, repo)  # read outside the lock: it is the slow part, and truth only grows
    with locked(cdir):
        path = cdir / "alloc.json"
        state = coord.read_json(path, {})
        first = max(t, int(state.get(kind, 0))) + 1
        state[kind] = first + count - 1
        coord.write_json(path, state)
    return first, first + count - 1


def seed(kind: str, value: int, cdir: pathlib.Path) -> int:
    """Raise the reserved high-water mark to `value` (never lowers it): records ids handed out before the allocator
    existed, such as an in-flight wave's code ranges. Returns the mark now in force."""
    with locked(cdir):
        path = cdir / "alloc.json"
        state = coord.read_json(path, {})
        state[kind] = max(int(state.get(kind, 0)), value)
        coord.write_json(path, state)
        return state[kind]


def peek(kind: str, repo: pathlib.Path, cdir: pathlib.Path) -> int:
    state = coord.read_json(cdir / "alloc.json", {})
    return max(truth(kind, repo), int(state.get(kind, 0))) + 1


def probe_line(repo: pathlib.Path, cdir: pathlib.Path) -> str:
    src_max, cat_max = code_scans(repo)
    reserved = int(coord.read_json(cdir / "alloc.json", {}).get("code", 0))
    nxt = max(src_max, cat_max, reserved) + 1
    code = lambda n: render("code", n, 1)  # noqa: E731
    res = f" · reserved max {code(reserved)}" if reserved else ""
    head = f"src-grep max {code(src_max)} · catalog max {code(cat_max)}{res}"
    if cat_max > src_max:
        return (f"{head} ⚠ CATALOG ABOVE SRC (orphan descriptor {code(cat_max)} never emitted) — reconcile before "
                f"allocating; next free = {code(nxt)}")
    return f"{head} → next free = {code(nxt)}"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("kind", choices=(*KINDS, "peek", "seed"))
    ap.add_argument("arg", nargs="?", help="count for pb/code; the kind for peek and seed")
    ap.add_argument("value", nargs="?", type=int, help="seed only: the highest value already handed out")
    ap.add_argument("--probe", action="store_true", help="with 'peek code': print the session-probe line")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--repo", default=str(coord.REPO), help="repository whose files are the truth (default: this one)")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    repo, cdir = pathlib.Path(a.repo), coord.coord_dir(a.coord)
    if a.kind == "seed":
        if a.arg not in KINDS or a.value is None or a.value < 1:
            ap.error(f"seed needs one of {KINDS} and a positive value")
        print(f"{a.arg} reserved through {render(a.arg, seed(a.arg, a.value, cdir), 1)}")
        return 0
    if a.kind == "peek":
        if a.arg not in KINDS:
            ap.error(f"peek needs one of {KINDS}")
        if a.probe:
            if a.arg != "code":
                ap.error("--probe applies to 'peek code' only")
            print(probe_line(repo, cdir))
        else:
            print(render(a.arg, peek(a.arg, repo, cdir), 1))
        return 0
    if a.kind == "devlog":
        count = int(a.arg or 1)
    else:
        if not a.arg or not a.arg.isdigit() or int(a.arg) < 1:
            ap.error(f"{a.kind} needs a positive count")
        count = int(a.arg)
    first, _ = allocate(a.kind, count, repo, cdir)
    print(render(a.kind, first, count))
    return 0


if __name__ == "__main__":
    sys.exit(main())
