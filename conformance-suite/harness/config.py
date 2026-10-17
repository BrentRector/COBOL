"""config.py — the formats of the conformance-suite harness's configuration (DESIGN-conformance-suite.md §4, §5).

Three files per compiler, each named for the compiler (`<compiler>` is the file's base name):

  adapters/<compiler>.json   HOW the harness drives the compiler: the compile, run and flag-mode command templates,
                             the edition map, how a diagnostic's line is read, how an ASSIGN name reaches a file, any
                             limit on the file names the harness chooses, the exit codes that mean the compiler
                             crashed, and optionally a Python hook that performs an operation itself.
  profiles/<compiler>.json   WHAT the compiler claims: the editions it supports, the Annex A.4 optional modules and
                             Annex A.3 processor-dependent elements it claims (each with its evidence), its value of
                             every Annex A.1 item whose values the standard enumerates, and the words it reserves
                             beyond the standard's (ISO 4.2.10 permits them), per edition.
  baselines/<compiler>.json  the compiler's recorded verdicts, per edition and test, written by `run.py
                             --record-baseline` and read for the CHANGES report.

This module is their ONE loader and validator: `run.py` loads them (shape only, because a released suite carries no
inventory), and the generator's audit (`generate.py --check`, a gate audit) validates every one of them against what
only the repository knows: the editions, the annex names, the A.1 items and the tests of each suite.

Standard library only (§5): the harness runs wherever Python 3 does, z/OS included.
"""
from __future__ import annotations

import importlib.util
import json
import re
from dataclasses import dataclass
from pathlib import Path
from types import ModuleType
from typing import Any, Iterable

#: The four verdicts of §5. NO VERDICT is counted on its own, never folded into PASS or FAIL.
PASS, FAIL, NOT_APPLICABLE, NO_VERDICT = "PASS", "FAIL", "NOT APPLICABLE", "NO VERDICT"
VERDICTS = (PASS, FAIL, NOT_APPLICABLE, NO_VERDICT)

#: The three operations an adapter performs (§5): compile and run an accepted test, and compile a flagged test with
#: the standard-conformance warning mechanism invoked (ISO 4.2.2).
OPERATIONS = ("compile", "run", "flag")
#: A placeholder of a command template: `{name}`.
PLACEHOLDER = re.compile(r"\{([a-z][a-z0-9-]*)\}")
#: The placeholders the harness fills for every test: the source file and program name it chose in the test's own
#: working directory, that directory, the adapter's spelling of the edition and of the test's reference format, and
#: the Python interpreter running the harness (for a compiler driven by a script).
BUILTIN_PLACEHOLDERS = frozenset({"source", "program", "work", "std", "source-format", "python"})
#: The placeholders of an ASSIGN mapping, per declared file: its base name without the extension, and its path.
ASSIGN_PLACEHOLDERS = frozenset({"name", "path"})
NAME = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
ANNEX_NAME = re.compile(r"^(?:A\.4\.[0-9]+|A\.3-[0-9]+)$")
WORD = re.compile(r"^[A-Z0-9][A-Z0-9_-]*$")
ADAPTER_FIELDS = frozenset({"$comment", "compiler", "profile", "editions", "source-formats", "variables",
                            "source-extension", "name-limit", "compile", "run", "flag", "diagnostic",
                            "crash-exit-codes", "environment", "assign", "hook"})
PROFILE_FIELDS = frozenset({"$comment", "compiler", "editions", "modules", "elements", "implementor-defined",
                            "extra-reserved-words"})
BASELINE_FIELDS = frozenset({"$comment", "compiler", "verdicts"})


