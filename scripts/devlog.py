#!/usr/bin/env python3
"""devlog.py — THE DEVLOG IS ONE FILE PER ENTRY under DEVLOG/, and this is its one tool (kb/Work PB3025;
docs/rearchitecture/DESIGN-test-build-ci.md section 3.17.4).

    python scripts/devlog.py render [--since NNNN] [--out FILE]   the descending narrative (README, then newest first)
    python scripts/devlog.py stamp <branch> [--coord DIR] [--base origin/main]
                                                                   number and stamp the branch's pending entry (or
                                                                   re-stamp one main has overtaken); one commit
    python scripts/devlog.py check [--base origin/main] [--rev HEAD] the landing rule, on the trees of the range
    python scripts/devlog.py next                                  the highest entry number in DEVLOG/ (the allocator's floor)
    python scripts/devlog.py --self-test

THE SHAPE. `DEVLOG/README.md` is the front matter (what the log is, how an entry is written). Every entry is its own
file, `DEVLOG/NNNN-<slug>.md` (the number zero-padded to four digits, the slug from the title), whose FIRST LINE is its
header, `## Entry NNNN — YYYY-MM-DD HH:MM TZ — Title`, followed by the body. Entries are LF text like every tracked
file (kb/Work PB3029). The narrative a reader wants is RENDERED, never committed: `render` prints the README and then
every entry, newest (highest number) first, by concatenating the files byte for byte; `DEVLOG.rendered.md` is the
gitignored place to put it. Why one file per entry: two landings that both insert at the top of one file collide, and a
merge queue bounces the second (owner 2026-10-11, kb/Work PB3015 Q2: "Yes, one file per entry"); CLAUDE.md rule 6 is
unchanged in substance — one entry per landing on main, newest first, the only history.

THE HISTORY (every entry of the monolithic DEVLOG.md, migrated byte for byte on 2026-10-11; DEVLOG/README.md names
the range, written by the migration itself) is looser than the rule for new entries: early headers carry no time,
some a `:` after the date, twelve a double-encoded dash; 1191a-1191d carry a letter; and twenty numbers were used
twice (008-014, 171-185). Each file keeps its header exactly; a number used
twice is told apart by a letter in the FILE NAME (`0185b-…` renders above `0185a-…`, as the monolith ordered them), so
the render order is the name order and needs no list. A NEW entry is held to the strict header and a plain number.

THE FLOW. The implementer writes `DEVLOG/pending-<slug>.md` whose first line is `## <Title>` (no number, no stamp) and
whose body follows. `stamp <branch>` runs when the branch is enqueued: it allocates the number through
`scripts/orchestrator/alloc.py devlog` (so number order is enqueue order is landing order), renames the file to
`NNNN-<slug>.md`, completes the header with the number and the current stamp, and commits that in the branch's
worktree. `check` is the landing rule (owner 2026-10-08, kb/Work PB2605: it covers "Only commits landing on main"),
run where every landing passes and on every pull request and merge group in CI.

WHAT `check` REFUSES (exit 1), on the committed trees of <base> and <rev>, never an index or a working tree:
  - a tree that still has the monolithic DEVLOG.md, a `pending-` file (an unstamped entry), or a DEVLOG/ file that is
    neither README.md nor an entry name;
  - an entry whose first line is not an `## Entry` header, whose header number disagrees with its file name, or whose
    number (with its letter) another file also holds;
  - a landing that adds no NEW entry, or more than one (new = a header line the base's history does not carry);
  - a new entry without the strict header, with a letter, or whose number is not above every number on <base> (a stale
    number after a rebase: re-stamp it);
  - a landing that deletes an entry <base> had: an entry FILE of <base> missing from <rev>, or, when <base> still has
    the monolithic DEVLOG.md (the one landing that migrates the log), a header line of it that no entry of <rev>
    carries, counted with multiplicity (the history repeats some headers).
Exit 0: the landing carries its one entry. Exit 2: the range could not be read (NOT RUN, never a pass).
"""
from __future__ import annotations

import argparse
import collections
import datetime as dt
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import unicodedata

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent
DIR = "DEVLOG"
README = "README.md"
MONOLITH = "DEVLOG.md"
DASH = "—"
FORMAT = f"## Entry NNNN {DASH} YYYY-MM-DD HH:MM TZ {DASH} Title"
ENTRY_NAME = re.compile(r"^(?P<num>\d{4,})(?P<sfx>[a-z]?)-(?P<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.md$")
PENDING_NAME = re.compile(r"^pending-(?P<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.md$")
HEADER = re.compile(r"^## Entry (?P<num>\d+)(?P<sfx>[a-z]?)(?![0-9A-Za-z])")
STAMPED = re.compile(rf"^## Entry (?P<num>\d+) {DASH} \d{{4}}-\d{{2}}-\d{{2}} \d{{2}}:\d{{2}} [A-Za-z][A-Za-z0-9+:-]* "
                     rf"{DASH} \S")
