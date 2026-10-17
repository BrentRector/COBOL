#!/usr/bin/env python3
"""temp_names.py — NO SCRIPT PUTS A FIXED NAME IN THE MACHINE-WIDE TEMP DIRECTORY (kb/Work PB2922).

    python scripts/temp_names.py --check        # every tracked script under live_tools.ROOTS; exit 1 on a finding
    python scripts/temp_names.py --self-test    # the planted cases, then --check's scan of this tree

⛔ WHY. The machine-wide temp directory (TEMP/TMP on Windows, TMPDIR or /tmp elsewhere) is ONE directory for every
checkout, gate, session and agent on the host. A fixed name there is one file for all of them. orchestrate.ps1's dry run
wrote its steer to `orchestrate-dryrun-<pid>.handoff.operator.json` there, and its self-test counted every file of that
shape: another dry run on the host, or one a process-tree kill left behind, turned four landings' SELF-TESTS audit RED in
the same minute on 2026-10-16. A fixed name is a collision between runs whatever the load, and a leak that outlives
the run. The shapes that do not collide: a name the run makes unique (a guid, the process id, `mktemp`,
`tempfile.TemporaryDirectory`), or a directory the CALLER owns (a coordination directory, a worktree's own scratch).

THE RULE, on code lines (whole-line comments and PowerShell block comments are skipped), per language:
  a temp ROOT   the machine-wide temp directory however the language names it: the TEMP, TMP or TMPDIR environment
                variable, the API, or the literal `/tmp`. PowerShell `[IO.Path]::GetTempPath()`, `$env:TEMP`/`TMP`/
                `TMPDIR`, `[Environment]::GetEnvironmentVariable('TEMP')`; Python `tempfile.gettempdir()`,
                `os.environ['TEMP']`, `os.environ.get('TEMP', d)`, `os.getenv('TMPDIR')`, `'/tmp'`, any of them as an
                operand of `or` or wrapped in `Path(…)`; bash `/tmp`, `$TEMP`, `$TMPDIR`, `${TMPDIR:-/tmp}`, and
                `$TMP` unless the script assigns TMP itself (then it is the script's own `mktemp -d` directory);
  is a finding when it is JOINED to a fixed name: a literal with no unique part (PowerShell: a single-quoted string,
                or a double-quoted one with no `$`; Python: a string constant or an f-string with no field; bash: the
                rest of the path) on a line with no uniqueness token (`UNIQUE`);
  or when it is ASSIGNED BARE to a variable: every later join of that variable (`TMP="${TMPDIR:-/tmp}"`, then
                `"$TMP/gf_population.txt"`) is out of this check's sight, so the bare root never goes into a variable;
                `mktemp -d`, `New-Item` under a guid name or `tempfile.TemporaryDirectory()` makes the variable a
                directory of the run's own;
  or, in Python, a literal path under `/tmp` with no unique part given to `Path`/`open`/`join` or assigned
                (`Path('/tmp/fixed.txt')`).
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

import live_tools  # this directory's: the one list of the live tools' trees, which the scan reads

REPO = Path(__file__).resolve().parents[1]
SUFFIXES = (".py", ".ps1", ".psm1", ".sh")

#: A part that makes a name the run's own. `$(date` is a per-run output directory named to the second.
UNIQUE = re.compile(r"guid|uuid|\$PID\b|\$\$|getpid|mktemp|mkdtemp|XXXXXX|\$RANDOM|\$BASHPID|GetRandomFileName"
                    r"|GetTempFileName|New-TemporaryFile|TemporaryDirectory|NamedTemporaryFile|\$\(date\b", re.I)

#: The environment variables that name the machine-wide temp directory, in every language.
TEMP_VARS = frozenset({"TEMP", "TMP", "TMPDIR"})
_ENV_ALT = "TEMP|TMPDIR|TMP"
#: Python calls whose first argument is a path the call reads or writes (`Path('/tmp/x')`, `open('/tmp/x', 'w')`).
PATH_OPENERS = frozenset({"Path", "PurePath", "open", "join"})

PS_ROOT = (r"(?:\[(?:System\.)?IO\.Path\]::GetTempPath\(\)|\$env:(?:" + _ENV_ALT + r")\b"
           r"|\[(?:System\.)?Environment\]::GetEnvironmentVariable\(\s*['\"](?:" + _ENV_ALT + r")['\"]\s*\))")
#: The root, then (after its closing parenthesis, a comma or -ChildPath) a quoted string: the name joined to it.
PS_JOIN = re.compile(PS_ROOT + r"""\)?\s*(?:,\s*|-ChildPath\s+)?\(?\s*(['"])(.*?)\1""", re.I)
#: The root inside a double-quoted path string: "$env:TEMP\\name".
PS_INLINE = re.compile(r"""\$(?:\(\s*)?env:(?:""" + _ENV_ALT + r""")\)?[\\/][^"$]*["]""", re.I)
PS_BARE = re.compile(r"^\s*\$[\w:]+\s*=\s*\(?\s*" + PS_ROOT + r"\s*\)?\s*;?\s*$", re.I)


def _sh_root(env_vars: str) -> str:
    """The temp root in bash: `/tmp`, or one of `env_vars` as `$V`, `${V}` or `${V:-default}`."""
    return (r"(?:\$\{(?:" + env_vars + r")(?::?[-=][^}]*)?\}|\$(?:" + env_vars + r")\b"
            r"|(?<![\w.$}/~-])/tmp\b)")


def _sh_rules(env_vars: str) -> tuple[re.Pattern[str], re.Pattern[str]]:
    root = _sh_root(env_vars)
    return (re.compile(root + r"/[\w.-]"),
            re.compile(r"""^\s*(?:export\s+|local\s+)?\w+=["']?""" + root + r"""["']?\s*;?\s*$"""))


#: `$TMP` is the temp root (Git Bash and many hosts export it) unless the script assigns TMP itself
#: (guard-fast.sh's `TMP="$(mktemp -d …)"`, a directory of the run's own).
SH_RULES_ENV = _sh_rules(_ENV_ALT)
SH_RULES_OWN_TMP = _sh_rules("TEMP|TMPDIR")
SH_ASSIGNS_TMP = re.compile(r"^\s*(?:export\s+|local\s+|declare\s+-\w+\s+)?TMP=", re.M)


@dataclass(frozen=True)
class Finding:
    path: str
    line: int
    why: str
    text: str

    def __str__(self) -> str:
        return f"{self.path}:{self.line}: {self.why}: {self.text.strip()}"


#: The one line break of a tracked script: the tree is LF (line_endings.py), and LF is what Python's `ast` line numbers,
#: PowerShell and bash count.
LINE_BREAK = "\n"


def source_lines(text: str) -> list[str]:
    """The file's lines, split at LINE_BREAK only. `str.splitlines` also splits at U+2028, U+2029, form feed and the
    other Unicode breaks a string literal may hold (conformance-suite/harness/run.py's normalization cases hold two),
    so its line numbers drift from the file's and a finding names the wrong line (kb/Work PB3176, branch review R2-1:
    a line planted at run.py:862 was reported with the text of line 856)."""
    return text.split(LINE_BREAK)


def _code_lines(text: str, block_comments: bool) -> list[tuple[int, str]]:
    """(line number, line) for each line that is code: not a whole-line `#` comment, not inside a `<# … #>` block."""
    out, in_block = [], False
    for n, line in enumerate(source_lines(text), 1):
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
    sh_join, sh_bare = SH_RULES_OWN_TMP if SH_ASSIGNS_TMP.search(text) else SH_RULES_ENV
    for n, line in _code_lines(text, block_comments=False):
        if sh_bare.search(line):
            found.append(Finding(path, n, "the bare temp root assigned to a variable (hides every later join)", line))
        elif sh_join.search(line) and not UNIQUE.search(line):
            found.append(Finding(path, n, "a fixed name joined to the temp root", line))
    return found


def _call_name(node: ast.AST) -> str | None:
    if not isinstance(node, ast.Call):
        return None
    f = node.func
    return f.attr if isinstance(f, ast.Attribute) else f.id if isinstance(f, ast.Name) else None


def _str_value(node: ast.AST) -> str | None:
    return node.value if isinstance(node, ast.Constant) and isinstance(node.value, str) else None


def _is_environ(node: ast.AST) -> bool:
    """`os.environ` or a bare `environ` (from `from os import environ`)."""
    return (isinstance(node, ast.Attribute) and node.attr == "environ") or (
        isinstance(node, ast.Name) and node.id == "environ")


def _is_env_temp(node: ast.AST) -> bool:
    """The temp root read from the environment: `os.environ['TEMP']`, `os.environ.get('TMP', d)`, `os.getenv('TMPDIR')`."""
    if isinstance(node, ast.Subscript):
        return _is_environ(node.value) and _str_value(node.slice) in TEMP_VARS
    name = _call_name(node)
    if (name == "get" and isinstance(node.func, ast.Attribute) and _is_environ(node.func.value)) or name == "getenv":
        return bool(node.args) and _str_value(node.args[0]) in TEMP_VARS
    return False


def _is_temp_root(node: ast.AST) -> bool:
    """The machine-wide temp directory as Python names it: `tempfile.gettempdir()`; the TEMP/TMP/TMPDIR environment
    variable (`os.environ[…]`, `os.environ.get(…)`, `os.getenv(…)`); the literal `'/tmp'`; any of them as an operand
    of `or` (`os.getenv('TMPDIR') or '/tmp'`) or wrapped in `Path(…)` / `pathlib.Path(…)`."""
    if _str_value(node) in ("/tmp", "/tmp/"):
        return True
    if isinstance(node, ast.BoolOp):
        return any(_is_temp_root(v) for v in node.values)
    if _is_env_temp(node):
        return True
    name = _call_name(node)
    if name == "gettempdir":
        return True
    return name in ("Path", "PurePath") and len(node.args) == 1 and _is_temp_root(node.args[0])


def _fixed_literal(node: ast.AST) -> bool:
    if isinstance(node, ast.Constant):
        return isinstance(node.value, str) and not UNIQUE.search(node.value)
    return isinstance(node, ast.JoinedStr) and not any(isinstance(v, ast.FormattedValue) for v in node.values)


def _fixed_tmp_path(node: ast.AST) -> bool:
    """A literal path under `/tmp` with no unique part: `'/tmp/fixed.txt'`, `f'/tmp/x.log'`."""
    if isinstance(node, ast.JoinedStr):
        head = _str_value(node.values[0]) if node.values else None
        return head is not None and head.startswith("/tmp/") and _fixed_literal(node)
    s = _str_value(node)
    return s is not None and s.startswith("/tmp/") and len(s) > len("/tmp/") and _fixed_literal(node)


def scan_py(path: str, text: str) -> list[Finding]:
    tree = ast.parse(text)
    lines = source_lines(text)
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
        elif _call_name(node) in PATH_OPENERS and node.args and _fixed_tmp_path(node.args[0]):
            hit(node, "a fixed path under /tmp")
        elif isinstance(node, (ast.Assign, ast.AnnAssign)) and node.value is not None:
            # `out = os.environ.get('TEMP', '.')`, and the same root as one element of a tuple assignment
            # (`pages, dpi, out = [], 300, os.environ.get('TEMP', '.')`, render-spec-page.py's shape).
            values = node.value.elts if isinstance(node.value, ast.Tuple) else [node.value]
            if any(_is_temp_root(v) for v in values):
                hit(node, "the bare temp root assigned to a variable (hides every later join)")
            elif any(_fixed_tmp_path(v) for v in values):
                hit(node, "a fixed path under /tmp")
    return found


SCANNERS = {".py": scan_py, ".ps1": scan_ps, ".psm1": scan_ps, ".sh": scan_sh}


def scan(path: str, text: str) -> list[Finding]:
    return SCANNERS[Path(path).suffix](path, text)


def live_scripts(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z", "--", *live_tools.ROOTS], capture_output=True,
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
    # the environment-variable roots and the literal /tmp (review finding R1-1: render-spec-page.py wrote the fixed
    # spec_p<N>.png into os.environ['TEMP'], and the Python arm modelled only tempfile.gettempdir())
    ("a.py", "import os\npages, dpi, out = [], 300, os.environ.get('TEMP', '.')", 1),
    ("a.py", "import os\np = os.path.join(os.environ.get('TEMP', '.'), 'spec_p1.png')", 1),
    ("a.py", "import os\nd = os.environ['TEMP']", 1),
    ("a.py", "import os\np = os.path.join(os.getenv('TMPDIR'), 'x')", 1),
    ("a.py", "from os import environ, getenv\nd = environ.get('TMP') or '/tmp'", 1),
    ("a.py", "import pathlib\np = pathlib.Path('/tmp/fixed.txt')", 1),
    ("a.py", "import pathlib\np = pathlib.Path('/tmp') / 'fixed.txt'", 1),
    ("a.py", "open('/tmp/fixed.log', 'w').write('x')", 1),
    ("a.ps1", "$s = Join-Path $env:TMPDIR 'x.json'", 1),
    ("a.ps1", "$s = Join-Path ([Environment]::GetEnvironmentVariable('TEMP')) 'x.json'", 1),
    ("a.sh", "echo x > \"$TEMP/fixed.log\"", 1),
    ("a.sh", "echo x > \"${TMP:-/tmp}/fixed.log\"", 1),
    ("a.sh", "echo x > \"$TMP/fixed.log\"", 1),
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
    # a script that assigns TMP itself owns that directory: its `$TMP/name` joins are no finding
    ("b.sh", "TMP=\"$(mktemp -d)\"\necho x > \"$TMP/gf_population.txt\"", 0),
    ("b.py", "import os, tempfile\nhome = os.environ.get('HOME')\nout = tempfile.mkdtemp(prefix='spec-pages-')\n"
             "check(native_path('/tmp/x'), '/tmp/x')\np = os.path.join(os.environ['TEMP'], f'x-{os.getpid()}')", 0),
    ("b.ps1", "$d = Join-Path $env:TEMP \"run-$PID\"\n$h = $env:HOME", 0),
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
    # Review finding R2-1 (kb/Work PB3176): this scan kept its own copy of the live trees and missed the conformance
    # suite's harness, which the self-test runner and line_endings.py's WRITERS arm both read as live.
    reached = sorted(r for r in live_tools.ROOTS if any(p.startswith(r) for p in live_scripts(REPO)))
    expect("the scan reaches every live tree (live_tools.ROOTS), the conformance suite's harness among them",
           reached, sorted(live_tools.ROOTS))
    sep = chr(0x2028)  # a Unicode line separator inside a string literal is no line break in the file
    for path, lines in (("c.py", [f"s = '{sep}'", "import tempfile, pathlib",
                                  "p = pathlib.Path(tempfile.gettempdir()) / 'x.json'"]),
                        ("c.ps1", [f"$s = 'a{sep}b'", "$p = Join-Path $env:TEMP 'x.json'"])):
        expect(f"{path}: a finding after a U+2028 in a literal names its own line and text",
               [(f.line, f.text) for f in scan(path, LINE_BREAK.join(lines))], [(len(lines), lines[-1])])
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
