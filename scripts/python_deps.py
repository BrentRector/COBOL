#!/usr/bin/env python3
"""python_deps.py — THE Python packages the gates run with: ONE declaration, ONE provisioning (kb/Work PB3097, branch
review R3-1).

    python scripts/python_deps.py ensure           # this interpreter holds every pinned package of
                                                   # scripts/requirements-gate.txt; installs what it lacks
    python scripts/python_deps.py venv <parent>    # a virtual environment under <parent> holding EXACTLY that set,
                                                   # built once per declaration; prints its interpreter's path
    python scripts/python_deps.py audit            # every third-party import is declared (the live half of the self-test)
    python scripts/python_deps.py --self-test

⛔ WHY (branch review R3-1, 2026-10-12). PB3097 made `tooling_check.py --self-test` parse every agent role with PyYAML,
pinned PyYAML in CI with an inline `pip install`, and added it to `scripts/wsl/setup-wsl.sh`, a script someone runs BY
HAND. The Linux gate's interpreter (WSL's uv CPython, first on the gate's PATH) had never been re-provisioned, so the
self-test went RED (6) under `linux-gate.sh` and every landing on that host would have gone GATE RED until someone ran
the setup again. The dependency set lived in five places (two CI steps, two setup scripts, each developer's box) and
nothing provisioned or checked it where the gates run.

THE RULE: the packages a gate needs are declared ONCE, pinned, in `scripts/requirements-gate.txt`; every gate
provisions them from that file with no hand step, or refuses loudly:
  - the local gate: `ensure` is an audit prerequisite (`scripts/run_gate_legs.py` AUDIT_PREREQUISITES), so a missing
    or mis-pinned package is installed into the gate's interpreter before any audit, or the gate is RED naming it;
  - CI: the `audits` job and `windows-build-test` run `ensure` on the runner's interpreter, from the same file;
  - the Linux gate: `venv` builds a virtual environment holding EXACTLY that set (keyed by the declaration's content
    and the base interpreter's build, so it is built once, when either changes, and reused after) and runs every leg
    with it first on PATH. Exactly, not "at least": the developer interpreter carries the tools' packages too (PyMuPDF,
    SciPy), which CI's runner does not, so a script a test runs that imports one of them would pass the gate and fail
    CI. A failed build is `=== LINUX GATE: NOT RUN (...) ===`, never a green over a missing package.
`requirements-tools.txt` declares the packages of the tools no gate runs (`-r` the gate file); the developer setups
install it. The self-test's live audit (`audit`) is RED while a third-party import under scripts/ or .claude/ is
declared in neither file, or while one a self-test reaches (its own imports and, transitively, the local modules and
the scripts it names) is declared only in the tools file — so the NEXT dependency is declared, and then provisioned
everywhere, before it can land.
"""
from __future__ import annotations

import ast
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
REPO = SCRIPTS.parent
GATE = SCRIPTS / "requirements-gate.txt"
TOOLS = SCRIPTS / "requirements-tools.txt"
#: The trees whose Python the import audit reads: the scripts and the agent tooling (hooks, skills).
SCANNED = ("scripts", ".claude")
#: A virtual environment not used for this long is removed by the next `venv` call under the same parent.
STALE_DAYS = 14
USED = ".last-used"
PIP = ["-m", "pip", "install", "--disable-pip-version-check", "--no-input"]

LINE_RE = re.compile(r"^(?P<name>[A-Za-z0-9][A-Za-z0-9._-]*)\s*(?:==\s*(?P<version>[^\s#;]+))?\s*(?:#(?P<comment>.*))?$")
IMPORTS_RE = re.compile(r"\bimports:\s*([A-Za-z_][\w.]*(?:\s*,\s*[A-Za-z_][\w.]*)*)")
SCRIPT_REF_RE = re.compile(r"(?:^|[\s\"'(=/])((?:scripts|\.claude)/[\w./-]+\.py)\b")


@dataclass(frozen=True)
class Requirement:
    name: str            # the distribution, as declared
    version: str | None  # the pin (`==`), or None
    imports: tuple[str, ...]
    source: str          # the declaring file's name

    def spec(self) -> str:
        return f"{self.name}=={self.version}" if self.version else self.name


