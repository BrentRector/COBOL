#!/usr/bin/env python3
"""run.py — the conformance-suite harness: runs the generator's per-edition suites against a compiler through its
adapter, and reports a verdict per test (DESIGN-conformance-suite.md §5).

    python conformance-suite/harness/run.py --compiler wiseowl --set cobol=<path to cobol>
    python conformance-suite/harness/run.py --compiler gnucobol [--editions 85,2014] [--test ID ...] [--jobs N]
        [--hang-guard SECONDS] [--out DIR] [--keep] [--record-baseline]
    python conformance-suite/harness/run.py --self-test

It executes `suites/<edition>/manifest.json`, which `generate.py` writes (run it first); it never reads a test's
header (§10: one reader of the headers). The compiler is described by `adapters/<compiler>.json`, what it claims by
the profile the adapter names, and its recorded verdicts by `baselines/<compiler>.json` (their formats: `config.py`).

EACH TEST runs in its own fresh working directory, holding its source (under a name the harness chooses, within the
adapter's name limit) and its declared files, with stdin closed (end of file) and the hang guard on every process;
the directory is removed afterwards unless `--keep`. Tests run in parallel without sharing a file.

THE VERDICTS (§5):
  PASS            an accepted test compiled, ran, and its standard output (normalized: line ends to LF, trailing
                  spaces of each line and trailing empty lines dropped, exactly CutRunner.Normalize) equals the
                  expected output for the profile's Annex A.1 values; a flagged test's control compiled and, with the
                  warning mechanism invoked (the adapter's `flag` command, ISO 4.2.2), a diagnostic was reported at
                  its marked line.
  FAIL            the output differs; an accepted test did not translate or did not run; a flagged test's control
                  compiled but no diagnostic was reported at the marked line. Whether a flagged program also
                  translates is not judged, and neither is a program's exit status (it is not standard, §3.2).
  NOT APPLICABLE  the profile lacks the edition, a module or element the test requires, or the test uses a word the
                  profile reserves beyond the standard (ISO 4.2.10); or the flagged test's control did not compile,
                  so the compiler lacks what the test needs and a diagnostic would prove nothing.
  NO VERDICT      the compiler crashed (a signal, or an exit code the adapter declares a crash), a process exceeded
                  the hang guard (a generous limit, never a time expectation), or the harness could not run the test.

RESULTS go to `--out` (default `conformance-suite/results/<compiler>/`, git-ignored): `results.json` (every verdict
with its reason; totals per edition and per clause; the CHANGES) and `junit.xml` (FAIL a failure, NO VERDICT an
error, NOT APPLICABLE skipped).

THE CHANGES (§5) compare the run with `baselines/<compiler>.json`: a regression (a recorded PASS that is no longer
one), a new FAIL or NO VERDICT, any other change of verdict, an improvement, and a recorded test that did not run.
The run is RED when a verdict differs from its recorded one (or has none) and is FAIL or NO VERDICT, or was a
recorded PASS; so a second compiler's known failures are a regression net, never a permanently red leg, and a
compiler with no baseline is RED on every FAIL. `--record-baseline` writes the run's verdicts as the baseline (only
for a whole run: never with `--test`).

Exit status: 0 GREEN (or a baseline recorded), 1 RED, 2 a usage or configuration error.
Standard library only (§5): Windows, Linux and z/OS.
"""
from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

HARNESS = Path(__file__).resolve().parent
SUITE = HARNESS.parent
sys.path.insert(0, str(HARNESS))

import config  # noqa: E402  (this directory's: the adapter, profile and baseline formats)
from config import FAIL, NO_VERDICT, NOT_APPLICABLE, PASS, VERDICTS, Step, StepContext  # noqa: E402

VERDICT = "=== CONFORMANCE RUN: "
#: The default hang guard per process, in seconds: generous, a guard against a hang and never a time expectation.
HANG_GUARD = 600
#: The verdicts a test must not newly reach (with no recorded verdict, or a different one).
BAD = (FAIL, NO_VERDICT)
#: Line ends as .NET's String.ReplaceLineEndings reads them: CR LF, CR, LF, NEL, LS, FF and PS
#: (CutRunner.Normalize, tests/Cobol.Net.Tests.Conformance/CompilerUnderTest.cs). The two normalizations are one rule.
_LINE_END = re.compile("\r\n|[\r\n\u0085  \u000c]")


def normalize(text: str) -> str:
    """CutRunner.Normalize: line ends to LF, each line's trailing spaces dropped, trailing LFs dropped."""
    return "\n".join(line.rstrip(" ") for line in _LINE_END.split(text)).rstrip("\n")


# ── one process ─────────────────────────────────────────────────────────────────────────────────────────────────────

def execute(argv: tuple[str, ...], work: Path, environment: dict[str, str], timeout: float) -> Step:
    """Run one process in `work` with stdin closed (end of file) under the hang guard."""
    try:
        done = subprocess.run(list(argv), cwd=work, env=environment, stdin=subprocess.DEVNULL, capture_output=True,
                              timeout=timeout)
    except subprocess.TimeoutExpired as e:
        return Step(None, e.stdout or b"", e.stderr or b"", timed_out=True)
    except OSError as e:
        return Step(None, error=f"{argv[0]}: {e.strerror or e}")
    return Step(done.returncode, done.stdout, done.stderr)


# ── the run ─────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class Result:
    edition: str
    test_id: str
    clause: str
    expect: str
    verdict: str
    reason: str
    seconds: float


