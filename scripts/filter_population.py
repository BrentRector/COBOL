#!/usr/bin/env python3
# SELF-TEST-NEEDS: build — its --self-test asks vstest (`dotnet test --no-build --list-tests`) what the BUILT Conformance and Unit assemblies discover
"""filter_population.py — EVERY TERM of a vstest `--filter` must name a real test (kb/Work PB708).

`test_leg_report.py` already refuses a leg that prints no `Passed!`/`Failed!` verdict line — the guard added
after the `--filter "~X|~Y"` lesson (a term without a property matches NOTHING and exits 0). That guard is
WHOLE-FILTER: it fires only when EVERY term is dead. One dead term OR'd among live ones still selects the
others, still prints a verdict line, and is never named. PB691's gate ran

    FullyQualifiedName~CorpusManifest|FullyQualifiedName~SpecTraceabilityInventory

against the Conformance assembly on every run and reported `Passed! … 1640` every time; the test with that name
is `SpecTraceabilityInventoryDriftTests`, which lives in Unit — the assembly that gate ran UNFILTERED. Its
implementer, its finisher and everyone who copied that filter forward believed the inventory drift test had gone
through the gate's filter. Nothing had.

A gate filter is a CLAIM about which tests ran, so the population assertion belongs per (term, assembly), not
per invocation: `feedback_verdict_evidence_invariant` ("a run must assert its population") and
`feedback_measure_the_selectors_complement` ("a selector is evidence about what it RETURNED, never about what
it dropped").

TWO OUTCOMES, because the two failures do different harm and a guard that cries wolf gets worked around:

  DEAD  the name matches nothing in ANY assembly the invocation runs — a typo or a renamed test. The subject
        the filter claims to cover was never tested by anything. ⛔ The gate does not run.
  INERT the name matches no test in the FILTERED assembly but does match in one the invocation runs WITHOUT
        the filter, so the term selected nothing and could not have. This is PB691 exactly. Those tests DID
        run, so the gate proceeds — but the term is named, and the caller stamps the verdict line, because
        the harm in PB691 was a verdict line that read clean while a false claim travelled with it.

⛔ VSTEST OWNS ITS OWN FILTER LANGUAGE AND THIS SCRIPT DOES NOT RE-IMPLEMENT IT. Each term is handed back to
`dotnet test --list-tests --filter <term>` and vstest answers. Modelling the terms in Python against the
`--list-tests` output is measurably WRONG: those are DISPLAY names carrying theory arguments
(`…DayOfWeek_MondayIs1_SundayIs7(clock: "2026-06-08T09:00:00", expected: "1")`) while `FullyQualifiedName`
carries none — `--filter FullyQualifiedName~clock` selects 1 test in Conformance where the display names contain
"clock" dozens of times, so a substring model would have CLEARED a term vstest calls dead, which is the exact
false-green this guard exists to end. Discovery costs ~1.6 s per term against an already-built tree.

A NEGATED term (`!~X`, `!=X`) is probed through its POSITIVE form: asking vstest for `!~X` returns every test in
the assembly and would call a stale exclusion live, whereas the question that matters is whether the name X is
real at all.

Usage (the wave-local gate applies its filter to ONE assembly and runs the other two unfiltered):

    python scripts/filter_population.py --filter "<expanded filter>" \
        --filtered tests/Cobol.Net.Tests.Conformance \
        --unfiltered tests/Cobol.Net.Tests.Unit --unfiltered tests/Cobol.Net.Tests.Characterization

⛔ `--allow-build` FOR A CALLER THAT HAS NOT BUILT YET (kb/Work PB751). Discovery runs `--no-build` by default,
because the gate callers build the solution themselves and re-building per term would multiply the gate's cost.
A caller whose OWN `dotnet test` does the building — `scripts/gen-vcr.ps1` and `scripts/gen-diagnostics-doc.ps1`
both do — has nothing built at the moment the guard must run, and would get exit 2 on a clean tree (MEASURED, not
assumed: on a fresh worktree the default probe answers "could not discover tests … build the solution first").
`--allow-build` drops `--no-build` from the discovery, so the first probe builds exactly what the caller's own run
was about to build and the guard costs that caller nothing. It is a MODE, not a fallback: the guard never retries
a failed discovery with a build, because a caller that believes it built must hear that it did not.

  exit 0  every term is live in a filtered assembly — each term's count is printed, which IS the population
          evidence the run needs to assert
  exit 1  ⛔ DEAD FILTER TERM(S) — the caller must not run the gate
  exit 2  the discovery itself failed — an error, never a finding (a MISSING observation is not a negative one)
  exit 3  ⛔ INERT FILTER TERM(S) — the caller may run the gate and must stamp its verdict line

`--self-test` proves every arm FIRES before the guard is trusted (`feedback_prove_the_watchdog_fails`): the live
arm, the dead arm, the mixed `live|dead` filter that is PB708 itself (the shape the whole-filter guard passes),
the dead-negative arm, the INERT arm built from a name that really is Unit-only, and the exit-2 arm that must
not masquerade as a finding. Every name it uses is DERIVED from what the assemblies discover, so the self-test
cannot rot into asserting against a term that stopped existing.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# The listing's shape and the scrub rule are test_population.py's (kb/Work PB1718): ONE parser of a
# `--list-tests` output, and ONE statement of which environment variables may narrow a `dotnet test` run.
from test_population import LISTED_MARK as AVAILABLE, PopulationError, parse_listing, scrubbed_env

# ⛔ The markers below are the ENGLISH strings vstest prints, so the probe pins the CLI's language rather than
# inheriting the machine's. Without this a non-English machine finds no marker, which this script reports as a
# failed DISCOVERY (exit 2) — fail-closed, but for a reason no one would guess. SCRUBBED, like every `dotnet
# test` caller: a stray `VSTestTestCaseFilter` or gate handshake in the environment would narrow the probe.
ENV = {**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en"}

# A vstest filter term: <property><operator><value>. The operators are ordered so `!=`/`!~` win over `=`/`~`.
TERM_RE = re.compile(r"^(?P<prop>[A-Za-z_][A-Za-z0-9_.]*)(?P<op>!=|!~|=|~)(?P<val>.*)$", re.DOTALL)
# The negated operators, keyed to the positive form that asks "does this name exist at all?".
POSITIVE_OF = {"!~": "~", "!=": "="}
NO_MATCH = "No test matches the given testcase filter"
DEAD, INERT = "DEAD FILTER TERM", "INERT FILTER TERM"
RC_OK, RC_DEAD, RC_ERROR, RC_INERT = 0, 1, 2, 3


def split_terms(text: str) -> list[str]:
    """Split a vstest filter into its terms. `&` and `|` join terms and `()` groups them; a literal one of
    those is backslash-escaped (vstest's own escape), so an escaped operator never splits a value."""
    terms: list[str] = []
    buf: list[str] = []
    i = 0
    while i < len(text):
        c = text[i]
        if c == "\\" and i + 1 < len(text):
            buf.append(text[i:i + 2])
            i += 2
            continue
        if c in "&|":
            terms.append("".join(buf))
            buf = []
            i += 1
            continue
        buf.append(c)
        i += 1
    terms.append("".join(buf))
    return [t for t in (ungroup(t) for t in terms) if t]


def ungroup(term: str) -> str:
    r"""Strip the grouping parentheses vstest allows around a term — never an escaped `\(` / `\)`, which is a
    literal paren inside a value."""
    t = term.strip()
    while t.startswith("("):
        t = t[1:].strip()
    while t.endswith(")") and not t.endswith("\\)"):
        t = t[:-1].strip()
    return t


def positive_form(term: str) -> tuple[str, bool]:
    """The probe to run for `term`, and whether the term was NEGATED."""
    m = TERM_RE.match(term)
    if m is None:
        # Not a property term — a bare "~X" the caller failed to expand, or a syntax this grammar does not
        # cover. Probe it verbatim and let vstest have the last word rather than guessing.
        return term, False
    op = m.group("op")
    return m.group("prop") + POSITIVE_OF.get(op, op) + m.group("val"), op in POSITIVE_OF


def assembly_name(project: str) -> str:
    p = Path(project)
    return p.stem if p.suffix == ".csproj" else p.name


def discovery_cmd(project: str, allow_build: bool) -> list[str]:
    """THE discovery command line — one place, so the two callers cannot disagree about whether a probe builds."""
    return ["dotnet", "test", project, *([] if allow_build else ["--no-build"]), "--list-tests"]


def discover(project: str, term: str | None, allow_build: bool = False) -> tuple[int, str]:
    """How many tests does `term` select in `project`? Returns (count, diagnostic); a count of -1 means the
    DISCOVERY failed and nothing at all was observed — never conflated with a term that selected zero."""
    cmd = discovery_cmd(project, allow_build)
    if term is not None:
        cmd += ["--filter", term]
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", env=ENV)
    out = (proc.stdout or "") + (proc.stderr or "")
    if AVAILABLE not in out and NO_MATCH not in out:
        return -1, out.strip()[-1500:]
    return (sum(parse_listing(out, project).values()) if AVAILABLE in out else 0), ""


def listed_names(project: str, allow_build: bool = False) -> list[str]:
    """Every test `project` discovers, as printed — display names, theory arguments and all."""
    proc = subprocess.run(discovery_cmd(project, allow_build),
                          capture_output=True, text=True, encoding="utf-8", errors="replace", env=ENV)
    try:
        return list(parse_listing((proc.stdout or "") + (proc.stderr or ""), project).elements())
    except PopulationError:
        return []


def check(filter_text: str, filtered: list[str], unfiltered: list[str],
          allow_build: bool = False) -> tuple[int, list[str]]:
    """The per-term population assertion. Returns (exit code, report lines)."""
    lines: list[str] = []
    terms = split_terms(filter_text)
    if not terms:
        return RC_ERROR, [f"FILTER POPULATION CHECK FAILED: {filter_text!r} has no terms"]

    dead: list[str] = []
    inert: list[str] = []
    for term in terms:
        probe, negated = positive_form(term)
        counts: dict[str, int] = {}
        for project in filtered:
            n, diag = discover(project, probe, allow_build)
            if n < 0:
                how = ("the project failed to build" if allow_build
                       else "build the solution first — this check runs --no-build, or pass --allow-build")
                return RC_ERROR, lines + [
                    f"FILTER POPULATION CHECK FAILED: could not discover tests in {project} ({how})", diag]
            counts[assembly_name(project)] = n
        if sum(counts.values()) > 0:
            verb = "excludes" if negated else "selects"
            lines.append(f"  {term}  →  {verb} " + " · ".join(f"{n} in {a}" for a, n in counts.items()))
            continue

        # Nothing in the assemblies the filter steers. Does the name exist in one this invocation runs
        # WITHOUT the filter? That distinguishes a term that lost coverage from one that never had any.
        elsewhere = []
        for project in unfiltered:
            n, _ = discover(project, probe, allow_build)
            if n > 0:
                elsewhere.append((assembly_name(project), n))
        verb = "excludes no test" if negated else "selects no test"
        if elsewhere:
            inert.append(term)
            lines.append(f"⛔ {INERT}: {term} {verb} in {', '.join(counts)}")
            for name, n in elsewhere:
                lines.append(f"      the name matches {n} test(s) in {name}, which this invocation runs "
                             f"UNFILTERED — those tests ran, but not because of this term, and the filter's "
                             f"claim about them is false. Drop the term or filter that assembly too.")
        else:
            dead.append(term)
            ran = ", ".join(list(counts) + [assembly_name(p) for p in unfiltered])
            lines.append(f"⛔ {DEAD}: {term} {verb} in {', '.join(counts)} — the name matches nothing in "
                         f"{ran}, so whatever it was meant to cover was never tested")

    if dead:
        lines.append(f"⛔ FILTER POPULATION: {len(dead)} of {len(terms)} term(s) DEAD — the gate does not run "
                     f"(kb/Work PB708)")
        return RC_DEAD, lines
    if inert:
        lines.append(f"⛔ FILTER POPULATION: {len(inert)} of {len(terms)} term(s) INERT — the gate runs and its "
                     f"verdict line says so (kb/Work PB708)")
        return RC_INERT, lines
    lines.append(f"filter population: {len(terms)} term(s), all live in "
                 f"{', '.join(assembly_name(p) for p in filtered)}")
    return RC_OK, lines


def classes_of(project: str, allow_build: bool = False) -> list[str]:
    """The distinct test-class simple names `project` discovers, in discovery order."""
    seen: list[str] = []
    for name in listed_names(project, allow_build):
        parts = name.split("(", 1)[0].split(".")
        if len(parts) >= 2 and parts[-2] not in seen:
            seen.append(parts[-2])
    return seen


def self_test(filtered: str, unfiltered: str, allow_build: bool = False) -> int:
    """Prove every arm of the guard fires (or stays silent) against the real assemblies. Every name is DERIVED
    from what they discover, so no arm can rot into asserting against a term that stopped existing."""
    here = classes_of(filtered, allow_build)
    if not here:
        print(f"⚠ SELF-TEST CANNOT RUN — {filtered} discovered no tests (build the solution first)")
        return 1
    live, other = here[0], (here[1] if len(here) > 1 else here[0])
    ghost = "ZzNoSuchTest_PB708_SelfTest"
    fq = "FullyQualifiedName"

    # The INERT arm needs a class that really is in the UNFILTERED assembly and really is not in the filtered
    # one — PB691's shape, derived rather than hand-named.
    elsewhere = next((c for c in classes_of(unfiltered, allow_build) if c not in here), None)

    cases = [
        ("a live term is silent", f"{fq}~{live}", [filtered], [unfiltered], RC_OK, ""),
        ("a dead term is named", f"{fq}~{ghost}", [filtered], [unfiltered], RC_DEAD, DEAD),
        # ⛔ THE PB708 CASE: one dead term OR'd among live ones. The whole-filter NO-VERDICT-LINE guard passes
        # this filter — it selects the live term's tests and prints `Passed!`.
        ("a dead term OR'd among live ones is named", f"{fq}~{live}|{fq}~{ghost}",
         [filtered], [unfiltered], RC_DEAD, DEAD),
        ("a dead exclusion is named", f"{fq}~{live}&{fq}!~{ghost}", [filtered], [unfiltered], RC_DEAD, DEAD),
        ("a live exclusion is silent", f"{fq}~{live}|{fq}!~{other}", [filtered], [unfiltered], RC_OK, ""),
        ("a grouped live filter is silent", f"({fq}~{live}|{fq}~{other})", [filtered], [unfiltered], RC_OK, ""),
        # A failed discovery is an ERROR, never a finding: a missing observation is not a negative one.
        ("a discovery failure is exit 2, not a finding", f"{fq}~{live}",
         ["tests/Cobol.Net.Tests.NoSuchProject_PB708"], [], RC_ERROR, ""),
    ]
    if elsewhere:
        # ⛔ PB691 ITSELF: a term whose tests live only in the assembly this invocation runs unfiltered.
        cases.insert(4, (f"a term only in {assembly_name(unfiltered)} is named INERT",
                         f"{fq}~{live}|{fq}~{elsewhere}", [filtered], [unfiltered], RC_INERT, INERT))

    failures = 0
    for label, flt, filt, unf, want_rc, want_marker in cases:
        rc, lines = check(flt, filt, unf, allow_build)
        marker = next((m for m in (DEAD, INERT) if any(m in line for line in lines)), "")
        ok = rc == want_rc and marker == want_marker
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}: rc={rc} (want {want_rc}), "
              f"names {marker or 'nothing'} (want {want_marker or 'nothing'})   [{flt}]")
        if not ok:
            for line in lines:
                print(f"        {line}")
    if not elsewhere:
        print(f"  ⚠ the INERT arm was SKIPPED — every class in {assembly_name(unfiltered)} also exists in "
              f"{assembly_name(filtered)}, so PB691's shape could not be built")
        failures += 1
    print(("ALL GREEN" if not failures else f"⛔ {failures} SELF-TEST FAILURE(S)")
          + f" — {len(cases)} cases against {assembly_name(filtered)}")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--filter", help="the EXPANDED vstest filter (every term carries its property)")
    ap.add_argument("--filtered", action="append", default=[],
                    help="a test project the filter IS applied to (repeatable); a term must be live in one")
    ap.add_argument("--unfiltered", action="append", default=[],
                    help="a test project the invocation runs WITHOUT the filter (repeatable); a term live "
                         "only here is INERT, not dead")
    ap.add_argument("--self-test", action="store_true", help="prove every arm of the guard fires")
    ap.add_argument("--allow-build", action="store_true",
                    help="the discovery may BUILD (drop --no-build) — for a caller whose own `dotnet test` "
                         "does the building, so nothing is built yet when the guard must run (kb/Work PB751)")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if args.self_test:
        return self_test(args.filtered[0] if args.filtered else "tests/Cobol.Net.Tests.Conformance",
                         args.unfiltered[0] if args.unfiltered else "tests/Cobol.Net.Tests.Unit",
                         args.allow_build)
    if not args.filter or not args.filtered:
        ap.error("--filter and at least one --filtered are required (or --self-test)")

    rc, lines = check(args.filter, args.filtered, args.unfiltered, args.allow_build)
    for line in lines:
        print(line)
    return rc


if __name__ == "__main__":
    sys.exit(main())
