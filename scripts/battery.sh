#!/bin/bash
# THE COMPREHENSIVE BATTERY — run the independent legs CONCURRENTLY, read every verdict from its own file.
#
# ⛔ WHY THIS EXISTS. The comprehensive gate was being run leg-by-leg by hand: Conformance (~14 min), then Unit
# (~2 min), then characterization, then the guard, then the differential — strictly serially, on a 32-core
# machine, because plan §0's caution "run the long legs ONE AT A TIME" was being read as "never overlap
# anything". THE ACTUAL HAZARD IS NARROWER AND IS STATED IN §0: a leg that REBUILDS must not overlap a
# `--no-build` leg, because it swaps assemblies mid-run (that is how a Conformance run once produced no verdict
# at all). Two `--no-build` test assemblies cannot do that to each other.
#
# So the legs are grouped by what they WRITE, not by how long they take:
#   PHASE 0  build the solution ONCE          — every --no-build leg below depends on it (a stale test-bin
#                                               compiler DLL hides regressions: plan §0 "Mechanics")
#   PHASE 1  Conformance ∥ Unit ∥ Characterization — three independent --no-build assemblies, fully concurrent.
#            Wall-clock becomes the POLE (Conformance), not the SUM.
#   PHASE 2a guard evidence-rule witnesses    — seconds. Proves the NIST guard's verdicts still MEAN what they
#                                               say (a lost observation is a NO-VERDICT, a wrong answer is a
#                                               REGRESSION) before phase 2's output is believed. §3.10
#                                               corollary 3; earned by kb/Work/PB473, where a green harness
#                                               reported a regression it had not observed.
#   PHASE 2  guard-fast                       — REBUILDS the `cobol` CLI + the legacy test projects, whose
#                                               closures include Cobol.Net.Frontend and the whole greenfield
#                                               compiler. Must not overlap phase 1.
#                                               ⛔ SINCE kb/Work/PB750 THIS LEG MEASURES WiseOwl COBOL. It used to
#                                               drive the LEGACY `cobolsharp.dll`, so `guard NIST: 353 MATCH`
#                                               was a statement about the oracle and battery #58's NC215A wrong
#                                               answer was invisible to it. The summary line now NAMES the
#                                               compiler: read `guard NIST (cobol): …`.
#   PHASE 3  GnuCOBOL differential            — drives cobol.exe; CPU-saturating, so it is not overlapped with
#                                               the guard (both would just split cores, and contention is the
#                                               known enemy of the guard's verdict).
#
# ⛔ EVERY VERDICT IS READ FROM A REDIRECTED FILE, NEVER `| tail -N` INTO A `&&` CHAIN (plan §0): `guard-fast`
# once reported exit 1 on a fully green run because the invoking chain ended in a `grep -c` that matched nothing.
#
# Usage:  bash scripts/battery.sh [outdir]        # everything
#         SKIP_GUARD=1 SKIP_DIFF=1 bash scripts/battery.sh    # the greenfield legs only
set -u
# ⛔ THE BATTERY RUNS COLD (kb/Work PB985; DESIGN-test-build-ci §3.12): the Conformance runner's compiled-program
# cache is for RE-GATES after a test-only fix; the comprehensive gate recompiles every program from scratch, so
# it is never the cache that is being measured. `COBOLNET_COMPILE_CACHE=on bash scripts/battery.sh` overrides.
export COBOLNET_COMPILE_CACHE="${COBOLNET_COMPILE_CACHE:-off}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT"
OUT="${1:-${TMPDIR:-/tmp}/battery-$(date +%Y%m%d-%H%M%S)}"
mkdir -p "$OUT"
T0=$(date +%s); el() { echo "[+$(( $(date +%s) - T0 ))s] $*"; }
RC=0; SUMMARY="$OUT/summary.txt"; : > "$SUMMARY"
note() { echo "$1" | tee -a "$SUMMARY"; }