@dataclass
class Run:
    """One run's fixed inputs: the suite root, the adapter and profile, the tests per edition, the settings."""

    root: Path
    adapter: config.Adapter
    profile: config.Profile
    variables: dict[str, str]
    #: edition -> {test id -> manifest entry}
    tests: dict[str, dict[str, dict[str, Any]]]
    work: Path
    timeout: float
    keep: bool
    environment: dict[str, str] = field(default_factory=lambda: dict(os.environ))

    # ── the working directory ──

    def stage(self, entry: dict[str, Any], work: Path) -> tuple[str, str]:
        """Put a test's source and declared files in `work`; return the source file's and the program's names."""
        name = entry["id"] if self.adapter.name_limit is None else entry["id"][:self.adapter.name_limit]
        source = name + self.adapter.source_extension
        work.mkdir(parents=True)
        shutil.copyfile(self.root / entry["source"], work / source)
        for declared in entry["files"]:
            target = work / Path(declared).name
            if target.name == source:
                raise HarnessError(f"its declared file {declared} has the name the harness gives its source")
            shutil.copyfile(self.root / declared, target)
        return source, name

    def placeholders(self, edition: str, entry: dict[str, Any], work: Path, source: str,
                     program: str) -> dict[str, str]:
        return {**self.variables, "source": source, "program": program, "work": str(work),
                "std": self.adapter.editions[edition], "source-format": self.adapter.source_formats[entry["source-format"]],
                "python": sys.executable}

    def environment_for(self, entry: dict[str, Any], work: Path, values: dict[str, str]) -> dict[str, str]:
        env = dict(self.environment)
        env.update({fill(k, values): fill(v, values) for k, v in self.adapter.environment.items()})
        for declared in entry["files"]:
            per_file = {"name": Path(declared).stem, "path": str(work / Path(declared).name)}
            env.update({fill(k, per_file): fill(v, per_file) for k, v in self.adapter.assign_environment.items()})
        return env

    def operation(self, op: str, edition: str, entry: dict[str, Any], work: Path, source: str,
                  program: str) -> Step:
        values = self.placeholders(edition, entry, work, source, program)
        template = self.adapter.commands[op]
        argv = tuple(fill(a, values) for a in template) if template is not None else None
        ctx = StepContext(op, argv, work, self.environment_for(entry, work, values), self.timeout, values)
        hook = getattr(self.adapter.hook, op, None) if self.adapter.hook is not None else None
        if callable(hook):
            step = hook(ctx)
            if not isinstance(step, Step):
                raise HarnessError(f"the adapter's hook {op}() returned {type(step).__name__}, not a Step")
            return step
        assert argv is not None  # config.load_adapter refuses an operation with neither a template nor a hook
        return execute(argv, work, ctx.environment, self.timeout)

    def crashed(self, step: Step) -> bool:
        return step.returncode is not None and (step.returncode < 0 or step.returncode in self.adapter.crash_exit_codes)

    def unjudgeable(self, step: Step, what: str) -> str | None:
        """Why a compile cannot be judged (NO VERDICT), or None: a hang, a crash, a compiler that did not start."""
        if step.timed_out:
            return f"{what} exceeded the hang guard ({self.timeout:g} s)"
        if step.error is not None:
            return f"{what} did not start: {step.error}"
        if self.crashed(step):
            return f"{what} crashed (exit {step.returncode})"
        return None

    def diagnostic_lines(self, step: Step, source: str) -> list[int]:
        """The lines of `source` at which the compiler reported a diagnostic, as the adapter reads them."""
        lines: set[int] = set()
        pattern = self.adapter.diagnostic
        for line in step.text().splitlines():
            m = pattern.search(line)
            if m is None:
                continue
            if "file" in pattern.groupindex and m.group("file") is not None:
                if Path(m.group("file").strip()).name.casefold() != source.casefold():
                    continue
            lines.add(int(m.group("line")))
        return sorted(lines)

    # ── one test ──

    def not_applicable(self, edition: str, entry: dict[str, Any]) -> str | None:
        if edition not in self.profile.editions:
            return f"the {self.profile.compiler} profile does not support {edition}"
        missing = [r for r in entry["requires"] if not self.profile.claims(r)]
        if missing:
            return f"it requires {', '.join(missing)}, which the profile does not claim"
        reserved = sorted(set(entry.get("words", ())) & self.profile.extra_reserved_words.get(edition, frozenset()))
        if reserved:
            return f"it uses {', '.join(reserved)}, which the compiler reserves beyond the standard (ISO 4.2.10)"
        return None

    def run_test(self, edition: str, entry: dict[str, Any]) -> Result:
        start = time.monotonic()
        verdict, reason = NOT_APPLICABLE, self.not_applicable(edition, entry)
        if reason is None:
            work = self.work / edition / entry["id"]
            try:
                if entry["expect"] == "accept":
                    verdict, reason = self.accept(edition, entry, work)
                else:
                    verdict, reason = self.flagged(edition, entry, work)
            except HarnessError as e:
                verdict, reason = NO_VERDICT, f"the harness could not run it: {e}"
            except OSError as e:
                verdict, reason = NO_VERDICT, f"the harness could not run it: {e}"
            finally:
                if not self.keep:
                    shutil.rmtree(work, ignore_errors=True)
        return Result(edition, entry["id"], entry.get("clause", ""), entry["expect"], verdict, reason or "",
                      round(time.monotonic() - start, 3))

    def accept(self, edition: str, entry: dict[str, Any], work: Path) -> tuple[str, str]:
        source, program = self.stage(entry, work)
        step = self.operation("compile", edition, entry, work, source, program)
        why = self.unjudgeable(step, "the compiler")
        if why:
            return NO_VERDICT, why
        if step.returncode != 0:
            return FAIL, f"it did not translate (exit {step.returncode}): {first_lines(step.text())}"
        ran = self.operation("run", edition, entry, work, source, program)
        if ran.timed_out:
            return NO_VERDICT, f"the program exceeded the hang guard ({self.timeout:g} s)"
        if ran.error is not None:
            return FAIL, f"it did not run: {ran.error}"
        key = ".".join(f"{item}={self.profile.implementor_defined[item]}" for item in sorted(entry["implementor-defined"]))
        expected_path = entry["expected-output"].get(key)
        if expected_path is None:
            raise HarnessError(f"the manifest has no expected output for {key or 'no A.1 value'}")
        expected = normalize((self.root / expected_path).read_text(encoding="utf-8"))
        actual = normalize(ran.stdout.decode("utf-8", errors="replace"))
        if actual == expected:
            return PASS, ""
        return FAIL, difference(expected, actual, expected_path)

    def flagged(self, edition: str, entry: dict[str, Any], work: Path) -> tuple[str, str]:
        control = self.tests[edition].get(entry["control"])
        if control is None:
            raise HarnessError(f"its control {entry['control']} is not a test of the {edition} suite")
        source, program = self.stage(entry, work)
        control_work = work / "control"
        c_source, c_program = self.stage(control, control_work)
        compiled = self.operation("compile", edition, control, control_work, c_source, c_program)
        why = self.unjudgeable(compiled, f"the compiler, on its control {control['id']},")
        if why:
            return NO_VERDICT, why
        if compiled.returncode != 0:
            return NOT_APPLICABLE, (f"its control {control['id']} did not compile (exit {compiled.returncode}), so "
                                    f"the compiler lacks what the test needs (§3.2)")
        step = self.operation("flag", edition, entry, work, source, program)
        why = self.unjudgeable(step, "the compiler")
        if why:
            return NO_VERDICT, why
        lines = self.diagnostic_lines(step, source)
        if entry["line"] in lines:
            return PASS, ""
        where = f"diagnostics at line(s) {', '.join(map(str, lines))}" if lines else "no diagnostic reported"
        return FAIL, f"no diagnostic at the marked line {entry['line']} ({where})"


