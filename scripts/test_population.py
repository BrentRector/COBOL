#!/usr/bin/env python3
"""test_population.py — did the union of the runs execute EXACTLY the discovered population? (kb/Work PB1718;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.3–3.14.4)

⛔ ONE TOOL FOR EVERY WHOLE-ASSEMBLY RUN. The gate's soundness rests on "every discovered test case ran exactly
once", and until this tool three runs asserted it three different ways or not at all: CI summed each shard's
`Total:` line against a `grep -c` of the listing (a count cannot see a dropped case offset by one that ran twice),
while the battery accepted any `Passed!` line with exit 0 and the gate checked nothing. Its callers are the
battery (`scripts/battery.sh` PHASE 1, all three assemblies) and CI's `conformance-population` job (each platform's
shard trx files); the ordered gate's driver (DESIGN §3.14.9 M13) calls `list_population` and `check` per assembly
over both legs.

THE QUESTION, answered as multisets of display names:
  population  `dotnet test --list-tests` — vstest DISCOVERY, which never calls the executor and so cannot see an
              in-assembly leg filter. Listed by THIS tool in a scrubbed environment (below), so an environment
              that narrows the run cannot narrow the population it is checked against.
  executed    the union of the trx files' TEST DEFINITIONS (`<UnitTest name>`, one per discovered case that
              executed). NOT the result names: xunit lists a theory whose data it cannot serialize as ONE case and
              then reports one result per row, so Unit's results carry names `--list-tests` never printed while
              its definitions equal the listing exactly (evidence `impact-map-pb1708/b8`).
  NEVER RAN   a listed name short in the definitions — red, each named (NOT RUN instead when the caller says the
              run stopped early: the verdict is RED/INCOMPLETE either way);
  RAN TWICE   a listed name over — red, named (two shards whose filters overlap);
  NOT IN THE POPULATION  a definition the listing never printed — red, named (a trx from another build or
              assembly);
  skipped     a definition with no Passed or Failed result (a `[Fact(Skip)]` is NotExecuted) — counted on the
              line, never as ran.

⛔ THE SCRUB (§3.14.3). Three channels can make a whole-assembly `dotnet test` run a PART of its assembly while
exiting 0, and every one of them is an ENVIRONMENT variable:
  COBOLNET_GATE_*          the gate's leg handshake (`COBOLNET_GATE_PLAN`, `_LEG`, `_PLAN_SHA256`): only the gate
                           driver may hand it to a test host;
  VSTest*                  MSBuild imports every environment variable as a property, and the VSTest target reads
                           `VSTestTestCaseFilter`, `VSTestSetting`, `VSTestListTests`, … — MEASURED 2026-09-28:
                           `VSTestTestCaseFilter=FullyQualifiedName~Snapshot` narrowed Characterization from 33
                           cases to 31, in the run AND in `--list-tests`, exit 0;
  RunSettingsFilePath      the same channel through the Test SDK's run-settings property (MEASURED: a runsettings
                           file carrying a TestCaseFilter narrowed the listing to 31 the same way).
Because the VSTest channel narrows DISCOVERY too, a population listed in the caller's environment would be blind
to it — which is why the listing here always runs scrubbed. `is_scrubbed` is the ONE statement of the rule;
python callers pass `env=scrubbed_env()`, every other caller runs `python scripts/test_population.py scrubbed
dotnet test …`, and `audit-callers` (GateLegDriftTests arm 6) is red on a `dotnet test` under `scripts/` that
does neither, or on a CI workflow that SETS a scrubbed variable (a GitHub-hosted job's environment is exactly
what the workflow declares, so a workflow that sets none is scrubbed by construction).

Usage:
  python scripts/test_population.py check --label L (--project P [--configuration C] [--save-listing F]
                                                     | --listing F [F …]) --trx T [T …] [--stopped-early]
  python scripts/test_population.py list --project P [--configuration C] --out F
  python scripts/test_population.py scrubbed <command> [args …]
  python scripts/test_population.py audit-callers
  python scripts/test_population.py --self-test

Several `--listing` files (one per CI shard) must be identical — the shards ran one build — or the check is an
ERROR. exit 0 EXACT · 1 RED (never ran / ran twice / not in the population / stopped early) · 2 the check could
not run (a missing or unparsable input, an empty population, listings that disagree): an error, never a finding.
"""

