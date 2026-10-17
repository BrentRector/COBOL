#!/usr/bin/env python3
"""line_endings.py — ONE LINE ENDING, LF, IN EVERY TRACKED TEXT FILE: the index AND the working tree (kb/Work PB3029).

    python scripts/line_endings.py --check          # exit 1 when any arm below finds a CR (the gate's audit, CI's too)
    python scripts/line_endings.py --fix            # rewrite this checkout's CRLF text files to LF (content unchanged)
    python scripts/line_endings.py --fix-writers    # give every Python text-mode write the check names `newline="\\n"`
    python scripts/line_endings.py --self-test

⛔ WHY (owner 2026-10-11 ~12:35 PDT: "We should probably standardize on a single line-ending format. CRLF vs others
keeps biting us"). The object database was already LF for 15,234 of 15,500 tracked text files, but `core.autocrlf=true`
checked every one of them out CRLF on Windows, so a Windows gate and a Linux gate read DIFFERENT BYTES from the same
commit; CRLF'd shell and Workflow scripts could not run at all (kb/Work PB1732, the `.gitattributes` history). Since
PB3029, `.gitattributes` opens with `* text=auto eol=lf`: git stores LF and checks out LF on every OS. Attributes alone do
not keep it so — a text file git's heuristic calls binary (it holds a lone CR) is stored as-is, a Windows tool that writes
CRLF leaves a CRLF working tree behind it until the next checkout, and a commit made outside git (an API upload) bypasses
the attributes — so this check holds the rule, in three arms:

  INDEX        no tracked TEXT file's staged blob holds a CR. A file is TEXT unless `.gitattributes` declares it
               byte-exact (`-text` / `binary`, each with its reason there) or it holds a NUL (an encoding fixture, an
               archive): a lone CR in a file nobody declared is the accident this arm exists for.
  WORKING TREE the same rule over the checkout's bytes, which are what a Windows gate compiles and compares. A checkout
               made before PB3029 is CRLF throughout: `--fix` rewrites it once, then re-states the index entries the
               rewrite made stale (`refresh_index`), so `git status` reads every rewritten file clean.
  WRITERS      every text-mode write in a live Python tool (scripts/, .claude/, conformance-suite/harness/) states
               `newline=`: Python on Windows otherwise translates each "\\n" to CRLF, so a generator run on Windows leaves a CRLF working tree that
               the second arm then reports. `--fix-writers` adds `newline="\\n"` mechanically (re-run it after a rebase
               brings in a new writer). PowerShell and C# writers of tracked files write LF by construction at their
               few sites (the generators, the test-side regeneration modes); the WORKING TREE arm catches a new one at
               the next Windows gate.

Explicitly byte-exact files are exempt from every arm: the pinned Unicode/CLDR inputs (content-addressed by SHA-256)
and a conformance fixture whose lone CRs are its subject.
"""
from __future__ import annotations

import argparse
import ast
import io
import os
import re
import subprocess
import sys
import tempfile
import tokenize
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
CR, LF, NUL = b"\r", b"\n", b"\0"
CRLF = CR + LF

#: Where the WRITERS arm looks: the live tools, including the conformance suite's harness (its generator is a gate
#: audit and writes the manifests every runner reads; kb/Work PB3174, PB3176). `docs/**/evidence/**` and
#: `adjudication/**` hold frozen records, never run against the tree, and `tools/claude-skills` is a submodule with
#: its own repository.
LIVE_ROOTS = ("scripts/", ".claude/", "conformance-suite/harness/")

#: How many findings of one arm the report prints before it says "and N more".
SHOWN = 40


@dataclass(frozen=True)
class Entry:
    """One tracked file as `git ls-files --eol` reports it: the staged blob's and the checkout's line-ending class
    (`lf`, `crlf`, `mixed`, `none`, `-text` for content git calls binary, empty when absent) and the attributes."""
    index: str
    worktree: str
    attrs: str
    path: str

    @property
    def byte_exact(self) -> bool:
        """`.gitattributes` declares the file byte-exact (`-text`, or the `binary` macro, which unsets `text`)."""
        return "-text" in self.attrs.split()