@dataclass(frozen=True)
class Adapter:
    compiler: str
    profile: str
    #: suite edition -> the compiler's spelling of it ({std}); an edition absent here is one the adapter cannot run.
    editions: dict[str, str]
    #: `fixed` / `free` -> the compiler's spelling ({source-format}).
    source_formats: dict[str, str]
    #: defaults of the adapter's own placeholders, each overridable on the command line (`run.py --set NAME=VALUE`).
    variables: dict[str, str]
    source_extension: str
    #: the longest base name the compiler accepts for the files the harness names (cobc refused long ones), or None.
    name_limit: int | None
    #: operation -> its argv template; None where the hook performs the operation.
    commands: dict[str, tuple[str, ...] | None]
    #: a diagnostic's line: a regular expression with a `line` group, and optionally a `file` group (a diagnostic
    #: whose file is not the test's source, such as a copybook's, is not the test's).
    diagnostic: re.Pattern[str]
    #: exit codes that mean the compiler crashed (a negative code, a POSIX signal, always does).
    crash_exit_codes: frozenset[int]
    #: environment variables set for every operation (templates over the placeholders).
    environment: dict[str, str]
    #: per declared file: environment variable template -> value template ({name}, {path}). Empty: an ASSIGN name is a
    #: file of the test's working directory, where the harness put every declared file.
    assign_environment: dict[str, str]
    hook: ModuleType | None
    path: Path

    def placeholders(self) -> frozenset[str]:
        return BUILTIN_PLACEHOLDERS | frozenset(self.variables)


@dataclass(frozen=True)
class Profile:
    compiler: str
    editions: tuple[str, ...]
    #: Annex A.4 module (`A.4.11`) -> the evidence for the claim.
    modules: dict[str, str]
    #: Annex A.3 element (`A.3-15`) -> the evidence for the claim.
    elements: dict[str, str]
    #: Annex A.1 item -> the compiler's value of it (one the standard enumerates; implementor-defined.json).
    implementor_defined: dict[int, str]
    #: edition -> the words the compiler reserves there beyond the standard's (upper case).
    extra_reserved_words: dict[str, frozenset[str]]
    path: Path

    def claims(self, name: str) -> bool:
        return name in self.modules or name in self.elements


# ── the hook's interface (an adapter's `hook`, §5) ──

@dataclass(frozen=True)
class Step:
    """What one process did: its exit code (None when it did not finish or did not start) and its output. A hook's
    operation returns one."""

    returncode: int | None
    stdout: bytes = b""
    stderr: bytes = b""
    timed_out: bool = False
    #: why it could not start, when it could not.
    error: str | None = None

    def text(self) -> str:
        return (self.stdout + b"\n" + self.stderr).decode("utf-8", errors="replace")


@dataclass(frozen=True)
class StepContext:
    """What an operation needs, handed to a hook's `compile(ctx)`, `run(ctx)` or `flag(ctx)`: the test's working
    directory (cwd), the environment, the hang guard, every placeholder's value, and the adapter's template for the
    operation filled in (None when the hook replaces a missing one)."""

    operation: str
    argv: tuple[str, ...] | None
    work: Path
    environment: dict[str, str]
    timeout: float
    placeholders: dict[str, str]


def _read_json(path: Path) -> tuple[Any, list[str]]:
    try:
        return json.loads(path.read_text(encoding="utf-8")), []
    except (OSError, ValueError) as e:
        return None, [f"{path.name}: not readable JSON ({e})"]


def _string_map(raw: Any) -> bool:
    return isinstance(raw, dict) and all(isinstance(k, str) and isinstance(v, str) for k, v in raw.items())


def _template(raw: Any) -> bool:
    return isinstance(raw, list) and bool(raw) and all(isinstance(a, str) for a in raw)


def _load_hook(path: Path) -> tuple[ModuleType | None, str | None]:
    spec = importlib.util.spec_from_file_location(f"conformance_hook_{path.stem}", path)
    if spec is None or spec.loader is None:
        return None, f"hook {path.name} is not a Python module"
    module = importlib.util.module_from_spec(spec)
    try:
        spec.loader.exec_module(module)
    except Exception as e:  # noqa: BLE001 — a hook that does not import is a configuration problem, reported as such
        return None, f"hook {path.name} does not import: {e!r}"
    return module, None