PENDING_HEADER = re.compile(r"^## (?!Entry\b)(?P<title>\S.*)$")
#: What may precede an old header's title: a separator (an em or en dash, a hyphen, a colon, or the twelve
#: double-encoded dashes of entries 1217-1228), a date, a time and a zone, another separator.
TITLE_PREFIX = re.compile(r"^\s*(?:[—–:-]|â\u0080\u0094)?\s*"
                          r"(?:\d{4}-\d{2}-\d{2}(?:\s+\d{2}:\d{2}(?:\s+[A-Za-z]{2,5})?)?)?\s*"
                          r"(?:[—–:-]|â\u0080\u0094)?\s*")
SLUG_MAX = 60


class RangeError(Exception):
    """A revision of the range could not be read (the check could not run: NOT RUN, never a pass)."""


# ── names, headers, slugs ────────────────────────────────────────────────────────────────────────────────────────
def slugify(title: str) -> str:
    """Lowercase ASCII words joined by '-', at most SLUG_MAX characters, cut at a word boundary; 'entry' if empty."""
    ascii_ = unicodedata.normalize("NFKD", title).encode("ascii", "ignore").decode("ascii").lower()
    slug = re.sub(r"[^a-z0-9]+", "-", ascii_).strip("-")
    if len(slug) > SLUG_MAX:
        cut = slug[:SLUG_MAX + 1]
        slug = cut[:cut.rfind("-")] if "-" in cut else slug[:SLUG_MAX]
    return slug or "entry"


def title_of(header: str) -> str:
    """The title of an `## Entry` header line, without its number, separators, date or stamp."""
    m = HEADER.match(header)
    rest = header[m.end():] if m else header
    return TITLE_PREFIX.sub("", rest, count=1).strip()


def entry_name(num: int, sfx: str, title: str) -> str:
    return f"{num:04d}{sfx}-{slugify(title)}.md"


def key_of(name: str) -> tuple[int, str] | None:
    """(number, letter) of an entry file name; None when the name is not an entry's."""
    m = ENTRY_NAME.match(name)
    return (int(m.group("num")), m.group("sfx")) if m else None


def first_line(text: str) -> str:
    return text.split("\n", 1)[0].rstrip("\r")


def entry_problems(name: str, text: str) -> list[str]:
    """What is wrong with one entry file (any entry, history included): its header and its name must agree."""
    head = first_line(text)
    m = HEADER.match(head)
    if not m:
        return [f"{DIR}/{name}: its first line is not an `## Entry` header ({head[:60]!r})"]
    num, sfx = key_of(name)  # the caller only passes entry names
    if int(m.group("num")) != num:
        return [f"{DIR}/{name}: the header says Entry {m.group('num')}{m.group('sfx')}, the file name {num}"]
    if m.group("sfx") and m.group("sfx") != sfx:
        return [f"{DIR}/{name}: the header's letter {m.group('sfx')!r} is not the file name's {sfx!r}"]
    return []


# ── reading a tree: the working tree (render, stamp, next) or a commit (check) ──────────────────────────────────
def _git(repo: pathlib.Path, *args: str, check: bool = False, inp: str | None = None) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace", input=inp, check=check)


def worktree_files(repo: pathlib.Path) -> dict[str, str]:
    """{file name: text, LF} of DEVLOG/ in the working tree (a CRLF checkout reads the same as the committed LF)."""
    d = repo / DIR
    if not d.is_dir():
        return {}
    return {p.name: p.read_bytes().decode("utf-8").replace("\r\n", "\n") for p in d.iterdir() if p.is_file()}


def commit_files(repo: pathlib.Path, rev: str) -> tuple[dict[str, str], bool]:
    """({file name: text} of DEVLOG/ at commit `rev`, whether that tree has the monolithic DEVLOG.md); every blob is
    read through ONE `git cat-file --batch` (two thousand `git show` processes would take a minute)."""
    if _git(repo, "rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}").returncode != 0:
        raise RangeError(f"{rev} is not a commit")
    listed = _git(repo, "ls-tree", "--name-only", rev, f"{DIR}/", MONOLITH)
    if listed.returncode != 0:
        raise RangeError(f"git ls-tree {rev} failed: {listed.stderr.strip()}")
    names = [n for n in listed.stdout.splitlines() if n.startswith(f"{DIR}/")]
    monolith = MONOLITH in listed.stdout.splitlines()
    if not names:
        return {}, monolith
    out = subprocess.run(["git", "-C", str(repo), "cat-file", "--batch"], capture_output=True, check=True,
                         input="".join(f"{rev}:{n}\n" for n in names).encode("utf-8")).stdout
    files, at = {}, 0
    for n in names:
        nl = out.index(b"\n", at)
        size = int(out[at:nl].split()[2])
        files[n[len(DIR) + 1:]] = out[nl + 1:nl + 1 + size].decode("utf-8").replace("\r\n", "\n")
        at = nl + 1 + size + 1
    return files, monolith