def git(root: Path, *args: str, binary: bool = False) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(root), *args], capture_output=True, check=True, text=not binary)


def entries(root: Path) -> list[Entry]:
    out = git(root, "ls-files", "--eol", "-z", binary=True).stdout.decode("utf-8")
    result = []
    for record in out.split("\0"):
        if not record:
            continue
        info, _, path = record.partition("\t")
        fields = info.split(None, 2)
        index, worktree = fields[0].removeprefix("i/"), fields[1].removeprefix("w/")
        attrs = fields[2].strip().removeprefix("attr/") if len(fields) > 2 else ""
        result.append(Entry(index, worktree, attrs, path))
    return result


def holds_cr(eol_class: str, read) -> bool:
    """Whether a TEXT file's bytes hold a CR, from git's class: `crlf`/`mixed` do; `lf`/`none`/absent do not; `-text` is
    git's binary verdict, which a NUL earns honestly and a lone CR earns too — so read the bytes and tell them apart."""
    if eol_class in ("crlf", "mixed"):
        return True
    if eol_class != "-text":
        return False
    data = read()
    return NUL not in data and CR in data


def scan_tree(root: Path) -> tuple[int, list[str], list[str]]:
    """(text files checked, INDEX findings, WORKING TREE findings)."""
    checked, in_index, in_tree = 0, [], []
    for e in entries(root):
        if e.byte_exact or not e.index:
            continue  # declared byte-exact, or a submodule / gitlink
        checked += 1
        if holds_cr(e.index, lambda: git(root, "cat-file", "blob", f":{e.path}", binary=True).stdout):
            in_index.append(e.path)
        if e.worktree and holds_cr(e.worktree, lambda: (root / e.path).read_bytes()):
            in_tree.append(e.path)
    return checked, in_index, in_tree


# ── the WRITERS arm ───────────────────────────────────────────────────────────────────────────────────────────────

#: A mode string as `open` reads it (so a file NAME passed where a mode could be is never mistaken for one).
MODE = re.compile(r"[rwxabt+U]{1,4}")
#: `io.open(file, mode, …)` is the builtin `open`: a mode without `b` is TEXT, and `newline` is the sixth positional.
TEXT_DEFAULT_OPENERS = frozenset({"io"})
#: `gzip.open`/`bz2.open`/`lzma.open(file, mode, …)` default to BINARY: a bare `w`/`a`/`x` writes bytes, and only a
#: mode carrying `t` is text (Python docs, each module's `open`). `newline` is never earlier than the sixth positional
#: (gzip, bz2) or is keyword-only (lzma), and passing it in a binary mode raises `ValueError`.
TEXT_ON_T_OPENERS = frozenset({"gzip", "bz2", "lzma"})
#: `X.open(name, mode)` of these opens BYTES (an archive member, a raw descriptor) or never translates line endings
#: (`codecs.open` always opens the file in binary mode): never a text writer, and `newline=` is not a parameter.
BINARY_OPENERS = frozenset({"os", "tarfile", "zipfile", "shelve", "dbm", "webbrowser", "codecs"})
TEMPFILES = frozenset({"NamedTemporaryFile", "TemporaryFile", "SpooledTemporaryFile"})
#: Tokens that carry no code: `add_newline` looks past them for the last token before a call's `)`.
LAYOUT_TOKENS = frozenset({tokenize.COMMENT, tokenize.NL, tokenize.NEWLINE, tokenize.INDENT, tokenize.DEDENT})