from __future__ import annotations

import argparse
import ast
import os
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
LISTED_MARK = "The following Tests are available:"
LISTED_LINE = re.compile(r"^ {4}\S")
TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
RAN_OUTCOMES = frozenset({"Passed", "Failed"})
RC_EXACT, RC_RED, RC_ERROR = 0, 1, 2

#: The gate's leg handshake (§3.14.3): written by the gate driver alone.
HANDSHAKE = ("COBOLNET_GATE_PLAN", "COBOLNET_GATE_LEG", "COBOLNET_GATE_PLAN_SHA256")
_SCRUBBED_PREFIXES = ("COBOLNET_GATE_", "VSTEST")
_SCRUBBED_NAMES = frozenset({"RUNSETTINGSFILEPATH"})


class PopulationError(Exception):
    """The check could not run — a missing observation, never a negative one (exit 2)."""


def is_scrubbed(name: str) -> bool:
    """THE scrub rule: may this environment variable narrow a `dotnet test` run? Case-insensitive, because both
    the Windows environment and MSBuild property names are."""
    upper = name.upper()
    return upper.startswith(_SCRUBBED_PREFIXES) or upper in _SCRUBBED_NAMES


def scrubbed_env(base: dict[str, str] | None = None) -> dict[str, str]:
    """`base` (default: this process's environment) without every variable `is_scrubbed` names. A caller that
    must add variables of its own merges them over the result: `{**scrubbed_env(), "X": "1"}`."""
    return {k: v for k, v in (os.environ if base is None else base).items() if not is_scrubbed(k)}


# ── the population ────────────────────────────────────────────────────────────────────────────────────────────


def parse_listing(text: str, source: str) -> Counter:
    """The display names a `dotnet test --list-tests` output lists, as a multiset (xunit truncates long theory
    arguments with `···`, so one name can cover several rows)."""
    if LISTED_MARK not in text:
        raise PopulationError(f"{source}: no `{LISTED_MARK}` line — not a `dotnet test --list-tests` output")
    return Counter(line[4:] for line in text.split(LISTED_MARK, 1)[1].splitlines() if LISTED_LINE.match(line))