def monolith_headers(repo: pathlib.Path, rev: str) -> list[str]:
    """The `## Entry` header lines of the monolithic DEVLOG.md at `rev`, in file order with every repeat kept (empty
    when that tree has none). The base of the one landing that migrated the log carries its history there; no later
    base does."""
    r = _git(repo, "show", f"{rev}:{MONOLITH}")
    return [ln.rstrip("\r") for ln in r.stdout.split("\n") if HEADER.match(ln)] if r.returncode == 0 else []


def entries(files: dict[str, str]) -> list[tuple[tuple[int, str], str]]:
    """[(key, name)] of the entry files, newest first: by number, then by letter (1191d above 1191a above 1191)."""
    return sorted(((k, n) for n in files if (k := key_of(n))), reverse=True)


def highest(names) -> int:
    """The highest entry number among file names (a {name: text} dict iterates its names)."""
    return max((k[0] for n in names if (k := key_of(n))), default=0)


def names_in(repo: pathlib.Path) -> list[str]:
    """The file names of DEVLOG/ in the working tree, without reading any file."""
    d = repo / DIR
    return [p.name for p in d.iterdir() if p.is_file()] if d.is_dir() else []


def newest(repo: pathlib.Path, count: int) -> list[str]:
    """The texts of the `count` newest entries of the working tree, newest first, reading only those files."""
    names = sorted(((k, n) for n in names_in(repo) if (k := key_of(n))), reverse=True)
    return [(repo / DIR / n).read_bytes().decode("utf-8").replace("\r\n", "\n") for _, n in names[:count]]


# ── render ───────────────────────────────────────────────────────────────────────────────────────────────────────
def render(files: dict[str, str], since: int | None = None) -> str:
    """The README, then every entry newest first, each file's text exactly as written (a file that does not end in a
    newline gets one). With `since`, only the entries numbered `since` and above, without the README."""
    parts = [] if since is not None or README not in files else [files[README]]
    for (num, _), name in entries(files):
        if since is None or num >= since:
            parts.append(files[name])
    return "".join(p if p.endswith("\n") else p + "\n" for p in parts)


# ── check ────────────────────────────────────────────────────────────────────────────────────────────────────────
def lint(files: dict[str, str], monolith: bool) -> list[str]:
    """What is wrong with a DEVLOG/ tree as a whole, whatever the landing: the monolith back, a stray file, an entry
    whose header disagrees with its name, two files holding one number. A pending entry is legal here (a branch
    carries one until its landing stamps it); `check` refuses it at the landing. The self-test runs this on the
    repository's own tree, so every gate holds the whole log to it."""
    reasons = []
    if monolith:
        reasons.append(f"the tree still has {MONOLITH}: the log is {DIR}/, one file per entry (kb/Work PB3025)")
    strays = sorted(n for n in files if n != README and not key_of(n) and not PENDING_NAME.match(n))
    reasons += [f"{DIR}/{n} is neither {README} nor an entry (`NNNN-<slug>.md`)" for n in strays]
    seen: dict[tuple[int, str], str] = {}
    for k, n in entries(files):
        reasons += entry_problems(n, files[n])
        if k in seen:
            reasons.append(f"{DIR}/{n} and {DIR}/{seen[k]} hold the same entry number {k[0]}{k[1]}")
        seen[k] = n
    return reasons


def check(base: dict[str, str], base_extra_headers: list[str], rev: dict[str, str], rev_monolith: bool) -> tuple[
        list[str], list[str]]:
    """(refusal reasons, the new entries' names) for a landing from the tree `base` to the tree `rev`.
    `base_extra_headers` is the header lines of the base's monolithic DEVLOG.md (`monolith_headers`), repeats kept."""
    reasons = lint(rev, rev_monolith)
    pending = sorted(n for n in rev if PENDING_NAME.match(n))
    for n in pending:
        reasons.append(f"{DIR}/{n} is an unstamped entry: run `python scripts/devlog.py stamp <branch>` before landing")
    history = {first_line(base[n]) for _, n in entries(base)} | set(base_extra_headers)
    new = [n for _, n in entries(rev) if first_line(rev[n]) not in history]
    floor = max([highest(base)] + [int(m.group("num")) for h in base_extra_headers if (m := HEADER.match(h))])
    if not new and not pending:
        reasons.append("the landing carries no new DEVLOG entry. Every landing on main carries one (owner 2026-10-08, "
                       f"kb/Work PB2605): write {DIR}/pending-<slug>.md (first line `## <Title>`, then the body) and "
                       "stamp it with `python scripts/devlog.py stamp <branch>`")
    if len(new) > 1:
        reasons.append(f"the landing adds {len(new)} new entries ({', '.join(new)}); a landing carries exactly one")
    for n in new:
        head = first_line(rev[n])
        if not STAMPED.match(head):
            reasons.append(f"{DIR}/{n}: the new header `{head[:90]}` is not `{FORMAT}`")
        k = key_of(n)
        if k[1]:
            reasons.append(f"{DIR}/{n}: a new entry carries a plain number, never a letter")
        if k[0] <= floor:
            reasons.append(f"{DIR}/{n}: number {k[0]} is not above the base's newest entry {floor} (a stale number "
                           "after a rebase): re-stamp it")
    gone = sorted(n for _, n in entries(base) if n not in rev)
    reasons += [f"{DIR}/{n} is on the base and the landing deletes it: the log is the only history" for n in gone]
    # The base's monolith is history too (kb/Work PB3025, review R1-1): every header it holds, as many times as it holds
    # it, must be the first line of an entry of rev, or the migrating landing has dropped an entry main had.
    dropped = collections.Counter(base_extra_headers) - collections.Counter(first_line(rev[n]) for _, n in entries(rev))
    reasons += [f"the base's {MONOLITH} holds `{h[:90]}`{f' ({c} times)' if c > 1 else ''} and the landing has no "
                f"{DIR}/ entry with that header: the log is the only history (re-run `scripts/devlog_migrate.py` on "
                f"main's {MONOLITH})" for h, c in sorted(dropped.items())]
    return reasons, new


