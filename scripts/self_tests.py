#!/usr/bin/env python3
"""self_tests.py — THE ONE RUNNER OF EVERY SCRIPT SELF-TEST under scripts/ (kb/Work PB2563).

    python scripts/self_tests.py            # every self-test that needs no build, in parallel; exit 0 only when all pass
    python scripts/self_tests.py --built    # the ones that read the BUILT test assemblies (after a solution build)
    python scripts/self_tests.py --list     # print what discovery found, and how each would run here
    python scripts/self_tests.py --self-test

⛔ WHY ONE RUNNER, AND WHY DISCOVERY. Until PB2563 the gates ran self-tests from hand lists: the gate driver's AUDITS
named five, CI's audits job named those five plus `landing_check.py`, linux-gate.sh named three, and eleven of the
fourteen orchestrator self-tests (the loop's unit choice, the allocator's lock, the wave planner, …) ran in no gate and
no CI job at all, so a regression in any of them passed everything. A list goes stale the day a self-test is added.
This runner FINDS them, and the local gate (scripts/run_gate_legs.py AUDITS, before the gate slot), the Linux gate
(scripts/linux-gate.sh leg `selftests`) and CI (the `audits` job on Linux and a step of `windows-build-test`) all run
THIS entry point, so no gate can run a self-test CI does not, or the reverse (the CI invariant, kb/Work PB1957).

THE DISCOVERY RULE (one rule, `discover`): a tracked file under scripts/ is a self-test when
  (a) it HANDLES `--self-test`: a .py file whose code (not a docstring or a comment) compares an argument with the
      string `--self-test` or declares it with `add_argument` — read from its syntax tree, so a script that only
      PASSES `--self-test` to another one is not one; a .sh file with a `--self-test)` case arm or a
      `= "--self-test"` test outside a comment. It runs as `<file> --self-test`.
  (b) otherwise, it is NAMED `test_*.py` or `test_*.ps1`: a standalone test script. It runs bare.
`SelfTestDiscoveryDriftTests` (Unit) holds the rule to a broader net, so a self-test written in a shape this rule
misses fails a test instead of silently never running.

PLATFORM: every self-test runs on every platform unless its file declares otherwise, on a `#` comment line among its
first HEADER_LINES lines (so a docstring that SHOWS the marker, as this one does, never declares it):
    # SELF-TEST-PLATFORM: windows — <why it cannot run elsewhere>
On another platform it is reported `NOT RUN on <platform>: …` with that reason, on its own line and in the verdict —
never skipped silently. CI's `windows-build-test` job runs this runner too, so a Windows-only self-test still runs in
CI.

BUILD: a self-test that reads the BUILT test assemblies (filter_population.py asks vstest which tests a filter selects)
declares it the same way:
    # SELF-TEST-NEEDS: build — <what it reads>
The default run names it `deferred to --built` in its verdict; `--built` runs exactly those, after a solution build
(the gate driver right after its build, concurrently with the legs; CI's `greenfield-unit` job; linux-gate.sh after its
unit and conformance legs). The default run never builds: it is one of the gate's audits, which run before the gate
slot, and the slot is what rations builds.

ISOLATION: each self-test runs with `COBOL_COORD_DIR` pointed at a fresh private directory, so none can read or write
the live coordination state (`E:\\COBOL-coord`: the loop's units, leases and allocations), and without git's
repository-selection variables (`git rev-parse --local-env-vars`), so a self-test that builds a scratch repository
never acts on the caller's (kb/Work PB1719), and with `GIT_CONFIG_GLOBAL` naming a private config that holds only a
git identity, so a self-test that commits behaves the same on a developer's machine and on a CI runner with no global
config. On Windows `bash` is Git for Windows' own (from `git --exec-path`), never System32's WSL launcher, which a
PATH lookup can find first. The public-skills submodule some of them import (`tools/claude-skills`:
`check_practices.py`) is fetched once when absent, as CI's fleet-practices step does.

THE VERDICT LINE: `=== SELF-TESTS: GREEN — n ran in m s (k not run on <platform>) ===` or `=== SELF-TESTS: RED — …`.
Exit 0 GREEN, 1 RED, 2 NOT RUN (discovery or the submodule fetch failed).
"""
from __future__ import annotations

