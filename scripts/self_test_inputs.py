#!/usr/bin/env python3
"""self_test_inputs.py — WHAT A SCRIPT SELF-TEST READ, recorded with its PASS, so a gate re-runs a self-test only when
something it reads has changed (kb/Work PB2914; docs/rearchitecture/DESIGN-test-build-ci.md §3.14.10).

    python scripts/self_test_inputs.py <self-test path>…   # each one's static inputs on this tree
    python scripts/self_test_inputs.py --self-test

⛔ WHY. A self-test whose inputs did not change cannot change its verdict, so a gate that re-runs it learns nothing and
pays for it: every implementer gate ran all 68 (kb/Work PB2563), the slowest 4-8 minutes on a quiet host, and a fleet's
gates ran them all at once until they passed the runner's time limit and failed every gate (kb/Work PB2914).
scripts/self_tests.py therefore records, with each PASS, the CONTENT of everything the self-test read, and an
implementer gate reuses that PASS while all of it is unchanged. The lander's gate and CI run every self-test and record.

THE INPUTS OF A RUN (`Deps`) are OBSERVED, not guessed — a list of what a script mentions is far wider than what its
self-test reads (a hand-derived closure over mentions reached the whole tree from most orchestrator scripts):
  TRACED   every self-test the runner RUNS runs with this module's TRACER as the `sitecustomize` of every Python process
           it starts (PYTHONPATH): through the interpreter's audit hooks (PEP 578) it records each file opened (a
           module's compiled `__pycache__` file counts as its source), each directory listed and each path handed to a
           child process, inside the repository;
  STATIC   what the trace cannot see, from the source (`Deriver`): the self-test's own file; the RUNNER (a change to how
           self-tests run re-runs them all); `# SELF-TEST-READS: <path> …` declarations on a header comment line; and,
           for a PowerShell or bash self-test, whose own reads no audit hook sees, the closure over the files its
           non-comment lines MENTION (a token naming a tracked file or directory relative to the repository root, to the
           mentioning file's directory or to a directory above it, as `Join-Path $PSScriptRoot 'stop.ps1'` does; a
           directory only as a bare word with no `/` and no `.` beside the mentioning file, so `kb/Work` in a message
           string never pulls in the register), followed through the PowerShell and bash scripts it reaches (a Python
           script it runs is traced when it runs);
  GIT      a `git` child a traced process runs inside the repository reads what `git_reads` says: nothing for its
           location, configuration, worktrees or a commit id (GIT_ENVIRONMENT); otherwise the NAMES (`ls-files`) or
           the CONTENT (`diff`, `ls-files -s`, `show <rev>:<path>`, any other command) under its pathspec — the whole
           tree when it names none.
A run's Deps hold, per input file, its git blob id (`-` when the run looked for an untracked path), and per directory it
saw a digest of what it saw (`Tree.digest`: the names in a listed directory, the paths or the content under a git
pathspec or a STATIC directory). A recorded PASS is REUSED only while every one of them is unchanged on the current tree
(`Deps.changed` is empty): an edit to anything it read, a new file in a directory it listed, a tracked file where it
found none — each re-runs it. The blob ids are the index's for an unedited file and `git hash-object`'s
for an edited one (a gate tests the working tree), a submodule's commit for anything under it.

⛔ WHAT NOTHING HERE SEES, named rather than hidden: a PowerShell or bash process's own reads of a path it builds at run
time (declare it: `SELF-TEST-READS`) and the git commands it runs itself, and `os.stat`-style probes (no audit event).
A trace that does not contain a Python self-test's own file means the tracer did not run: the runner records no PASS
for it. The backstop for all three is the lander's gate and CI, which never reuse a PASS.
"""
from __future__ import annotations