def list_population(project: str | Path, configuration: str | None = None,
                    cwd: str | Path = REPO) -> tuple[Counter, str]:
    """Discover `project`'s population with `--list-tests`, scrubbed, in English. Returns (names, raw output)."""
    cmd = ["dotnet", "test", str(project), "--no-build", "--list-tests"]
    if configuration:
        cmd += ["--configuration", configuration]
    proc = subprocess.run(cmd, cwd=cwd, capture_output=True,
                          env={**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en"})
    try:
        # STRICT: a mis-decoded name would read as NEVER RAN beside a NOT IN THE POPULATION twin.
        text = proc.stdout.decode("utf-8")
    except UnicodeDecodeError as e:
        raise PopulationError(f"{project}: the listing is not UTF-8 ({e})") from e
    if proc.returncode != 0 or LISTED_MARK not in text:
        tail = (text + proc.stderr.decode("utf-8", "replace")).strip()[-1500:]
        raise PopulationError(f"{project}: `--list-tests` failed (exit {proc.returncode}) — build the solution "
                              f"first, it runs --no-build:\n{tail}")
    return parse_listing(text, str(project)), text


def read_listings(paths: list[str]) -> Counter:
    """One population from one or more saved listings — which must agree: the runs came from one build."""
    listed: Counter | None = None
    first = ""
    for p in paths:
        try:
            names = parse_listing(Path(p).read_text(encoding="utf-8"), p)
        except (OSError, UnicodeDecodeError) as e:
            raise PopulationError(f"{p}: cannot read the listing ({e})") from e
        if listed is None:
            listed, first = names, p
        elif names != listed:
            raise PopulationError(f"LISTINGS DISAGREE: {p} and {first} differ by "
                                  f"{sum(((names - listed) + (listed - names)).values())} name(s) — the trx "
                                  "files do not come from one build")
    if listed is None:
        raise PopulationError("no listing given")
    return listed


# ── the executed set ──────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True)
class TrxCases:
    definitions: Counter   # every <UnitTest name> — one per discovered case that executed
    skipped: Counter       # the definitions with no Passed or Failed result


def read_trx(path: str | Path) -> TrxCases:
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as e:
        raise PopulationError(f"{path}: cannot read the trx ({e})") from e
    if root.tag != TRX_NS + "TestRun":
        raise PopulationError(f"{path}: not a trx file (root element {root.tag})")
    outcomes: dict[str, set[str]] = defaultdict(set)
    for r in root.iter(TRX_NS + "UnitTestResult"):
        outcomes[r.get("testId", "")].add(r.get("outcome", ""))
    definitions, skipped = Counter(), Counter()
    for u in root.iter(TRX_NS + "UnitTest"):
        name = u.get("name", "")
        definitions[name] += 1
        if not outcomes[u.get("id", "")] & RAN_OUTCOMES:
            skipped[name] += 1
    return TrxCases(definitions, skipped)


# ── the check ─────────────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True)
class Population:
    label: str
    discovered: int
    ran: int
    skipped: int
    trx_files: int
    never_ran: Counter
    ran_twice: Counter
    not_listed: Counter
    stopped_early: bool

    @property
    def exact(self) -> bool:
        return not (self.never_ran or self.ran_twice or self.not_listed or self.stopped_early)

    def verdict_line(self) -> str:
        tally = (f"{self.ran:,}/{self.discovered:,} cases ran (skipped {self.skipped:,}) across "
                 f"{self.trx_files} trx")
        if self.exact:
            return f"=== POPULATION {self.label}: EXACT — {tally} ==="
        parts = [f"{sum(self.never_ran.values()):,} "
                 + ("NOT RUN (the run stopped early)" if self.stopped_early else "NEVER RAN")] if self.never_ran else []
        if self.ran_twice:
            parts.append(f"{sum(self.ran_twice.values()):,} RAN TWICE")
        if self.not_listed:
            parts.append(f"{sum(self.not_listed.values()):,} NOT IN THE POPULATION")
        head = "RED/INCOMPLETE" if self.stopped_early else "RED"
        return f"=== POPULATION {self.label}: {head} — {' · '.join(parts) or 'the run stopped early'} — {tally} ==="

    def report(self) -> list[str]:
        lines = [self.verdict_line()]
        tag = "NOT RUN" if self.stopped_early else "NEVER RAN"
        for label, names in ((tag, self.never_ran), ("RAN TWICE", self.ran_twice),
                             ("NOT IN THE POPULATION", self.not_listed)):
            for name in sorted(names):
                lines.append(f"  {label:<21} x{names[name]}  {name}")
        return lines


def check(label: str, listed: Counter, trx_paths: list[str], stopped_early: bool = False) -> Population:
    """Did the union of `trx_paths` execute exactly `listed`? Raises PopulationError when it cannot tell."""
    if not listed:
        raise PopulationError(f"{label}: the population is EMPTY — a check over nothing can never be clean")
    if not trx_paths:
        raise PopulationError(f"{label}: no trx file given")
    definitions, skipped = Counter(), Counter()
    for p in trx_paths:
        cases = read_trx(p)
        definitions += cases.definitions
        skipped += cases.skipped
    over = definitions - listed
    ran_twice = Counter({n: c for n, c in over.items() if n in listed})
    not_listed = Counter({n: c for n, c in over.items() if n not in listed})
    skip_total = sum(skipped.values())
    return Population(label=label, discovered=sum(listed.values()),
                      ran=sum(definitions.values()) - skip_total, skipped=skip_total, trx_files=len(trx_paths),
                      never_ran=listed - definitions, ran_twice=ran_twice, not_listed=not_listed,
                      stopped_early=stopped_early)


# ── the caller audit (GateLegDriftTests arm 6) ────────────────────────────────────────────────────────────────

#: `dotnet test` in COMMAND position: at the start of a line or string, or after a shell separator, `&`, `(`,
#: `$(` or a quote (not a backtick: in this tree that is Markdown prose far more often than a substitution). The
#: scrubbed form (`… test_population.py scrubbed dotnet test`) is never in command position, so it needs no
#: exemption of its own.
_COMMAND_POSITION = re.compile(r"(?:^|[;&|(\"']|\$\()\s*dotnet\s+test\b")
_ANY_DOTNET_TEST = re.compile(r"\bdotnet\s+test\b")
_SCRUBBED_RUN = re.compile(r"\bscrubbed\s+dotnet\s+test\b")
#: A name immediately followed by `=` or `:` — an assignment, an `env:` key, `-p:Name=`, `$env:Name =`.
_ASSIGNED = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*[:=]")
MECHANISM = "scripts/test_population.py"
#: Documentation and data under scripts/ — prose that may QUOTE a command, never a runner.
_PROSE_SUFFIXES = frozenset({".md", ".txt", ".json", ".tsv", ".csv"})


def _code_lines(text: str) -> list[tuple[int, str]]:
    """Every line that is not a WHOLE-line `#` comment (bash, PowerShell, YAML)."""
    return [(i, line) for i, line in enumerate(text.splitlines(), 1) if not line.lstrip().startswith("#")]


def _python_findings(text: str, rel: str) -> tuple[bool, list[str]]:
    """A python file runs `dotnet test` through a `["dotnet", "test", …]` literal (must then call
    `scrubbed_env`) or prints one as an instruction (must then print the scrubbed form). Docstrings are prose."""
    tree = ast.parse(text, filename=rel)
    docstrings = set()
    for node in ast.walk(tree):
        if isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef)) and node.body:
            first = node.body[0]
            if isinstance(first, ast.Expr) and isinstance(first.value, ast.Constant):
                docstrings.add(id(first.value))
    subprocess_run, printed, scrubs, findings = False, False, False, []
    for node in ast.walk(tree):
        if isinstance(node, (ast.List, ast.Tuple)) and len(node.elts) >= 2 and all(
                isinstance(e, ast.Constant) for e in node.elts[:2]) and \
                [e.value for e in node.elts[:2]] == ["dotnet", "test"]:
            subprocess_run = True
        elif isinstance(node, ast.Call):
            f = node.func
            scrubs |= (isinstance(f, ast.Name) and f.id == "scrubbed_env") or (
                isinstance(f, ast.Attribute) and f.attr == "scrubbed_env")
        elif isinstance(node, ast.Constant) and isinstance(node.value, str) and id(node) not in docstrings:
            for line in node.value.splitlines():
                printed |= bool(_SCRUBBED_RUN.search(line))
                if _COMMAND_POSITION.search(line):
                    printed = True
                    findings.append(f"{rel}:{node.lineno}: prints an unscrubbed `dotnet test` instruction: "
                                    f"{line.strip()[:120]}")
    if subprocess_run and not scrubs:
        findings.append(f"{rel}: runs `dotnet test` without `env=scrubbed_env()`")
    return subprocess_run or printed, findings