def run_check(repo: pathlib.Path, base: str, rev: str) -> int:
    try:
        base_files, _ = commit_files(repo, base)
        rev_files, rev_monolith = commit_files(repo, rev)
    except RangeError as e:
        print(f"⛔ devlog check: {e}")
        print("=== DEVLOG CHECK: NOT RUN (the range could not be read) ===")
        return 2
    reasons, new = check(base_files, monolith_headers(repo, base), rev_files, rev_monolith)
    for r in reasons:
        print(f"⛔ devlog check: {r}")
    if reasons:
        print(f"=== DEVLOG CHECK: REFUSED ({len(reasons)} reason(s)) ===")
        return 1
    print(f"=== DEVLOG CHECK: PASS (1 new entry: {first_line(rev_files[new[0]])[:80]}) ===")
    return 0


# ── stamp ────────────────────────────────────────────────────────────────────────────────────────────────────────
def zone_abbrev(now: dt.datetime) -> str:
    """The zone as `date +%Z` prints it (PDT); Windows' tzname is spelled out ('Pacific Daylight Time')."""
    name = now.tzname() or "UTC"
    return "".join(w[0] for w in name.split()) if " " in name else name


def worktree_of(repo: pathlib.Path, branch: str) -> pathlib.Path:
    """The worktree that has `branch` checked out."""
    path = None
    for line in _git(repo, "worktree", "list", "--porcelain", check=True).stdout.splitlines():
        if line.startswith("worktree "):
            path = pathlib.Path(line[len("worktree "):])
        elif line == f"branch refs/heads/{branch}" and path is not None:
            return path
    raise SystemExit(f"⛔ devlog stamp: no worktree has branch {branch} checked out")


def stamp(wt: pathlib.Path, number: int, now: dt.datetime) -> tuple[str, str]:
    """Rename the worktree's one pending entry to its numbered name and complete its header; returns (name, header).
    The caller allocates `number` and commits."""
    files = worktree_files(wt)
    pending = sorted(n for n in files if PENDING_NAME.match(n))
    if len(pending) != 1:
        raise SystemExit(f"⛔ devlog stamp: {wt / DIR} holds {len(pending)} pending entries ({', '.join(pending) or 'none'}); "
                         "a branch carries exactly one, `pending-<slug>.md` whose first line is `## <Title>`")
    text = files[pending[0]]
    m = PENDING_HEADER.match(first_line(text))
    if not m:
        raise SystemExit(f"⛔ devlog stamp: {DIR}/{pending[0]} must start with `## <Title>` (no number, no stamp)")
    title = m.group("title").strip()
    header = f"## Entry {number} {DASH} {now.strftime('%Y-%m-%d %H:%M')} {zone_abbrev(now)} {DASH} {title}"
    body = text.split("\n", 1)[1] if "\n" in text else ""
    name = entry_name(number, "", title)
    (wt / DIR / name).write_bytes((header + "\n" + body.rstrip("\n") + "\n\n").encode("utf-8"))
    (wt / DIR / pending[0]).unlink()
    # exactly these two paths: the entry, and the pending file's removal (tracked or not); nothing else in DEVLOG/
    _git(wt, "add", "--", f"{DIR}/{name}", check=True)
    _git(wt, "rm", "-q", "--cached", "--ignore-unmatch", "--", f"{DIR}/{pending[0]}", check=True)
    return name, header


def unstamp_overtaken(wt: pathlib.Path, base: str) -> str | None:
    """The branch's ONE added entry, when <base> (main) has since reached its number, made pending again so the next
    stamp numbers it above main's (kb/Work PB3023: a branch waits after its stamp — a merge-queue bounce, a finisher —
    while other landings take higher numbers, and `check` then refuses the stale number). Returns the stale file's
    name, or None when there is nothing to re-stamp (no added entry, more than one, or one still above main's)."""
    try:
        base_files, _ = commit_files(wt, base)
    except RangeError:
        return None
    files = worktree_files(wt)
    added = [n for n in files if key_of(n) and n not in base_files]
    if len(added) != 1 or key_of(added[0])[0] > highest(base_files):
        return None
    stale, text = added[0], files[added[0]]
    title = title_of(first_line(text))
    body = text.split("\n", 1)[1] if "\n" in text else ""
    (wt / DIR / f"pending-{slugify(title)}.md").write_bytes(f"## {title}\n{body}".encode("utf-8"))
    (wt / DIR / stale).unlink()
    _git(wt, "rm", "-q", "--cached", "--ignore-unmatch", "--", f"{DIR}/{stale}", check=True)
    return stale


