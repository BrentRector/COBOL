#!/bin/bash
# Parallel guard — the same checks as scripts/guard.sh, but the 364-program NIST suite runs across cores with
# per-group isolation. Intended to be PROVEN equivalent to the serial guard via scripts/guard-verify.sh (which
# diffs the per-test verdicts). Until that diff is clean, trust scripts/guard.sh as the authority.
#
# WHY THIS IS SAFE (isolation model): NIST tests couple ONLY through shared on-disk data files (TF### producer/
# consumer chains), and that coupling is DECLARED — `tests/nist/corpus.tsv`'s `chain-preds` column. Groups are
# the CONNECTED COMPONENTS of that graph (step 2), each running serially in its own scratch dir; everything else
# is a singleton, fully parallel. 332 groups over 376 programs, longest 9.
#   ⚠ This REPLACED a hand-written "these six suites run serially" list (see §11 A12d). The declared graph is
#     already proven sufficient by the GREENFIELD leg: `NistDifferentialTests` runs all 349 programs in
#     per-program directories with only their declared predecessors, and is green.
#   ⚠ Per-component directories are STRICTLY SAFER than the ordering guard.sh relies on: guard.sh's prose
#     carries ANTI-dependencies ("no other TF022 writer between them") that exist only because it shares ONE
#     directory. Here a non-member cannot touch a chain's file at all.
# A mis-grouping can only make a consumer LOSE its producer -> that consumer goes RED, NEVER a false GREEN (an
# isolated clean dir cannot make a test pass that should fail) — and step 3b's audit now names any program whose
# verdict differs from what the manifest predicts, which is an ABSOLUTE check, not a diff against another run.
#
# Output: a sorted "TEST: VERDICT" list (same vocabulary as guard.sh), the NIST AUDIT, and ALL GREEN / failures.
# ⛔ READ THE AUDIT LINE, NOT THE MATCH COUNT. "353 MATCH" is a number a human compares against memory, which is
# exactly how a run at 352 once passed as green.
#
# ⛔ AND READ WHICH COMPILER IT DROVE. Until 2026-09-06 this leg hard-coded the legacy byte engine's CLI, so its
# headline MATCH count was a statement about the oracle (kb/Work/PB750). It drives `cobol` (src/Cobol.Net.Cli),
# refuses to start unless the resolved binary really references `Cobol.Net.Compiler`, and NAMES the compiler in
# every summary line: `=== NIST (cobol): ... ===`.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export ROOT

# WHICH COMPILER, asserted against the binary's own dependency graph (scripts/guard-compiler.sh).
. "$(dirname "$0")/guard-compiler.sh"
guard_select_compiler
guard_announce_compiler
CLI="$GUARD_CLI_DLL"
JOBS="${JOBS:-$(nproc)}"
# ⛔ FULL FAN-OUT IS KEPT, AND THE LOST OBSERVATIONS ARE RE-TAKEN INSTEAD (plan §11 A12b/A12d).
# Five runs on ONE unchanged tree gave five different answers, and the cause is CONTENTION at -P$(nproc) — every
# affected program compiled and ran clean serially. The obvious response, halving the fan-out, is the WRONG one:
# it slows every run to protect against something that is now DETECTED rather than mis-scored. With the evidence
# rules in guard-run-group.sh in place, contention can no longer corrupt a verdict — it can only LOSE one, and a
# lost observation is re-taken (step 3b below) at full serial isolation. Cost is proportional to the damage
# instead of paid up front on every run.
# Set CJOBS/RJOBS to throttle deliberately on a small machine; the default is every core.
CJOBS="${CJOBS:-$JOBS}"
RJOBS="${RJOBS:-$JOBS}"
TMP="${TMPDIR:-/tmp}"
T0=$(date +%s); el() { echo "[+$(( $(date +%s) - T0 ))s] $*"; }

# ⛔ THE GUARD'S OWN INSTRUMENTS FIRST, every run (kb/Work PB1955). The population reader, the verdict audit and
# the evidence-rule witnesses each carry a self-test that proves its checks can FAIL, and until 2026-10-04 no gate
# ran any of them, so a change to how corpus.tsv is read reached CI unexercised. Seconds, no compiler, no corpus.
el "=== Guard instruments' self-tests (population reader, verdict audit, evidence-rule witnesses) ==="
SELF_RC=0
for selftest in "scripts/guard-population.sh --self-test" "scripts/guard-nist-audit.sh --self-test" \
                "scripts/guard-verify.sh --witnesses"; do
    # shellcheck disable=SC2086 # the entry is a script and its one argument
    if ! bash $selftest > "$TMP/gf_selftest.log" 2>&1; then
        echo "  RED: bash $selftest — its complete log:"; sed 's/^/    /' "$TMP/gf_selftest.log"; SELF_RC=1
    fi