import ast
import bisect
import hashlib
import json
import os
import posixpath
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
#: The runner itself: a change to how a self-test is run (its environment, isolation, tracer) re-runs every self-test.
RUNNER = ("scripts/self_tests.py", "scripts/self_test_inputs.py")
#: Scripts whose own reads the tracer cannot see: their mentions are followed (`Deriver.static`).
UNTRACED_SCRIPTS = (".ps1", ".psm1", ".sh")
READS_RE = re.compile(r"^#\s*SELF-TEST-READS:\s*(\S.*?)\s*$", re.MULTILINE)
TOKEN_RE = re.compile(r"[\w.\-/\\]+")
#: The environment the tracer reads: where its records go, and the repository whose reads it records.
TRACE_ENV = "COBOL_SELF_TEST_TRACE"
TRACE_ROOT_ENV = "COBOL_SELF_TEST_TRACE_ROOT"
UNTRACKED = "-"
#: What a run can have seen of a directory (`Tree.digest`): the names in it, every path under it, or all their content.
DIGESTS = ("entries", "paths", "content")

#: ⛔ THE TRACER — written as `sitecustomize.py` into a directory the runner puts first on PYTHONPATH, so every Python
#: process a traced self-test starts (itself, and every child that inherits its environment) records its reads. It
#: records only paths inside the traced repository, keeps them in memory and writes one file per process at exit;
#: it never prints, so no self-test's output changes. F = a file opened, D = a directory listed, X = a path argument
#: of a child process (a script it runs), G = a `git` child started inside the repository (its cwd).
TRACER = r'''
import atexit, os, sys
_root, _out = os.environ.get("COBOL_SELF_TEST_TRACE_ROOT"), os.environ.get("COBOL_SELF_TEST_TRACE")
if _root and _out:
    _prefix = os.path.normcase(os.path.abspath(_root)) + os.sep
    _seen = set()

    def _inside(path, cwd=None):
        try:
            if isinstance(path, int):
                return None
            path = os.fsdecode(path)
            full = os.path.normcase(os.path.abspath(os.path.join(cwd, path) if cwd else path))
        except Exception:
            return None
        if full.startswith(_prefix):
            return full[len(_prefix):]
        return "" if full + os.sep == _prefix else None

    def _record(kind, path, cwd=None):
        rel = _inside(path, cwd)
        if rel is not None:
            _seen.add(kind + "\t" + rel)

    def _hook(event, args):
        if event == "open":
            _record("F", args[0])
        elif event in ("os.listdir", "os.scandir"):
            _record("D", "." if args[0] is None else args[0])
        elif event == "subprocess.Popen":
            argv, cwd = args[1], args[2]
            try:
                cwd = os.fsdecode(cwd) if cwd is not None else os.getcwd()
            except Exception:
                return
            argv = argv.split() if isinstance(argv, (str, bytes)) else list(argv or ())
            argv = [os.fsdecode(a) for a in argv if isinstance(a, (str, bytes, os.PathLike))]
            for a in argv:
                _record("X", a, cwd)
            if argv and os.path.basename(argv[0]).lower() in ("git", "git.exe"):
                where, rest = cwd, argv[1:]
                while len(rest) >= 2 and rest[0] == "-C":
                    where, rest = os.path.join(where, rest[1]), rest[2:]
                rel = _inside(where)
                if rel is not None:
                    _seen.add("G\t" + rel + "\t" + chr(31).join(rest))  # every argument: the pathspec decides

    sys.addaudithook(_hook)

    def _write():
        name = os.path.join(_out, "%d-%s.trace" % (os.getpid(), os.urandom(6).hex()))
        try:
            with open(name, "w", encoding="utf-8") as fh:
                fh.write("\n".join(sorted(_seen)))
        except OSError:
            # Only an orphan that outlived its self-test's run gets here (the runner removed the trace directory after
            # reading it), and it must not print into whatever output it still has: its reads came too late to count.
            pass

    atexit.register(_write)
'''


# ── the tracked tree ───────────────────────────────────────────────────────────────────────────────────────────────


