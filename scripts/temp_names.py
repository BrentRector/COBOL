#!/usr/bin/env python3
"""temp_names.py — NO SCRIPT PUTS A FIXED NAME IN THE MACHINE-WIDE TEMP DIRECTORY (kb/Work PB2922).

    python scripts/temp_names.py --check        # every tracked script under scripts/ and .claude/; exit 1 on a finding
    python scripts/temp_names.py --self-test    # the planted cases, then --check's scan of this tree

⛔ WHY. The machine-wide temp directory (TEMP/TMP on Windows, TMPDIR or /tmp elsewhere) is ONE directory for every
checkout, gate, session and agent on the host. A fixed name there is one file for all of them. orchestrate.ps1's dry run
wrote its steer to `orchestrate-dryrun-<pid>.handoff.operator.json` there, and its self-test counted every file of that
shape: another dry run on the host, or one a process-tree kill left behind, turned four landings' SELF-TESTS audit RED in
the same minute on 2026-10-16. A fixed name is a collision between runs whatever the load, and a leak that outlives
the run. The shapes that do not collide: a name the run makes unique (a guid, the process id, `mktemp`,
`tempfile.TemporaryDirectory`), or a directory the CALLER owns (a coordination directory, a worktree's own scratch).

THE RULE, on code lines (whole-line comments and PowerShell block comments are skipped), per language:
  a temp ROOT   PowerShell `[IO.Path]::GetTempPath()`, `$env:TEMP`, `$env:TMP`; Python `tempfile.gettempdir()`;
                bash `/tmp`, `$TMPDIR`, `${TMPDIR:-/tmp}`;
  is a finding when it is JOINED to a fixed name: a literal with no unique part (PowerShell: a single-quoted string,
                or a double-quoted one with no `$`; Python: a string constant or an f-string with no field; bash: the
                rest of the path) on a line with no uniqueness token (`UNIQUE`);
  or when it is ASSIGNED BARE to a variable: every later join of that variable (`TMP="${TMPDIR:-/tmp}"`, then
                `"$TMP/gf_population.txt"`) is out of this check's sight, so the bare root never goes into a variable;
                `mktemp -d`, `New-Item` under a guid name or `tempfile.TemporaryDirectory()` makes the variable a
                directory of the run's own.
Passing the bare root as a value (`cwd=tempfile.gettempdir()`, `mktemp -d "${TMPDIR:-/tmp}/x.XXXXXX"`) is no finding.
Python is read from its syntax tree; PowerShell and bash by line, as written in this repository.
"""
from __future__ import annotations

import argparse
import ast
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
#: The live tools (as line_endings.py's WRITERS arm): `tools/claude-skills` is a submodule with its own repository.
LIVE_ROOTS = ("scripts/", ".claude/")
SUFFIXES = (".py", ".ps1", ".psm1", ".sh")

#: A part that makes a name the run's own. `$(date` is a per-run output directory named to the second.
UNIQUE = re.compile(r"guid|uuid|\$PID\b|\$\$|getpid|mktemp|mkdtemp|XXXXXX|\$RANDOM|\$BASHPID|GetRandomFileName"
                    r"|GetTempFileName|New-TemporaryFile|TemporaryDirectory|NamedTemporaryFile|\$\(date\b", re.I)

PS_ROOT = r"(?:\[(?:System\.)?IO\.Path\]::GetTempPath\(\)|\$env:(?:TEMP|TMP)\b)"
#: The root, then (after its closing parenthesis, a comma or -ChildPath) a quoted string: the name joined to it.
PS_JOIN = re.compile(PS_ROOT + r"""\)?\s*(?:,\s*|-ChildPath\s+)?\(?\s*(['"])(.*?)\1""", re.I)
#: The root inside a double-quoted path string: "$env:TEMP\\name".
PS_INLINE = re.compile(r"""\$(?:\(\s*)?env:(?:TEMP|TMP)\)?[\\/][^"$]*["]""", re.I)
PS_BARE = re.compile(r"^\s*\$[\w:]+\s*=\s*\(?\s*" + PS_ROOT + r"\s*\)?\s*;?\s*$", re.I)

SH_ROOT = r"(?:\$\{TMPDIR:-/tmp\}|\$\{TMPDIR\}|\$TMPDIR\b|(?<![\w.$}/~-])/tmp\b)"
SH_JOIN = re.compile(SH_ROOT + r"/[\w.-]")
SH_BARE = re.compile(r"""^\s*(?:export\s+|local\s+)?\w+=["']?""" + SH_ROOT + r"""["']?\s*;?\s*$""")