class HarnessError(Exception):
    """The harness could not run a test (a manifest or file problem): the test's verdict is NO VERDICT."""


def fill(template: str, values: dict[str, str]) -> str:
    return config.PLACEHOLDER.sub(lambda m: values[m.group(1)], template)


def first_lines(text: str, n: int = 3) -> str:
    lines = [line.strip() for line in text.splitlines() if line.strip()]
    return " | ".join(lines[:n])[:400] or "no output"


def difference(expected: str, actual: str, expected_path: str) -> str:
    want, got = expected.split("\n"), actual.split("\n")
    for i in range(max(len(want), len(got))):
        w = want[i] if i < len(want) else "<end of output>"
        g = got[i] if i < len(got) else "<end of output>"
        if w != g:
            return f"the output differs from {expected_path} at line {i + 1}: expected {w[:160]!r}, got {g[:160]!r}"
    return f"the output differs from {expected_path}"


# ── results ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def totals(results: list[Result]) -> tuple[dict[str, dict[str, int]], dict[str, dict[str, dict[str, int]]]]:
    by_edition: dict[str, dict[str, int]] = {}
    by_clause: dict[str, dict[str, dict[str, int]]] = {}
    for r in results:
        by_edition.setdefault(r.edition, dict.fromkeys(VERDICTS, 0))[r.verdict] += 1
        by_clause.setdefault(r.edition, {}).setdefault(r.clause, dict.fromkeys(VERDICTS, 0))[r.verdict] += 1
    return by_edition, by_clause


def changes(results: list[Result], baseline: dict[str, dict[str, str]] | None, whole: bool) -> dict[str, list]:
    """The CHANGES against the recorded verdicts, and which of them make the run RED."""
    out: dict[str, list] = {"regression": [], "new": [], "changed": [], "improvement": [], "not-run": [], "red": []}
    recorded = baseline or {}
    seen: set[tuple[str, str]] = set()
    for r in results:
        seen.add((r.edition, r.test_id))
        before = recorded.get(r.edition, {}).get(r.test_id)
        if before == r.verdict:
            continue
        row = {"edition": r.edition, "id": r.test_id, "was": before, "now": r.verdict}
        if before is None:
            if baseline is None and r.verdict not in BAD:
                continue  # with no baseline recorded, only a verdict that makes the run RED is a change worth naming
            kind = "new"
        elif before == PASS:
            kind = "regression"
        elif r.verdict == PASS:
            kind = "improvement"
        else:
            kind = "changed"
        out[kind].append(row)
        if r.verdict in BAD or before == PASS:
            out["red"].append(row)
    if whole:
        ran = {r.edition for r in results}
        for edition, tests in sorted(recorded.items()):
            if edition in ran:
                out["not-run"] += [{"edition": edition, "id": t, "was": v, "now": None}
                                   for t, v in sorted(tests.items()) if (edition, t) not in seen]
    return out


def junit(compiler: str, results: list[Result]) -> ET.ElementTree:
    suites = ET.Element("testsuites", name=f"conformance-suite {compiler}")
    by_edition: dict[str, list[Result]] = {}
    for r in results:
        by_edition.setdefault(r.edition, []).append(r)
    grand = dict.fromkeys(("tests", "failures", "errors", "skipped"), 0)
    for edition, rs in by_edition.items():
        counts = {"tests": len(rs), "failures": sum(r.verdict == FAIL for r in rs),
                  "errors": sum(r.verdict == NO_VERDICT for r in rs),
                  "skipped": sum(r.verdict == NOT_APPLICABLE for r in rs)}
        suite = ET.SubElement(suites, "testsuite", name=f"COBOL {edition}", **{k: str(v) for k, v in counts.items()},
                              time=f"{sum(r.seconds for r in rs):.3f}")
        for r in rs:
            case = ET.SubElement(suite, "testcase", classname=r.clause or edition, name=r.test_id,
                                 time=f"{r.seconds:.3f}")
            if r.verdict == FAIL:
                ET.SubElement(case, "failure", message=r.reason, type=FAIL)
            elif r.verdict == NO_VERDICT:
                ET.SubElement(case, "error", message=r.reason, type=NO_VERDICT)
            elif r.verdict == NOT_APPLICABLE:
                ET.SubElement(case, "skipped", message=f"{NOT_APPLICABLE}: {r.reason}")
        for k in grand:
            grand[k] += counts[k]
    for k, v in grand.items():
        suites.set(k, str(v))
    return ET.ElementTree(suites)


def write_results(out: Path, run: Run, results: list[Result], found: dict[str, list]) -> None:
    by_edition, by_clause = totals(results)
    out.mkdir(parents=True, exist_ok=True)
    document = {
        "compiler": run.adapter.compiler, "profile": run.profile.compiler, "harness": "conformance-suite/harness/run.py",
        "editions": list(by_edition), "totals": by_edition, "clauses": by_clause,
        "changes": {k: v for k, v in found.items() if k != "red"}, "red": found["red"],
        "tests": [{"edition": r.edition, "id": r.test_id, "clause": r.clause, "expect": r.expect,
                   "verdict": r.verdict, "reason": r.reason, "seconds": r.seconds} for r in results],
    }
    (out / "results.json").write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
    tree = junit(run.adapter.compiler, results)
    ET.indent(tree)
    tree.write(out / "junit.xml", encoding="utf-8", xml_declaration=True)