def audit_callers(root: Path = REPO) -> tuple[list[str], list[str]]:
    """(the sites that run `dotnet test`, the offenders). Scope: every script under `scripts/` except the hooks
    (they receive commands as data and run no test) and this mechanism; every workflow under `.github/workflows/`."""
    sites, offenders = [], []
    for path in sorted((root / "scripts").rglob("*")):
        rel = path.relative_to(root).as_posix()
        if not path.is_file() or "__pycache__" in rel or rel.startswith("scripts/hooks/") or rel == MECHANISM \
                or path.suffix in _PROSE_SUFFIXES:
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        if path.suffix not in (".sh", ".ps1", ".py"):
            # FAIL CLOSED: a runner this audit cannot parse is named, never skipped as clean.
            if _ANY_DOTNET_TEST.search(text):
                sites.append(rel)
                offenders.append(f"{rel}: mentions `dotnet test` in a file type this audit cannot check "
                                 f"({path.suffix or 'no extension'}) — make it a .sh, .ps1 or .py script")
            continue
        if path.suffix == ".py":
            runs, findings = _python_findings(text, rel)
            if runs:
                sites.append(rel)
            offenders += findings
            continue
        code = _code_lines(text)
        if any(_ANY_DOTNET_TEST.search(line) for _, line in code):
            sites.append(rel)
        offenders += [f"{rel}:{i}: runs `dotnet test` unscrubbed: {line.strip()[:120]}"
                      for i, line in code if _COMMAND_POSITION.search(line)]
    wf_dir = root / ".github" / "workflows"
    for path in sorted([*wf_dir.glob("*.yml"), *wf_dir.glob("*.yaml")]) if wf_dir.is_dir() else []:
        rel = path.relative_to(root).as_posix()
        code = _code_lines(path.read_text(encoding="utf-8", errors="replace"))
        if any(_ANY_DOTNET_TEST.search(line) for _, line in code):
            sites.append(rel)
        offenders += [f"{rel}:{i}: sets `{m.group(1)}`, a variable that narrows a `dotnet test` run"
                      for i, line in code for m in _ASSIGNED.finditer(line) if is_scrubbed(m.group(1))]
    return sites, offenders