def parse(path: Path, *, pinned: bool = False) -> list[Requirement]:
    """The requirements `path` declares, `-r` includes first (relative to the file, as pip reads them). With `pinned`,
    every one must carry `==<version>`. A line this parser cannot read is an error, never skipped: pip would read it."""
    out: list[Requirement] = []
    for n, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("-r "):
            out += parse(path.parent / line[3:].split("#")[0].strip(), pinned=pinned)
            continue
        m = LINE_RE.match(line)
        if not m:
            raise ValueError(f"{path.name}:{n}: not a `<distribution>[==<version>]   # imports: <module>` line: {raw}")
        if pinned and not m["version"]:
            raise ValueError(f"{path.name}:{n}: {m['name']} is not pinned (`=={{version}}`): every gate and CI install "
                             f"the same version")
        im = IMPORTS_RE.search(m["comment"] or "")
        out.append(Requirement(m["name"], m["version"],
                               tuple(s.strip() for s in im.group(1).split(",")) if im else (), path.name))
    return out


def gate_requirements(path: Path = GATE) -> list[Requirement]:
    return parse(path, pinned=True)


# ── provisioning ─────────────────────────────────────────────────────────────────────────────────────────────────────

#: Run under the interpreter being provisioned: the installed version of each distribution named in argv[1] (a JSON
#: list), and how pip must install into it (an externally managed interpreter, PEP 668; a site-packages this user
#: cannot write).
_PROBE = """
import importlib.metadata as md, json, os, sys, sysconfig
names = json.loads(sys.argv[1])
def version(n):
    try:
        return md.version(n)
    except md.PackageNotFoundError:
        return None
venv = sys.prefix != sys.base_prefix
print(json.dumps({
    "versions": {n: version(n) for n in names},
    "managed": not venv and os.path.exists(os.path.join(sysconfig.get_path("stdlib"), "EXTERNALLY-MANAGED")),
    "user": not venv and not os.access(sysconfig.get_path("purelib"), os.W_OK),
}))
"""


def probe(python: str, reqs: list[Requirement]) -> dict:
    p = subprocess.run([python, "-c", _PROBE, json.dumps([r.name for r in reqs])], capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    if p.returncode != 0:
        raise OSError(f"cannot probe the interpreter {python} (exit {p.returncode}): {p.stderr.strip()}")
    return json.loads(p.stdout)


def lacking(reqs: list[Requirement], versions: dict) -> list[Requirement]:
    """The requirements whose pinned version is not the installed one (absent counts)."""
    return [r for r in reqs if versions.get(r.name) is None or (r.version and versions[r.name] != r.version)]


def pip_flags(state: dict) -> list[str]:
    return [*(["--break-system-packages"] if state["managed"] else []), *(["--user"] if state["user"] else [])]


def run_pip(cmd: list[str]) -> tuple[int, str]:
    p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8",
                       errors="replace")
    return p.returncode, p.stdout


def ensure(python: str = sys.executable, reqs: list[Requirement] | None = None, install=run_pip, say=print) -> int:
    """The interpreter `python` holds every gate requirement at its pin, installing what it lacks. Exit 0, or 1 with
    the `=== PYTHON PACKAGES: MISSING …` line naming what is still lacking and pip's output."""
    reqs = gate_requirements() if reqs is None else reqs
    state = probe(python, reqs)
    need = lacking(reqs, state["versions"])
    if not need:
        say(f"=== PYTHON PACKAGES: READY — {', '.join(r.spec() for r in reqs)} ({python}) ===")
        return 0
    cmd = [python, *PIP, *pip_flags(state), *(r.spec() for r in need)]
    say(f"python_deps: {python} lacks {', '.join(r.spec() for r in need)}: {' '.join(cmd)}")
    rc, text = install(cmd)
    still = lacking(reqs, probe(python, reqs)["versions"])
    if still:
        say(text.rstrip())
        say(f"=== PYTHON PACKAGES: MISSING — {', '.join(r.spec() for r in still)} in {python} (pip exit {rc}; "
            f"declared in scripts/requirements-gate.txt) ===")
        return 1
    say(f"=== PYTHON PACKAGES: INSTALLED — {', '.join(r.spec() for r in need)} ({python}) ===")
    return 0