def _git(repo: Path, *args: str, stdin: str | None = None) -> str:
    return subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True, text=True, encoding="utf-8",
                          input=stdin).stdout


@dataclass
class Tree:
    """The tracked files of one checkout and their content ids (git blob ids; a submodule's commit)."""
    repo: Path
    blobs: dict[str, str]
    gitlinks: frozenset[str]
    paths: list[str] = field(init=False)
    dirs: frozenset[str] = field(init=False)
    _folded: dict[str, str] = field(init=False)
    _digests: dict[tuple[str, str], str] = field(init=False, default_factory=dict)

    def __post_init__(self) -> None:
        self.paths = sorted(self.blobs)
        dirs = {""}
        for p in self.paths:
            parts = p.split("/")[:-1]
            for i in range(1, len(parts) + 1):
                dirs.add("/".join(parts[:i]))
        self.dirs = frozenset(dirs)
        # Windows compares paths without case, and the trace records them normcase'd.
        self._folded = {os.path.normcase(p.replace("/", os.sep)): p for p in (*self.paths, *self.dirs)}

    @classmethod
    def load(cls, repo: Path) -> "Tree":
        """`git ls-files -s` for the index, then `git hash-object` for every file the working tree has edited (the
        marker DELETED for one it has removed) and for every UNTRACKED file git does not ignore: a gate tests the working
        tree, uncommitted edits and not-yet-added files included, so an untracked helper a self-test imports is keyed
        by its content, never by the "-" of a path that is absent (train 1052 review: a false REUSED)."""
        blobs, gitlinks = {}, set()
        for entry in _git(repo, "ls-files", "-s", "-z").split("\0"):
            if not entry:
                continue
            meta, path = entry.split("\t", 1)
            mode, blob, _stage = meta.split(" ")
            blobs[path] = blob
            if mode == "160000":
                gitlinks.add(path)
        edited = [p for p in _git(repo, "ls-files", "-m", "-z").split("\0") if p and p not in gitlinks]
        present = [p for p in edited if (repo / p).is_file()]
        blobs.update((p, "DELETED") for p in edited if p not in present)
        present += [p for p in _git(repo, "ls-files", "-o", "--exclude-standard", "-z").split("\0")
                    if p and "__pycache__/" not in p and (repo / p).is_file()]   # a .pyc is keyed by its source (owner)
        if present:
            ids = _git(repo, "hash-object", "--stdin-paths", stdin="\n".join(present) + "\n").split()
            blobs.update(zip(present, ids))
        return cls(repo, blobs, frozenset(gitlinks))

    def under(self, d: str) -> list[str]:
        """Every tracked path under directory `d` (`""`: the whole tree)."""
        if not d:
            return list(self.paths)
        return self.paths[bisect.bisect_left(self.paths, d + "/"):bisect.bisect_left(self.paths, d + "0")]

    def entries(self, d: str) -> list[str]:
        """The names directly in directory `d` that the tree tracks (files and directories)."""
        cut = len(d) + 1 if d else 0
        return sorted({p[cut:].split("/", 1)[0] for p in self.under(d)})

    def digest(self, kind: str, d: str) -> str:
        """What a run saw of directory `d` (`""`: the whole tree), as one digest — by KIND (DIGESTS): `entries`, the
        names directly in it (a directory listing); `paths`, every tracked path under it (`git ls-files`); `content`,
        every tracked path under it and its content id (a STATIC directory input, `git diff` or `ls-files -s`)."""
        if (kind, d) not in self._digests:
            if kind == "entries":
                lines = self.entries(d)
            elif kind == "paths":
                lines = self.under(d)
            elif kind == "content":
                lines = [f"{p}\0{self.blobs[p]}" for p in self.under(d)]
            else:
                raise ValueError(f"a tree digest kind is one of {DIGESTS}, not {kind!r}")
            self._digests[(kind, d)] = hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest()[:20]
        return self._digests[(kind, d)]

    def blob(self, path: str) -> str:
        return self.blobs.get(path, UNTRACKED)

    def owner(self, rel: str) -> str | None:
        """The tracked path a repository-relative path names: itself, the gitlink of the submodule it sits in, or —
        for a compiled module (`__pycache__/m.cpython-3xx.pyc`) — its source. None when it names none."""
        if rel in self.blobs or rel in self.dirs:
            return rel
        head, _, name = rel.rpartition("/")
        if head.rpartition("/")[2] == "__pycache__" and name.endswith(".pyc"):
            return self.owner(posixpath.join(posixpath.dirname(head), name.split(".", 1)[0] + ".py"))
        for g in self.gitlinks:
            if rel.startswith(g + "/"):
                return g
        return None

    def unfold(self, folded: str) -> str:
        """A trace record's normcase'd OS path → the repository-relative path, in the tree's own case when tracked."""
        tracked = self._folded.get(folded)
        if tracked is not None:
            return tracked
        rel = folded.replace(os.sep, "/")
        head = posixpath.dirname(rel)
        while head:  # keep a tracked directory's own case, so an untracked file under it still maps (`owner`)
            known = self._folded.get(os.path.normcase(head.replace("/", os.sep)))
            if known is not None:
                return known + rel[len(head):]
            head = posixpath.dirname(head)
        return rel