# ── the self-test ─────────────────────────────────────────────────────────────────────────────────────────────


def _trx(path: Path, cases: list[tuple[str, list[str]]], stray_results: list[tuple[str, str, str]] = ()) -> str:
    """A minimal trx: each case is (definition name, [result outcomes]); a stray result is (definition name it
    belongs to, the RESULT's own name, outcome) — the shape of a theory xunit could not serialize."""
    from xml.sax.saxutils import quoteattr
    ids = {}
    defs, results = [], []
    for i, (name, outs) in enumerate(cases):
        tid = f"00000000-0000-0000-0000-{i:012d}"
        ids.setdefault(name, tid)
        defs.append(f"<UnitTest name={quoteattr(name)} id=\"{tid}\" />")
        results += [f"<UnitTestResult testId=\"{tid}\" testName={quoteattr(name)} outcome=\"{o}\" />" for o in outs]
    for owner, result_name, outcome in stray_results:
        results.append(f"<UnitTestResult testId=\"{ids[owner]}\" testName={quoteattr(result_name)} "
                       f"outcome=\"{outcome}\" />")
    path.write_text("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                    "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">"
                    f"<Results>{''.join(results)}</Results><TestDefinitions>{''.join(defs)}</TestDefinitions>"
                    "</TestRun>\n", encoding="utf-8")
    return str(path)


def _listing(path: Path, names: list[str]) -> str:
    path.write_text("Test run for X.dll (.NETCoreApp,Version=v10.0)\n" + LISTED_MARK + "\n"
                    + "".join(f"    {n}\n" for n in names), encoding="utf-8")
    return str(path)