def _mode(node: ast.Call, position: int) -> str | None:
    """The literal mode the call passes at `position` or as `mode=`, or None when it passes none or a computed one."""
    for kw in node.keywords:
        if kw.arg == "mode":
            value = kw.value
            break
    else:
        if len(node.args) <= position:
            return None
        value = node.args[position]
    if isinstance(value, ast.Constant) and isinstance(value.value, str) and MODE.fullmatch(value.value):
        return value.value
    return None


def _writes_text(mode: str | None, *, text_needs_t: bool = False) -> bool:
    """Whether a literal `mode` writes TEXT: it writes (`w`, `a`, `x` or `+`) and is not binary — no `b`, and, for an
    opener whose default is binary (`text_needs_t`), an explicit `t`."""
    if mode is None or "b" in mode or not any(c in mode for c in "wax+"):
        return False
    return "t" in mode or not text_needs_t


def unstated_newline(node: ast.Call) -> bool:
    """Whether `node` writes text without stating `newline=` (by keyword or at its parameter's position)."""
    if any(kw.arg is None for kw in node.keywords):
        return False  # `**kwargs`: what it passes cannot be read here
    if any(kw.arg == "newline" for kw in node.keywords):
        return False
    func = node.func
    name = func.attr if isinstance(func, ast.Attribute) else func.id if isinstance(func, ast.Name) else None
    owner = func.value.id if isinstance(func, ast.Attribute) and isinstance(func.value, ast.Name) else None
    if name == "write_text" and isinstance(func, ast.Attribute):
        return len(node.args) < 4  # Path.write_text(data, encoding, errors, newline)
    if name == "open" and (isinstance(func, ast.Name) or owner in TEXT_DEFAULT_OPENERS):
        return _writes_text(_mode(node, 1)) and len(node.args) < 6  # open(file, mode, buffering, encoding, errors, newline)
    if name == "open" and owner in TEXT_ON_T_OPENERS:
        return _writes_text(_mode(node, 1), text_needs_t=True) and len(node.args) < 6  # (file, mode, level, enc, err, nl)
    if name == "fdopen" and owner == "os":
        return _writes_text(_mode(node, 1)) and len(node.args) < 6
    if name == "open" and isinstance(func, ast.Attribute) and owner not in BINARY_OPENERS:
        return _writes_text(_mode(node, 0)) and len(node.args) < 5  # Path.open(mode, buffering, encoding, errors, newline)
    if name in TEMPFILES:
        return _writes_text(_mode(node, 0)) and len(node.args) < 4  # (mode, buffering, encoding, newline, …)
    return False


def writer_calls(source: str) -> list[ast.Call]:
    return [n for n in ast.walk(ast.parse(source)) if isinstance(n, ast.Call) and unstated_newline(n)]


def live_python(root: Path) -> list[str]:
    files = git(root, "ls-files", "-z", "--", "*.py").stdout.split("\0")
    return sorted(f for f in files if f.startswith(LIVE_ROOTS))


def scan_writers(root: Path) -> list[str]:
    found = []
    for rel in live_python(root):
        path = root / rel
        if not path.exists():
            continue
        found += [f"{rel}:{c.lineno}" for c in writer_calls(path.read_text(encoding="utf-8"))]
    return found