import argparse
import ast
import concurrent.futures
import functools
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
ROOT = "scripts"
FLAG = "--self-test"
VERDICT = "=== SELF-TESTS: "
#: The submodule `check_practices.py` lives in (test_dispatch_guard.py, test_plan_wave.py import it).
SUBMODULE = "tools/claude-skills"
#: A self-test that runs longer than this is RED, never waited on forever.
TIMEOUT_S = 900
#: A marker counts only on a `#` line among a file's first lines: its header comment.
HEADER_LINES = 5

PLATFORM_RE = re.compile(r"^#\s*SELF-TEST-PLATFORM:\s*(windows|linux)\s*[—-]+\s*(\S.*?)\s*$", re.MULTILINE)
NEEDS_RE = re.compile(r"^#\s*SELF-TEST-NEEDS:\s*(build)\s*[—-]+\s*(\S.*?)\s*$", re.MULTILINE)
SH_FLAG_RE = re.compile(r"""^(?!\s*#).*(?:^|\s)--self-test\)|^(?!\s*#).*=\s*["']--self-test["']""", re.MULTILINE)


def _tool(name: str) -> str:
    """The interpreter the PATH names. On Windows, `bash` is Git Bash (`_git_bash`), never a PATH lookup."""
    if name == "bash" and os.name == "nt":
        return _git_bash()
    return shutil.which(name) or name


@functools.cache
def _git_bash() -> str:
    """Git for Windows' own bash, found from git's install directory (`git --exec-path` is
    <root>/mingw64/libexec/git-core). A bare `bash` handed to CreateProcess is System32's WSL launcher, which runs the
    script in Linux (exit 127 for a Windows path), and a PATH lookup finds that launcher first on a host whose PATH puts
    System32 ahead of Git (a CI runner's can); git's own directory names the same bash on every Windows host."""
    r = subprocess.run(["git", "--exec-path"], capture_output=True, text=True, encoding="utf-8")
    exec_path = r.stdout.strip()
    if r.returncode != 0 or not exec_path:
        raise OSError(f"`git --exec-path` failed (exit {r.returncode}): {r.stderr.strip()}")
    root = Path(exec_path).parents[2]
    for candidate in (root / "bin" / "bash.exe", root / "usr" / "bin" / "bash.exe"):
        if candidate.is_file():
            return str(candidate)
    raise OSError(f"no Git Bash under git's install directory {root} (from `git --exec-path` = {exec_path})")


def this_platform() -> str:
    return "windows" if os.name == "nt" else "linux"


@dataclass(frozen=True)
class SelfTest:
    path: str                 # repository-relative, forward slashes
    handles_flag: bool        # runs as `<file> --self-test` (rule a), else bare (rule b)
    platform: str | None      # None: every platform
    platform_reason: str
    needs_build: bool = False  # reads the built test assemblies: run by `--built`, never by the default run
    build_reason: str = ""

    def command(self) -> list[str]:
        """The command line, run with the repository root as its cwd. The path stays RELATIVE with forward slashes:
        Git Bash on Windows reads the backslashes of an absolute Windows path as escapes."""
        f = self.path
        tail = [FLAG] if self.handles_flag else []
        if self.path.endswith(".py"):
            return [sys.executable, f, *tail]
        if self.path.endswith(".ps1"):
            return [_tool("pwsh"), "-NoProfile", "-NonInteractive", "-File", f, *tail]
        return [_tool("bash"), f, *tail]

    def runs_on(self, platform: str) -> bool:
        return self.platform is None or self.platform == platform