def self_test() -> int:
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))

    def rc_of(fn) -> tuple[int, str]:
        try:
            return (RC_EXACT, "") if fn() else (RC_RED, "")
        except PopulationError as e:
            return RC_ERROR, str(e)

    with tempfile.TemporaryDirectory() as tmp:
        d = Path(tmp)
        trunc = "N.T.Theory(text: \"aaaa\"···"
        population = ["N.A.One", "N.A.Two", "N.B.Three", trunc, trunc, "N.C.Skipped"]
        listing = _listing(d / "all.list.txt", population)
        listed = read_listings([listing])
        ok = lambda outs=("Passed",): list(outs)  # noqa: E731
        full = [(n, ok()) for n in population[:-1]] + [("N.C.Skipped", ["NotExecuted"])]

        p = check("exact", listed, [_trx(d / "exact.trx", full)])
        arm("exact population, a truncated name listed twice, one skip", p.exact and p.discovered == 6
            and p.ran == 5 and p.skipped == 1 and "EXACT — 5/6 cases ran (skipped 1)" in p.verdict_line(),
            p.verdict_line())

        p = check("short", listed, [_trx(d / "short.trx", [c for c in full if c[0] != "N.A.Two"])])
        arm("short: a dropped case is NEVER RAN, named", not p.exact and p.never_ran == Counter({"N.A.Two": 1})
            and any("NEVER RAN" in ln and "N.A.Two" in ln for ln in p.report()[1:]), p.verdict_line())

        p = check("truncated", listed, [_trx(d / "trunc.trx", [c for c in full if c[0] != trunc] + [(trunc, ok())])])
        arm("short by multiplicity: one of two rows sharing a truncated name dropped",
            p.never_ran == Counter({trunc: 1}), p.verdict_line())

        p = check("over", listed, [_trx(d / "over.trx", full + [("N.B.Three", ok())])])
        arm("over: a case twice is RAN TWICE, named", p.ran_twice == Counter({"N.B.Three": 1}) and not p.never_ran
            and "1 RAN TWICE" in p.verdict_line(), p.verdict_line())

        shard1 = _trx(d / "shard1.trx", [c for c in full if c[0] in ("N.A.One", "N.A.Two", trunc)])
        shard2 = _trx(d / "shard2.trx", [c for c in full if c[0] in ("N.A.Two", "N.C.Skipped")])
        executed = sum(read_trx(shard1).definitions.values()) + sum(read_trx(shard2).definitions.values())
        p = check("overlap", listed, [shard1, shard2])
        arm("shard overlap that KEEPS THE COUNT (a count guard passes it; this is red)",
            executed == sum(listed.values()) and p.ran_twice == Counter({"N.A.Two": 1})
            and p.never_ran == Counter({"N.B.Three": 1}), f"counted {executed}; {p.verdict_line()}")

        p = check("skip", listed, [_trx(d / "skip.trx", full)])
        arm("skipped: a NotExecuted-only definition is counted skipped, never ran", p.skipped == 1 and p.ran == 5)

        stray = [("N.A.One", "N.A.One(row: 1)", "Passed"), ("N.A.One", "N.A.One(row: 2)", "Passed")]
        p = check("defs", listed, [_trx(d / "defs.trx", full, stray)])
        arm("definitions, not result names: an unserializable theory's per-row results are not cases",
            p.exact, p.verdict_line())

        p = check("foreign", listed, [_trx(d / "foreign.trx", full + [("Other.Assembly.Test", ok())])])
        arm("a definition the listing never printed is NOT IN THE POPULATION", not p.exact
            and p.not_listed == Counter({"Other.Assembly.Test": 1}), p.verdict_line())

        p = check("early", listed, [_trx(d / "early.trx", full[:2])], stopped_early=True)
        arm("stopped early: the remainder is NOT RUN and the verdict RED/INCOMPLETE", not p.exact
            and "RED/INCOMPLETE" in p.verdict_line() and "NOT RUN" in p.verdict_line(), p.verdict_line())

        other = _listing(d / "other.list.txt", population[:-1])
        rc, why = rc_of(lambda: read_listings([listing, other]))
        arm("two listings that disagree are an ERROR", rc == RC_ERROR and "LISTINGS DISAGREE" in why, why)
        rc, why = rc_of(lambda: check("gone", listed, [str(d / "absent.trx")]).exact)
        arm("a missing trx is an ERROR, never a finding", rc == RC_ERROR, why)
        (d / "bad.trx").write_text("<not-a-trx/>", encoding="utf-8")
        rc, why = rc_of(lambda: check("bad", listed, [str(d / "bad.trx")]).exact)
        arm("a file that is not a trx is an ERROR", rc == RC_ERROR, why)
        (d / "nomark.txt").write_text("No test is available in X.dll\n", encoding="utf-8")
        rc, why = rc_of(lambda: read_listings([str(d / "nomark.txt")]))
        arm("a listing without the vstest marker is an ERROR", rc == RC_ERROR, why)
        rc, why = rc_of(lambda: check("empty", read_listings([_listing(d / "e.txt", [])]),
                                      [_trx(d / "e.trx", [])]).exact)
        arm("an EMPTY population is an ERROR, never clean", rc == RC_ERROR and "EMPTY" in why, why)
        body = parse_listing(LISTED_MARK + "\n    A.B\n     \n  not a case\n    C.D  \n", "inline")
        arm("the listing parser keeps 4-space-indented names verbatim", body == Counter({"A.B": 1, "C.D  ": 1}),
            str(body))

        scrubbed = ["COBOLNET_GATE_PLAN", "cobolnet_gate_leg", "COBOLNET_GATE_PLAN_SHA256", "VSTestTestCaseFilter",
                    "VSTestSetting", "vstest_host_debug", "RunSettingsFilePath"]
        kept = ["COBOLNET_COMPILE_CACHE", "PATH", "RUNSETTINGS", "COBOLNET_GATE"]
        arm("the scrub rule names the handshake, VSTest* and RunSettingsFilePath (any case), nothing else",
            all(map(is_scrubbed, scrubbed)) and not any(map(is_scrubbed, kept)) and all(map(is_scrubbed, HANDSHAKE)))
        planted = {**os.environ, "COBOLNET_GATE_LEG": "1", "VSTestTestCaseFilter": "X~Y", "W71D_KEEP": "kept"}
        probe = subprocess.run([sys.executable, __file__, "scrubbed", sys.executable, "-c",
                                "import os; print(sorted(k for k in os.environ if k.upper() in "
                                "('COBOLNET_GATE_LEG', 'VSTESTTESTCASEFILTER', 'W71D_KEEP')))"],
                               env=planted, capture_output=True, text=True)
        arm("`scrubbed <command>` runs the command without a planted handshake or VSTest variable",
            probe.returncode == 0 and probe.stdout.strip() == "['W71D_KEEP']", probe.stdout + probe.stderr)

        r = d / "repo"
        files = {
            "scripts/bad.sh": "dotnet test tests/X --no-build\n",
            "scripts/good.sh": "\"$PY\" scripts/test_population.py scrubbed dotnet test tests/X --no-build\n"
                               "# dotnet test in a comment is not a run\n",
            "scripts/bad.ps1": "& dotnet test @testArgs *> $log\n",
            "scripts/good.ps1": "& python scripts/test_population.py scrubbed dotnet test @testArgs *> $log\n",
            "scripts/bad.py": "import subprocess\nsubprocess.run([\"dotnet\", \"test\", \"x\"])\n",
            "scripts/good.py": "from test_population import scrubbed_env\nimport subprocess\n"
                               "subprocess.run([\"dotnet\", \"test\", \"x\"], env=scrubbed_env())\n",
            "scripts/prose.py": "\"\"\"Runs one `dotnet test` leg.\n\ndotnet test tests/X\n\"\"\"\n"
                                "HELP = 'the leg\\'s complete `dotnet test` output'\n",
            "scripts/instruction.py": "GATE = f'    dotnet test {1} --no-build'\n",
            "scripts/good_instruction.py": "GATE = f'    python scripts/test_population.py scrubbed dotnet test {1}'\n",
            "scripts/hooks/hook.sh": "dotnet test tests/X\n",
            "scripts/runner.cmd": "dotnet test tests/X\n",
            "scripts/NOTES.md": "run dotnet test tests/X by hand\n",
            ".github/workflows/bad.yml": "jobs:\n  a:\n    env:\n      VSTestTestCaseFilter: x\n    steps:\n"
                                        "      - run: dotnet test tests/X\n"
                                        "      - run: echo \"COBOLNET_GATE_LEG=1\" >> $GITHUB_ENV\n",
            ".github/workflows/good.yml": "jobs:\n  a:\n    env:\n      COBOLNET_NIST_STD: '2023'\n    steps:\n"
                                         "      - run: dotnet test tests/X\n",
        }
        for rel, text in files.items():
            (r / rel).parent.mkdir(parents=True, exist_ok=True)
            (r / rel).write_text(text, encoding="utf-8")
        sites, offenders = audit_callers(r)
        flagged = {o.split(":", 1)[0] for o in offenders}
        want = {"scripts/bad.sh", "scripts/bad.ps1", "scripts/bad.py", "scripts/instruction.py",
                "scripts/runner.cmd", ".github/workflows/bad.yml"}
        arm("audit-callers flags every unscrubbed shape and passes every scrubbed one (an unparsable runner type "
            "fails closed, a Markdown note is prose)", flagged == want,
            f"flagged {sorted(flagged)}")
        arm("audit-callers: a docstring or a help string is prose, a hook is out of scope, a comment is not a run",
            "scripts/prose.py" not in sites and "scripts/hooks/hook.sh" not in sites and "scripts/good.sh" in sites
            and "scripts/good_instruction.py" in sites and "scripts/good.py" in sites
            and sum(1 for o in offenders if o.startswith(".github/workflows/bad.yml")) == 2, f"sites {sites}")
        shutil.rmtree(r, ignore_errors=True)

    for name, passed, detail in results:
        print(f"  {'PASS' if passed else 'FAIL'}  {name}" + ("" if passed or not detail else f"\n        {detail}"))
    failed = sum(1 for _, passed, _ in results if not passed)
    print(f"=== test_population SELF-TEST: {'PASS' if failed == 0 else f'FAIL ({failed})'} "
          f"({len(results)} arms) ===")
    return RC_EXACT if failed == 0 else RC_RED