# ⛔ PHASE -1: THE STATIC CITATION AUDITS. They take a second, they need no build, and they are the only gate
# that can see a WRONG CLAUSE NUMBER — the defect CLAUDE.md rule 1 exists for, which no test can ever fail on
# (`// MOVE (§14.9.24)` compiles perfectly; §14.9.24 is MERGE). Baseline is ZERO findings for ALL THREE; each
# has a `--self-test` proving its checks still fail on a real defect. The first two need `specs/ISO_COBOL.md`,
# which is TRACKED here, and REFUSE when it is absent, so a green is never green-by-absence; the third reads only
# the tree it checks against and asserts its own population instead. The same four also gate CI's `audits` job
# on every push (kb/Work PB1574).
#
# ⛔ AND THERE ARE TWO OF THEM, BECAUSE ONE OF THEM RAN FOR MONTHS WITH NOBODY READING IT. `audit_code_citations`
# (clause vs CONSTRUCT) sat in this leg while its sibling `audit_doc_citations` (quoted fragment vs the clause it
# is filed under) sat in no gate at all — and at the battery #42 close it reported SIX misfiled citations in
# tracked files, two of them in `src/`, that no note owned (kb/Work PB379). Same rule 1, same second of runtime,
# same log, same red: an audit nobody runs is a checker that has never contradicted anything.
el "=== PHASE -1: citation audits (static, no build) ==="
python3 scripts/spec/audit_code_citations.py --check > "$OUT/citations.log" 2>&1
CIT=$?
note "$(printf '%-16s %s' 'citations:' "$(grep -E '^(⛔|[0-9]+ files)' "$OUT/citations.log" | tail -1)")"
[ "$CIT" -eq 0 ] || { note "citations:       ⛔ findings — see $OUT/citations.log"; RC=1; }
python3 scripts/spec/audit_doc_citations.py --check >> "$OUT/citations.log" 2>&1
DOCCIT=$?
note "$(printf '%-16s %s' 'doc citations:' "$(grep -E '^⛔ [0-9]+ MISFILED' "$OUT/citations.log" | tail -1)")"
[ "$DOCCIT" -eq 0 ] || { note "doc citations:   ⛔ misfiled — see $OUT/citations.log"; RC=1; }

# ⛔ AND A THIRD, in the same leg for the same reason (kb/Work PB785). The two above check a citation of the
# STANDARD; this one checks a FROZEN evidence file's citation of the TREE — `docs/rearchitecture/evidence/*.json`
# is an agent's recorded output, never edited to stay current (both audits above skip the directory by name), so
# a claim the tree has since refuted cannot be repaired and must carry a `superseded_by` marker instead. One of
# those files told PB712's implementer that a refuted grammar "must NOT be re-implemented". No submodule needed.
python3 scripts/spec/audit_evidence_supersession.py --check >> "$OUT/citations.log" 2>&1
EVSUP=$?
note "$(printf '%-16s %s' 'evidence:' "$(grep -E '^⛔ [0-9]+ UNMARKED' "$OUT/citations.log" | tail -1)")"
[ "$EVSUP" -eq 0 ] || { note "evidence:        ⛔ unmarked refuted claims — see $OUT/citations.log"; RC=1; }

# ⛔ AND THE INVENTORY'S WITNESS COUNT (kb/Work PB959). SpecTraceabilityInventoryDriftTests checks that every
# surviving reference RESOLVES — deliberately not how many there are — so a verdict batch that SUBTRACTED valid
# evidence left the battery green. This compares the inventory against the merge-base with main and is RED on
# any witness lost without a `retired-witness:` mark or a verdict change that re-sites it.
python3 scripts/spec/audit_witness_loss.py --check >> "$OUT/citations.log" 2>&1
WLOSS=$?
note "$(printf '%-16s %s' 'witnesses:' "$(grep -E '^=== WITNESS LOSS' "$OUT/citations.log" | tail -1)")"
[ "$WLOSS" -eq 0 ] || { note "witnesses:       ⛔ inventory evidence lost — see $OUT/citations.log"; RC=1; }

el "=== PHASE 0: build the solution (once) ==="
if ! dotnet build CobolSharp.sln -v quiet > "$OUT/build.log" 2>&1; then
    note "BUILD: FAILED — see $OUT/build.log"; tail -20 "$OUT/build.log"; exit 1
fi
note "BUILD: ok"

if [ "${SKIP_TESTS:-0}" != "1" ]; then
    el "=== PHASE 1: Conformance ∥ Unit ∥ Characterization (independent --no-build assemblies) ==="
    dotnet test tests/Cobol.Net.Tests.Conformance --no-build --verbosity quiet \
        --logger "trx;LogFileName=conformance.trx" --results-directory "$OUT" > "$OUT/conformance.log" 2>&1 &
    P_CONF=$!
    dotnet test tests/Cobol.Net.Tests.Unit --no-build --verbosity quiet \
        --logger "trx;LogFileName=unit.trx" --results-directory "$OUT" > "$OUT/unit.log" 2>&1 &
    P_UNIT=$!
    # Every leg writes a TRX (kb/Work PB1573): at --verbosity quiet the console log carries only the verdict line,
    # so the TRX is the ONLY record of a failing test's message and stack — and this leg used to have none.
    dotnet test tests/Cobol.Net.Tests.Characterization --no-build --verbosity quiet \
        --logger "trx;LogFileName=characterization.trx" --results-directory "$OUT" > "$OUT/characterization.log" 2>&1 &
    P_CHAR=$!
    wait $P_CONF; RC_CONF=$?
    wait $P_UNIT; RC_UNIT=$?
    wait $P_CHAR; RC_CHAR=$?
    for leg in conformance unit characterization; do
        v=$(grep -E "^(Passed!|Failed!)" "$OUT/$leg.log" | tail -1)
        note "$(printf '%-16s %s' "$leg:" "${v:-NO VERDICT LINE — the leg produced no result, which is a FAILURE}")"
        [ -n "$v" ] || RC=1
    done
    [ "$RC_CONF" -eq 0 ] && [ "$RC_UNIT" -eq 0 ] && [ "$RC_CHAR" -eq 0 ] || RC=1