# ── discovery ────────────────────────────────────────────────────────────────────────────────────────────────────────


def _is_flag(node: ast.AST) -> bool:
    return isinstance(node, ast.Constant) and node.value == FLAG


def _contains_flag(node: ast.AST) -> bool:
    return any(_is_flag(n) for n in ast.walk(node))


def py_handles_flag(source: str) -> bool:
    """True when the module's CODE compares an argument with `--self-test` (`"--self-test" in sys.argv`,
    `argv[:1] == ["--self-test"]`) or declares it (`add_argument("--self-test", …)`). A list that only passes the flag
    to a subprocess, a docstring and a comment are not. A module that does not parse raises SyntaxError: discovery
    cannot tell whether it is a self-test, so it fails rather than leaving it out."""
    tree = ast.parse(source)
    for node in ast.walk(tree):
        if isinstance(node, ast.Compare) and _contains_flag(node):
            return True
        if (isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute) and node.func.attr == "add_argument"
                and any(_is_flag(a) for a in node.args)):
            return True
    return False


def classify(rel: str, text: str) -> SelfTest | None:
    """The discovery rule for ONE file (module docstring): a SelfTest, or None when the file is not one."""
    name = rel.rsplit("/", 1)[-1]
    if rel.endswith(".py"):
        handles = py_handles_flag(text)
    elif rel.endswith(".sh"):
        handles = bool(SH_FLAG_RE.search(text))
    elif rel.endswith(".ps1"):
        handles = False
    else:
        return None
    if not handles and not (name.startswith("test_") and name.endswith((".py", ".ps1"))):
        return None
    header = "\n".join(text.splitlines()[:HEADER_LINES])
    m = PLATFORM_RE.search(header)
    b = NEEDS_RE.search(header)
    return SelfTest(rel, handles, m.group(1) if m else None, m.group(2) if m else "", b is not None,
                    b.group(2) if b else "")


def tracked_scripts(repo: Path) -> list[str]:
    out = subprocess.run(["git", "ls-files", "-z", "--", ROOT], cwd=repo, check=True, capture_output=True,
                         env=_scrubbed_env(os.environ)).stdout
    return sorted(p for p in out.decode("utf-8").split("\0") if p.endswith((".py", ".sh", ".ps1")))


def discover(repo: Path = REPO) -> list[SelfTest]:
    found = []
    for rel in tracked_scripts(repo):
        # A tracked script that cannot be read is a discovery FAILURE (the verdict says NOT RUN), never a file quietly
        # left out: an unreadable self-test would otherwise run nowhere while the verdict stayed GREEN.
        try:
            text = (repo / rel).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError) as e:
            raise OSError(f"cannot read the tracked script {rel} to classify it: {e}") from e
        try:
            t = classify(rel, text)
        except SyntaxError as e:
            raise OSError(f"the tracked script {rel} does not parse, so discovery cannot classify it: {e}") from e
        if t is not None:
            found.append(t)
    return found


# ── running ──────────────────────────────────────────────────────────────────────────────────────────────────────────

_GIT_LOCAL_ENV: list[str] | None = None


def _git_local_env_vars() -> list[str]:
    global _GIT_LOCAL_ENV
    if _GIT_LOCAL_ENV is None:
        env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
        _GIT_LOCAL_ENV = subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                                        text=True, encoding="utf-8", env=env, cwd=tempfile.gettempdir()).stdout.split()
    return _GIT_LOCAL_ENV


def _scrubbed_env(base: dict[str, str]) -> dict[str, str]:
    """`base` without git's repository-selection variables (kb/Work PB1719)."""
    drop = set(_git_local_env_vars())
    return {k: v for k, v in base.items() if k not in drop}