done
[ "$SELF_RC" -eq 0 ] && echo "  all three GREEN"

el "=== Building ($GUARD_COMPILER CLI) ==="
dotnet build "$GUARD_CLI_PROJECT" -v quiet

# ⛔ THE IDENTITY WATCHDOG, AFTER THE BUILD AND BEFORE ANY MEASUREMENT (kb/Work/PB750). A gate that cannot say
# which compiler it drove is not a gate; a gate that drives the wrong one is worse, because it is green.
guard_assert_compiler_identity "$CLI" || exit 1

el "=== NIST: parallel compile + grouped parallel run (JOBS=$JOBS compile=$CJOBS run=$RJOBS) ==="
# Authoritative test list — extracted from guard.sh's NIST_TESTS so the two never drift.
TESTS=$(sed -n '/^NIST_TESTS="/,/^"/p' scripts/guard.sh | grep -vE '^NIST_TESTS=|^"$' | tr '\n' ' ')
# GUARD_TESTS overrides the population with a subset — the SAME override guard.sh honours, so a subset can be
# driven through BOTH guards (guard-verify.sh does exactly that). The audit still applies in full: it takes the
# population as an argument. ⚠ A subset run is NOT the regression gate; it proves the loop, not the corpus.
if [ -n "${GUARD_TESTS:-}" ]; then
    el "  ⚠ GUARD_TESTS override in effect — running a SUBSET, not the regression gate: $GUARD_TESTS"
    TESTS="$GUARD_TESTS"
fi
# The POPULATION, written down so the audit can assert against it rather than against a remembered count.
POP="$TMP/gf_population.txt"
echo "$TESTS" | tr ' ' '\n' | grep . | sort > "$POP"
# ⛔ THE TERMINATES ROWS (kb/Work PB1955), DERIVED from tests/nist/corpus.tsv through scripts/guard-population.sh —
# the same ONE reader guard.sh and guard-nist-audit.sh use. Every other program, a `divergent` one included, is
# compared with its golden (those goldens are the ISO-conforming output, which NistDifferentialTests also locks
# byte-exact). A TERMINATES row's golden records a run that continued past a fatal I-O status nothing covers,
# which WiseOwl COBOL's Annex A.1 item 103 choice ends, so the group runner checks that the run ENDS naming the
# declared exception (GUARD_TERMINATES, `NAME=EC-…`).
. "$(dirname "$0")/guard-population.sh"
GUARD_TERMINATES="$(guard_terminating)" || exit 1
export GUARD_TERMINATES
export GUARD_RUNTIME_DLL