# ── the static inputs: what the trace cannot see ───────────────────────────────────────────────────────────────────


def declared_reads(text: str, header_lines: int) -> list[str]:
    header = "\n".join(text.splitlines()[:header_lines])
    return [p for m in READS_RE.finditer(header) for p in m.group(1).split()]


PS_BLOCK_COMMENT_RE = re.compile(r"<#.*?#>", re.DOTALL)


def _code(line: str) -> str:
    """A PowerShell or bash line without its comment: from the first `#` that starts a word outside quotes."""
    quote = None
    for i, c in enumerate(line):
        if quote:
            if c == quote:
                quote = None
        elif c in "'\"":
            quote = c
        elif c == "#" and (i == 0 or line[i - 1].isspace()):
            return line[:i]
    return line


def _script_tokens(text: str) -> set[str]:
    """The tokens of a PowerShell or bash script's CODE: block comments (`<# … #>`) and line comments dropped."""
    text = PS_BLOCK_COMMENT_RE.sub("", text)
    return {t for line in text.splitlines() for t in TOKEN_RE.findall(_code(line))}


class Deriver:
    """The static half of a self-test's inputs (module docstring), each file's mentions resolved once."""

    def __init__(self, tree: Tree, header_lines: int):
        self.tree, self.header_lines = tree, header_lines
        self._mentions: dict[str, tuple[set[str], set[str]]] = {}

    def resolve(self, token: str, here: str, declared: bool = False) -> tuple[set[str], set[str]]:
        """The tracked files and directories one token names (module docstring, STATIC). A MENTIONED directory counts
        only as a bare word beside the mentioning file (`testdata`); a DECLARED one (`declared`) anywhere."""
        t = token.replace("\\", "/").strip("/")
        while t.startswith("./"):
            t = t[2:]
        if not t or t in (".", ".."):
            return set(), set()
        bare = "/" not in t and "." not in t
        bases, d = [""], here
        while d:
            bases.append(d)
            d = posixpath.dirname(d)
        files, dirs = set(), set()
        for b in bases:
            p = posixpath.normpath(posixpath.join(b, t))
            if p.startswith(".."):
                continue
            owner = self.tree.owner(p)
            if owner is None:
                continue
            if owner in self.tree.blobs:
                files.add(owner)
            elif declared or (bare and b == here):
                dirs.add(owner)
        return files, dirs

    def mentions(self, rel: str) -> tuple[set[str], set[str]]:
        if rel not in self._mentions:
            try:
                text = (self.tree.repo / rel).read_text(encoding="utf-8")
            except (OSError, UnicodeDecodeError):
                text = ""
            files, dirs = set(), set()
            here = posixpath.dirname(rel)
            for tok in _script_tokens(text):
                f, d = self.resolve(tok, here)
                files |= f
                dirs |= d
            self._mentions[rel] = (files, dirs)
        return self._mentions[rel]

    def static(self, test: str) -> tuple[set[str], set[str]]:
        """(files, directories) the trace cannot see for self-test `test` — its own file, the runner, its declared
        reads, and for an untraced script the closure over its mentions (module docstring)."""
        files = {test, *(r for r in RUNNER if r in self.tree.blobs)}
        dirs: set[str] = set()
        text = (self.tree.repo / test).read_text(encoding="utf-8")
        for declared in declared_reads(text, self.header_lines):
            f, d = self.resolve(declared, "", declared=True)
            if not f and not d:
                raise ValueError(f"{test}: `SELF-TEST-READS: {declared}` names no tracked file or directory")
            files |= f
            dirs |= d
        if test.endswith(UNTRACED_SCRIPTS):
            queue, seen = [test], {test}
            while queue:
                f, d = self.mentions(queue.pop())
                files |= f
                dirs |= d
                for p in f - seen:
                    seen.add(p)
                    if p.endswith(UNTRACED_SCRIPTS):
                        queue.append(p)
        return files, dirs