def add_newline(source: str) -> tuple[str, int]:
    """`source` with `newline="\\n"` added to every call `writer_calls` names, and how many it changed.

    The keyword goes right after the last TOKEN before the call's closing parenthesis. A comment, a blank line or a
    comment line between that token and the parenthesis is not a token, so the keyword never lands inside a comment
    (review finding R1-1: the textual look-back this replaced appended it after a trailing `# ...`). Insertions are
    made last first, so no position moves. The rewrite is re-scanned before it is returned: one that no longer parses
    (SyntaxError) or that leaves a writer unstated (ValueError) raises, so `--fix-writers` never writes a file or
    reports a change it did not make."""
    calls = writer_calls(source)
    if not calls:
        return source, 0
    lines = source.split("\n")
    code = [t for t in tokenize.generate_tokens(io.StringIO(source).readline) if t.type not in LAYOUT_TOKENS]
    at = {t.start: k for k, t in enumerate(code)}
    edits = []
    for call in calls:
        row = call.end_lineno
        # The AST's columns are UTF-8 byte offsets; tokenize's, and the str lines', are characters.
        col = len(lines[row - 1].encode("utf-8")[:call.end_col_offset - 1].decode("utf-8"))
        k = at.get((row, col))
        if k is None or code[k].string != ")":
            raise ValueError(f"line {row}: the call does not end at a parenthesis")
        last = code[k - 1]
        if last.string == ",":  # a trailing comma: the keyword keeps one after it
            text = ' newline="\\n",'
        elif call.args or call.keywords:
            text = ', newline="\\n"'
        else:  # no argument: `last` is the call's own `(`
            text = 'newline="\\n"'
        edits.append((last.end, text))
    for (row, col), text in sorted(edits, reverse=True):
        lines[row - 1] = lines[row - 1][:col] + text + lines[row - 1][col:]
    fixed = "\n".join(lines)
    left = writer_calls(fixed)
    if left:
        raise ValueError(f"the rewrite left {len(left)} writer(s) unstated, at line(s) {[c.lineno for c in left]}")
    return fixed, len(calls)


def fix_writers(root: Path) -> int:
    total = 0
    for rel in live_python(root):
        path = root / rel
        if not path.exists():
            continue
        fixed, n = add_newline(path.read_bytes().decode("utf-8"))
        if n:
            path.write_bytes(fixed.encode("utf-8"))
            print(f"  {rel}: {n} writer(s)")
            total += n
    return total


# ── the working-tree fix ─────────────────────────────────────────────────────────────────────────────────────────

def refresh_index(root: Path) -> int:
    """Re-state the index entry of every tracked file whose recorded stat data is stale but whose content is the staged
    blob's, so `git status` reads it clean. Returns how many entries were re-stated.

    WHY: the index caches each file's stat data, the CRLF checkout's SIZE among it. When the bytes are rewritten to LF
    (by `--fix`, or a generator writing LF over a CRLF checkout) the size no longer matches, and git's `ie_modified`
    trusts a size mismatch on an entry with a nonzero recorded size as DATA CHANGED without comparing content: `git
    status --porcelain` lists the file ` M` while `git diff` is empty, `git update-index --refresh` answers "needs
    update", and enqueue.py's dirty-tree check refuses the landing (kb/Work PB3029, 15,179 files on one worktree).
    Only an entry whose working-tree content, cleaned by its attributes (`git hash-object --stdin-paths`), IS the
    staged blob AND whose working-tree mode IS the staged mode is re-stated (`git update-index`, which re-hashes to the
    same blob and records the file's current mode): a file with a real edit, or with an unstaged mode change (`chmod
    +x` under `core.filemode`), stays modified and is never staged (branch review R3-2). Both sides come from ONE
    `git diff-files --raw`: its source side is the index entry, its destination mode is the working tree's as git
    itself reads it (so `core.filemode false` reports the index mode, and no mode is ever compared there)."""
    raw = git(root, "diff-files", "--raw", "--no-abbrev", "-z", binary=True).stdout.decode("utf-8").split("\0")
    records = [(info.lstrip(":").split(), path) for info, path in zip(raw[0::2], raw[1::2])]
    # mid-merge a conflicted path has a `U` record AND an `M` record against ours: re-stating it would put ours at
    # stage 0 and silently resolve the conflict (branch review R5-1), so every path with a `U` record is skipped
    unmerged = {path for fields, path in records if fields[-1:] == ["U"]}
    staged: dict[str, str] = {}
    for fields, path in records:  # fields: src mode, dst mode, src sha, dst sha, status
        # a plain modification only, the same mode on both sides, and never a symlink (120000) or a gitlink (160000)
        if (path not in unmerged and len(fields) == 5 and fields[4] == "M" and fields[0] == fields[1]
                and fields[0] not in ("120000", "160000")):
            staged[path] = fields[2]
    candidates = [p for p in staged if "\n" not in p and (root / p).is_file()]
    if not candidates:
        return 0
    hashed = subprocess.run(["git", "-C", str(root), "hash-object", "--stdin-paths"], check=True, capture_output=True,
                            input="".join(p + "\n" for p in candidates).encode("utf-8")).stdout.decode("ascii").split()
    same = [p for p, sha in zip(candidates, hashed, strict=True) if staged[p] == sha]
    if same:
        subprocess.run(["git", "-C", str(root), "update-index", "-z", "--stdin"], check=True, capture_output=True,
                       input="".join(p + "\0" for p in same).encode("utf-8"))
    return len(same)