def load_adapter(path: Path, editions: Iterable[str] | None = None) -> tuple[Adapter | None, list[str]]:
    """An adapter and its problems; `editions` (the repository's) is checked when given."""
    raw, problems = _read_json(path)
    if raw is None:
        return None, problems
    name = path.name
    if not isinstance(raw, dict):
        return None, [f"{name}: an adapter is a JSON object"]
    problems += [f"{name}: unknown field {k!r}" for k in sorted(set(raw) - ADAPTER_FIELDS)]
    if raw.get("compiler") != path.stem:
        problems.append(f"{name}: compiler is the file's base name {path.stem!r}, not {raw.get('compiler')!r}")
    if not isinstance(raw.get("profile"), str) or not NAME.match(raw.get("profile", "")):
        problems.append(f"{name}: profile names profiles/<compiler>.json")
    eds = raw.get("editions")
    if not _string_map(eds) or not eds:
        problems.append(f"{name}: editions maps each suite edition the adapter runs to the compiler's spelling")
        eds = {}
    if editions is not None:
        known = list(editions)
        problems += [f"{name}: edition {e!r} is not one of {', '.join(known)}" for e in eds if e not in known]
    formats = raw.get("source-formats")
    if not _string_map(formats) or set(formats) != {"fixed", "free"}:
        problems.append(f"{name}: source-formats maps both fixed and free to the compiler's spelling")
        formats = {"fixed": "fixed", "free": "free"}
    variables = raw.get("variables", {})
    if not _string_map(variables) or not all(NAME.match(k) for k in variables):
        problems.append(f"{name}: variables maps lower-case names to their defaults")
        variables = {}
    clash = sorted(set(variables) & BUILTIN_PLACEHOLDERS)
    if clash:
        problems.append(f"{name}: variables {', '.join(clash)} are the harness's own placeholders")
    extension = raw.get("source-extension", ".cob")
    if not isinstance(extension, str) or not re.match(r"^\.[A-Za-z0-9]+$", extension):
        problems.append(f"{name}: source-extension is a dot and letters or digits, not {extension!r}")
        extension = ".cob"
    limit = raw.get("name-limit")
    if limit is not None and (not isinstance(limit, int) or isinstance(limit, bool) or limit < 1):
        problems.append(f"{name}: name-limit is a positive integer or absent")
        limit = None
    hook = None
    if "hook" in raw:
        hook_name = raw["hook"]
        hook_path = path.parent / hook_name if isinstance(hook_name, str) else None
        if hook_path is None or hook_path.suffix != ".py" or not hook_path.is_file():
            problems.append(f"{name}: hook names a .py file beside the adapter, not {hook_name!r}")
        else:
            hook, problem = _load_hook(hook_path)
            if problem:
                problems.append(f"{name}: {problem}")
    allowed = BUILTIN_PLACEHOLDERS | frozenset(variables)
    commands: dict[str, tuple[str, ...] | None] = {}
    for op in OPERATIONS:
        template = raw.get(op)
        if template is None and hook is not None and callable(getattr(hook, op, None)):
            commands[op] = None
            continue
        if not _template(template):
            problems.append(f"{name}: {op} is an argv template (a list of strings), or performed by the hook")
            commands[op] = None
            continue
        for arg in template:
            for p in PLACEHOLDER.findall(arg):
                if p not in allowed:
                    problems.append(f"{name}: {op} uses {{{p}}}, which is neither a harness placeholder "
                                    f"({', '.join(sorted(BUILTIN_PLACEHOLDERS))}) nor one of its variables")
        commands[op] = tuple(template)
    diagnostic = re.compile(r"(?!)")
    pattern = raw.get("diagnostic")
    try:
        diagnostic = re.compile(pattern) if isinstance(pattern, str) else diagnostic
        if not isinstance(pattern, str) or "line" not in diagnostic.groupindex:
            problems.append(f"{name}: diagnostic is a regular expression with a `line` group")
    except re.error as e:
        problems.append(f"{name}: diagnostic does not compile: {e}")
    crash = raw.get("crash-exit-codes", [])
    if not isinstance(crash, list) or not all(isinstance(c, int) and not isinstance(c, bool) for c in crash):
        problems.append(f"{name}: crash-exit-codes lists integers")
        crash = []
    environment = raw.get("environment", {})
    if not _string_map(environment):
        problems.append(f"{name}: environment maps variable names to templates")
        environment = {}
    assign = raw.get("assign", {})
    assign_env: dict[str, str] = {}
    if not isinstance(assign, dict) or set(assign) - {"environment"} or not _string_map(assign.get("environment", {})):
        problems.append(f"{name}: assign is absent (an ASSIGN name is a file of the working directory) or "
                        f"{{\"environment\": {{VARIABLE: VALUE}}}} over {{name}} and {{path}}")
    else:
        assign_env = dict(assign.get("environment", {}))
        for template in (*assign_env, *assign_env.values()):
            for p in PLACEHOLDER.findall(template):
                if p not in ASSIGN_PLACEHOLDERS:
                    problems.append(f"{name}: assign uses {{{p}}}; its placeholders are {{name}} and {{path}}")
    if problems:
        return None, problems
    return Adapter(compiler=raw["compiler"], profile=raw["profile"], editions=dict(eds), source_formats=dict(formats),
                   variables=dict(variables), source_extension=extension, name_limit=limit, commands=commands,
                   diagnostic=diagnostic, crash_exit_codes=frozenset(crash), environment=dict(environment),
                   assign_environment=assign_env, hook=hook, path=path), []