def venv_key(reqs: list[Requirement], base: str) -> str:
    """What a virtual environment is built from: the declared set and the base interpreter's exact build."""
    build = subprocess.run([base, "-c", "import sys, platform; print(sys.version, platform.machine())"],
                           capture_output=True, text=True, encoding="utf-8", check=True).stdout.strip()
    text = "\n".join(sorted(r.spec() for r in reqs)) + "\n" + os.path.realpath(base) + "\n" + build
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]


def venv_python(env: Path) -> Path:
    return env / ("Scripts/python.exe" if os.name == "nt" else "bin/python3")


def prune(parent: Path, keep: str, now: float) -> None:
    """Remove every environment under `parent` other than `keep` not used for STALE_DAYS (an abandoned build too)."""
    for d in parent.iterdir():
        if d.name == keep or not d.is_dir():
            continue
        used = d / USED
        stamp = used.stat().st_mtime if used.exists() else d.stat().st_mtime
        if now - stamp > STALE_DAYS * 86400:
            shutil.rmtree(d, ignore_errors=True)


def venv(parent: Path, base: str = sys.executable, reqs: list[Requirement] | None = None, install=run_pip,
         say=print) -> Path:
    """The interpreter of a virtual environment under `parent` holding EXACTLY the gate requirements: reused while
    its key (`venv_key`) is unchanged and it still holds them, built otherwise — in a private directory renamed into
    place, so two gates building at once each get a whole one and never a half-built one. Raises OSError, with pip's
    output, when it cannot be built."""
    reqs = gate_requirements() if reqs is None else reqs
    base = os.path.realpath(base)
    parent.mkdir(parents=True, exist_ok=True)
    key = venv_key(reqs, base)
    env = parent / key
    py = venv_python(env)
    if py.exists() and not lacking(reqs, probe(str(py), reqs)["versions"]):
        (env / USED).touch()
        prune(parent, key, time.time())
        return py
    work = Path(tempfile.mkdtemp(prefix=f"{key}.building-", dir=parent))
    try:
        built = work / "env"
        p = subprocess.run([base, "-m", "venv", "--without-pip", str(built)], capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
        if p.returncode != 0:
            raise OSError(f"`{base} -m venv` failed (exit {p.returncode}): {p.stderr.strip()}")
        if reqs:
            # The base interpreter's pip installs INTO the environment (`--python`), so the environment carries no pip.
            cmd = [base, "-m", "pip", "--python", str(venv_python(built)), *PIP[2:], *(r.spec() for r in reqs)]
            say(f"python_deps: building the gate's environment {key}: {' '.join(cmd)}")
            rc, text = install(cmd)
            if rc != 0 or lacking(reqs, probe(str(venv_python(built)), reqs)["versions"]):
                raise OSError(f"installing {', '.join(r.spec() for r in reqs)} failed (pip exit {rc}):\n{text.rstrip()}")
        (built / USED).touch()
        if env.exists():  # present but lacking (a broken build, a removed base): rebuilt, never trusted
            shutil.rmtree(env, ignore_errors=True)
        try:
            built.rename(env)
        except OSError:
            if not (py.exists() and not lacking(reqs, probe(str(py), reqs)["versions"])):
                raise  # not another gate's finished build: a real failure
    finally:
        shutil.rmtree(work, ignore_errors=True)
    prune(parent, key, time.time())
    return py


# ── the declaration audit ────────────────────────────────────────────────────────────────────────────────────────────

def _self_tests():
    sys.path.insert(0, str(SCRIPTS))
    import self_tests  # the ONE discovery rule (kb/Work PB2563) and the ONE submodule fetch
    return self_tests


def ls_files(repo: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(repo), "ls-files", "-z"], check=True, capture_output=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def registered_submodules(repo: Path) -> set[str]:
    """The submodule paths `repo`'s .gitmodules registers (none without the file)."""
    if not (repo / ".gitmodules").is_file():
        return set()
    p = subprocess.run(["git", "-C", str(repo), "config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$"],
                       capture_output=True, text=True, encoding="utf-8")
    return {line.split(None, 1)[1].strip() for line in p.stdout.splitlines() if " " in line}


def tracked(repo: Path, submodules: tuple[str, ...] | None = None, say=print) -> list[str]:
    """Every tracked file of the repository, and of EXACTLY the submodules in `submodules` it registers (default: the
    public-skills one, `self_tests.SUBMODULE`, whose modules — `fleet_config` — are local, never third-party), each
    FETCHED first when this checkout has not initialized it (`self_tests.ensure_submodule`), and of no other.

    ⛔ WHY (branch review R4-1, 2026-10-12): `git ls-files --recurse-submodules` read whatever submodules the checkout
    HAPPENED to have initialized, so `fleet_config` was local in a developer's checkout and an undeclared third-party
    import in a fresh clone (CI's `submodules: false` checkout; a new worktree), and a module only the private
    `specs-private` held would be local on the owner's box and third-party on every CI runner, which can never fetch it.
    The set read is now a property of the repository, never of the checkout. Raises OSError when the fetch fails."""
    submodules = (_self_tests().SUBMODULE,) if submodules is None else submodules
    files = ls_files(repo)
    registered = registered_submodules(repo)
    for sub in submodules:
        if sub not in registered:
            continue
        failure = _self_tests().ensure_submodule(repo, say, sub)
        if failure:
            raise OSError(failure)
        files += [f"{sub}/{p}" for p in ls_files(repo / sub)]
    return files


def imported(source: str) -> set[str]:
    """Every absolute module name `source` imports, anywhere in it (a function-local or guarded import included)."""
    names: set[str] = set()
    for node in ast.walk(ast.parse(source)):
        if isinstance(node, ast.Import):
            names |= {a.name for a in node.names}
        elif isinstance(node, ast.ImportFrom) and node.level == 0 and node.module:
            names.add(node.module)
            names |= {f"{node.module}.{a.name}" for a in node.names}
    return names


def audit(repo: Path, gate: Path, tools: Path, self_tests: list[str], submodules: tuple[str, ...] | None = None,
          say=print) -> list[str]:
    """The findings (empty when clean): a third-party import under SCANNED declared in neither file, and one a
    self-test in `self_tests` reaches declared only in the tools file. Reaching follows a local module the file imports
    (every tracked `<name>.py` that one of the import's components names) and, from a shell or PowerShell self-test, a
    `scripts/…py` or `.claude/…py` path its text names (it runs scripts by path). A Python file's mere MENTION of a
    path (a docstring, a message) is not followed: the Linux gate's exact environment and CI's runner catch a script
    run by path whose import is not provisioned, both alike (module docstring)."""
    files = tracked(repo, submodules, say)
    py = [f for f in files if f.endswith(".py")]
    local = {Path(f).stem for f in py} | {part for f in py for part in Path(f).parts[:-1]}
    by_stem: dict[str, list[str]] = {}
    for f in py:
        by_stem.setdefault(Path(f).stem, []).append(f)
    gate_imports = {m for r in gate_requirements(gate) for m in r.imports}
    all_imports = {m for r in parse(tools) for m in r.imports} | gate_imports

    parsed: dict[str, set[str]] = {}

    def imports_of(f: str) -> set[str]:
        if f not in parsed:
            parsed[f] = imported((repo / f).read_text(encoding="utf-8")) if f.endswith(".py") else set()
        return parsed[f]

    def third_party(f: str) -> set[str]:
        return {m.split(".")[0] for m in imports_of(f)} - set(sys.stdlib_module_names) - local - {"__future__"}

    findings = []
    for f in sorted(p for p in py if p.split("/")[0] in SCANNED):
        for m in sorted(third_party(f) - all_imports):
            findings.append(f"{f}: imports `{m}`, declared in neither scripts/requirements-gate.txt nor "
                            f"scripts/requirements-tools.txt")
    seen: set[str] = set()
    todo = list(self_tests)
    while todo:
        f = todo.pop()
        if f in seen or not (repo / f).is_file():
            continue
        seen.add(f)
        for m in sorted(third_party(f) - gate_imports):
            if m in all_imports:
                findings.append(f"{f}: imports `{m}` and a self-test reaches it, so it belongs in "
                                f"scripts/requirements-gate.txt, which every gate provisions, not only in "
                                f"scripts/requirements-tools.txt")
        for m in imports_of(f):
            for part in m.split("."):
                todo += by_stem.get(part, [])
        if not f.endswith(".py"):  # a shell or PowerShell self-test runs scripts BY PATH; a Python one imports them
            text = (repo / f).read_text(encoding="utf-8", errors="replace")
            todo += [ref for ref in SCRIPT_REF_RE.findall(text) if ref in by_stem.get(Path(ref).stem, [])]
    return findings


#: scripts/cloud/setup-env.sh runs before the clone exists, so it names the tools' packages itself: the words of its
#: one `pip install` before `>/tmp/pip-deps.log`.
SETUP_ENV = SCRIPTS / "cloud" / "setup-env.sh"
SETUP_ENV_RE = re.compile(r"--no-warn-script-location\s*\\\s*\n\s*([^>\n]+?)\s*>/tmp/pip-deps\.log")


def setup_env_drift(setup_env: Path = SETUP_ENV, tools: Path = TOOLS) -> list[str]:
    """What the cloud setup installs that the tools file does not declare, or the reverse (by distribution, case and
    `-`/`_` folded); [] when they agree."""
    m = SETUP_ENV_RE.search(setup_env.read_text(encoding="utf-8"))
    if not m:
        return [f"{setup_env.name}: its `pip install … >/tmp/pip-deps.log` line is gone, so nothing holds it to "
                f"{tools.name}"]
    fold = lambda n: n.lower().replace("_", "-")  # noqa: E731
    named = {fold(w) for w in m.group(1).split()}
    declared = {fold(r.name) for r in parse(tools) if r.source == tools.name}
    return ([f"{setup_env.name} installs {n}, which {tools.name} does not declare" for n in sorted(named - declared)]
            + [f"{setup_env.name} does not install {n}, which {tools.name} declares" for n in sorted(declared - named)])


def discovered_self_tests(repo: Path) -> list[str]:
    return [t.path for t in _self_tests().discover(repo)]


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    failures: list[str] = []

    def check(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f"\n        got:  {got!r}\n        want: {want!r}"))
        if not ok:
            failures.append(label)

    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as d:
        root = Path(d)

        def plant(rel: str, text: str) -> Path:
            p = root / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text, encoding="utf-8", newline="\n")
            return p

        gate = plant("scripts/requirements-gate.txt",
                     "# a comment\nwidget==1.2   # imports: widgetlib, widget_extra\ntzdata==2026.4   # data: zoneinfo\n")
        tools = plant("scripts/requirements-tools.txt", "-r requirements-gate.txt\nbigpdf   # imports: bigpdf\n")
        check("arm PARSE: the gate file's pins and imports, in order",
              [(r.name, r.version, r.imports) for r in gate_requirements(gate)],
              [("widget", "1.2", ("widgetlib", "widget_extra")), ("tzdata", "2026.4", ())])
        check("arm PARSE: the tools file includes the gate file (`-r`, relative to itself) ahead of its own",
              [r.name for r in parse(tools)], ["widget", "tzdata", "bigpdf"])
        unpinned = plant("scripts/unpinned.txt", "widget   # imports: widgetlib\n")
        try:
            gate_requirements(unpinned)
            got = "accepted"
        except ValueError as e:
            got = "refused" if "not pinned" in str(e) else str(e)
        check("arm PARSE: an unpinned gate requirement is refused (every gate and CI install the same version)", got,
              "refused")

        # ensure(): the probe is real (this interpreter); the installer is a stand-in that records its command line.
        here = sys.executable
        have = probe(here, [Requirement("pip", None, (), "")])["versions"]["pip"]
        present = [Requirement("pip", have, (), "t")] if have else []
        absent = Requirement("no-such-distribution-pb3097", "1.0", (), "t")
        calls: list[list[str]] = []
        heard: list[str] = []

        def fake_install(cmd: list[str]) -> tuple[int, str]:
            calls.append(cmd)
            return 1, "ERROR: No matching distribution found"

        check("arm ENSURE: an interpreter holding every pin installs nothing and is READY",
              (ensure(here, present, fake_install, heard.append), calls), (0, []))
        rc = ensure(here, [*present, absent], fake_install, heard.append)
        check("arm ENSURE: only the lacking pin is installed, by pip, into the same interpreter",
              [(c[:3], [a for a in c if "==" in a]) for c in calls], [([here, "-m", "pip"], [absent.spec()])])
        check("arm ENSURE: a pin still lacking after pip is exit 1 and a MISSING line naming it, with pip's output",
              (rc, any(h.startswith("=== PYTHON PACKAGES: MISSING — no-such-distribution-pb3097==1.0") for h in heard),
               "ERROR: No matching distribution found" in heard), (1, True, True))
        check("arm ENSURE: a different installed version counts as lacking (the pin is the set CI runs)",
              [r.spec() for r in lacking([Requirement("pip", "0.0.1", (), "t")], {"pip": have})], ["pip==0.0.1"])
        check("arm ENSURE: an externally managed interpreter gets --break-system-packages, an unwritable one --user",
              pip_flags({"managed": True, "user": True}), ["--break-system-packages", "--user"])

        # venv(): a real environment, built with NO requirement (no network), then reused; a changed declaration is a
        # new environment; an abandoned one is pruned.
        parent = root / "envs"
        first = venv(parent, here, [], fake_install, heard.append)
        again = venv(parent, here, [], fake_install, heard.append)
        check("arm VENV: an environment is built once, and its interpreter runs isolated from the base's packages",
              (first == again, first.exists(),
               subprocess.run([str(first), "-c", "import sys; print(sys.prefix != sys.base_prefix)"],
                              capture_output=True, text=True).stdout.strip()), (True, True, "True"))
        check("arm VENV: a different declared set is a different environment (keyed by the set and the base's build)",
              venv_key([absent], here) != venv_key([], here), True)
        stale = parent / "0000stale0000000"
        stale.mkdir()
        old = time.time() - (STALE_DAYS + 1) * 86400
        os.utime(stale, (old, old))
        venv(parent, here, [], fake_install, heard.append)
        check("arm VENV: an environment unused for STALE_DAYS is pruned; the live one stays",
              (stale.exists(), first.exists()), (False, True))
        try:
            venv(parent, here, [absent], fake_install, heard.append)
            got = "built"
        except OSError as e:
            got = "refused" if "No matching distribution" in str(e) else str(e)
        check("arm VENV: a set pip cannot install is an OSError carrying pip's output, never an environment",
              (got, [p.name for p in parent.iterdir() if ".building-" in p.name]), ("refused", []))

        # The declaration audit over a planted repository.
        subprocess.run(["git", "-C", d, "init", "-q"], check=True)
        plant("scripts/uses_widget.py", "import widgetlib\n")
        plant("scripts/helper.py", "import bigpdf\n")
        plant("scripts/test_thing.py", "import helper\nimport json\n")
        plant("scripts/check.sh", "python3 scripts/render.py --self-test\n")
        plant("scripts/render.py", "import bigpdf\n")
        plant(".claude/hooks/hook.py", "from mystery.sub import x\n")
        plant("scripts/tool.py", "import fitzlike  # never declared\nimport bigpdf\n")
        subprocess.run(["git", "-C", d, "add", "-A"], check=True)
        found = audit(root, gate, tools, ["scripts/test_thing.py", "scripts/check.sh"])
        check("arm AUDIT: an import declared nowhere is found, under scripts/ and .claude/",
              sorted(f.split(":")[0] for f in found if "neither" in f), [".claude/hooks/hook.py", "scripts/tool.py"])
        check("arm AUDIT: a tools-only import a self-test reaches (through a local module, or a script it names) "
              "belongs in the gate file",
              sorted(f.split(":")[0] for f in found if "self-test reaches" in f), ["scripts/helper.py", "scripts/render.py"])
        check("arm AUDIT: a tools-only import no self-test reaches, and a gate-declared one, are not findings",
              [f for f in found if f.startswith(("scripts/uses_widget.py", "scripts/tool.py: imports `bigpdf`"))], [])
        # R4-1: what counts as local is the REPOSITORY's, never the checkout's. `sup` registers two submodules: `pub`
        # (the audited set, like the public skills) and `priv` (registered, never read, like specs-private, which no CI
        # runner can fetch). `sup` has both initialized (a developer's checkout); `fresh` is a clone with neither (CI's
        # `submodules: false`, a new worktree). Both must give the same findings: only `privmod`.
        saved = os.environ.get("GIT_CONFIG_GLOBAL")
        os.environ["GIT_CONFIG_GLOBAL"] = str(plant("gitconfig", "[protocol \"file\"]\n\tallow = always\n"
                                                                 "[user]\n\tname = self-test\n\temail = t@example.invalid\n"
                                                                 "[core]\n\tautocrlf = false\n"))
        try:
            def repo(name: str, files: dict[str, str]) -> Path:
                for rel, text in files.items():
                    plant(f"{name}/{rel}", text)
                subprocess.run(["git", "-C", str(root / name), "init", "-q"], check=True)
                subprocess.run(["git", "-C", str(root / name), "add", "-A"], check=True)
                return root / name

            def commit(r: Path) -> None:
                subprocess.run(["git", "-C", str(r), "commit", "-qm", "planted"], check=True, capture_output=True)

            for name, mod in (("pub", "pubmod"), ("priv", "privmod")):
                commit(repo(name, {f"lib/{mod}.py": "X = 1\n"}))
            sup = repo("sup", {"scripts/uses.py": "import pubmod\nimport privmod\n"})
            for name in ("pub", "priv"):
                subprocess.run(["git", "-C", str(sup), "submodule", "add", "-q", (root / name).as_posix(),
                                f"tools/{name}"], check=True, capture_output=True)
            commit(sup)
            fresh = root / "fresh"
            subprocess.run(["git", "clone", "-q", "--no-recurse-submodules", str(sup), str(fresh)], check=True,
                           capture_output=True)
            before = (fresh / "tools" / "pub" / ".git").exists()
            got_fresh = audit(fresh, gate, tools, [], ("tools/pub",), heard.append)
            got_sup = audit(sup, gate, tools, [], ("tools/pub",), heard.append)
            want = ["scripts/uses.py: imports `privmod`, declared in neither scripts/requirements-gate.txt nor "
                    "scripts/requirements-tools.txt"]
            check("arm SUBMODULES: a clone without submodules fetches the audited one, so its module is local there "
                  "too (R4-1: it read as an undeclared third-party import)",
                  (before, got_fresh, (fresh / "tools" / "pub" / ".git").exists()), (False, want, True))
            check("arm SUBMODULES: an initialized submodule outside the audited set is never read, so a module only it "
                  "holds is third-party in every checkout (R4-1: local only where it happened to be initialized)",
                  got_sup, want)
        finally:
            if saved is None:
                os.environ.pop("GIT_CONFIG_GLOBAL", None)
            else:
                os.environ["GIT_CONFIG_GLOBAL"] = saved
        setup = plant("scripts/cloud/setup-env.sh", "  python -m pip install -q --no-warn-script-location \\\n"
                                                    "      BigPDF stray >/tmp/pip-deps.log 2>&1 \\\n")
        check("arm SETUP-ENV: the cloud setup's pip line is held to the tools file's own packages (case folded)",
              setup_env_drift(setup, tools), ["setup-env.sh installs stray, which requirements-tools.txt does not declare"])

    # THE LIVE TREE: every declaration parses, and every import is declared where it belongs.
    try:
        live = audit(REPO, GATE, TOOLS, discovered_self_tests(REPO)) + setup_env_drift()
    except (OSError, ValueError, SyntaxError, subprocess.CalledProcessError) as e:
        live = [f"the audit could not run: {e}"]
    check("arm LIVE: this repository's third-party imports are all declared, and the self-tests' in the gate file",
          live, [])
    print(f"=== PYTHON_DEPS SELF-TEST: {'RED (' + str(len(failures)) + ')' if failures else 'GREEN'} ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if argv[:1] == ["--self-test"]:
        return self_test()
    if argv[:1] == ["ensure"] and len(argv) == 1:
        try:
            return ensure()
        except (OSError, ValueError) as e:
            print(f"=== PYTHON PACKAGES: MISSING — {e} ===")
            return 1
    if argv[:1] == ["venv"] and len(argv) == 2:
        try:
            print(venv(Path(argv[1]), say=lambda s: print(s, file=sys.stderr)))
            return 0
        except (OSError, ValueError, subprocess.CalledProcessError) as e:
            print(f"python_deps: the gate's environment could not be built: {e}", file=sys.stderr)
            return 1
    if argv[:1] == ["audit"] and len(argv) == 1:
        try:
            findings = audit(REPO, GATE, TOOLS, discovered_self_tests(REPO))
        except (OSError, ValueError, SyntaxError, subprocess.CalledProcessError) as e:
            print(f"=== PYTHON IMPORTS: NOT RUN — the audit could not run: {e} ===")
            return 2
        for f in findings:
            print(f"  {f}")
        print(f"=== PYTHON IMPORTS: {'RED (' + str(len(findings)) + ')' if findings else 'GREEN'} ===")
        return 1 if findings else 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