# ── the trace ──────────────────────────────────────────────────────────────────────────────────────────────────────


def install_tracer(directory: Path) -> Path:
    """Write the tracer as `sitecustomize.py` into `directory`, which `traced_env` puts first on PYTHONPATH."""
    directory.mkdir(parents=True, exist_ok=True)
    (directory / "sitecustomize.py").write_text(TRACER, encoding="utf-8")
    return directory


def traced_env(env: dict[str, str], tracer_dir: Path, trace_out: Path, repo: Path) -> dict[str, str]:
    env = dict(env)
    prior = env.get("PYTHONPATH")
    env["PYTHONPATH"] = str(tracer_dir) + (os.pathsep + prior if prior else "")
    env[TRACE_ENV], env[TRACE_ROOT_ENV] = str(trace_out), str(repo)
    return env


#: The git commands that read no tracked content of the repository they run in: its location and configuration, the
#: worktree list, creating a repository. Any OTHER git command run inside the repository reads tracked content
#: (`git_reads`), and `log` reads history: only its commit id (`--format=%h`/`%H`) is environment.
GIT_ENVIRONMENT = frozenset({"rev-parse", "config", "init", "worktree", "version", "var", "symbolic-ref"})
GIT_LOG_ID_FORMATS = frozenset({"--format=%h", "--format=%H", "--pretty=%h", "--pretty=%H"})
#: `ls-files` reads only the NAMES of the tracked files, unless one of these asks for their content ids.
GIT_LS_FILES_CONTENT = frozenset({"-s", "--stage", "-m", "--modified", "-d", "--deleted", "--debug"})


def git_reads(args: list[str], where: str) -> list[tuple[str, str]]:
    """What a git command run in repository directory `where` (its arguments after any `-C <dir>`) reads of the
    tracked tree: (kind, path) pairs, kind `paths` (the names under path) or `content` (the names and their content);
    path "" is the whole tree. Its pathspec (after `--`) narrows it; a pathspec with glob magic, and no pathspec, is
    the whole tree. `show <rev>:<path>` reads that path. An environment command reads nothing."""
    opts, specs = (args[:args.index("--")], args[args.index("--") + 1:]) if "--" in args else (args, [])
    sub = next((a for a in opts if not a.startswith("-")), None)
    if sub is None or sub in GIT_ENVIRONMENT or (sub == "log" and any(a in GIT_LOG_ID_FORMATS for a in opts)):
        return []
    if sub == "show" and not specs:
        shown = [a.split(":", 1)[1] for a in opts if ":" in a and not a.startswith("-")]
        if shown:
            return [("content", posixpath.normpath(p)) for p in shown]
    kind = "paths" if sub == "ls-files" and not GIT_LS_FILES_CONTENT.intersection(opts) else "content"
    paths = []
    for spec in specs or [""]:
        if not spec or spec.startswith(":") or any(c in spec for c in "*?["):
            return [(kind, "")]
        p = posixpath.normpath(posixpath.join(where, spec)).strip("/")
        paths.append("" if p == "." else p)
    return [(kind, p) for p in paths]