@dataclass(frozen=True)
class Finding:
    path: str
    line: int
    why: str
    text: str

    def __str__(self) -> str:
        return f"{self.path}:{self.line}: {self.why}: {self.text.strip()}"


def _code_lines(text: str, block_comments: bool) -> list[tuple[int, str]]:
    """(line number, line) for each line that is code: not a whole-line `#` comment, not inside a `<# … #>` block."""
    out, in_block = [], False
    for n, line in enumerate(text.splitlines(), 1):
        s = line.strip()
        if block_comments:
            if in_block:
                in_block = "#>" not in s
                continue
            if s.startswith("<#"):
                in_block = "#>" not in s
                continue
        if s.startswith("#"):
            continue
        out.append((n, line))
    return out


def scan_ps(path: str, text: str) -> list[Finding]:
    found = []
    for n, line in _code_lines(text, block_comments=True):
        if PS_BARE.search(line):
            found.append(Finding(path, n, "the bare temp root assigned to a variable (hides every later join)", line))
            continue
        if UNIQUE.search(line):
            continue
        for m in PS_JOIN.finditer(line):
            if m.group(1) == "'" or "$" not in m.group(2):
                found.append(Finding(path, n, f"a fixed name '{m.group(2)}' joined to the temp root", line))
        if PS_INLINE.search(line):
            found.append(Finding(path, n, "a fixed name inside a temp-root path string", line))
    return found


def scan_sh(path: str, text: str) -> list[Finding]:
    found = []
    for n, line in _code_lines(text, block_comments=False):
        if SH_BARE.search(line):
            found.append(Finding(path, n, "the bare temp root assigned to a variable (hides every later join)", line))
        elif SH_JOIN.search(line) and not UNIQUE.search(line):
            found.append(Finding(path, n, "a fixed name joined to the temp root", line))
    return found


def _is_temp_root(node: ast.AST) -> bool:
    """`tempfile.gettempdir()`, `gettempdir()`, or either wrapped in `Path(…)` / `pathlib.Path(…)`."""
    if not isinstance(node, ast.Call):
        return False
    f = node.func
    name = f.attr if isinstance(f, ast.Attribute) else f.id if isinstance(f, ast.Name) else None
    if name == "gettempdir":
        return True
    return name in ("Path", "PurePath") and len(node.args) == 1 and _is_temp_root(node.args[0])


def _fixed_literal(node: ast.AST) -> bool:
    if isinstance(node, ast.Constant):
        return isinstance(node.value, str) and not UNIQUE.search(node.value)
    return isinstance(node, ast.JoinedStr) and not any(isinstance(v, ast.FormattedValue) for v in node.values)


def scan_py(path: str, text: str) -> list[Finding]:
    tree = ast.parse(text)
    lines = text.splitlines()
    found = []

    def hit(node: ast.AST, why: str) -> None:
        found.append(Finding(path, node.lineno, why, lines[node.lineno - 1]))

    for node in ast.walk(tree):
        if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Div) and _is_temp_root(node.left):
            if _fixed_literal(node.right):
                hit(node, "a fixed name joined to the temp root")
        elif (isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute) and node.func.attr == "join"
              and node.args and _is_temp_root(node.args[0])):
            if node.args[1:] and all(_fixed_literal(a) for a in node.args[1:]):
                hit(node, "a fixed name joined to the temp root")
        elif isinstance(node, (ast.Assign, ast.AnnAssign)) and node.value is not None and _is_temp_root(node.value):
            hit(node, "the bare temp root assigned to a variable (hides every later join)")
    return found


SCANNERS = {".py": scan_py, ".ps1": scan_ps, ".psm1": scan_ps, ".sh": scan_sh}


def scan(path: str, text: str) -> list[Finding]:
    return SCANNERS[Path(path).suffix](path, text)


def live_scripts(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z", "--", *LIVE_ROOTS], capture_output=True,
                         check=True).stdout.decode("utf-8")
    return sorted(p for p in out.split("\0") if p.endswith(SUFFIXES))


def scan_tree(root: Path) -> list[Finding]:
    found = []
    for rel in live_scripts(root):
        p = root / rel
        if p.is_file():  # a tracked file deleted in this working tree is no script
            found += scan(rel, p.read_text(encoding="utf-8"))
    return found


def check(root: Path) -> int:
    found = scan_tree(root)
    for f in found:
        print(f"  {f}")
    if found:
        print(f"=== TEMP NAMES: RED — {len(found)} fixed name(s) in the machine-wide temp directory: give each a name "
              "the run makes unique (guid, pid, mktemp, tempfile.TemporaryDirectory) or a directory the caller owns "
              "(kb/Work PB2922) ===")
        return 1
    print(f"=== TEMP NAMES: GREEN — {len(live_scripts(root))} scripts, no fixed name in the machine-wide temp directory ===")
    return 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