def ensure_submodule(repo: Path, say) -> str | None:
    """Fetch the public-skills submodule when this checkout lacks it. Returns the failure, if any."""
    if (repo / SUBMODULE / "skills").is_dir():
        return None
    say(f"self-tests: fetching the {SUBMODULE} submodule (absent in this checkout)")
    r = subprocess.run(["git", "submodule", "update", "--init", SUBMODULE], cwd=repo, capture_output=True, text=True,
                       encoding="utf-8", errors="replace", env=_scrubbed_env(os.environ))
    if r.returncode != 0 or not (repo / SUBMODULE / "skills").is_dir():
        return f"`git submodule update --init {SUBMODULE}` failed (exit {r.returncode}): {r.stderr.strip()}"
    return None


@dataclass
class Result:
    test: SelfTest
    rc: int | None            # None: not run here
    seconds: float
    output: str


#: The git identity every self-test commits under, from its private global config (`private_git_config`).
GIT_IDENTITY = ("self-test", "self-test@localhost")


def private_git_config(scratch: Path) -> Path:
    """A private global git config holding only an identity. A self-test that commits in a scratch repository then
    behaves the same on a developer's machine (whose global config names a user, an editor, credential helpers) and on
    a CI runner (whose global config names no user, so `git commit` fails): local == CI, the CI invariant (kb/Work
    PB1957). The system config (Git for Windows' core.autocrlf) is left as it is, as on every host."""
    cfg = scratch / "gitconfig"
    cfg.write_text("\n".join(["[user]", f"\tname = {GIT_IDENTITY[0]}", f"\temail = {GIT_IDENTITY[1]}", ""]),
                   encoding="utf-8")
    return cfg


def run_one(test: SelfTest, repo: Path, scratch: Path, git_config: Path) -> Result:
    coord = Path(tempfile.mkdtemp(prefix="coord-", dir=scratch))
    env = _scrubbed_env(os.environ)
    env.update(COBOL_COORD_DIR=str(coord), GIT_CONFIG_GLOBAL=str(git_config), PYTHONIOENCODING="utf-8",
               PYTHONUTF8="1")
    start = time.monotonic()
    try:
        r = subprocess.run(test.command(), cwd=repo, env=env, stdin=subprocess.DEVNULL, capture_output=True,
                           text=True, encoding="utf-8", errors="replace", timeout=TIMEOUT_S)
        rc, out = r.returncode, (r.stdout or "") + (r.stderr or "")
    except subprocess.TimeoutExpired as e:
        rc = 124
        out = f"{_text(e.stdout)}{_text(e.stderr)}\n⛔ TIMED OUT after {TIMEOUT_S} s"
    except OSError as e:
        rc, out = 127, f"⛔ could not start {test.command()[0]}: {e}"
    return Result(test, rc, time.monotonic() - start, out)


def _text(b) -> str:
    if b is None:
        return ""
    return b if isinstance(b, str) else b.decode("utf-8", "replace")


def run_all(tests: list[SelfTest], repo: Path, platform: str, say, jobs: int | None = None) -> list[Result]:
    here = [t for t in tests if t.runs_on(platform)]
    results = [Result(t, None, 0.0, "") for t in tests if not t.runs_on(platform)]
    scratch = Path(tempfile.mkdtemp(prefix="self-tests-"))
    try:
        git_config = private_git_config(scratch)
        workers = max(1, min(len(here) or 1, jobs or (os.cpu_count() or 4)))
        with concurrent.futures.ThreadPoolExecutor(max_workers=workers) as pool:
            futures = {pool.submit(run_one, t, repo, scratch, git_config): t for t in here}
            for f in concurrent.futures.as_completed(futures):
                r = f.result()
                results.append(r)
                state = "GREEN" if r.rc == 0 else f"RED (exit {r.rc})"
                say(f"self-test {r.test.path}{' --self-test' if r.test.handles_flag else ''}: {state} in "
                    f"{r.seconds:.1f}s")
                if r.rc != 0:  # a red self-test's COMPLETE output, as for a red gate leg (kb/Work PB1573)
                    for line in r.output.rstrip().splitlines():
                        say(f"    {line}")
    finally:
        shutil.rmtree(scratch, ignore_errors=True)
    return sorted(results, key=lambda r: r.test.path)