@dataclass(frozen=True)
class Trace:
    files: frozenset[str]          # repository-relative paths opened or run (tracked or not)
    dirs: frozenset[str]           # tracked directories listed ("" is the repository root)
    git_in_repo: frozenset[str]    # `<dir>: git <command>` for each git child run inside the repository
    git_read: frozenset[tuple[str, str]] = frozenset()   # what those commands read (`git_reads`)

    def saw(self, path: str) -> bool:
        return path in self.files


def read_trace(trace_out: Path, tree: Tree) -> Trace:
    files, dirs, git, git_read = set(), set(), set(), set()
    for f in sorted(trace_out.glob("*.trace")):
        for line in f.read_text(encoding="utf-8").splitlines():
            kind, _, folded = line.partition("\t")
            if kind == "G":
                where, _, command = folded.partition("\t")
                where = tree.unfold(where) if where else ""
                args = command.split(chr(31))  # the tracer joins a command's arguments with the unit separator
                git.add(f"{where or '.'}: git {' '.join(args)}")
                git_read.update(git_reads(args, where))
                continue
            rel = tree.unfold(folded.rstrip(os.sep)) if folded not in ("", ".") else ""
            if rel.startswith(".git/") or rel == ".git" or "/.git/" in rel:
                continue
            owner = tree.owner(rel) if rel else ""
            if kind == "D":
                if owner is not None and owner in tree.dirs:
                    dirs.add(owner)
            elif owner is not None and owner in tree.blobs:
                files.add(owner)
            elif owner is None and "__pycache__" not in rel:
                files.add(rel)  # looked for and not tracked: a tracked file appearing there must re-run it
    return Trace(frozenset(files), frozenset(dirs), frozenset(git), frozenset(git_read))


# ── the deps of one run ────────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True)
class Deps:
    """Everything one run read, with what it read: {path: blob id, or UNTRACKED where it found none} and
    {`<kind>:<directory>`: Tree.digest} — `entries:d` for a directory it listed, `paths:d` for the names a git command
    listed, `content:d` for a STATIC directory input or a git command that read content (`content:` is the whole
    tree)."""
    files: dict[str, str]
    trees: dict[str, str]

    @classmethod
    def of(cls, tree: Tree, files: set[str], seen: set[tuple[str, str]] = frozenset()) -> "Deps":
        """`files` read, and directories `seen` as (kind, directory) pairs — a `content` read of a single tracked file
        is that file."""
        files = set(files) | {d for kind, d in seen if kind == "content" and d in tree.blobs}
        trees = {f"{kind}:{d}": tree.digest(kind, d) for kind, d in seen if not (kind == "content" and d in tree.blobs)}
        return cls({p: tree.blob(p) for p in sorted(files)}, dict(sorted(trees.items())))

    def changed(self, tree: Tree) -> list[str]:
        """What differs on `tree` from what the run read (empty: the PASS still holds)."""
        out = [p for p, blob in self.files.items() if tree.blob(p) != blob]
        for key, digest in self.trees.items():
            kind, _, d = key.partition(":")
            if tree.digest(kind, d) != digest:
                out.append(key)
        return out

    def to_json(self) -> dict:
        return {"files": self.files, "trees": self.trees}

    @classmethod
    def from_json(cls, data: dict) -> "Deps":
        return cls(dict(data["files"]), dict(data["trees"]))