def print_summary(run: Run, results: list[Result], found: dict[str, list], baseline_path: Path) -> None:
    by_edition, by_clause = totals(results)
    print(f"=== CONFORMANCE RESULTS: {run.adapter.compiler} (DESIGN-conformance-suite.md §5) ===")
    print("  " + "  ".join(f"{h:>14}" for h in ("edition", *VERDICTS)))
    for edition, counts in by_edition.items():
        print("  " + "  ".join(f"{v:>14}" for v in (edition, *(str(counts[k]) for k in VERDICTS))))
    for r in results:
        if r.verdict != PASS:
            print(f"  {r.verdict:<14} {r.edition:>4} {r.test_id}: {r.reason}")
    shown = [(k, row) for k in ("regression", "new", "changed", "improvement", "not-run") for row in found[k]]
    print(f"  CHANGES against {baseline_path.relative_to(run.root).as_posix() if baseline_path.is_relative_to(run.root) else baseline_path}"
          f"{'' if baseline_path.is_file() else ' (no baseline recorded)'}: {len(shown) or 'none'}")
    for kind, row in shown:
        print(f"    {kind:<11} {row['edition']:>4} {row['id']}: {row['was'] or '-'} -> {row['now'] or 'not run'}")


# ── the command line ────────────────────────────────────────────────────────────────────────────────────────────────

def load_tests(suites: Path, editions: list[str] | None, only: set[str] | None) -> dict[str, dict[str, dict[str, Any]]]:
    found = sorted((p.parent.name for p in suites.glob("*/manifest.json") if p.parent.name.isdigit()), key=int)
    if not found:
        raise UsageError(f"no suite manifests under {suites}: run conformance-suite/harness/generate.py first")
    chosen = editions or found
    missing = [e for e in chosen if e not in found]
    if missing:
        raise UsageError(f"no suite manifest for {', '.join(missing)} under {suites} (it has {', '.join(found)})")
    tests: dict[str, dict[str, dict[str, Any]]] = {}
    for e in chosen:
        try:
            manifest = json.loads((suites / e / "manifest.json").read_text(encoding="utf-8"))
            tests[e] = {t["id"]: t for t in manifest["tests"]}
        except (OSError, ValueError, KeyError, TypeError) as err:
            raise UsageError(f"{suites / e / 'manifest.json'} is not a generator manifest ({err!r}): run "
                             f"conformance-suite/harness/generate.py again") from err
    if not any(tests.values()):
        # A run over nothing is never GREEN: the regression net would pass a suite that lost every test.
        raise UsageError(f"the chosen suites ({', '.join(chosen)}) hold no test")
    if only is not None:
        unknown = sorted(only - {t for ts in tests.values() for t in ts})
        if unknown:
            raise UsageError(f"--test names {', '.join(unknown)}, which no chosen suite holds")
    return tests


class UsageError(Exception):
    pass