def select(tests: list[SelfTest], built: bool) -> tuple[list[SelfTest], list[SelfTest]]:
    """(this phase's self-tests, the other phase's): `--built` runs exactly the ones that need a build."""
    return [t for t in tests if t.needs_build == built], [t for t in tests if t.needs_build != built]


def verdict(results: list[Result], platform: str, wall: float,
            deferred: list[SelfTest] | None = None) -> tuple[int, str]:
    ran = [r for r in results if r.rc is not None]
    red = [r for r in ran if r.rc != 0]
    skipped = [r for r in results if r.rc is None]
    by_reason: dict[tuple[str, str], list[str]] = {}
    for r in skipped:  # one reason, said once, however many files share it (the twelve test_orchestrate parts do)
        by_reason.setdefault((r.test.platform or "", r.test.platform_reason), []).append(r.test.path)
    not_run = (f" ({len(skipped)} not run on {platform}: "
               + "; ".join(f"{', '.join(paths)} — {p}-only: {reason}" for (p, reason), paths in by_reason.items()) + ")"
               if skipped else "")
    if deferred:
        not_run += f" ({len(deferred)} deferred to --built: {', '.join(t.path for t in deferred)})"
    if red:
        return 1, (f"{VERDICT}RED — {len(red)} of {len(ran)} failed: {', '.join(r.test.path for r in red)}"
                   f"{not_run} ===")
    return 0, f"{VERDICT}GREEN — {len(ran)} ran in {wall:.0f}s{not_run} ==="


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--list", action="store_true", help="print what discovery found and exit")
    ap.add_argument("--built", action="store_true",
                    help="run the self-tests that read the built test assemblies (SELF-TEST-NEEDS: build)")
    ap.add_argument("--jobs", type=int, default=None, help="how many self-tests run at once (default: the cores)")
    ap.add_argument("--self-test", action="store_true", help="prove the discovery rule and the verdict")
    args = ap.parse_args(argv)
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if args.self_test:
        return self_test()
    platform = this_platform()
    try:
        tests = discover(REPO)
    except (OSError, subprocess.CalledProcessError) as e:
        print(f"{VERDICT}NOT RUN — discovery failed: {e} ===")
        return 2
    if args.list:
        for t in tests:
            where = "here" if t.runs_on(platform) else f"NOT on {platform} ({t.platform}-only: {t.platform_reason})"
            phase = "  [--built]" if t.needs_build else ""
            print(f"{t.path}{' --self-test' if t.handles_flag else ''}  [{where}]{phase}")
        print(f"{len(tests)} self-tests")
        return 0
    say = lambda s: print(s, flush=True)  # noqa: E731
    failure = ensure_submodule(REPO, say)
    if failure:
        print(f"{VERDICT}NOT RUN — {failure} ===")
        return 2
    phase, deferred = select(tests, args.built)
    start = time.monotonic()
    results = run_all(phase, REPO, platform, say, args.jobs)
    rc, line = verdict(results, platform, time.monotonic() - start, None if args.built else deferred)
    print(line, flush=True)
    return rc


# ── the self-test: the discovery rule, the platform marker, the isolation and the verdict ─────────────────────────