OUT="tests/nist/output"
mkdir -p "$OUT"
# Clear stale compiled output first so a compile FAILURE leaves NO .dll -> the run reports COMPILE FAILED instead
# of silently running a previous run's binary (a stale-dll false green).
# ⚠ `*.g.cs` is a greenfield artefact and must be cleaned with everything else: `cobol` writes the generated
# C# next to each output assembly, where a stale copy would mislead a post-mortem. (`*.cbattr` used to be swept
# here too — the fixed-file-attribute catalog sidecar. It no longer exists: owner decision 2026-09-07,
# kb/Work PB802, put a keyed file's §9.1.6 attributes in the store's own header, so nothing outlives the data
# file the `*.txt` sweep removes.)
rm -f "$OUT"/*.dll "$OUT"/*.runtimeconfig.json "$OUT"/*.txt "$OUT"/*.compile.log "$OUT"/*.compile.rc \
      "$OUT"/*.g.cs
cp "$GUARD_RUNTIME_DLL" "$OUT/"

# (1) Parallel compile — fully independent (distinct .dll/.runtimeconfig.json per test, no shared run state).
# ⛔ THE DIAGNOSTICS ARE KEPT (plan §11 A12b). They used to go to /dev/null and the verdict was inferred from
# whether a .dll existed, so a transient and a genuine syntax error reported IDENTICALLY and the one thing that
# would have settled it was thrown away. Each compile now records its own log and its own exit status; the
# group runner reads both, and refuses to call a compile FAILED without evidence.
export GUARD_CLI_DLL="$CLI"
echo "$TESTS" | tr ' ' '\n' | grep . \
  | xargs -P"$CJOBS" -I {} bash "$ROOT/scripts/guard-compile.sh" {} "tests/nist/programs/{}.cob" "$OUT"

# (2) Group tests for isolation — FROM THE DECLARED CHAIN GRAPH, not from a hand-maintained suite list.
#
# ⛔ WHAT THIS REPLACED, AND WHY (plan §11 A12d + the owner's standing concurrency directive). The previous
# grouping ran six WHOLE SUITES serially in one directory each ("IC RL ST SM IX OB"), a deliberately
# conservative over-approximation of the real producer/consumer coupling. Its cost is the whole leg's floor:
# per-test `dotnet` cold-start × the LONGEST serial group, and IX/ST are ~40 programs each — MEASURED at
# 9 m 14 s for the NIST phase on a 32-core Windows box, against a documented ~3.3 min. Thirty-one cores idled
# while one suite walked forty programs one at a time.
#
# ⭐ THE COUPLING IS ALREADY WRITTEN DOWN, AND ALREADY PROVEN SUFFICIENT. `tests/nist/corpus.tsv` carries a
# `chain-preds` column (DESIGN-test-build-ci §3.2), and the GREENFIELD NIST leg — `NistDifferentialTests`, 349
# programs, green in the 4180-case Conformance run — ALREADY runs every program in its own directory with only
# its DECLARED predecessors. That is a live, passing proof that the declared graph is complete; the whole-suite
# heuristic was insurance against a risk the other leg had already retired.
#
# ⚠ AND ISOLATION IS STRICTLY SAFER THAN ORDERING. guard.sh's prose carries ANTI-dependencies that chain-preds
# cannot express ("RL107A→RL117A … with no other TF022 writer, e.g. RL118A, between them"). Those constraints
# exist ONLY because guard.sh shares ONE directory across all tests. Here each component gets its own mktemp
# dir, so a non-member cannot touch a chain's TF### file at all — the anti-dependency does not merely hold, it
# becomes unstateable. A MISSING pred edge still shows as a consumer DIFF, never a false green, and the audit
# in step (3b) now names it against the manifest. Re-prove with scripts/guard-verify.sh after any change here.
#
# NOTE: do NOT name a variable GROUPS — that collides with bash's built-in $GROUPS array.
SPEC="$TMP/gf_groups.txt"
awk -v corpus="tests/nist/corpus.tsv" -v pop="$POP" '
    function find(x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x] } return x }
    function union(a, b,   ra, rb) { ra = find(a); rb = find(b); if (ra != rb) parent[rb] = ra }
    BEGIN {
        # The population, in the order $POP lists it. A group is emitted in THAT order, so a producer must
        # appear before its consumers or a chain would run backwards.
        while ((getline t < pop) > 0) {
            gsub(/[ \t\r]/, "", t); if (t == "") continue
            n++; pos[t] = n; order[n] = t; parent[t] = t
        }
        close(pop)
        while ((getline line < corpus) > 0) {
            if (line ~ /^#/) continue
            m = split(line, f, "\t")
            if (m < 4 || f[1] == "" || !(f[1] in pos) || f[4] == "-" || f[4] == "") continue
            k = split(f[4], preds, " ")
            for (i = 1; i <= k; i++) if (preds[i] in pos) {
                union(f[1], preds[i])
                # ⛔ THE ORDER IS ASSERTED, NOT ASSUMED. Every declared edge today happens to satisfy
                # pred < consumer in the sorted population, so emitting in that order is correct — but that is
                # LUCK, not construction, and a future chain (say `X99A <- X100A`) would silently run its
                # producer second and diff its consumer for no visible reason. Checking costs one comparison.
                if (pos[preds[i]] > pos[f[1]])
                    print "  ⛔ CHAIN ORDER VIOLATION: " f[1] " needs " preds[i] \
                          ", which the population lists AFTER it — the group would run backwards" > "/dev/stderr"
            }
        }
        close(corpus)
        for (i = 1; i <= n; i++) {
            t = order[i]; r = find(t)
            members[r] = members[r] " " t
            if (!(r in seenroot)) { seenroot[r] = 1; roots[++nr] = r }
        }
        for (j = 1; j <= nr; j++) { g = members[roots[j]]; sub(/^ /, "", g); printf "g%d|%s\n", j, g }
    }' < /dev/null > "$SPEC"
el "=== NIST: $(wc -l < "$SPEC") isolated groups from the declared chain graph (longest: $(awk -F'|' '{n=split($2,a," "); if(n>m){m=n; g=$1} } END{print m" in "g}' "$SPEC")) ==="

# (3) Run groups in parallel; each group is serial-in-its-own-dir via guard-run-group.sh.
# ⛔ THE GROUP RUNNER'S STDERR IS KEPT. It used to go to /dev/null, which is how a group could die outright and
# take its programs' verdict lines with it in total silence — the mechanism behind the SQ135A false green.
RESULTS="$TMP/gf_results.txt"
GROUPERR="$TMP/gf_group_stderr.txt"
# ⛔ A NON-MATCH'S EVIDENCE OUTLIVES ITS GROUP (kb/Work/PB473 item 4). Every group's work dir is a mktemp the
# group's own EXIT trap deletes, so battery #43's one red had nothing left to examine and attributing it took
# hours. Run-scoped, and written to ONLY when a group scores something other than MATCH.
GUARD_FORENSICS="${GUARD_FORENSICS:-$TMP/gf_forensics}"
export GUARD_FORENSICS
rm -rf "$GUARD_FORENSICS"
xargs -P"$RJOBS" -a "$SPEC" -I LINE bash -c \
  'line="LINE"; bash "$ROOT/scripts/guard-run-group.sh" "$ROOT" "${line%%|*}" "${line#*|}"' \
  > "$RESULTS" 2>"$GROUPERR"

el "=== NIST run complete ==="
sort "$RESULTS"
if [ -s "$GROUPERR" ]; then
    echo "=== GROUP RUNNER STDERR (was previously discarded — read it) ==="
    head -40 "$GROUPERR"
fi
# (3b) ⛔ RE-OBSERVE THE LOST RESULTS, SERIALLY — the half that lets step (1)/(3) keep every core.
# A NO-VERDICT means the harness observed NOTHING about that program: a compile that produced neither a .dll
# nor a diagnostic, or a run that was killed before it finished. Nothing was learned, so re-taking the
# measurement discards no result — it is not re-rolling a failed assertion. The retry runs the program's WHOLE
# GROUP (a chain member alone would lose its producers) with no other work in flight, which is the quietest
# machine this run can offer. Anything still unobserved after that stays a NO-VERDICT and the audit fails on it.
# ⛔ A PROGRAM WITH NO LINE AT ALL IS LOST TOO (battery-8, 2026-08-18): SQ201M's single-program group emitted
# neither a verdict nor a byte of stderr, the audit reported missing=1 — and this step never re-observed it,
# because it re-ran only the programs that had SAID "NO-VERDICT". The population minus the observed programs is
# the other half of "lost", and it is re-taken by the same serial mechanism (a clean serial re-run is what a
# lost result needs; the audit still fails on anything unobserved after that).
# ⚠ NO PROCESS SUBSTITUTION HERE. `comm -23 "$POP" <(…)` reads one side through /dev/fd/N, and a short delivery
# there would silently shrink the "lost" set — the same plumbing that manufactured battery #43's false
# regression (kb/Work/PB473). Both sides are real files whose writes have completed.
REPORTED="$TMP/gf_reported.txt"
sed 's/:.*//' "$RESULTS" | sort -u > "$REPORTED"
LOST=$( { grep -E "NO-VERDICT" "$RESULTS" | sed 's/:.*//'; comm -23 "$POP" "$REPORTED"; } | sort -u | tr '\n' ' ')
if [ -n "${LOST// /}" ]; then
    el "=== NIST: re-observing $(echo "$LOST" | wc -w) lost result(s) SERIALLY: $LOST ==="
    RETRY_SPEC="$TMP/gf_retry.txt"; : > "$RETRY_SPEC"
    RETRY_RESULTS="$TMP/gf_retry_results.txt"; : > "$RETRY_RESULTS"
    while IFS= read -r line; do
        gtests=" ${line#*|} "
        for t in $LOST; do
            case "$gtests" in *" $t "*) echo "$line" >> "$RETRY_SPEC"; break ;; esac
        done
    done < "$SPEC"
    # Recompile the affected groups' programs serially, keeping the diagnostics, then re-run each group alone.
    while IFS= read -r line; do
        for t in ${line#*|}; do
            bash "$ROOT/scripts/guard-compile.sh" "$t" "tests/nist/programs/$t.cob" "$OUT"
        done
        # ⚠ stderr goes to the GROUP ERROR file, never into the results: merging it with 2>&1 would inject
        # non-verdict lines into the verdict file, which the audit would then report as STRAY verdicts.
        bash "$ROOT/scripts/guard-run-group.sh" "$ROOT" "${line%%|*}" "${line#*|}" >> "$RETRY_RESULTS" 2>>"$GROUPERR"
    done < "$RETRY_SPEC"
    # Replace the affected programs' lines in $RESULTS with the re-observed ones.
    if [ -s "$RETRY_RESULTS" ]; then
        RETRIED=$(sed 's/:.*//' "$RETRY_RESULTS" | sort -u)
        grep -vE "^($(echo "$RETRIED" | tr '\n' '|' | sed 's/|$//')): " "$RESULTS" > "$TMP/gf_kept.txt" || true
        cat "$TMP/gf_kept.txt" "$RETRY_RESULTS" | sort > "$RESULTS"
        rm -f "$RETRY_RESULTS"
        echo "=== after re-observation ==="; sort "$RESULTS"
    fi
fi

NIST_FAILS=$(grep -cE "REGRESSION!" "$RESULTS" || true); NIST_FAILS=${NIST_FAILS:-0}
NIST_MATCH=$(grep -cE ": MATCH" "$RESULTS" || true); NIST_MATCH=${NIST_MATCH:-0}
NIST_TERM=$(grep -cE ": TERMINATES " "$RESULTS" || true); NIST_TERM=${NIST_TERM:-0}
# ⛔ THE LINE NAMES THE COMPILER (kb/Work/PB750). "NIST: 353 MATCH" was quoted in plan §9 battery records as
# evidence about WiseOwl COBOL for months while it measured the legacy oracle; a verdict that does not say what it
# measured is a verdict a reader will attribute to whatever they were thinking about.
echo "=== NIST (${GUARD_COMPILER}): ${NIST_MATCH} MATCH, ${NIST_TERM} TERMINATES (declared), ${NIST_FAILS} REGRESSION(S) ==="

# ⛔ (3c) THE POPULATION + EXPECTATION AUDIT — plan §11 A12c. The two counts above are NOT a verdict: they are
# computed from the lines that ARRIVED, so losing a program lowers MATCH and still reads as green. The audit
# asserts that every declared program produced exactly one verdict and that it is the verdict the committed
# manifest predicts. It is the half that makes a false GREEN impossible.
bash scripts/guard-nist-audit.sh "$RESULTS" "$POP"
NIST_AUDIT=$?

# (4) Baseline-cleanliness check — ONE implementation, shared with guard.sh (scripts/guard-baselines.sh).
# It used to be a hand-kept copy of guard.sh's loop ("parity with guard.sh"), and the copies drifted the moment
# kb/Work PB436 gave one golden a DECLARED, spec-derived CCVS failure: guard-verdict.sh learned the allowance
# and neither copy did (landing train 36).
bash scripts/guard-baselines.sh tests/nist/valid tests/nist/corpus.tsv
BASE_FAILS=$?

# The verdict says what it measured — `=== ALL GREEN ===` itself is left byte-identical because callers
# (scripts/battery.sh, CI) gate on that exact token.
echo "=== COMPILER UNDER TEST WAS: $GUARD_COMPILER ($CLI) ==="
if [ "$NIST_FAILS" -eq 0 ] && [ "$NIST_AUDIT" -eq 0 ] && [ "$BASE_FAILS" -eq 0 ] && [ "$SELF_RC" -eq 0 ]; then
    echo "=== ALL GREEN ==="
    exit 0
fi
echo "=== FAILURES ($GUARD_COMPILER): nist=$NIST_FAILS audit=$NIST_AUDIT baselines=$BASE_FAILS selftests=$SELF_RC ==="
if [ -d "$GUARD_FORENSICS" ]; then
    echo "=== EVIDENCE for every non-MATCH (report, stdout, stderr, both normalized sides): $GUARD_FORENSICS ==="
    ls "$GUARD_FORENSICS"
fi
exit 1
