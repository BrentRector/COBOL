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
               made before PB3029 is CRLF throughout: `--fix` rewrites it once (git sees every rewritten file clean).
  WRITERS      every text-mode write in a live Python tool (scripts/, .claude/) states `newline=`: Python on Windows
               otherwise translates each "\\n" to CRLF, so a generator run on Windows leaves a CRLF working tree that
               the second arm then reports. `--fix-writers` adds `newline="\\n"` mechanically (re-run it after a rebase
               brings in a new writer). PowerShell and C# writers of tracked files write LF by construction at their
               few sites (the generators, the test-side regeneration modes); the WORKING TREE arm catches a new one at
               the next Windows gate.

Explicitly byte-exact files are exempt from every arm: the pinned Unicode/CLDR inputs (content-addressed by SHA-256),
DEVLOG.md until kb/Work PB3025 migrates it, and a golden that carries a deliberate lone CR.
"""
from __future__ import annotations

import argparse
import ast
import os
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
CR, LF, NUL = b"\r", b"\n", b"\0"
CRLF = CR + LF

#: Where the WRITERS arm looks: the live tools. `docs/**/evidence/**` and `adjudication/**` hold frozen records, never run
#: against the tree, and `tools/claude-skills` is a submodule with its own repository.
LIVE_ROOTS = ("scripts/", ".claude/")

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
#: `module.open(file, mode, …)` — the module-level openers whose `newline` is the sixth positional parameter.
MODULE_OPENERS = frozenset({"io", "gzip", "bz2", "lzma"})
#: `X.open(name, mode)` of these opens BYTES (an archive member, a raw descriptor): never a text writer.
BINARY_OPENERS = frozenset({"os", "tarfile", "zipfile", "shelve", "dbm", "webbrowser"})
TEMPFILES = frozenset({"NamedTemporaryFile", "TemporaryFile", "SpooledTemporaryFile"})


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


def _writes_text(mode: str | None) -> bool:
    return mode is not None and "b" not in mode and any(c in mode for c in "wax+")


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
    if name == "open" and (isinstance(func, ast.Name) or owner in MODULE_OPENERS):
        return _writes_text(_mode(node, 1)) and len(node.args) < 6  # open(file, mode, buffering, encoding, errors, newline)
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
    """`source` with `newline="\\n"` added to every call `writer_calls` names, and how many it changed. The AST's end
    offsets are UTF-8 byte columns, so the insertion is made in bytes, last call first, so no offset moves."""
    calls = sorted(writer_calls(source), key=lambda c: (c.end_lineno, c.end_col_offset), reverse=True)
    lines = source.encode("utf-8").split(LF)
    for call in calls:
        row = call.end_lineno - 1
        close = call.end_col_offset - 1  # the call's closing parenthesis
        line = lines[row]
        assert line[close:close + 1] == b")", f"line {call.end_lineno}: the call does not end at a parenthesis"
        before = line[:close].rstrip()
        # Look back past blank lines (a multi-line call) for a trailing comma.
        back, prev = row, before
        while not prev and back > 0:
            back -= 1
            prev = lines[back].rstrip()
        sep = b" " if prev.endswith(b",") else b", "
        if not before:  # `)` alone on its line: put the keyword on the line above, after its comma
            lines[back] = prev + (b"" if prev.endswith(b",") else b",") + b' newline="\\n",' + lines[back][len(prev):]
            continue
        lines[row] = before + sep + b'newline="\\n"' + line[close:]
    return LF.join(lines).decode("utf-8"), len(calls)


def fix_writers(root: Path) -> int:
    total = 0
    for rel in live_python(root):
        path = root / rel
        if not path.exists():
            continue
        data = path.read_bytes()
        source = data.decode("utf-8")
        fixed, n = add_newline(source)
        if n:
            ast.parse(fixed)  # a rewrite that no longer parses is a defect of this tool: stop before writing it
            path.write_bytes(fixed.encode("utf-8"))
            print(f"  {rel}: {n} writer(s)")
            total += n
    return total


# ── the working-tree fix ─────────────────────────────────────────────────────────────────────────────────────────

def fix_tree(root: Path) -> tuple[int, list[str]]:
    """Rewrite every TEXT file whose checkout holds CRLF to LF. Returns (files rewritten, files left: a lone CR that no
    mechanical rule may remove — the file is either declared byte-exact in .gitattributes, with its reason, or edited)."""
    rewritten, left = 0, []
    for e in entries(root):
        if e.byte_exact or not e.index or e.worktree not in ("crlf", "mixed", "-text"):
            continue
        path = root / e.path
        data = path.read_bytes()
        if NUL in data or CR not in data:
            continue
        fixed = data.replace(CRLF, LF)
        if fixed != data:
            path.write_bytes(fixed)
            rewritten += 1
        if CR in fixed:
            left.append(e.path)
    return rewritten, left


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
        rewritten, left = fix_tree(root)
        expect("--fix rewrites the CRLF checkout to LF and leaves nothing", (rewritten, left), (1, []))
        expect("--fix leaves the bytes LF", (root / "crlf.txt").read_bytes(), b"one" + LF + b"two" + LF)
        expect("--fix never touches a declared -text file", (root / "pinned.xml").read_bytes(), b"<a/>" + CRLF)
        _, in_index, in_tree = scan_tree(root)
        expect("after --fix the tree is clean", (in_index, in_tree), ([], []))

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
        "",
    ])
    lines = sorted(c.lineno for c in writer_calls(planted))
    expect("arm WRITERS: every unstated text-mode write is found, and nothing else", lines,
           [4, 6, 10, 11, 12, 13, 14, 19, 23])
    fixed, n = add_newline(planted)
    expect("--fix-writers changes every finding", n, 9)
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
        rewritten, left = fix_tree(args.root)
        print(f"=== LINE ENDINGS FIX: {rewritten} working-tree file(s) rewritten CRLF -> LF ===")
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