def run_stamp(repo: pathlib.Path, branch: str, coord_override: str | None, base: str = "origin/main") -> int:
    sys.path.insert(0, str(HERE / "orchestrator"))
    import alloc  # noqa: PLC0415
    import coord  # noqa: PLC0415
    wt = worktree_of(repo, branch)
    if not any(PENDING_NAME.match(n) for n in names_in(wt)):
        stale = unstamp_overtaken(wt, base)
        if not stale:
            raise SystemExit(f"⛔ devlog stamp: {wt / DIR} has no pending entry to stamp (and no stamped entry that "
                             f"{base} has overtaken)")
        print(f"re-stamping {DIR}/{stale}: {base} already holds its number")
    number, _ = alloc.allocate("devlog", 1, wt, coord.coord_dir(coord_override))
    name, header = stamp(wt, number, dt.datetime.now().astimezone())
    _git(wt, "commit", "-q", "-m", f"DEVLOG {number}: stamp the entry ({DIR}/{name})", check=True)
    print(f"stamped {DIR}/{name}: {header}")
    return 0


# ── self-test: real repositories and real ranges ─────────────────────────────────────────────────────────────────
def self_test() -> int:
    # HERMETIC, like landing_check's: drop git's repository-selection variables so nothing reaches the caller's repo.
    for name in subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                               text=True).stdout.split():
        os.environ.pop(name, None)
    root = pathlib.Path(tempfile.mkdtemp(prefix="devlog-self-test-"))
    results: list[tuple[str, bool, str]] = []

    def ok(label: str, cond: bool, got: object = "") -> None:
        results.append((label, bool(cond), str(got)))

    # names, slugs, titles
    ok("slug: words, ASCII, lowercase", slugify("PB40: a CLASS screen — the fix") == "pb40-a-class-screen-the-fix")
    ok("slug: capped at a word boundary", len(slugify("word " * 40)) <= SLUG_MAX and not slugify("word " * 40).endswith("-"))
    ok("title of a stamped header", title_of(f"## Entry 1960 {DASH} 2026-10-11 13:38 PDT {DASH} Train 1061: x") ==
       "Train 1061: x", title_of(f"## Entry 1960 {DASH} 2026-10-11 13:38 PDT {DASH} Train 1061: x"))
    ok("title of an early header (date and colon)", title_of(f"## Entry 185 {DASH} 2026-03-30: Ledger") == "Ledger")
    ok("title of a double-encoded dash header", title_of("## Entry 1228 â\u0080\u0094 2026-08-08 13:24 PDT "
                                                         "â\u0080\u0094 R20: walk") == "R20: walk")
    ok("title of an undated header", title_of(f"## Entry 511 {DASH} COBOL.NET: IS NUMERIC") == "COBOL.NET: IS NUMERIC")

    def e(num: str, title: str = "t", body: str = "body\n\n") -> str:
        return f"## Entry {num} {DASH} 2026-10-11 10:00 PDT {DASH} {title}\n\n{body}"

    # render: README first; number, then letter, descending; a duplicate's file-name letter orders it
    files = {README: "# Log\n\n", "1191-a.md": e("1191"), "1191a-b.md": e("1191a"), "1191d-c.md": e("1191d"),
             "0185b-first.md": e("185", "first"), "0185a-second.md": e("185", "second"), "1192-x.md": e("1192")}
    order = [first_line(p) for p in render(files).split("\n") if p.startswith("## Entry")]
    ok("render: README, then 1192, 1191d, 1191a, 1191, 185 (b), 185 (a)",
       render(files).startswith("# Log\n\n## Entry 1192") and
       [h.split()[2] for h in order] == ["1192", "1191d", "1191a", "1191", "185", "185"] and
       order[4].endswith("first"), order)
    ok("render --since: the entries at or above, no README", render(files, since=1191).startswith("## Entry 1192") and
       "## Entry 185" not in render(files, since=1191))
    ok("render is byte-faithful concatenation", render({README: "a\n", "0001-x.md": "## Entry 001 x\n"}) ==
       "a\n## Entry 001 x\n")

    # check, against real commits
    repo = root / "repo"
    repo.mkdir()
    env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t",
               GIT_COMMITTER_EMAIL="t@t")

    def sh(*args: str, cwd: pathlib.Path = repo) -> str:
        return subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True, text=True,
                              env=env).stdout.strip()

    def tree(spec: dict[str, str | None], cwd: pathlib.Path = repo) -> None:
        for rel, text in spec.items():
            p = cwd / rel
            if text is None:
                p.unlink()
            else:
                p.parent.mkdir(parents=True, exist_ok=True)
                p.write_bytes(text.encode("utf-8"))

    sh("init", "-q", "-b", "main")
    tree({f"{DIR}/{README}": "# Log\n", f"{DIR}/0011-older.md": e("11", "older"),
          f"{DIR}/0010-oldest.md": e("10", "oldest"), "code.py": "x = 1\n"})
    sh("add", "-A")
    sh("commit", "-qm", "base")
    base = sh("rev-parse", "HEAD")

    def landing(label: str, spec: dict[str, str | None], want: int, needle: str = "") -> None:
        sh("checkout", "-q", "-B", "landing", base)
        tree(spec)
        sh("add", "-A")
        sh("commit", "-qm", "WIP checkpoint: x", "--allow-empty")
        b, _ = commit_files(repo, base)
        r, mono = commit_files(repo, "HEAD")
        reasons, _ = check(b, monolith_headers(repo, base), r, mono)
        got = 1 if reasons else 0
        ok(label, got == want and (not needle or any(needle in x for x in reasons)), "; ".join(reasons) or "PASS")

    new12 = {f"{DIR}/0012-the-landing.md": e("12", "the landing")}
    landing("one new stamped entry above the base -> PASS", {**new12, "code.py": "x = 2\n"}, 0)
    landing("no new entry -> REFUSED", {"code.py": "x = 3\n"}, 1, "no new DEVLOG entry")
    landing("two new entries -> REFUSED", {**new12, f"{DIR}/0013-another.md": e("13", "another")}, 1, "exactly one")
    landing("an unstamped header -> REFUSED", {f"{DIR}/0012-x.md": f"## Entry 12 - x\n\nbody\n"}, 1, "is not `")
    landing("a stale number (not above the base) -> REFUSED", {f"{DIR}/0011-again.md": e("11", "again")}, 1, "stale")
    landing("a pending entry -> REFUSED", {f"{DIR}/pending-x.md": "## X\n\nbody\n"}, 1, "unstamped")
    landing("a letter on a new entry -> REFUSED", {f"{DIR}/0012a-x.md": e("12a", "x")}, 1, "never a letter")
    landing("a header that disagrees with its name -> REFUSED", {f"{DIR}/0012-x.md": e("13", "x")}, 1, "file name")
    landing("a stray file -> REFUSED", {**new12, f"{DIR}/notes.txt": "x"}, 1, "neither")
    landing("deleting a base entry -> REFUSED", {**new12, f"{DIR}/0010-oldest.md": None}, 1, "deletes")
    landing("the monolith back in the tree -> REFUSED", {**new12, MONOLITH: e("12")}, 1, MONOLITH)
    landing("two files holding one number -> REFUSED", {**new12, f"{DIR}/0012-twin.md": e("12", "twin")}, 1,
            "same entry number")
    landing("editing an old entry's body, with the new entry -> PASS",
            {**new12, f"{DIR}/0010-oldest.md": e("10", "oldest", "body, corrected\n\n")}, 0)
    # the migrating landing: the base has only the monolith; its headers are history, one new entry is new
    sh("checkout", "-q", "--orphan", "mono")
    sh("rm", "-rq", "--cached", ".")
    shutil.rmtree(repo / DIR)
    tree({MONOLITH: "# Log\n" + e("11", "older") + e("10", "oldest"), "code.py": "x = 1\n"})
    sh("add", "-A")
    sh("commit", "-qm", "monolith")
    mono_base = sh("rev-parse", "HEAD")
    sh("rm", "-q", MONOLITH)
    tree({f"{DIR}/{README}": "# Log\n", f"{DIR}/0011-older.md": e("11", "older"), f"{DIR}/0010-oldest.md": e("10", "oldest"),
          **new12})
    sh("add", "-A")
    sh("commit", "-qm", "migrate")
    b, _ = commit_files(repo, mono_base)
    r, mono = commit_files(repo, "HEAD")
    reasons, new = check(b, monolith_headers(repo, mono_base), r, mono)
    ok("the migrating landing: the monolith's headers are history, its one new entry passes",
       not reasons and new == ["0012-the-landing.md"], reasons)
    tree({f"{DIR}/0011-older.md": e("11", "older, retitled")})
    sh("commit", "-qam", "retitle")
    reasons, _ = check(b, monolith_headers(repo, mono_base), commit_files(repo, "HEAD")[0], False)
    ok("the migrating landing with a header changed in the split -> REFUSED (two new)", any("exactly one" in x for x in
                                                                                             reasons), reasons)
    # review R1-1: main's monolith gained an entry after the branch split it; a landing that keeps the deletion instead
    # of re-running the migration carries its own new entry above main's newest, and must still be refused.
    sh("checkout", "-q", "-B", "mono-newer", mono_base)
    tree({MONOLITH: "# Log\n" + e("12", "main's newer") + e("11", "older") + e("10", "oldest")})
    sh("commit", "-qam", "main gains an entry")
    newer_base = sh("rev-parse", "HEAD")
    sh("rm", "-q", MONOLITH)
    tree({f"{DIR}/{README}": "# Log\n", f"{DIR}/0011-older.md": e("11", "older"), f"{DIR}/0010-oldest.md": e("10", "oldest"),
          f"{DIR}/0013-the-landing.md": e("13", "the landing")})
    sh("add", "-A")
    sh("commit", "-qm", "a stale split")
    reasons, new = check(commit_files(repo, newer_base)[0], monolith_headers(repo, newer_base),
                         *commit_files(repo, "HEAD"))
    ok("the migrating landing that drops an entry of the base's monolith -> REFUSED (deletes), though its one new "
       "entry is above the base",
       new == ["0013-the-landing.md"] and len(reasons) == 1 and f"no {DIR}/ entry with that header" in reasons[0]
       and "main's newer" in reasons[0], reasons)
    twice = e("9", "twice")
    reasons, _ = check({}, [first_line(twice), first_line(twice)],
                       {README: "# Log\n", "0009a-twice.md": twice, "0013-new.md": e("13", "new")}, False)
    ok("a header the base's monolith holds twice needs two entries -> REFUSED with one",
       len(reasons) == 1 and "twice" in reasons[0] and "times)" not in reasons[0], reasons)
    try:
        commit_files(repo, "no-such-rev")
        ok("an unreadable revision -> NOT RUN (exit 2), never a pass", False, "no RangeError")
    except RangeError:
        ok("an unreadable revision -> NOT RUN (exit 2), never a pass", True)

    # stamp: a branch in a linked worktree; the number comes from the allocator; one commit
    sh("checkout", "-q", "main")
    wt = root / "wt"
    sh("worktree", "add", "-q", "-b", "impl", str(wt), base)
    tree({f"{DIR}/pending-my-fix.md": "## PB9: the fix — and why\n\nThe body.\n"}, cwd=wt)
    sh("add", "-A", cwd=wt)
    sh("commit", "-qm", "WIP checkpoint: the fix and its entry", cwd=wt)
    cdir = root / "coord"
    cdir.mkdir()
    out = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "stamp", "impl", "--repo", str(repo),
                          "--coord", str(cdir)], capture_output=True, text=True, encoding="utf-8", env=env)
    names = sorted(p.name for p in (wt / DIR).iterdir())
    head = first_line((wt / DIR / "0012-pb9-the-fix-and-why.md").read_text(encoding="utf-8")) if \
        (wt / DIR / "0012-pb9-the-fix-and-why.md").exists() else ""
    ok("stamp: the pending entry becomes 0012-<slug>.md with a stamped header, in one commit",
       out.returncode == 0 and "0012-pb9-the-fix-and-why.md" in names and not any(n.startswith("pending-") for n in names)
       and STAMPED.match(head) and head.endswith("PB9: the fix — and why")
       and sh("log", "-1", "--format=%s", cwd=wt).startswith("DEVLOG 12:") and not sh("status", "--porcelain", cwd=wt),
       out.stdout + out.stderr + repr(names) + head)
    ok("stamp: the stamped branch passes check", not check(commit_files(repo, base)[0], [],
                                                           *commit_files(repo, "impl"))[0])
    ok("stamp: the allocator recorded the number (the next stamp takes 13)",
       json.loads((cdir / "alloc.json").read_text(encoding="utf-8")).get("devlog") == 12)
    again = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "stamp", "impl", "--repo", str(repo),
                            "--coord", str(cdir)], capture_output=True, text=True, encoding="utf-8", env=env)
    ok("stamp: a branch with no pending entry is refused, nothing allocated", again.returncode != 0 and
       json.loads((cdir / "alloc.json").read_text(encoding="utf-8")).get("devlog") == 12, again.stdout + again.stderr)
    # RE-STAMP (kb/Work PB3023): another landing took 12 and 13 on main while `impl` waited (a queue bounce); main is
    # merged in, and the stale 12 is numbered again above main's, in one commit that removes the stale file
    sh("checkout", "-q", "main")
    tree({f"{DIR}/0012-another-landing.md": e("12", "another landing"), f"{DIR}/0013-a-third.md": e("13", "a third")})
    sh("add", "-A")
    sh("commit", "-qm", "two landings on main")
    sh("merge", "-q", "--no-edit", "main", cwd=wt)
    restamp = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "stamp", "impl", "--repo",
                              str(repo), "--coord", str(cdir), "--base", "main"], capture_output=True, text=True,
                             encoding="utf-8", env=env)
    names = sorted(p.name for p in (wt / DIR).iterdir())
    ok("stamp: an entry main has overtaken is re-stamped above main's, the stale file removed, in one commit",
       restamp.returncode == 0 and "0014-pb9-the-fix-and-why.md" in names and "0012-pb9-the-fix-and-why.md" not in names
       and not any(n.startswith("pending-") for n in names) and not sh("status", "--porcelain", cwd=wt)
       and not check(commit_files(repo, "main")[0], set(), *commit_files(repo, "impl"))[0],
       restamp.stdout + restamp.stderr + repr(names))
    ok("next: the highest number in the tree", highest(names_in(wt)) == 14)
    ok("zone: a spelled-out zone name is abbreviated as date +%Z prints it",
       zone_abbrev(dt.datetime(2026, 10, 11, tzinfo=dt.timezone(dt.timedelta(hours=-7), "Pacific Daylight Time")))
       == "PDT")

    # The PB2605 repro, kept from landing_devlog.py: the rule is the LANDING's, so a commit in a WORKTREE with nothing
    # but code staged (an implementer's WIP checkpoint) draws no DEVLOG ask or deny from ANY PreToolUse hook this
    # repository registers, in either shell and either shape (`cd <wt> && git commit`, `git -C <wt> commit`).
    tree({"code.py": "x = 9\n"}, cwd=wt)
    sh("add", "code.py", cwd=wt)
    settings = json.loads((REPO / ".claude" / "settings.json").read_text(encoding="utf-8"))
    hooks = [(m.get("matcher", ""), h["command"]) for m in settings["hooks"].get("PreToolUse", []) for h in m["hooks"]]
    script = re.compile(r'"\$CLAUDE_PROJECT_DIR/([^"]+)"')
    asked, ran, missing = [], 0, set()
    for tool, command in (("Bash", f"cd {wt.as_posix()} && git commit -m 'WIP checkpoint: x'"),
                          ("Bash", f"git -C {wt.as_posix()} commit -m 'WIP checkpoint: x'"),
                          ("PowerShell", f"Set-Location {wt}; git commit -m 'WIP checkpoint: x'")):
        payload = json.dumps({"hook_event_name": "PreToolUse", "tool_name": tool, "cwd": str(REPO),
                              "tool_input": {"command": command}})
        for matcher, cmd in hooks:
            if not re.fullmatch(matcher, tool) or not (m := script.search(cmd)):
                continue
            if not (REPO / m.group(1)).is_file():
                missing.add(m.group(1))
                continue
            ran += 1
            hook_out = subprocess.run([sys.executable, str(REPO / m.group(1))], input=payload, capture_output=True,
                                      text=True, encoding="utf-8", errors="replace",
                                      env=dict(os.environ, CLAUDE_PROJECT_DIR=str(REPO))).stdout
            if "DEVLOG" in hook_out:
                asked.append(f"{m.group(1)} on `{command}`")
    staged = sh("diff", "--cached", "--name-only", cwd=wt).split()
    ok("the PB2605 repro: a worktree WIP commit without a DEVLOG entry draws no DEVLOG ask from any hook",
       staged == ["code.py"] and not asked and not missing and ran >= 3,
       f"staged={staged}; asked by: {asked}; hooks run: {ran}; registered but missing: {sorted(missing)}")

    # THE REPOSITORY'S OWN LOG: every gate and CI run this self-test, so the whole of DEVLOG/ is held to `lint`.
    own_files = worktree_files(REPO)
    own = lint(own_files, (REPO / MONOLITH).exists())
    ok(f"this repository's {DIR}/ passes lint ({len(entries(own_files))} entries)", not own, "; ".join(own[:5]))

    sh("worktree", "remove", "--force", str(wt))
    shutil.rmtree(root, ignore_errors=True)
    bad = [r for r in results if not r[1]]
    for label, good, got in results:
        print(f"  {'ok ' if good else 'BAD'} {label}" + ("" if good else f": {got}"))
    print(f"=== DEVLOG SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    r = sub.add_parser("render", help="print the descending narrative")
    r.add_argument("--since", type=int, help="only the entries numbered this and above, without the README")
    r.add_argument("--out", help="write it here (DEVLOG.rendered.md is gitignored) instead of stdout")
    s = sub.add_parser("stamp", help="number and stamp a branch's pending entry, in one commit in its worktree")
    s.add_argument("branch")
    s.add_argument("--coord", help="the coordination directory alloc.py reserves the number in")
    s.add_argument("--base", default="origin/main", help="main: a stamped entry whose number it has reached is re-stamped")
    c = sub.add_parser("check", help="the landing rule on the committed range base..rev")
    c.add_argument("--base", default="origin/main")
    c.add_argument("--rev", default="HEAD")
    sub.add_parser("next", help="the highest entry number in DEVLOG/")
    for p in (r, s, c, sub.choices["next"]):
        p.add_argument("--repo", default=None, help="a checkout of the repository (default: the current one)")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    if not a.cmd:
        ap.error("a command is required")
    if a.repo:
        repo = pathlib.Path(a.repo)
    else:
        top = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
        if top.returncode != 0:
            print("=== DEVLOG: NOT RUN (not inside a git work tree) ===")
            return 2
        repo = pathlib.Path(top.stdout.strip())
    if a.cmd == "check":
        return run_check(repo, a.base, a.rev)
    if a.cmd == "stamp":
        return run_stamp(repo, a.branch, a.coord, a.base)
    if a.cmd == "next":
        print(highest(names_in(repo)))
        return 0
    files = worktree_files(repo)
    text = render(files, a.since)
    if a.out:
        pathlib.Path(a.out).write_bytes(text.encode("utf-8"))
        print(f"wrote {a.out} ({text.count(chr(10)):,} lines, {len(entries(files)):,} entries)")
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