def run_deps(test: str, tree: Tree, deriver: Deriver, trace: Trace) -> Deps:
    """The Deps of one PASSED run: its trace plus its static inputs (a static directory is read whole)."""
    files, dirs = deriver.static(test)
    seen = {("entries", d) for d in trace.dirs} | {("content", d) for d in dirs} | set(trace.git_read)
    return Deps.of(tree, files | set(trace.files), seen)


def main(argv: list[str] | None = None) -> int:
    args = sys.argv[1:] if argv is None else argv
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if args[:1] == ["--self-test"]:
        return self_test()
    sys.path.insert(0, str(REPO / "scripts"))
    import self_tests  # noqa: E402 — the runner owns HEADER_LINES
    tree = Tree.load(REPO)
    deriver = Deriver(tree, self_tests.HEADER_LINES)
    for test in args:
        files, dirs = deriver.static(test)
        print(f"{test}: {len(files)} static files, {len(dirs)} directories")
        for p in sorted(files):
            print(f"  {p}")
        for d in sorted(dirs):
            print(f"  {d}/ ({len(tree.under(d))} files)")
    return 0


def self_test() -> int:
    import tempfile
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        print(f"  {'ok  ' if got == want else 'FAIL'} {name}" + ("" if got == want else f": got {got!r}, want {want!r}"))
        if got != want:
            fails.append(name)

    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        planted = {
            "scripts/o/test_x.ps1": "<#\nkb/Work in a block comment\n#>\n. (Join-Path $Here 'testdata/lib.ps1')\n"
                                    "$s = Join-Path $Here 'stop.ps1'   # docs/ignored.md in a line comment\n"
                                    "Write-Host 'no kb/Work note names it'\n$t = Join-Path $Here testdata\n",
            "scripts/o/testdata/lib.ps1": "& python scripts/o/runner.py\n$d = 'data.json'\n",
            "scripts/o/stop.ps1": "exit 0\n",
            "scripts/o/runner.py": "print(1)\n",
            "scripts/o/testdata/data.json": "{}\n",
            "scripts/o/test_y.py": "#!/usr/bin/env python3\n# SELF-TEST-READS: kb/Work docs/ignored.md\nprint(1)\n",
            "scripts/o/test_z.py": "# SELF-TEST-READS: no/such/path\n",
            "docs/ignored.md": "x\n",
            "kb/Work/n.md": "x\n",
        }
        for rel, text in planted.items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(text, encoding="utf-8")
        blobs = {rel: f"blob-{i}" for i, rel in enumerate(planted)}
        blobs["tools/sub"] = "commit-1"
        tree = Tree(root, blobs, frozenset({"tools/sub"}))
        deriver = Deriver(tree, 5)
        files, dirs = deriver.static("scripts/o/test_x.ps1")
        check("arm: a PowerShell self-test's static inputs follow what its CODE mentions — beside it, below it, from "
              "the root, through the scripts it reaches — never a comment, and a mentioned directory only as a bare "
              "word beside it", (sorted(files), sorted(dirs)),
              (["scripts/o/runner.py", "scripts/o/stop.ps1", "scripts/o/test_x.ps1", "scripts/o/testdata/data.json",
                "scripts/o/testdata/lib.ps1"], ["scripts/o/testdata"]))
        files, dirs = deriver.static("scripts/o/test_y.py")
        check("arm: a Python self-test's static inputs are its own file and its declared reads (the trace sees the "
              "rest)", (sorted(files), sorted(dirs)), (["docs/ignored.md", "scripts/o/test_y.py"], ["kb/Work"]))
        try:
            deriver.static("scripts/o/test_z.py")
            check("arm: a declared read that names nothing tracked is an error, never an empty set", "derived", "raised")
        except ValueError as e:
            check("arm: a declared read that names nothing tracked is an error, never an empty set",
                  "no/such/path" in str(e), True)
        check("arm: a compiled module counts as its source, and a path inside a submodule as its gitlink",
              (tree.owner("scripts/o/__pycache__/runner.cpython-314.pyc"), tree.owner("tools/sub/skills/x.py")),
              ("scripts/o/runner.py", "tools/sub"))

        deps = Deps.of(tree, {"scripts/o/runner.py", "scripts/o/new.txt"}, {("entries", "kb/Work"), ("content", "docs")})
        whole = Deps.of(tree, set(), {("content", "")})
        names = Deps.of(tree, set(), {("paths", "kb")})
        check("arm: an unchanged tree changes nothing", (deps.changed(tree), whole.changed(tree), names.changed(tree)),
              ([], [], []))
        planted_tree = lambda **more: Tree(root, {**blobs, **more}, frozenset({"tools/sub"}))  # noqa: E731
        edited = planted_tree(**{"scripts/o/runner.py": "blob-edited"})
        added = planted_tree(**{"kb/Work/m.md": "blob-new"})
        appeared = planted_tree(**{"scripts/o/new.txt": "blob-new"})
        deeper = planted_tree(**{"docs/sub/new.md": "blob-new"})
        check("arm: an edited input, a new file in a listed directory, a tracked file where the run found none and a "
              "new file anywhere under a directory read whole each invalidate the PASS",
              (deps.changed(edited), deps.changed(added), deps.changed(appeared), deps.changed(deeper)),
              (["scripts/o/runner.py"], ["entries:kb/Work"], ["scripts/o/new.txt"], ["content:docs"]))
        check("arm: a git command that read the whole tree's content re-runs on any edit; one that listed names "
              "re-runs on a new name under its pathspec, never on an edit",
              (whole.changed(edited), names.changed(edited), names.changed(added)),
              (["content:"], [], ["paths:kb"]))
        check("arm: what a git command reads: nothing for the repository's location, worktrees or a commit id; the "
              "names or the content under its pathspec (from its directory); a shown file; the whole tree otherwise",
              [git_reads(c.split(), w) for c, w in (
                  ("rev-parse --show-toplevel", ""), ("--exec-path", ""), ("worktree list --porcelain", ""),
                  ("log -1 --format=%h", ""), ("log -5", ""), ("ls-files -z", ""), ("ls-files -z -- scripts", ""),
                  ("ls-files -s -- src/ tests/", ""), ("diff --name-only -- x", "docs"), ("diff HEAD", ""),
                  ("show HEAD:a/b.json", ""), ("ls-files -- *.py", ""))],
              [[], [], [], [], [("content", "")], [("paths", "")], [("paths", "scripts")],
               [("content", "src"), ("content", "tests")], [("content", "docs/x")], [("content", "")],
               [("content", "a/b.json")], [("paths", "")]])

        out = root / "trace-out"
        out.mkdir()
        tracer = install_tracer(root / "tracer-dir")
        child = ("import os, subprocess, sys\nopen(os.path.join('scripts', 'o', 'run' + 'ner.py')).read()\n"
                 "os.listdir('kb/Work')\nsubprocess.run([sys.executable, '-c', 'pass', 'scripts/o/stop.ps1'])\n")
        subprocess.run([sys.executable, "-c", child], cwd=root, check=True,
                       env=traced_env(dict(os.environ), tracer, out, root))
        seen = read_trace(out, tree)
        check("arm: the tracer records a file opened by a built name, a directory listed and a path handed to a child "
              "process, in every Python process the self-test starts",
              (seen.saw("scripts/o/runner.py"), "kb/Work" in seen.dirs, seen.saw("scripts/o/stop.ps1"),
               len(list(out.glob("*.trace"))) >= 2), (True, True, True, True))
    print(f"self_test_inputs.py --self-test: {'ALL GREEN' if not fails else f'{len(fails)} FAILED'}")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