# ── the command line ──────────────────────────────────────────────────────────────────────────────────────────


def main(argv: list[str]) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass
    if argv[:1] == ["scrubbed"]:
        if len(argv) < 2:
            print("test_population.py scrubbed: no command given", file=sys.stderr)
            return RC_ERROR
        # Not exec: on Windows os.exec* does not replace the process, and the caller waits on OUR exit code.
        return subprocess.run(argv[1:], env=scrubbed_env()).returncode
    if argv[:1] == ["--self-test"]:
        return self_test()

    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("check", help="assert the trx files' union is exactly the discovered population")
    c.add_argument("--label", required=True)
    source = c.add_mutually_exclusive_group(required=True)
    source.add_argument("--project", help="list the population now, scrubbed (the project must be built)")
    source.add_argument("--listing", nargs="+", help="saved `--list-tests` output(s) — several must agree")
    c.add_argument("--configuration", help="with --project: the configuration that was built")
    c.add_argument("--save-listing", help="with --project: also write the raw listing here")
    c.add_argument("--trx", nargs="+", required=True)
    c.add_argument("--stopped-early", action="store_true", help="the run stopped at a red: short = NOT RUN")
    ls = sub.add_parser("list", help="write a project's scrubbed `--list-tests` output to a file")
    ls.add_argument("--project", required=True)
    ls.add_argument("--configuration")
    ls.add_argument("--out", required=True)
    sub.add_parser("audit-callers", help="every `dotnet test` caller scrubs (GateLegDriftTests arm 6)")
    args = ap.parse_args(argv)

    try:
        if args.cmd == "list":
            names, raw = list_population(args.project, args.configuration)
            Path(args.out).write_text(raw, encoding="utf-8")
            print(f"test_population: {sum(names.values()):,} cases listed for {args.project} -> {args.out}")
            return RC_EXACT
        if args.cmd == "audit-callers":
            sites, offenders = audit_callers()
            for s in sites:
                print(f"  site  {s}")
            for o in offenders:
                print(f"  ⛔ {o}")
            if not sites:
                print("=== CALLER AUDIT: ERROR — found no `dotnet test` caller at all; the scan is broken ===")
                return RC_ERROR
            print(f"=== CALLER AUDIT: {'CLEAN' if not offenders else f'{len(offenders)} UNSCRUBBED'} — "
                  f"{len(sites)} site(s) run `dotnet test` ===")
            return RC_EXACT if not offenders else RC_RED
        if args.project:
            listed, raw = list_population(args.project, args.configuration)
            if args.save_listing:
                Path(args.save_listing).write_text(raw, encoding="utf-8")
        else:
            if args.configuration or args.save_listing:
                ap.error("--configuration and --save-listing go with --project")
            listed = read_listings(args.listing)
        result = check(args.label, listed, args.trx, args.stopped_early)
    except PopulationError as e:
        print(f"=== POPULATION {getattr(args, 'label', '')}: ERROR — the check could not run: {e} ===")
        return RC_ERROR
    print("\n".join(result.report()))
    return RC_EXACT if result.exact else RC_RED


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