fi

if [ "${SKIP_GUARD:-0}" != "1" ]; then
    # ⛔ PROVE THE INSTRUMENT BEFORE BELIEVING IT (§3.10 corollary 3). Seconds: 21 synthetic cases through the
    # real group runner, each asserting what ONE verdict MEANS. Battery #43 spent a whole run — and hours of
    # attribution — on a `DIFF — REGRESSION!` that meant "the harness could not compare" (kb/Work/PB473).
    el "=== PHASE 2a: guard evidence-rule witnesses + the compiler-identity watchdog ==="
    bash scripts/guard-verify.sh --witnesses > "$OUT/guard-witnesses.log" 2>&1
    note "$(printf '%-16s %s' 'guard witnesses:' "$(grep -E '^=== \(1\) WITNESSES' "$OUT/guard-witnesses.log" | tail -1)")"
    grep -q '^=== (1) WITNESSES: ALL GREEN' "$OUT/guard-witnesses.log" || RC=1
    note "$(printf '%-16s %s' 'guard compiler:' "$(grep -E '^=== guard-compiler --self-test:' "$OUT/guard-witnesses.log" | tail -1)")"
    grep -q '^=== guard-compiler --self-test: ALL GREEN' "$OUT/guard-witnesses.log" || RC=1

    el "=== PHASE 2: guard-fast (rebuilds — never overlapped with phase 1) ==="
    bash scripts/guard-fast.sh > "$OUT/guard.log" 2>&1
    # ⛔ THE COMPILER IS PART OF THE VERDICT (PB750): the pattern requires the `(cobol)` / `(legacy)` tag, so a
    # guard that somehow printed the old unlabelled line reports NO VERDICT LINE here rather than a green.
    note "$(printf '%-16s %s' 'guard NIST:' "$(grep -E '^=== NIST \(' "$OUT/guard.log" | tail -1)")"
    grep -qE '^=== NIST \(cobol\): ' "$OUT/guard.log" || { note "guard NIST:      ⛔ the guard did not drive WiseOwl COBOL — see $OUT/guard.log"; RC=1; }
    note "$(printf '%-16s %s' 'guard audit:' "$(grep -E '^=== NIST AUDIT: (CLEAN|[0-9])' "$OUT/guard.log" | tail -1)")"
    note "$(printf '%-16s %s' 'guard verdict:' "$(grep -E '^=== (ALL GREEN|FAILURES)' "$OUT/guard.log" | tail -1)")"
    grep -q '^=== ALL GREEN ===' "$OUT/guard.log" || RC=1
fi

if [ "${SKIP_DIFF:-0}" != "1" ]; then
    el "=== PHASE 3: GnuCOBOL external differential ==="
    python3 scripts/gnucobol_differential.py --exe src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe \
        --report "$OUT/gnucobol-report.json" > "$OUT/gnucobol.log" 2>&1
    note "$(printf '%-16s %s' 'gnucobol:' "$(grep -E '^cases run:' "$OUT/gnucobol.log" | tail -1)")"
    grep -E '^  (AGREE|WE_)' "$OUT/gnucobol.log" | sed 's/^/                 /' | tee -a "$SUMMARY"
    if grep -q 'NO COMPILER VERDICT' "$OUT/gnucobol.log"; then
        note "gnucobol:        ⛔ cases with NO COMPILER VERDICT — see $OUT/gnucobol.log"; RC=1
    fi
    # ⛔ THE PER-CASE DIFF IS THE LEG THAT MATTERS, NOT THE FOUR TOTALS ABOVE. Identical totals are consistent
    # with OFFSETTING flips, so this gates on the per-case verdict line (never the exit code — §0's standing
    # rule) and names every flip in the log. An ABSENT baseline is a RED, not a pass: without it the run
    # cannot claim zero flips, which is precisely the hole this closes.
    note "$(printf '%-16s %s' 'gnucobol diff:' "$(grep -E '^=== DIFFERENTIAL: ' "$OUT/gnucobol.log" | tail -1)")"
    grep -E '^(  corpus changed:|    (FLIP|NEW|REMOVED) )' "$OUT/gnucobol.log" \
        | sed 's/^/                 /' | tee -a "$SUMMARY"
    if ! grep -qF '=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===' "$OUT/gnucobol.log"; then
        note "gnucobol diff:   ⛔ per-case verdict flips (or no baseline) — see $OUT/gnucobol.log"; RC=1
    fi
fi

el "=== BATTERY SUMMARY (artifacts in $OUT) ==="
cat "$SUMMARY"
[ "$RC" -eq 0 ] && echo "=== BATTERY: ALL GREEN ===" || echo "=== BATTERY: NOT GREEN (rc=$RC) ==="
exit $RC