#: (file name, source, how many findings). The first rows are the PLANTED violations — the shapes that reached the
#: tree (orchestrate.ps1's dry-run steer, semgrep/verify.py, arch/r2_inputs.py, guard-fast.sh, guard-verdict.sh).
CASES = (
    ("a.ps1", "$s = Join-Path ([IO.Path]::GetTempPath()) 'orchestrate-dryrun.handoff.operator.json'", 1),
    ("a.ps1", "$s = Join-Path ([System.IO.Path]::GetTempPath()) \"steer.json\"", 1),
    ("a.ps1", "$s = Join-Path $env:TEMP 'x.json'", 1),
    ("a.ps1", "$s = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'x.json')", 1),
    ("a.ps1", "$s = \"$env:TMP\\x.json\"", 1),
    ("a.ps1", "$t = [IO.Path]::GetTempPath()", 1),
    ("a.py", "import tempfile, pathlib\np = pathlib.Path(tempfile.gettempdir()) / 'cobolnet-invariants-nopaths.yml'", 1),
    ("a.py", "import tempfile, os\np = os.path.join(tempfile.gettempdir(), 'r2root')", 1),
    ("a.py", "import tempfile\nroot = tempfile.gettempdir()", 1),
    ("a.sh", "POP=\"${TMPDIR:-/tmp}/gf_population.txt\"", 1),
    ("a.sh", "TMP=\"${TMPDIR:-/tmp}\"", 1),
    ("a.sh", "curl -o /tmp/dotnet-install.sh https://example.invalid", 1),
    ("a.sh", "F=\"${F:-${TMPDIR:-/tmp}/nist-forensics}\"", 1),
    # the shapes that are a run's own, or no name at all: no finding
    ("b.ps1", "$d = Join-Path ([IO.Path]::GetTempPath()) \"orch-test-$([guid]::NewGuid().ToString('N'))\"", 0),
    ("b.ps1", "$d = Join-Path ([IO.Path]::GetTempPath()) (\"autostart-test-\" + [guid]::NewGuid().ToString('N'))", 0),
    ("b.ps1", "$d = Join-Path $env:TEMP \"claude/$sessionId/tasks\"", 0),
    ("b.ps1", "<#\nJoin-Path $env:TEMP 'in-a-block-comment'\n#>\n# Join-Path $env:TEMP 'in-a-comment'", 0),
    ("b.py", "import tempfile, os, pathlib\nr = subprocess.run(['x'], cwd=tempfile.gettempdir())\n"
             "p = pathlib.Path(tempfile.gettempdir()) / f'x-{os.getpid()}'\nq = pathlib.Path(tempfile.gettempdir()) / name", 0),
    ("b.py", "import tempfile\nwith tempfile.TemporaryDirectory() as tmp:\n    pass\n'''tempfile.gettempdir() / \"doc\"'''", 0),
    ("b.sh", "G=\"${G:-${TMPDIR:-/tmp}/nist-forensics.$$}\"\nd=\"$(mktemp -d -t guard.XXXXXX)\"", 0),
    ("b.sh", "OUT=\"${1:-${TMPDIR:-/tmp}/battery-$(date +%Y%m%d-%H%M%S)}\"\n# curl -o /tmp/in-a-comment.sh", 0),
    ("b.sh", "T=\"$(mktemp -d \"${TMPDIR:-/tmp}/guard-fast.XXXXXX\")\"\nx=\"$HOME/tmp/q\"", 0),
)


def self_test() -> int:
    failures = []

    def expect(name: str, got, want) -> None:
        ok = got == want
        print(f"{'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(name)

    for path, source, want in CASES:
        found = scan(path, source)
        expect(f"{path} {source.splitlines()[0][:70]!r} -> {want} finding(s)", len(found), want)
    found = scan_tree(REPO)
    for f in found:
        print(f"  {f}")
    expect("this tree's scripts name no fixed path in the machine-wide temp directory", len(found), 0)
    print(f"=== TEMP NAMES SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    group = ap.add_mutually_exclusive_group(required=True)
    group.add_argument("--check", action="store_true")
    group.add_argument("--self-test", action="store_true")
    ap.add_argument("--root", type=Path, default=REPO, help=argparse.SUPPRESS)
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    return self_test() if args.self_test else check(args.root)


if __name__ == "__main__":
    sys.exit(main())
