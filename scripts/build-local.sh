#!/usr/bin/env bash
# build-local.sh — the PER-COMMIT wave-local gate, as one command (kb/Work PB108; plan §0 "Gates"):
#   build the SOLUTION (Debug — the same binaries battery.sh PHASE 0 builds; a stale test-bin compiler DLL hides
#   regressions, so no --no-build leg ever runs on an unbuilt tree), then Conformance filtered on the SUBJECT of the
#   change, full Unit (~2 min), Characterization. NOT the comprehensive gate (battery.sh), NOT guard-fast.sh, NOT
#   Release (the Windows CI leg). ⛔ The filter is REQUIRED: the wave-local gate is a filter chosen from what the
#   change TOUCHES ("~Arithmetic|~Inspect"; add "~VersionMatrix" for an edition gate) — a default would re-create the
#   PB36 mistake of filtering on where the new goldens sat. Shorthand terms ("~X|~Y") are expanded to
#   FullyQualifiedName~X|FullyQualifiedName~Y — vstest silently matches NOTHING for a bare "~X" (and exits 0), so a
#   leg with NO verdict line is RED here, never green by absence. That check is WHOLE-filter, so EVERY
#   TERM is additionally put back to vstest before the legs run (scripts/filter_population.py, kb/Work
#   PB708): a term naming no test anywhere is DEAD and the gate does not run; one whose tests live only in
#   an assembly this gate runs UNFILTERED is INERT — it selected nothing, so it is named and the verdict
#   line says so.
# Usage:  bash scripts/build-local.sh "<filter>"        e.g.  bash scripts/build-local.sh "~Collation|~Locale"
set -u
F="${1:-}"
if [ -z "$F" ]; then echo "usage: $0 \"<conformance filter, e.g. ~Area|~Other>\" — the wave-local gate needs the SUBJECT of the change" >&2; exit 2; fi
cd "$(dirname "$0")/.."
F="$(printf '%s' "$F" | sed -E 's/(^|[|&(])(!=|=|~)/\1FullyQualifiedName\2/g')"
RC=0
# ⛔ THE CITATION AUDITS RUN FIRST, BEFORE THE BUILD — they are a second and cost nothing, and a wrong § is the
# one defect class no test can ever catch (CLAUDE.md rule 1: the failure mode is INHERITING a clause number).
# `audit_code_citations` gates on six checks — three over the clause vs the CONSTRUCT the comment is about, and
# three over the ORDINAL inside a right clause (a general-format number, and a rule number paired with one; it
# also MEASURES two more, printing a count under --check and the sites without it) — and
# `audit_doc_citations` on one (a QUOTED fragment vs the clause it is filed under); both have a proven-zero
# baseline and a `--self-test` proving each check still fails on a real defect. They need `specs/ISO_COBOL.md`,
# which is TRACKED in this repository (only the PDF is in the private submodule), so they also gate CI's `audits`
# job on every push, docs-only pushes included (kb/Work PB1574) — this gate is the early warning, CI the refusal.
# The four audits below are the SAME list, in the same order, in build-local.ps1, battery.sh PHASE -1 and CI.
# ⛔ AND A THIRD, over the OTHER half of the same problem (kb/Work PB785). The two above ask whether a citation
# of the STANDARD is right; `audit_evidence_supersession` asks whether a FROZEN evidence file's citation of the
# TREE is still true — `docs/rearchitecture/evidence/*.json` is a record that is never edited to stay current
# (which is why both audits above skip it by name), and one of its `LANDED` verdicts is what told PB712's
# implementer not to look. Same second of runtime, same proven-zero baseline, same `--self-test`. This one
# needs no submodule — it reads the tree it is checking against.
python scripts/spec/audit_code_citations.py --check || { echo "=== CITATIONS: RED (see above) ==="; RC=1; }
python scripts/spec/audit_doc_citations.py --check || { echo "=== DOC CITATIONS: RED (see above) ==="; RC=1; }
python scripts/spec/audit_evidence_supersession.py --check || { echo "=== EVIDENCE SUPERSESSION: RED (see above) ==="; RC=1; }
# The drift-rule index (docs/DRIFT_RULES.md) is GENERATED from every *DriftTests summary — stale = red.
python scripts/spec/drift_rules.py --check || { echo "=== DRIFT RULES INDEX: RED (run python scripts/spec/drift_rules.py) ==="; RC=1; }
python scripts/hooks/test_forbidden_commands.py || { echo "=== GUARD HOOK SELF-TEST: RED ==="; RC=1; }
python scripts/hooks/test_readonly_repo.py || { echo "=== READ-ONLY HOOK SELF-TEST: RED ==="; RC=1; }
# ⛔ The inventory's WITNESS COUNT (kb/Work PB959) — the axis the resolution drift test deliberately does not
# measure: RED on any code-location/test-ref lost since the merge-base with main without a retirement mark.
python scripts/spec/audit_witness_loss.py --check || { echo "=== WITNESS LOSS: RED (see above) ==="; RC=1; }
# The GPL GnuCOBOL corpus is git-ignored and PER WORKTREE (scripts/fetch-gnucobol-tests.ps1): a fresh worktree has
# none, and ExternalCorpusPopulationDriftTests in the UNFILTERED unit leg is RED BY DESIGN when it is absent
# (kb/Work PB209). Fetch it here so every worktree's gate measures the population.
# ⛔ A FAILED FETCH REFUSES THE GATE (kb/Work PB897), and `|| true` is exactly how this arm used to swallow the
# fetch's exit code — the two-arm defect: the pwsh twin checked $LASTEXITCODE and this one discarded it. The reds
# are ATTRIBUTED by cause and exit code, and the verdict line says the population was never measured.
# (Same block as build-local.ps1 — change BOTH.)
CORPUS_NOTE=""
if [ ! -d tests/external/gnucobol/tests/testsuite.src ]; then
    echo "=== EXTERNAL CORPUS: absent in this worktree — fetching (GPL, git-ignored, never committed) ==="
    FETCH_RC=0
    pwsh -NoProfile -File scripts/fetch-gnucobol-tests.ps1 || FETCH_RC=$?
    if [ "$FETCH_RC" -ne 0 ] || [ ! -d tests/external/gnucobol/tests/testsuite.src ]; then
        echo "=== EXTERNAL CORPUS: FETCH FAILED (exit $FETCH_RC; the FETCH FAILED line above names the cause) — the ExternalCorpusPopulationDriftTests reds in the unit leg are ATTRIBUTABLE TO IT, not to the change under test, and this gate is RED because it could not measure that population ==="
        CORPUS_NOTE=" — EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED"
        RC=1
    fi