def main(argv: list[str], root: Path = SUITE) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--compiler", help="the adapter: adapters/<compiler>.json")
    ap.add_argument("--set", action="append", default=[], metavar="NAME=VALUE",
                    help="override one of the adapter's variables (repeatable)")
    ap.add_argument("--editions", help="comma-separated editions to run (default: every suite the generator wrote)")
    ap.add_argument("--test", action="append", metavar="ID", help="run only this test (repeatable)")
    ap.add_argument("--suites", type=Path, help="the generator's output (default: conformance-suite/suites)")
    ap.add_argument("--out", type=Path, help="where results go (default: conformance-suite/results/<compiler>)")
    ap.add_argument("--jobs", type=int, default=max(1, min(8, (os.cpu_count() or 2) // 2)))
    ap.add_argument("--hang-guard", type=float, default=HANG_GUARD, metavar="SECONDS",
                    help=f"the limit on any one process (default {HANG_GUARD} s; a guard, never an expectation)")
    ap.add_argument("--keep", action="store_true", help="keep each test's working directory")
    ap.add_argument("--record-baseline", action="store_true", help="write this run's verdicts as the baseline")
    ap.add_argument("--self-test", action="store_true", help="drive a stub compiler through every verdict")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    try:
        return _run(args, root)
    except UsageError as e:
        print(f"{VERDICT}ERROR — {e} ===")
        return 2


def _run(args: argparse.Namespace, root: Path) -> int:
    if not args.compiler:
        raise UsageError("--compiler names the adapter (adapters/<compiler>.json)")
    adapter, problems = config.load_adapter(root / "adapters" / f"{args.compiler}.json")
    if adapter is None:
        raise UsageError("; ".join(problems))
    profile, problems = config.load_profile(root / "profiles" / f"{adapter.profile}.json")
    if profile is None:
        raise UsageError("; ".join(problems))
    unmapped = [e for e in profile.editions if e not in adapter.editions]
    if unmapped:
        raise UsageError(f"the profile supports {', '.join(unmapped)}, which the adapter's editions do not map")
    variables = dict(adapter.variables)
    for item in args.set:
        name, eq, value = item.partition("=")
        if not eq or name not in adapter.variables:
            raise UsageError(f"--set {item}: the adapter's variables are {', '.join(adapter.variables) or 'none'}")
        variables[name] = value
    if args.record_baseline and args.test:
        raise UsageError("--record-baseline records a whole run; it cannot be combined with --test")
    only = set(args.test) if args.test else None
    editions = [e.strip() for e in args.editions.split(",")] if args.editions else None
    tests = load_tests(args.suites or root / "suites", editions, only)
    needed = {i for ts in tests.values() for t in ts.values() for i in t.get("implementor-defined", ())}
    unstated = sorted(needed - set(profile.implementor_defined))
    if unstated:
        raise UsageError(f"the profile states no value of Annex A.1 item(s) {', '.join(map(str, unstated))}, "
                         f"which tests of these suites depend on")
    baseline_path = root / "baselines" / f"{adapter.compiler}.json"
    baseline, problems = config.load_baseline(baseline_path)
    if problems:
        raise UsageError("; ".join(problems))
    with tempfile.TemporaryDirectory(prefix="conformance-") as scratch:
        run = Run(root=root, adapter=adapter, profile=profile, variables=variables, tests=tests, work=Path(scratch),
                  timeout=args.hang_guard, keep=args.keep)
        if args.keep:
            run.work = Path(tempfile.mkdtemp(prefix="conformance-kept-"))
        jobs = [(e, t) for e in tests for i, t in sorted(tests[e].items()) if only is None or i in only]
        with ThreadPoolExecutor(max_workers=max(1, args.jobs)) as pool:
            results = list(pool.map(lambda job: run.run_test(*job), jobs))
        found = changes(results, baseline, whole=only is None)
        out = args.out or root / "results" / adapter.compiler
        write_results(out, run, results, found)
        print_summary(run, results, found, baseline_path)
        if args.keep:
            print(f"  working directories kept under {run.work}")
    by = {v: sum(r.verdict == v for r in results) for v in VERDICTS}
    counts = " · ".join(f"{v} {by[v]}" for v in VERDICTS)
    where = f"{len(results)} test(s) at {len(tests)} edition(s): {counts}; results in {out}"
    if args.record_baseline:
        config.write_baseline(baseline_path, adapter.compiler,
                              {e: {r.test_id: r.verdict for r in results if r.edition == e} for e in tests})
        print(f"{VERDICT}BASELINE RECORDED — {adapter.compiler}: {where}; written to {baseline_path} ===")
        return 0
    red = found["red"]
    if red:
        print(f"{VERDICT}RED — {adapter.compiler}: {where}; {len(red)} verdict(s) worse than "
              f"{'the baseline' if baseline is not None else 'PASS, and no baseline is recorded'} ===")
        return 1
    print(f"{VERDICT}GREEN — {adapter.compiler}: {where}; no verdict worse than "
          f"{'the baseline' if baseline is not None else 'PASS'} ===")
    return 0


# ── the self-test ───────────────────────────────────────────────────────────────────────────────────────────────────

#: A stub compiler, a Python script: `stubcc.py compile|flag <source> <program>` and `stubcc.py run <program>`. It
#: reads `STUB <directive>` lines of its source, so each planted test states the behaviour it drives.
_STUB = '''
import os, sys, time
from pathlib import Path
op = sys.argv[1]
if op == "run":
    program = Path(sys.argv[2] + ".prog")
    if not program.is_file():
        sys.exit(3)
    lines = program.read_text(encoding="utf-8").splitlines()
    out = []
    for line in lines:
        word, _, rest = line.partition(" ")
        if word == "out":
            out.append(rest)
        elif word == "out-crlf":
            sys.stdout.buffer.write((rest + "   " + chr(13) + chr(10)).encode("utf-8"))
        elif word == "stdin":
            out.append("STDIN=" + repr(sys.stdin.read()))
        elif word == "run-hang":
            time.sleep(60)
        elif word == "leave":
            Path(rest).write_text("x")
        elif word == "absent":
            out.append("ABSENT" if not Path(rest).exists() else "PRESENT")
        elif word == "exit":
            sys.stdout.write("".join(o + chr(10) for o in out))
            sys.exit(int(rest))
    sys.stdout.write("".join(o + chr(10) for o in out))
    sys.exit(0)
source, program = sys.argv[2], sys.argv[3]
directives = [l.split("STUB ", 1)[1].strip() for l in Path(source).read_text(encoding="utf-8").splitlines() if "STUB " in l]
limit = os.environ.get("STUB_NAME_LIMIT")
if limit and len(Path(source).stem) > int(limit):
    print(source + ":1: error: file name too long", file=sys.stderr)
    sys.exit(1)
for d in directives:
    word, _, rest = d.partition(" ")
    if word == "diag":
        line, _, file = rest.partition(" ")
        print((file or source) + ":" + line + ": warning: flagged")
    elif word == "crash" and op in ("compile", "flag"):
        sys.exit(77)
    elif word == "hang" and op == "compile":
        time.sleep(60)
    elif word == "refuse" and op == "compile":
        print(source + ":2: error: refused", file=sys.stderr)
        sys.exit(1)
    elif word == "need" and not Path(rest).is_file():
        print("missing declared file " + rest, file=sys.stderr)
        sys.exit(1)
    elif word == "env" and not os.environ.get(rest.split("=")[0], "").endswith(rest.split("=")[1]):
        print("no assign mapping " + rest, file=sys.stderr)
        sys.exit(1)
if op == "compile" and "no-program" not in directives:
    Path(program + ".prog").write_text("".join(d.partition(" ")[2] + chr(10) for d in directives if d.startswith("run ")),
                                       encoding="utf-8")
sys.exit(0)
'''

#: A hook that performs `run` itself (an adapter's Python hook, §5): it prints what the stub's program would, upper-cased.
_HOOK = '''
import subprocess
import config
def run(ctx):
    done = subprocess.run([ctx.placeholders["python"], ctx.placeholders["stub"], "run", ctx.placeholders["program"]],
                          cwd=ctx.work, env=ctx.environment, stdin=subprocess.DEVNULL, capture_output=True,
                          timeout=ctx.timeout)
    return config.Step(done.returncode, done.stdout.upper(), done.stderr)
'''


def _stub_adapter(name: str, stub: Path, **extra: Any) -> dict[str, Any]:
    adapter = {
        "compiler": name, "profile": "stub", "editions": {"85": "85", "2002": "2002", "2023": "2023"},
        "source-formats": {"fixed": "fixed", "free": "free"}, "variables": {"stub": str(stub)},
        "compile": ["{python}", "{stub}", "compile", "{source}", "{program}"],
        "run": ["{python}", "{stub}", "run", "{program}"],
        "flag": ["{python}", "{stub}", "flag", "{source}", "{program}"],
        "diagnostic": r"^(?P<file>[^:\s]+):(?P<line>[0-9]+): (?:error|warning)",
        "crash-exit-codes": [77], "assign": {"environment": {"DD_{name}": "{path}"}},
    }
    adapter.update(extra)
    return adapter


def _entry(test_id: str, clause: str, expect: str = "accept", files: tuple[str, ...] = (), requires: tuple[str, ...] = (),
           words: tuple[str, ...] = (), outputs: dict[str, str] | None = None, items: tuple[int, ...] = (),
           line: int = 0, control: str = "") -> dict[str, Any]:
    entry: dict[str, Any] = {"id": test_id, "clause": clause, "source": f"cases/{clause}/{test_id}.cob",
                             "source-format": "fixed", "rules": ["GR-1.1.1-1"],
                             "files": [f"cases/{clause}/{f}" for f in files], "requires": list(requires),
                             "words": list(words)}
    if expect == "accept":
        entry.update({"expect": "accept", "implementor-defined": list(items),
                      "expected-output": outputs if outputs is not None else {"": f"cases/{clause}/{test_id}.out"}})
    else:
        entry.update({"expect": "flag", "flag": "syntax", "line": line, "control": control})
    return entry


def self_test() -> int:
    rc = 0

    def check(name: str, ok: bool, got: object = None) -> None:
        nonlocal rc
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + name + ("" if ok else f" — got {got!r}"))
        rc |= 0 if ok else 1

    print("=== run.py --self-test ===")
    check("arm: normalize is CutRunner.Normalize (CR LF, CR, NEL, LS, FF and PS end a line; trailing spaces and "
          "trailing line ends go; inner spaces stay)",
          normalize("a  \r\nb\rc\u0085d e\u000cf g \n\n") == "a\nb\nc\nd\ne\nf\ng"
          and normalize(" x ] \n") == " x ]", normalize("a  \r\nb\rc\u0085d e\u000cf g \n\n"))
    c = "1.1.1-Rule"
    stub_tests: dict[str, tuple[list[str], str | None, dict[str, Any]]] = {
        # id: (STUB directives, expected .out text or None, manifest entry)
        "passes": (["run out-crlf HELLO", "run out WORLD  "], "HELLO\nWORLD\n", _entry("passes", c)),
        "differs": (["run out HELLO"], "GOODBYE\n", _entry("differs", c)),
        "lower": (["run out hello"], "HELLO\n", _entry("lower", c)),
        "refused": (["refuse"], "x\n", _entry("refused", c)),
        "no-program": (["no-program"], "x\n", _entry("no-program", c)),
        "crashes": (["crash"], "x\n", _entry("crashes", c)),
        "hangs": (["hang"], "x\n", _entry("hangs", c)),
        "run-hangs": (["run run-hang"], "x\n", _entry("run-hangs", c)),
        "stdin": (["run stdin"], "STDIN=''\n", _entry("stdin", c)),
        "staged": (["need DATA1.dat", "env DD_DATA1=DATA1.dat", "run out OK"], "OK\n",
                   _entry("staged", c, files=("DATA1.dat",))),
        "leaves": (["run leave LEFT.txt", "run absent LEFT.txt"], "PRESENT\n", _entry("leaves", c)),
        "fresh": (["run absent LEFT.txt"], "ABSENT\n", _entry("fresh", c)),
        "exit-status": (["run out DONE", "run exit 9"], "DONE\n", _entry("exit-status", c)),
        "valued": (["run out TWO"], None, _entry("valued", c, items=(9,), outputs={
            "9=one": f"cases/{c}/valued.9=one.out", "9=two": f"cases/{c}/valued.9=two.out"})),
        "needs-module": (["run out x"], "x\n", _entry("needs-module", c, requires=("A.4.11",))),
        "needs-claimed": (["run out x"], "x\n", _entry("needs-claimed", c, requires=("A.4.4", "A.3-15"))),
        "reserved-word": (["run out x"], "x\n", _entry("reserved-word", c, words=("W-OK", "VENDORWORD"))),
        "a-very-long-test-identifier": (["run out LONG"], "LONG\n", _entry("a-very-long-test-identifier", c)),
        "control": (["run out ok"], "ok\n", _entry("control", c)),
        "flag-pass": (["diag 4"], None, _entry("flag-pass", c, "flag", line=4, control="control")),
        "flag-elsewhere": (["diag 9"], None, _entry("flag-elsewhere", c, "flag", line=4, control="control")),
        "flag-copybook": (["diag 4 CPY.cpy"], None, _entry("flag-copybook", c, "flag", line=4, control="control")),
        "flag-silent": ([], None, _entry("flag-silent", c, "flag", line=4, control="control")),
        "flag-crash": (["crash"], None, _entry("flag-crash", c, "flag", line=4, control="control")),
        "weak-control": (["refuse"], "x\n", _entry("weak-control", c)),
        "flag-na": (["diag 4"], None, _entry("flag-na", c, "flag", line=4, control="weak-control")),
    }
    expected = {
        "passes": PASS, "differs": FAIL, "lower": FAIL, "refused": FAIL, "no-program": FAIL, "crashes": NO_VERDICT,
        "hangs": NO_VERDICT, "run-hangs": NO_VERDICT, "stdin": PASS, "staged": PASS, "leaves": PASS, "fresh": PASS,
        "exit-status": PASS, "valued": PASS, "needs-module": NOT_APPLICABLE, "needs-claimed": PASS,
        "reserved-word": NOT_APPLICABLE, "a-very-long-test-identifier": PASS, "control": PASS, "flag-pass": PASS,
        "flag-elsewhere": FAIL, "flag-copybook": FAIL, "flag-silent": FAIL, "flag-crash": NO_VERDICT,
        "weak-control": FAIL, "flag-na": NOT_APPLICABLE,
    }
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        stub = root / "stubcc.py"
        stub.write_text(_STUB, encoding="utf-8", newline="\n")
        for test_id, (directives, out, _) in stub_tests.items():
            folder = root / "cases" / c
            folder.mkdir(parents=True, exist_ok=True)
            (folder / f"{test_id}.cob").write_text("".join(f"      * STUB {x}\n" for x in directives), encoding="utf-8",
                                                                newline="\n")
            if out is not None:
                (folder / f"{test_id}.out").write_text(out, encoding="utf-8", newline="\n")
        (root / "cases" / c / "DATA1.dat").write_text("data\n", encoding="utf-8", newline="\n")
        (root / "cases" / c / "valued.9=one.out").write_text("ONE\n", encoding="utf-8", newline="\n")
        (root / "cases" / c / "valued.9=two.out").write_text("TWO\n", encoding="utf-8", newline="\n")
        manifest = [entry for _, _, entry in stub_tests.values()]
        for e in ("85", "2023"):
            (root / "suites" / e).mkdir(parents=True)
            (root / "suites" / e / "manifest.json").write_text(json.dumps({"edition": e, "tests": manifest}),
                                                               encoding="utf-8", newline="\n")
        (root / "adapters").mkdir()
        (root / "profiles").mkdir()
        (root / "adapters" / "stub.json").write_text(json.dumps(_stub_adapter(
            "stub", stub, **{"name-limit": 12, "environment": {"STUB_NAME_LIMIT": "12"}})), encoding="utf-8", newline="\n")
        profile = {"compiler": "stub", "editions": ["85", "2002"], "modules": {"A.4.4": "planted"},
                   "elements": {"A.3-15": "planted"}, "implementor-defined": {"9": "two"},
                   "extra-reserved-words": {"85": ["VENDORWORD"]}}
        (root / "profiles" / "stub.json").write_text(json.dumps(profile), encoding="utf-8", newline="\n")
        out = root / "results"

        def quiet(args: list[str]) -> int:
            """One run of the harness on the planted tree, its printed report kept out of the self-test's output."""
            with contextlib.redirect_stdout(io.StringIO()):
                return main(args, root)

        def results_of(code: int) -> tuple[int, dict[str, Any]]:
            doc = json.loads((out / "results.json").read_text(encoding="utf-8")) if (out / "results.json").is_file() else {}
            return code, doc

        code, doc = results_of(quiet(["--compiler", "stub", "--out", str(out), "--hang-guard", "3", "--jobs", "4"]))
        got = {(t["edition"], t["id"]): t["verdict"] for t in doc.get("tests", [])}
        verdicts85 = {i: v for (e, i), v in got.items() if e == "85"}
        for test_id, want in expected.items():
            check(f"arm: {test_id} is {want}", verdicts85.get(test_id) == want,
                  (verdicts85.get(test_id), next((t["reason"] for t in doc.get("tests", [])
                                                  if t["id"] == test_id and t["edition"] == "85"), None)))
        reasons = {t["id"]: t["reason"] for t in doc.get("tests", []) if t["edition"] == "85"}
        check("arm: a FAIL names the line that differs", "at line 1: expected 'GOODBYE', got 'HELLO'"
              in reasons.get("differs", ""), reasons.get("differs"))
        check("arm: a flagged FAIL names where the diagnostics were", "diagnostics at line(s) 9"
              in reasons.get("flag-elsewhere", ""), reasons.get("flag-elsewhere"))
        check("arm: the edition the profile lacks is NOT APPLICABLE for every test",
              {v for (e, _), v in got.items() if e == "2023"} == {NOT_APPLICABLE}, {v for (e, _), v in got.items()})
        check("arm: totals per edition", doc.get("totals", {}).get("85") == {
            PASS: sum(v == PASS for v in expected.values()), FAIL: sum(v == FAIL for v in expected.values()),
            NOT_APPLICABLE: sum(v == NOT_APPLICABLE for v in expected.values()),
            NO_VERDICT: sum(v == NO_VERDICT for v in expected.values())}, doc.get("totals"))
        check("arm: totals per clause", doc.get("clauses", {}).get("85", {}).get(c) == doc.get("totals", {}).get("85"),
              doc.get("clauses"))
        check("arm: no baseline and FAILs: RED, exit 1", code == 1, code)
        try:
            tree = ET.parse(out / "junit.xml").getroot()
            suite85 = next(s for s in tree if s.get("name") == "COBOL 85")
            check("arm: JUnit XML counts FAIL as failures, NO VERDICT as errors, NOT APPLICABLE as skipped",
                  (suite85.get("failures"), suite85.get("errors"), suite85.get("skipped"), suite85.get("tests"))
                  == (str(sum(v == FAIL for v in expected.values())), str(sum(v == NO_VERDICT for v in expected.values())),
                      str(sum(v == NOT_APPLICABLE for v in expected.values())), str(len(expected))),
                  dict(suite85.attrib))
        except (ET.ParseError, StopIteration, OSError) as e:
            check("arm: JUnit XML is well-formed", False, e)
        # --keep: the working directory holds the staged source under the adapter's name limit (12 here).
        before = set(Path(tempfile.gettempdir()).glob("conformance-kept-*"))
        quiet(["--compiler", "stub", "--out", str(out), "--editions", "85", "--test", "a-very-long-test-identifier",
              "--test", "staged", "--keep"])
        kept = sorted(set(Path(tempfile.gettempdir()).glob("conformance-kept-*")) - before)
        staged = kept[0] / "85" / "staged" if kept else Path("missing")
        check("arm: a kept working directory holds the source under the name limit and the declared files",
              bool(kept) and (kept[0] / "85" / "a-very-long-test-identifier" / "a-very-long-.cob").is_file()
              and (staged / "DATA1.dat").is_file() and (staged / "staged.cob").is_file(),
              [str(p.relative_to(kept[0])) for p in kept[0].rglob("*")] if kept else None)
        for k in kept:
            shutil.rmtree(k, ignore_errors=True)

        # A hook performs an operation (§5: a compiler that needs more supplies a Python hook).
        (root / "adapters" / "hooked.py").write_text(_HOOK, encoding="utf-8", newline="\n")
        hooked = _stub_adapter("hooked", stub, hook="hooked.py")
        del hooked["run"]
        (root / "adapters" / "hooked.json").write_text(json.dumps(hooked), encoding="utf-8", newline="\n")
        code, doc = results_of(quiet(["--compiler", "hooked", "--out", str(out), "--editions", "85", "--test", "passes",
                                     "--test", "lower"]))
        hv = {t["id"]: t["verdict"] for t in doc.get("tests", [])}
        check("arm: the hook's run() replaces the template (it upper-cases the output, so `lower` now PASSes)",
              hv == {"passes": PASS, "lower": PASS}, (hv, doc.get("tests")))

        # The baseline and the CHANGES (§5).
        small = ["--compiler", "stub", "--out", str(out), "--editions", "85", "--hang-guard", "3"]
        code = quiet(small + ["--record-baseline"])
        recorded, problems = config.load_baseline(root / "baselines" / "stub.json")
        check("arm: --record-baseline writes every verdict of the run", code == 0 and not problems
              and recorded == {"85": verdicts85}, (code, problems))
        code, doc = results_of(quiet(small))
        check("arm: a run equal to its baseline is GREEN with no CHANGES despite its FAILs",
              code == 0 and not any(doc["changes"][k] for k in doc["changes"]), (code, doc.get("changes")))
        (root / "cases" / c / "passes.out").write_text("CHANGED\n", encoding="utf-8", newline="\n")
        (root / "cases" / c / "differs.out").write_text("HELLO\n", encoding="utf-8", newline="\n")
        code, doc = results_of(quiet(small))
        ch = doc.get("changes", {})
        check("arm: a lost PASS is a regression and RED", code == 1 and [r["id"] for r in ch.get("regression", [])]
              == ["passes"], (code, ch))
        check("arm: a gained PASS is an improvement", [r["id"] for r in ch.get("improvement", [])] == ["differs"], ch)
        recorded["85"]["gone"] = PASS
        config.write_baseline(root / "baselines" / "stub.json", "stub", recorded)
        code, doc = results_of(quiet(small))
        check("arm: a recorded test that no longer runs is reported not-run",
              [r["id"] for r in doc.get("changes", {}).get("not-run", [])] == ["gone"], doc.get("changes"))
        check("arm: --record-baseline with --test is a usage error (2)",
              quiet(small + ["--record-baseline", "--test", "passes"]) == 2)
        check("arm: an unknown --set is a usage error (2)", quiet(small + ["--set", "nope=1"]) == 2)

        # A compiler that does not start is NO VERDICT, never FAIL.
        broken = _stub_adapter("broken", stub)
        broken["compile"] = [str(root / "no-such-compiler"), "{source}"]
        (root / "adapters" / "broken.json").write_text(json.dumps(broken), encoding="utf-8", newline="\n")
        code, doc = results_of(quiet(["--compiler", "broken", "--out", str(out), "--editions", "85", "--test", "passes"]))
        check("arm: a compiler that does not start is NO VERDICT",
              [t["verdict"] for t in doc.get("tests", [])] == [NO_VERDICT] and "did not start"
              in doc["tests"][0]["reason"], doc.get("tests"))

        # The profile must state the value of every A.1 item a test depends on.
        (root / "profiles" / "stub.json").write_text(json.dumps({**profile, "implementor-defined": {}}),
                                                     encoding="utf-8", newline="\n")
        check("arm: a profile with no value of an A.1 item a test needs is a configuration error (2)",
              quiet(small) == 2)
        (root / "profiles" / "stub.json").write_text(json.dumps(profile), encoding="utf-8", newline="\n")
        check("arm: a missing suite manifest is a usage error (2)", quiet(small[:-4] + ["--editions", "2014"]) == 2)
        (root / "suites" / "2014").mkdir()
        (root / "suites" / "2014" / "manifest.json").write_text(json.dumps({"edition": "2014", "tests": []}),
                                                                 encoding="utf-8", newline="\n")
        check("arm: a run over suites holding no test is a usage error (2), never GREEN",
              quiet(small[:-4] + ["--editions", "2014"]) == 2)
        (root / "suites" / "2014" / "manifest.json").write_text("{not json", encoding="utf-8", newline="\n")
        check("arm: an unreadable manifest is a usage error (2)", quiet(small[:-4] + ["--editions", "2014"]) == 2)
        shutil.rmtree(root / "suites" / "2014")

        # The configuration formats (config.py), as the generator's audit checks them.
        bad_adapter = _stub_adapter("bad", stub, **{"compile": ["{nope}"], "diagnostic": "no-line-group"})
        (root / "adapters" / "bad.json").write_text(json.dumps(bad_adapter), encoding="utf-8", newline="\n")
        (root / "profiles" / "lax.json").write_text(json.dumps(
            {"compiler": "lax", "editions": ["2002", "85"], "modules": {"A.4.99": "x", "A.3-1": "y"},
             "implementor-defined": {"9": "three"}}), encoding="utf-8", newline="\n")
        (root / "baselines" / "orphan.json").write_text(json.dumps(
            {"compiler": "orphan", "verdicts": {"85": {"ghost": "MAYBE"}}}), encoding="utf-8", newline="\n")
        found = [f"{p}: {m}" for p, m in config.check_tree(root, ["85", "2002", "2014", "2023"],
                                                            frozenset({"A.4.4", "A.4.11", "A.3-15"}), {9: ("one", "two")},
                                                            {"85": set(expected) - {"gone"}})]
        for needle in ("adapters/bad.json: compile uses {nope}", "adapters/bad.json: diagnostic is a regular expression",
                       "profiles/lax.json: editions are listed in ascending order",
                       "profiles/lax.json: A.4.99 is not an Annex A.4 module", "profiles/lax.json: modules names A.4.",
                       "profiles/lax.json: item 9 is 'three'", "baselines/orphan.json: 85 ghost: 'MAYBE'",
                       "baselines/orphan.json: a baseline of 'orphan', which has no adapter",
                       "baselines/stub.json: 85 gone: no test of the 85 suite"):
            check(f"arm: the configuration audit reports {needle!r}", any(f.startswith(needle) for f in found), found)
        check("arm: the well-formed stub adapter and profile are not reported",
              not any(f.startswith(("adapters/stub.json", "profiles/stub.json", "adapters/hooked.json"))
                      for f in found), found)
    print("=== run.py --self-test: " + ("ALL GREEN" if not rc else "FAILED") + " ===")
    return rc


if __name__ == "__main__":
    for _stream in (sys.stdout, sys.stderr):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    sys.exit(main(sys.argv[1:]))