def load_profile(path: Path, editions: Iterable[str] | None = None, requirable: frozenset[str] | None = None,
                 implementor_defined: dict[int, tuple[str, ...]] | None = None) -> tuple[Profile | None, list[str]]:
    """A profile and its problems. What only the repository knows (its editions, the annexes' names, the A.1 items
    whose values the standard enumerates) is checked when given: by the generator's audit, never by a run."""
    raw, problems = _read_json(path)
    if raw is None:
        return None, problems
    name = path.name
    if not isinstance(raw, dict):
        return None, [f"{name}: a profile is a JSON object"]
    problems += [f"{name}: unknown field {k!r}" for k in sorted(set(raw) - PROFILE_FIELDS)]
    if raw.get("compiler") != path.stem:
        problems.append(f"{name}: compiler is the file's base name {path.stem!r}, not {raw.get('compiler')!r}")
    eds = raw.get("editions")
    if not isinstance(eds, list) or not all(isinstance(e, str) for e in eds) or len(set(eds)) != len(eds or []):
        problems.append(f"{name}: editions lists the editions the compiler supports, each once")
        eds = []
    if editions is not None:
        known = list(editions)
        problems += [f"{name}: edition {e!r} is not one of {', '.join(known)}" for e in eds if e not in known]
        if [e for e in known if e in eds] != eds:
            problems.append(f"{name}: editions are listed in ascending order")
    claims: dict[str, dict[str, str]] = {}
    for key, prefix in (("modules", "A.4."), ("elements", "A.3-")):
        value = raw.get(key, {})
        if not _string_map(value):
            problems.append(f"{name}: {key} maps each claimed name to the evidence for the claim")
            value = {}
        for claim, why in value.items():
            if not ANNEX_NAME.match(claim) or not claim.startswith(prefix):
                problems.append(f"{name}: {key} names {prefix}<n>, not {claim!r}")
            elif requirable is not None and claim not in requirable:
                problems.append(f"{name}: {claim} is not an {'Annex A.4 module' if prefix == 'A.4.' else 'Annex A.3 element'} "
                                f"of the standard")
            if not why.strip():
                problems.append(f"{name}: the claim {claim} states its evidence")
        claims[key] = dict(value)
    values: dict[int, str] = {}
    a1 = raw.get("implementor-defined", {})
    if not _string_map(a1) or not all(k.isdigit() for k in a1):
        problems.append(f"{name}: implementor-defined maps Annex A.1 item numbers to the compiler's value")
        a1 = {}
    values = {int(k): v for k, v in a1.items()}
    if implementor_defined is not None:
        for item, allowed in sorted(implementor_defined.items()):
            if item not in values:
                problems.append(f"{name}: states no value of Annex A.1 item {item} (one of {', '.join(allowed)})")
            elif values[item] not in allowed:
                problems.append(f"{name}: item {item} is {values[item]!r}; the standard enumerates "
                                f"{', '.join(allowed)}")
        problems += [f"{name}: item {i} is not an A.1 item whose values implementor-defined.json enumerates"
                     for i in sorted(set(values) - set(implementor_defined))]
    words = raw.get("extra-reserved-words", {})
    extra: dict[str, frozenset[str]] = {}
    if not isinstance(words, dict):
        problems.append(f"{name}: extra-reserved-words maps an edition to its list of words")
        words = {}
    for edition, listed in words.items():
        if edition not in eds:
            problems.append(f"{name}: extra-reserved-words names {edition!r}, which the profile does not support")
        if not isinstance(listed, list) or not all(isinstance(w, str) and WORD.match(w) for w in listed):
            problems.append(f"{name}: extra-reserved-words {edition} lists upper-case COBOL words")
            continue
        extra[edition] = frozenset(listed)
    if problems:
        return None, problems
    return Profile(compiler=raw["compiler"], editions=tuple(eds), modules=claims["modules"],
                   elements=claims["elements"], implementor_defined=values, extra_reserved_words=extra,
                   path=path), []