fi
dotnet build CobolSharp.sln -v quiet || { echo "=== WAVE-LOCAL GATE: BUILD FAILED ==="; exit 1; }
# ⛔ EVERY TERM OF THE FILTER MUST NAME A REAL TEST (kb/Work PB708) — the NO-VERDICT-LINE check on each
# leg is WHOLE-filter: it fires only when EVERY term is dead, so one dead term OR'd among live ones selects
# the others, prints a verdict line and is never named. PB691's gate carried
# `FullyQualifiedName~SpecTraceabilityInventory` against Conformance — where that test does not live, it is
# in Unit — and reported `Passed! … 1640` on every run. vstest answers for its own filter language, one
# discovery probe per term (~1.6 s each); `filter_population.py --self-test` proves every arm fires.
python scripts/filter_population.py --filter "$F" --filtered tests/Cobol.Net.Tests.Conformance --unfiltered tests/Cobol.Net.Tests.Unit --unfiltered tests/Cobol.Net.Tests.Characterization
POP=$?
# ⛔ ANY code but 0 (all live) or 3 (inert, named) REFUSES the gate — a missing python or a crashed probe
# must not become a silent skip of the guard (feedback_green_gates_arent_evidence).
if [ "$POP" -ne 0 ] && [ "$POP" -ne 3 ]; then
    echo "=== WAVE-LOCAL GATE: NOT RUN — the filter does not name what it claims, or the check itself could not run (rc=$POP, filter $F) ==="; exit 2
fi
INERT=""; [ "$POP" -eq 3 ] && INERT=" — WITH INERT FILTER TERM(S), SEE ABOVE"
# ⛔ WHAT A LEG PRINTS IS DECIDED IN ONE PLACE — scripts/test_leg_report.py (kb/Work PB1573): a GREEN leg prints its
# verdict line, a RED one its COMPLETE output, untrimmed; no verdict line (a filter that matched nothing) is RED.
# This function used to keep the last 20 lines matching `error|[FAIL]|Passed!|Failed!` — a failing test's name
# without its message or stack. The full log is always kept under TestResults/build-local/ (git-ignored).
# (Same reporter as build-local.ps1 and guard-fast.sh — change it THERE, never here.)
LEG_LOGS="TestResults/build-local"; mkdir -p "$LEG_LOGS"
leg() {   # leg <name> <dotnet test args…>
    local name="$1"; shift
    local log="$LEG_LOGS/$name.log"
    # SCRUBBED (kb/Work PB1718): no leg handshake or VSTest*/RunSettingsFilePath property reaches the test host.
    python scripts/test_population.py scrubbed dotnet test "$@" > "$log" 2>&1; local rc=$?
    python scripts/test_leg_report.py --name "$name" --log "$log" --rc "$rc" || RC=1
}
leg conformance      tests/Cobol.Net.Tests.Conformance --no-build --filter "$F"
leg unit             tests/Cobol.Net.Tests.Unit --no-build
leg characterization tests/Cobol.Net.Tests.Characterization --no-build
[ "$RC" -eq 0 ] && echo "=== WAVE-LOCAL GATE: GREEN (filter $F)$INERT ===" || echo "=== WAVE-LOCAL GATE: RED (filter $F)$INERT$CORPUS_NOTE ==="
exit $RC