def fix_tree(root: Path) -> tuple[int, int, list[str]]:
    """Rewrite every TEXT file whose checkout holds CRLF to LF, then re-state the index entries the rewrite made stale
    (`refresh_index`), so `git status` reads the rewritten tree clean. Returns (files rewritten, index entries
    re-stated, files left). A file holding a lone CR is LEFT UNTOUCHED, terminators included: whether that CR is an
    accident or the file's subject (a fixture of the line-terminator rule) is a judgment no mechanical rule makes — it
    is removed by hand, or the file is declared byte-exact in .gitattributes with its reason."""
    rewritten, left = 0, []
    for e in entries(root):
        if e.byte_exact or not e.index or e.worktree not in ("crlf", "mixed", "-text"):
            continue
        path = root / e.path
        data = path.read_bytes()
        if NUL in data or CR not in data:
            continue
        fixed = data.replace(CRLF, LF)
        if CR in fixed:
            left.append(e.path)
            continue
        path.write_bytes(fixed)
        rewritten += 1
    return rewritten, refresh_index(root), left


# ── the check ────────────────────────────────────────────────────────────────────────────────────────────────────

def _show(title: str, items: list[str], hint: str) -> list[str]:
    lines = [f"{title}: {len(items)} — {hint}"]
    lines += [f"    {p}" for p in items[:SHOWN]]
    if len(items) > SHOWN:
        lines.append(f"    … and {len(items) - SHOWN} more")
    return lines