def self_test() -> int:
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        print(f"  {'ok  ' if got == want else 'FAIL'} {name}" + ("" if got == want else f": got {got!r}, want {want!r}"))
        if got != want:
            fails.append(name)

    def kind(rel: str, text: str):
        t = classify(rel, text)
        return None if t is None else (t.handles_flag, t.platform)

    # (a) the flag, read from the syntax tree
    check("arm: argparse --self-test is a self-test",
          kind("scripts/x.py", 'ap.add_argument("--self-test", action="store_true")\n'), (True, None))
    check("arm: `\"--self-test\" in sys.argv` is a self-test",
          kind("scripts/x.py", 'import sys\nif "--self-test" in sys.argv: pass\n'), (True, None))
    check("arm: `argv[:1] == [\"--self-test\"]` is a self-test",
          kind("scripts/x.py", 'def main(argv):\n    if argv[:1] == ["--self-test"]: pass\n'), (True, None))
    check("arm: a script that only PASSES --self-test to another is not",
          kind("scripts/x.py", 'CMDS = [["scripts/y.py", "--self-test"]]\n'), None)
    check("arm: a docstring or comment mention is not",
          kind("scripts/x.py", '"""run it with --self-test"""\n# python x.py --self-test\n'), None)
    check("arm: a bash `--self-test)` case arm is a self-test",
          kind("scripts/x.sh", 'case "$1" in\n  --self-test) t ;;\nesac\n'), (True, None))
    check("arm: a bash `= \"--self-test\"` test is a self-test",
          kind("scripts/x.sh", 'if [ "${1:-}" = "--self-test" ]; then t; fi\n'), (True, None))
    check("arm: a bash comment mention is not",
          kind("scripts/x.sh", '# bash x.sh --self-test\necho hi\n'), None)
    # (b) the name
    check("arm: test_*.py is a standalone self-test", kind("scripts/o/test_a.py", "print(1)\n"), (False, None))
    check("arm: test_*.ps1 is a standalone self-test", kind("scripts/o/test_a.ps1", "1\n"), (False, None))
    check("arm: a test_*.py that handles --self-test runs with it",
          kind("scripts/test_tool.py", 'ap.add_argument("--self-test")\n'), (True, None))
    check("arm: a plain script is not", kind("scripts/tool.py", "print(1)\n"), None)
    check("arm: a test_*.sh is not (bash self-tests declare the flag)", kind("scripts/test_a.sh", "true\n"), None)
    # the platform marker
    t = classify("scripts/test_w.ps1", "# SELF-TEST-PLATFORM: windows — needs Task Scheduler\n")
    check("arm: the platform marker is read with its reason", (t.platform, t.platform_reason),
          ("windows", "needs Task Scheduler"))
    check("arm: a windows-only self-test does not run on linux", t.runs_on("linux"), False)
    check("arm: a windows-only self-test runs on windows", t.runs_on("windows"), True)
    b = classify("scripts/x.py", "# SELF-TEST-NEEDS: build — reads the built assemblies\nap.add_argument('--self-test')\n")
    check("arm: the build marker is read with its reason", (b.needs_build, b.build_reason),
          (True, "reads the built assemblies"))
    shown = classify("scripts/test_doc.py", '"""\n1\n2\n3\n4\n    # SELF-TEST-PLATFORM: windows — shown, not declared\n"""\n')
    check("arm: a marker SHOWN past the header (a docstring example) declares nothing", shown.platform, None)
    check("arm: --built runs exactly the build-needing ones, the default run the rest",
          (select([b, t], built=True), select([b, t], built=False)), (([b], [t]), ([t], [b])))
    if os.name == "nt":
        check("arm: on Windows `bash` is Git Bash from git's install directory, never System32's WSL launcher",
              ("system32" in _tool("bash").lower(), Path(_tool("bash")).name.lower()), (False, "bash.exe"))
    check("arm: the default verdict names what it deferred to --built",
          "1 deferred to --built: scripts/x.py" in verdict([], "linux", 0.0, [b])[1], True)

    # the runner over a planted scratch tree: a passing, a failing and a platform-excluded self-test; the isolation
    other = "linux" if this_platform() == "windows" else "windows"
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        (root / "scripts").mkdir()
        (root / "scripts" / "test_green.py").write_text(
            "import os, subprocess, sys\nprint('coord=' + os.environ.get('COBOL_COORD_DIR', ''))\n"
            "print('user=' + subprocess.run(['git', 'config', '--global', 'user.name'], capture_output=True,"
            " text=True).stdout.strip())\n"
            "sys.exit(0 if os.environ.get('GIT_DIR') is None else 3)\n", encoding="utf-8")
        (root / "scripts" / "test_red.py").write_text("import sys\nprint('planted failure')\nsys.exit(1)\n",
                                                      encoding="utf-8")
        (root / "scripts" / "elsewhere.py").write_text(
            f"# SELF-TEST-PLATFORM: {other} — planted\nimport sys\nif '--self-test' in sys.argv: sys.exit(1)\n",
            encoding="utf-8")
        env = _scrubbed_env(os.environ)
        for cmd in (["init", "-q"], ["add", "-A"]):
            subprocess.run(["git", *cmd], cwd=root, check=True, capture_output=True, env=env)
        tests = discover(root)
        check("arm: discovery finds the planted tree's three self-tests", [t.path for t in tests],
              ["scripts/elsewhere.py", "scripts/test_green.py", "scripts/test_red.py"])
        for planted, body in (("broken.py", b"def (:\n"), ("latin1.py", b"# caf\xe9\n")):
            (root / "scripts" / planted).write_bytes(body)
            subprocess.run(["git", "add", "-A"], cwd=root, check=True, capture_output=True, env=env)
            try:
                discover(root)
                check(f"arm: a tracked script discovery cannot read ({planted}) fails discovery, never drops out",
                      "discovered", "raised")
            except OSError as e:
                check(f"arm: a tracked script discovery cannot read ({planted}) fails discovery, never drops out",
                      f"scripts/{planted}" in str(e), True)
            (root / "scripts" / planted).unlink()
            subprocess.run(["git", "add", "-A"], cwd=root, check=True, capture_output=True, env=env)
        saved = os.environ.get("GIT_DIR")
        os.environ["GIT_DIR"] = str(root / "not-a-repo")   # the caller's variable must not reach a self-test
        try:
            said: list[str] = []
            results = run_all(tests, root, this_platform(), said.append)
        finally:
            if saved is None:
                os.environ.pop("GIT_DIR", None)
            else:
                os.environ["GIT_DIR"] = saved
        by = {r.test.path: r for r in results}
        check("arm: a passing self-test is GREEN", by["scripts/test_green.py"].rc, 0)
        coord = next((ln[6:] for ln in by["scripts/test_green.py"].output.splitlines() if ln.startswith("coord=")), "")
        check("arm: each self-test gets a private COBOL_COORD_DIR",
              bool(coord) and Path(coord).name.startswith("coord-") and coord != os.environ.get("COBOL_COORD_DIR"),
              True)
        check("arm: each self-test commits under the private global config's identity, never the caller's",
              f"user={GIT_IDENTITY[0]}" in by["scripts/test_green.py"].output.splitlines(), True)
        check("arm: a failing self-test is RED", by["scripts/test_red.py"].rc, 1)
        check("arm: a failing self-test's output reaches the console", any("planted failure" in s for s in said), True)
        check("arm: a self-test for another platform is NOT RUN, not run", by["scripts/elsewhere.py"].rc, None)
        rc, line = verdict(results, this_platform(), 1.0)
        check("arm: one red makes the verdict RED", (rc, line.startswith(VERDICT + "RED")), (1, True))
        check("arm: the verdict names the red and the not-run one with its reason",
              "scripts/test_red.py" in line and f"{other}-only: planted" in line, True)
        rc, line = verdict([r for r in results if r.test.path != "scripts/test_red.py"], this_platform(), 1.0)
        check("arm: no red is GREEN, still naming what did not run here",
              (rc, line.startswith(VERDICT + "GREEN"), "1 not run on" in line), (0, True, True))

    print(f"self_tests.py --self-test: {'ALL GREEN' if not fails else f'{len(fails)} FAILED'}")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