def load_baseline(path: Path) -> tuple[dict[str, dict[str, str]] | None, list[str]]:
    """`{edition: {test id: verdict}}` and its problems; a missing file is no baseline (None) and no problem."""
    if not path.is_file():
        return None, []
    raw, problems = _read_json(path)
    if raw is None:
        return None, problems
    name = path.name
    if not isinstance(raw, dict):
        return None, [f"{name}: a baseline is a JSON object"]
    problems += [f"{name}: unknown field {k!r}" for k in sorted(set(raw) - BASELINE_FIELDS)]
    if raw.get("compiler") != path.stem:
        problems.append(f"{name}: compiler is the file's base name {path.stem!r}, not {raw.get('compiler')!r}")
    verdicts = raw.get("verdicts")
    if not isinstance(verdicts, dict) or not all(_string_map(v) for v in verdicts.values()):
        return None, problems + [f"{name}: verdicts maps each edition to {{test id: verdict}}"]
    for edition, tests in verdicts.items():
        problems += [f"{name}: {edition} {t}: {v!r} is not one of {', '.join(VERDICTS)}"
                     for t, v in tests.items() if v not in VERDICTS]
    return (None if problems else {e: dict(t) for e, t in verdicts.items()}), problems


def write_baseline(path: Path, compiler: str, verdicts: dict[str, dict[str, str]]) -> None:
    content = {
        "$comment": [
            f"{compiler}'s recorded conformance-suite verdicts, per edition and test (DESIGN-conformance-suite.md §5),",
            "written by `run.py --record-baseline`; a run reports its CHANGES against them, and a verdict that",
            "differs from its recorded one for the worse (FAIL or NO VERDICT, or a PASS lost) makes the run RED.",
        ],
        "compiler": compiler,
        "verdicts": {e: dict(sorted(t.items())) for e, t in sorted(verdicts.items(), key=lambda kv: int(kv[0]))},
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(content, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def check_tree(root: Path, editions: list[str], requirable: frozenset[str],
               implementor_defined: dict[int, tuple[str, ...]],
               suites: dict[str, set[str]]) -> list[tuple[str, str]]:
    """Every problem of every adapter, profile and baseline under `root`, as `(relative path, message)`: the shape
    of each, an adapter naming no profile or mapping an edition its profile supports to nothing, and a baseline of no
    adapter or naming a test the edition's suite does not hold (`suites`: edition -> its test ids)."""
    out: list[tuple[str, str]] = []
    profiles: dict[str, Profile] = {}
    for path in sorted((root / "profiles").glob("*.json")):
        profile, problems = load_profile(path, editions, requirable, implementor_defined)
        out += [(f"profiles/{path.name}", p) for p in problems]
        if profile is not None:
            profiles[path.stem] = profile
    adapters: set[str] = set()
    for path in sorted((root / "adapters").glob("*.json")):
        adapter, problems = load_adapter(path, editions)
        out += [(f"adapters/{path.name}", p) for p in problems]
        if adapter is None:
            continue
        adapters.add(adapter.compiler)
        profile = profiles.get(adapter.profile)
        if profile is None:
            if not (root / "profiles" / f"{adapter.profile}.json").is_file():
                out.append((f"adapters/{path.name}", f"profile {adapter.profile!r} has no profiles/{adapter.profile}.json"))
            continue
        for e in profile.editions:
            if e not in adapter.editions:
                out.append((f"adapters/{path.name}", f"its profile supports {e}, which editions does not map"))
    for path in sorted((root / "baselines").glob("*.json")):
        verdicts, problems = load_baseline(path)
        rel = f"baselines/{path.name}"
        out += [(rel, p) for p in problems]
        if path.stem not in adapters:
            out.append((rel, f"a baseline of {path.stem!r}, which has no adapter"))
        for edition, tests in (verdicts or {}).items():
            if edition not in suites:
                out.append((rel, f"edition {edition!r} is no suite"))
                continue
            out += [(rel, f"{edition} {t}: no test of the {edition} suite has this id")
                    for t in sorted(set(tests) - suites[edition])]
    # The loaders name the file in each problem (a run reports them alone); here the path already does.
    return [(rel, message.removeprefix(f"{Path(rel).name}: ")) for rel, message in out]