def check(root: Path) -> int:
    checked, in_index, in_tree = scan_tree(root)
    writers = scan_writers(root)
    report = []
    if in_index:
        report += _show("INDEX: tracked text files whose staged blob holds a CR", in_index,
                        "`git add --renormalize <file>` for CRLF; a lone CR is removed by hand, or the file is declared "
                        "`-text` in .gitattributes with the reason it must keep it")
    if in_tree:
        report += _show("WORKING TREE: tracked text files whose checkout holds a CR", in_tree,
                        "`python scripts/line_endings.py --fix` (a checkout made before kb/Work PB3029, or a tool that "
                        "wrote CRLF — fix the tool too)")
    if writers:
        report += _show("WRITERS: Python text-mode writes that do not state newline= (CRLF on Windows)", writers,
                        "`python scripts/line_endings.py --fix-writers`")
    if not checked:
        # A green over nothing is not a green: a wrong root or an unreadable index lists no file at all.
        report.append(f"POPULATION: no tracked text file was read under {root} — nothing was checked")
    for line in report:
        print(line)
    if report:
        print(f"=== LINE ENDINGS: RED — {len(in_index)} index · {len(in_tree)} working tree · {len(writers)} writers "
              f"(of {checked} tracked text files; kb/Work PB3029) ===")
        return 1
    print(f"=== LINE ENDINGS: CLEAN — {checked} tracked text files LF in the index and the working tree; every Python "
          "writer states newline= ===")
    return 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    failures = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    with tempfile.TemporaryDirectory() as d:
        root = Path(d)

        def run(*args: str) -> None:
            subprocess.run(["git", "-C", d, "-c", "core.autocrlf=false", *args], check=True, capture_output=True)

        run("init", "-q")
        run("config", "user.email", "self-test@example.invalid")
        run("config", "user.name", "self-test")
        expect("an EMPTY population is RED, never a green over nothing", check(root), 1)
        (root / "lf.txt").write_bytes(b"one" + LF + b"two" + LF)
        (root / "crlf.txt").write_bytes(b"one" + CRLF + b"two" + CRLF)
        (root / "lonecr.md").write_bytes(b"one" + CR + b"two" + LF)
        (root / "fixture.bin").write_bytes(b"A" + NUL + CR + b"B" + CRLF)
        (root / "pinned.xml").write_bytes(b"<a/>" + CRLF)
        (root / ".gitattributes").write_bytes(b"pinned.xml -text" + LF)
        run("add", "-A")
        run("commit", "-q", "-m", "fixture")
        _, in_index, in_tree = scan_tree(root)
        expect("arm INDEX: a CRLF blob and a lone-CR text blob are found; LF, a NUL file and a declared -text file "
               "are not", sorted(in_index), ["crlf.txt", "lonecr.md"])
        expect("arm WORKING TREE: the same files in the checkout", sorted(in_tree), ["crlf.txt", "lonecr.md"])

        (root / ".gitattributes").write_bytes(b"* text=auto eol=lf" + LF + b"pinned.xml -text" + LF
                                              + b"lonecr.md -text" + LF)
        run("add", "--renormalize", ".")
        run("add", "-A")
        run("commit", "-q", "-m", "normalized")
        _, in_index, in_tree = scan_tree(root)
        expect("arm INDEX: renormalized under text=auto eol=lf, nothing is found", in_index, [])
        expect("arm WORKING TREE: the pre-normalization CRLF checkout is still found", in_tree, ["crlf.txt"])
        (root / "lf.txt").write_bytes(b"one" + LF + b"TWO" + LF)  # a real edit: --fix must neither hide nor stage it
        rewritten, refreshed, left = fix_tree(root)
        expect("--fix rewrites the CRLF checkout to LF, re-states its index entry and leaves nothing",
               (rewritten, refreshed, left), (1, 1, []))
        expect("--fix leaves the bytes LF", (root / "crlf.txt").read_bytes(), b"one" + LF + b"two" + LF)
        expect("--fix never touches a declared -text file", (root / "pinned.xml").read_bytes(), b"<a/>" + CRLF)
        _, in_index, in_tree = scan_tree(root)
        expect("after --fix the tree is clean", (in_index, in_tree), ([], []))
        # kb/Work PB3029 finisher: the rewrite changed crlf.txt's SIZE, and git trusts a size mismatch on a nonzero
        # recorded size without comparing content, so without the re-stat `git status` listed it ` M` over an empty
        # `git diff`, and enqueue.py's dirty-tree check refused the landing.
        status = git(root, "status", "--porcelain", binary=True).stdout.decode("utf-8")
        expect("after --fix git status lists only the real edit, unstaged", status, " M lf.txt\n")
        run("checkout", "--", "lf.txt")
        # Branch review R3-2: `git update-index` records the working tree's MODE too, so re-stating an entry whose
        # content is the staged blob but whose mode is not STAGED an unstaged `chmod +x` (Linux, core.filemode true).
        # Planted portably: the index says 100755 and the checkout (no exec bit; Git for Windows reads every file so)
        # says 100644 — the same mismatch from the other side; the index must come out of --fix untouched.
        run("config", "core.filemode", "true")
        (root / "mode.sh").write_bytes(b"echo" + LF)
        run("add", "mode.sh")
        run("commit", "-q", "-m", "a script")
        run("update-index", "--chmod=+x", "mode.sh")
        before = git(root, "ls-files", "-s", "mode.sh").stdout
        expect("--fix never re-states an entry whose working-tree mode differs from the index's (no mode is staged)",
               (refresh_index(root), git(root, "ls-files", "-s", "mode.sh").stdout), (0, before))
        run("reset", "-q", "--", "mode.sh")
        # Branch review R5-1: mid-merge, `git diff-files --raw` gives a conflicted path a `U` record AND an `M` record
        # against ours, so re-stating it would put ours at stage 0 and silently resolve the conflict. Planted as a
        # modify/delete conflict; --fix must leave both of its stages.
        (root / "conflict.txt").write_bytes(b"base" + LF)
        run("add", "conflict.txt")
        run("commit", "-q", "-m", "base")
        run("checkout", "-q", "-b", "side")
        run("rm", "-q", "conflict.txt")
        run("commit", "-q", "-m", "side deletes")
        run("checkout", "-q", "-")
        (root / "conflict.txt").write_bytes(b"ours" + LF)
        run("commit", "-q", "-am", "ours modifies")
        subprocess.run(["git", "-C", d, "-c", "core.autocrlf=false", "merge", "-q", "side"], capture_output=True)  # conflicts
        expect("--fix never re-states a conflicted path (a modify/delete conflict keeps its two stages)",
               (refresh_index(root), len(git(root, "ls-files", "-u", "--", "conflict.txt").stdout.splitlines())), (0, 2))
        run("merge", "--abort")

        accident = b"one" + CRLF + b"two" + CR + CRLF
        (root / "accident.md").write_bytes(accident)
        run("add", "-A")
        run("commit", "-q", "-m", "a stray CR")
        _, in_index, in_tree = scan_tree(root)
        expect("arm INDEX + WORKING TREE: an undeclared lone CR is found although git stores the file as binary",
               (in_index, in_tree), (["accident.md"], ["accident.md"]))
        expect("--fix leaves a lone-CR file untouched, terminators included, and names it",
               (fix_tree(root), (root / "accident.md").read_bytes()), ((0, 0, ["accident.md"]), accident))

    planted = "\n".join([
        "from pathlib import Path",
        "import io, os, tarfile, tempfile, zipfile",
        "p = Path('x')",
        "p.write_text('a')",                                  # 4  finding
        "p.write_text('a', encoding='utf-8', newline='\\n')",  # ok
        "open('x', 'w')",                                     # 6  finding
        "open('x', 'w', newline='')",                         # ok
        "open('x', 'wb')",                                    # ok
        "open('x')",                                          # ok: a read
        "open('x', mode='a', encoding='utf-8')",              # 10 finding
        "p.open('w')",                                        # 11 finding
        "io.open('x', 'w')",                                  # 12 finding
        "os.fdopen(3, 'w')",                                  # 13 finding
        "tempfile.NamedTemporaryFile('w', delete=False)",     # 14 finding
        "tempfile.NamedTemporaryFile()",                      # ok: w+b
        "zipfile.ZipFile('z').open('data.txt', 'w')",          # ok: an archive member is bytes
        "tarfile.open('t', 'w')",                             # ok: an archive
        "open('x', 'w', **{})",                               # ok: unreadable
        "open(",
        "    'x',",
        "    'w',",
        ")",                                                  # 19 finding (multi-line, trailing comma)
        "open('x', 'w',",
        "     encoding='utf-8')",                             # 23 finding
        "import bz2, codecs, gzip, lzma",
        "gzip.open('x.gz', 'w')",                             # ok: gzip's bare `w` is BINARY
        "bz2.open('x.bz2', 'a')",                             # ok: so is bz2's bare `a`
        "lzma.open('x.xz', mode='x')",                        # ok: and lzma's bare `x`
        "gzip.open('y.gz', 'wt', encoding='utf-8')",          # 29 finding: `t` makes it text
        "bz2.open('y.bz2', mode='at')",                       # 30 finding
        "lzma.open('y.xz', 'wt')",                            # 31 finding
        "codecs.open('x', 'w', 'utf-8')",                     # ok: always binary underneath; `'x'` is a NAME here
        "open(",
        "    'x',",
        "    'w',  # the mode",
        ")",                                                  # 33 finding: a comment after the trailing comma
        "p.write_text(",
        "    'd', encoding='utf-8'  # enc",
        ")",                                                  # 37 finding: a comment, no trailing comma
        "open(",
        "    'x',",
        "    'w'",
        "    # a comment line, then a blank line",
        "",
        ")",                                                  # 40 finding
        "open('x', ('w'))",                                   # 46 finding: a parenthesized last argument
        "",
    ])
    lines = sorted(c.lineno for c in writer_calls(planted))
    expect("arm WRITERS: every unstated text-mode write is found, and nothing else", lines,
           [4, 6, 10, 11, 12, 13, 14, 19, 23, 29, 30, 31, 33, 37, 40, 46])
    fixed, n = add_newline(planted)
    expect("--fix-writers changes every finding", n, 16)
    # Review finding R1-1: the keyword follows the last argument TOKEN, so a comment after it stays a comment.
    expect("--fix-writers never writes the keyword into a comment",
           [fixed.splitlines()[k] for k in (34, 37, 41, 45)],
           ["    'w', newline=\"\\n\",  # the mode", "    'd', encoding='utf-8', newline=\"\\n\"  # enc",
            "    'w', newline=\"\\n\"", "open('x', ('w'), newline=\"\\n\")"])
    compressed = planted.splitlines()[24:32]  # lines 25-32: the import, then the gzip/bz2/lzma/codecs opens
    expect("--fix-writers leaves a binary-mode gzip/bz2/lzma/codecs open untouched",
           [line for line in fixed.splitlines()[24:32] if line in compressed],
           [compressed[k] for k in (0, 1, 2, 3, 7)])
    # Python itself is the authority on what the rewrite may pass: run the fixed compressed and codecs calls.
    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as d:
        here = os.getcwd()
        os.chdir(d)
        try:
            scope: dict = {}
            for stmt in fixed.splitlines()[24:32]:
                exec(f"with {stmt} as fh: pass" if "open(" in stmt else stmt, scope)
            ran = "ok"
        except (TypeError, ValueError) as e:
            ran = f"{type(e).__name__}: {e}"
        finally:
            os.chdir(here)
    expect("the --fix-writers output runs: no newline= reaches a binary-mode opener", ran, "ok")
    expect("--fix-writers leaves nothing to find", writer_calls(fixed), [])
    expect("--fix-writers is idempotent", add_newline(fixed), (fixed, 0))
    expect("--fix-writers keeps a single-line call on its line", fixed.splitlines()[3], "p.write_text('a', newline=\"\\n\")")
    expect("--fix-writers puts the keyword after a trailing comma",
           fixed.splitlines()[20], "    'w', newline=\"\\n\",")

    print(f"=== LINE ENDINGS SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    group = ap.add_mutually_exclusive_group(required=True)
    group.add_argument("--check", action="store_true")
    group.add_argument("--fix", action="store_true")
    group.add_argument("--fix-writers", action="store_true")
    group.add_argument("--self-test", action="store_true")
    ap.add_argument("--root", type=Path, default=REPO, help=argparse.SUPPRESS)
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    if args.self_test:
        return self_test()
    if args.fix:
        rewritten, refreshed, left = fix_tree(args.root)
        print(f"=== LINE ENDINGS FIX: {rewritten} working-tree file(s) rewritten CRLF -> LF; {refreshed} index "
              "entr(ies) re-stated, so git status reads them clean ===")
        for p in left:
            print(f"    still holds a lone CR (remove it, or declare the file -text with its reason): {p}")
        return 1 if left else 0
    if args.fix_writers:
        n = fix_writers(args.root)
        print(f"=== LINE ENDINGS FIX-WRITERS: {n} Python writer(s) now state newline= ===")
        return 0
    return check(args.root)


if __name__ == "__main__":
    sys.exit(main())
