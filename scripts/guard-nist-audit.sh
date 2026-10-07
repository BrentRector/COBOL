#!/bin/bash
# THE NIST VERDICT AUDIT — plan §11 A12c/A12d; DESIGN-test-build-ci.md §3.10 (the verdict-evidence invariant).
#
# ⛔ WHY THIS EXISTS. On 2026-08-02 `guard-fast.sh` printed `=== NIST: 352 MATCH, 0 REGRESSION(S) ===` and
# `=== ALL GREEN ===` against a 353-MATCH baseline: `SQ135A` produced NO LINE AT ALL — not MATCH, not
# REGRESSION, not DIVERGENT — and because the verdict was computed from the REGRESSION count alone, losing a
# program lowered MATCH and still passed. That is a FALSE GREEN, the direction §12 R-6 names as the real risk
# to v1.0. It was caught only because the previous run's output happened to still be on disk to diff against.
#
# The rule this file enforces: **A MISSING OBSERVATION IS NOT A NEGATIVE OBSERVATION.** A verdict the harness
# declined to produce is a FAILURE, never a silent subtraction — and the expected verdict is derived from a
# COMMITTED MANIFEST and compared BY THE SCRIPT, never by a human against a remembered number.
#
# It is consumed by BOTH guards (serial `guard.sh` and parallel `guard-fast.sh`) so the rule is written down
# exactly once (`feedback_one_rule_one_place`).
#
# Usage:  bash scripts/guard-nist-audit.sh <results-file> <tests-file>
#           <results-file>  one "NAME: VERDICT" line per test (leading whitespace tolerated)
#           <tests-file>    the authoritative population, one test name per line
#         bash scripts/guard-nist-audit.sh --self-test
#           proves every check below can FAIL (feedback_green_gates_arent_evidence) — a check that has never
#           been shown to fail is not evidence.
#
# Exits 0 iff every check passes. Prints "=== NIST AUDIT: ... ===" either way.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
. "$(dirname "$0")/guard-population.sh"   # guard_terminating: the ONE shell reader of a row's kind

# ── The audit, as ONE pass ────────────────────────────────────────────────────────────────────────────────
# ⚠ WRITTEN AS A SINGLE awk PROGRAM ON PURPOSE. The first draft looped in bash and spawned an `awk` plus a
# `grep` per program; over the real 376-program population that took well over a MINUTE — a 50% tax on an
# otherwise ~3.3-minute guard, which is exactly how a correct check gets deleted later for being slow. Three
# files are read once each: the manifest, the population, the results.
#
# tests/nist/corpus.tsv is the SSOT for "which programs are green" (DESIGN-test-build-ci §3.2). Columns:
#   name  suite  status(green|divergent|pending)  chain-preds  golden(valid|none)  note
#
# THE EXPECTED VERDICT, in precedence order:
#   TERMINATES row   -> TERMINATES <EC>  (a `divergent` row whose note begins `TERMINATES EC-…`, kb/Work PB1955: the
#                                         golden records a run that continued past a fatal I-O status nothing
#                                         covers, which WiseOwl COBOL's ISO §9.1.13.1 choice — Annex A.1 item 103 —
#                                         ends; the run must end naming that exception)
#   golden == none   -> NO BASELINE      (no tests/nist/valid/<name>.txt to compare against)
#   otherwise        -> MATCH            (a plain `divergent` row included: its golden IS the ISO-conforming output,
#                                         which WiseOwl COBOL must reproduce byte-exact, as NistDifferentialTests
#                                         does. No row is exempt from the comparison — kb/Work PB750, PB2109.)
# ⚠ `pending` is NOT "no golden": it means the program is not yet in the greenfield NistDifferentialTests set.
# 15 pending programs carry a valid golden and are expected to MATCH. Keying on `status` instead of `golden`
# would have wrongly expected NO BASELINE for all 27 of them.

audit() {
    # ⛔ WHICH `divergent` ROWS ARE WHICH KIND IS ASKED OF THE ONE SHELL READER (scripts/guard-population.sh), the
    # same answer the two runners act on — never re-derived here (kb/Work PB898, PB1955).
    local term
    term="$(guard_terminating "$CORPUS")" || return 2
    awk -v corpus="$CORPUS" -v validdir="$ROOT/tests/nist/valid" -v pop="$2" -v term="$term" '
    # Every verdict word the two runners can emit. An UNRECOGNIZED verdict is itself a finding: a runner that
    # grows a new word without teaching this audit about it would otherwise slip through classified as nothing.
    # A TERMINATES verdict classifies WITH its exception-name, so ending on the wrong exception is UNEXPECTED.
    function actual_class(v,   w) {
        if (v ~ /^TERMINATES EC-/)    { split(v, w, " "); return "TERMINATES " w[2] }
        if (v ~ /^MATCH/)             return "MATCH"
        if (v ~ /^NO BASELINE/)       return "NO BASELINE"
        if (v ~ /^DIFF/)              return "DIFF"
        if (v ~ /^FOOTER/)            return "FOOTER"
        if (v ~ /^COMPILE FAILED/)    return "COMPILE FAILED"
        if (v ~ /NO-VERDICT/)         return "NO-VERDICT"
        return "UNRECOGNIZED"
    }
    function finding(msg) { print "  " msg; findings++ }
    BEGIN {
        FS = "\t"
        nt = split(term, tv, " ");   for (i = 1; i <= nt; i++) { split(tv[i], kv, "="); terminates[kv[1]] = kv[2] }
        while ((getline line < corpus) > 0) {
            if (line ~ /^#/) continue
            n = split(line, f, "\t"); if (n < 5 || f[1] == "") continue
            gold[f[1]] = f[5]
            expect[f[1]] = (f[1] in terminates) ? "TERMINATES " terminates[f[1]] \
                         : (f[5] == "none" ? "NO BASELINE" : "MATCH")
        }
        close(corpus)
        while ((getline line < pop) > 0) { gsub(/[ \t\r]/, "", line); if (line != "") { declared[line] = 1; npop++ } }
        close(pop)
        print "=== NIST AUDIT (cobol) ==="
    }
    # The results file: "NAME: VERDICT", leading whitespace tolerated.
    {
        line = $0
        sub(/^[ \t]+/, "", line); sub(/\r$/, "", line)
        if (match(line, /^[A-Za-z][A-Za-z0-9]*: /) == 0) next
        name = substr(line, 1, RLENGTH - 2)
        verd = substr(line, RLENGTH + 1)
        seen[name]++
        if (!(name in verdict)) verdict[name] = verd
    }
    END {
        # (1) POPULATION — every declared test produced EXACTLY ONE verdict line. This is the check whose
        #     absence let SQ135A vanish into a green run.
        for (t in declared) {
            if (!(t in seen)) {
                finding("NO VERDICT: " t " produced no result line — a missing observation is a FAILURE, not a subtraction")
                n_missing++
            } else if (seen[t] > 1) {
                finding("DUPLICATE VERDICT: " t " reported " seen[t] " times — the run is not a partition of the population")
                n_dup++
            }
        }
        # (1b) The mirror: a verdict for a program nobody asked to run.
        for (t in seen) if (!(t in declared)) finding("STRAY VERDICT: " t " is not in the declared population but reported \x27" verdict[t] "\x27")

        # (2) MANIFEST INTEGRITY — every test has a corpus.tsv row, and that row agrees with what is actually on
        #     disk. Without this cross-check a golden that VANISHES would silently turn its program into an
        #     expected "NO BASELINE" pass, because the runner reads the same directory the expectation would.
        for (t in declared) {
            if (!(t in gold)) { finding("NO MANIFEST ROW: " t " is run by the guard but absent from tests/nist/corpus.tsv"); n_manifest++; continue }
            on_disk = ((getline junk < (validdir "/" t ".txt")) >= 0) ? "valid" : "none"
            close(validdir "/" t ".txt")
            if (gold[t] != on_disk) {
                finding("MANIFEST DRIFT: " t " corpus.tsv says golden=" gold[t] " but tests/nist/valid/" t ".txt is " on_disk)
                n_manifest++
            }
        }

        # (3) EXPECTATION — the verdict CLASS each program produced equals the one the manifest predicts. This
        #     replaces "353 MATCH" as a number a human remembers: per-program, derived from a committed file,
        #     and it self-updates the moment a golden lands.
        # (4) NO-VERDICTS — an evidence-free outcome the runner declined to score. Loud, and never folded into
        #     MATCH or REGRESSION.
        for (t in seen) {
            if (!(t in declared) || !(t in expect)) continue
            a = actual_class(verdict[t])
            if (a == "NO-VERDICT")          { finding("NO-VERDICT: " t " — " verdict[t]); n_nv++ }
            else if (a == "UNRECOGNIZED")   { finding("UNRECOGNIZED VERDICT: " t " — \x27" verdict[t] "\x27 (teach actual_class() this word)"); n_unrec++ }
            else if (a != expect[t])        { finding("UNEXPECTED: " t " expected " expect[t] ", got " verdict[t]); n_unexp++ }
        }

        printf "=== NIST AUDIT: population=%d  missing=%d  duplicate=%d  manifest=%d  unexpected=%d  no-verdict=%d  unrecognized=%d ===\n",
               npop, n_missing+0, n_dup+0, n_manifest+0, n_unexp+0, n_nv+0, n_unrec+0
        if (findings+0 == 0) {
            print "=== NIST AUDIT: CLEAN — every declared program produced exactly the verdict the manifest predicts ==="
            exit 0
        }
        print "=== NIST AUDIT: " findings " FINDING(S) — the run\x27s verdict is NOT evidence until these are explained ==="
        exit 1
    }' "$1"
}

# ── Self-test: prove every check can FAIL ──────────────────────────────────────────────────────────────────
# `feedback_green_gates_arent_evidence` — a passing check proves nothing if it never looked at what changed.
# Each case below is built to break exactly one check; the self-test fails if any of them passes the audit.
self_test() {
    local d rc=0 out
    d="$(mktemp -d)"
    trap 'rm -rf "$d"' RETURN

    # A minimal manifest + population with one program of each expected class, and a golden on disk for the
    # MATCH row so check (2) is satisfied in the control case.
    CORPUS="$d/corpus.tsv"
    printf '# name\tsuite\tstatus\tpreds\tgolden\tnote\n' > "$CORPUS"
    printf 'AA1A\tAA\tgreen\t-\tvalid\t-\n'      >> "$CORPUS"
    printf 'AA2A\tAA\tdivergent\t-\tvalid\t-\n'  >> "$CORPUS"
    printf 'AA3A\tAA\tpending\t-\tnone\t-\n'     >> "$CORPUS"
    printf 'AA5A\tAA\tdivergent\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - status 47 ends the run unit\n' >> "$CORPUS"
    # ⚠ The population list must NOT be named "$d/tests" — ROOT/tests/nist/valid is a DIRECTORY under the same
    # root, and the collision made the first draft's control case fail for a reason unrelated to what it tests.
    printf 'AA1A\nAA2A\nAA3A\nAA5A\n' > "$d/pop"
    ROOT="$d"; mkdir -p "$d/tests/nist/valid"
    : > "$d/tests/nist/valid/AA1A.txt"
    : > "$d/tests/nist/valid/AA2A.txt"
    : > "$d/tests/nist/valid/AA5A.txt"

    # The CONTROL. AA2A (a plain `divergent` row) and AA5A (a TERMINATES row, kb/Work PB1955) are the two kinds of
    # `divergent` row: WiseOwl COBOL must reproduce AA2A's ISO golden exactly and end AA5A's run unit on the
    # declared exception.
    control() {
        printf '  AA1A: MATCH\n  AA2A: MATCH\n  AA3A: NO BASELINE (0 FAIL*)\n'
        printf '  AA5A: TERMINATES EC-I-O-LOGIC-ERROR (run exited 1, as corpus.tsv declares)\n'
    }

    # $1 = case name, $2 = expected rc (0 pass / 1 fail), $3 = the finding text the case MUST produce.
    # ⛔ The third argument is the point. Without it a case can pass because SOME OTHER check fired — which is
    # exactly what happened on the first run of this self-test, where two cases "passed" while the control was
    # already red for an unrelated path bug. A green check that never looked at what it claims to look at is
    # the very defect this whole file exists to close.
    check() {
        out=$(audit "$d/results" "$d/pop" 2>&1); local got=$?
        if [ "$got" -ne "$2" ]; then
            echo "SELF-TEST FAILED: '$1' expected rc=$2, got rc=$got"; echo "$out" | sed 's/^/    /'; rc=1
        elif [ -n "${3:-}" ] && ! printf '%s' "$out" | grep -q "$3"; then
            echo "SELF-TEST FAILED: '$1' returned rc=$got but for the WRONG reason (no '$3' in the report)"
            echo "$out" | sed 's/^/    /'; rc=1
        else
            echo "  ok: $1 (rc=$got)"
        fi
    }

    echo "=== guard-nist-audit --self-test ==="
    control > "$d/results";                                check "control: a clean run passes" 0 "CLEAN"

    # (1) THE SQ135A CASE — the one that produced the false green. A program vanishes from the report.
    control | grep -v AA1A > "$d/results";                  check "missing verdict is caught" 1 "NO VERDICT: AA1A"
    # (1) duplicate — the run is no longer a partition of the population.
    { control; echo "  AA1A: MATCH"; } > "$d/results";      check "duplicate verdict is caught" 1 "DUPLICATE VERDICT: AA1A"
    # (1b) stray — a verdict for something nobody asked to run.
    { control; echo "  ZZ9Z: MATCH"; } > "$d/results";      check "stray verdict is caught" 1 "STRAY VERDICT: ZZ9Z"
    # (2) the golden vanishes from disk while the manifest still claims it — without this cross-check the
    #     program would silently become an expected NO BASELINE and pass forever.
    control > "$d/results"; mv "$d/tests/nist/valid/AA1A.txt" "$d/g.bak"
    check "manifest drift (golden gone) is caught" 1 "MANIFEST DRIFT: AA1A"
    mv "$d/g.bak" "$d/tests/nist/valid/AA1A.txt"
    # (2) a program the guard runs but the manifest has never heard of.
    printf 'AA1A\nAA2A\nAA3A\nAA4A\nAA5A\n' > "$d/pop"
    { control; echo "  AA4A: MATCH"; } > "$d/results";      check "unmanifested program is caught" 1 "NO MANIFEST ROW: AA4A"
    printf 'AA1A\nAA2A\nAA3A\nAA5A\n' > "$d/pop"
    # (3) a real regression.
    control | sed 's/AA1A: MATCH/AA1A: DIFF — REGRESSION!/' > "$d/results"
    check "a DIFF where MATCH was expected is caught" 1 "UNEXPECTED: AA1A expected MATCH"
    # (3) THE SILENT DIRECTION — a program quietly downgrades to NO BASELINE. The old verdict line, which
    #     counted only REGRESSIONs, would have reported this as green.
    control | sed 's/AA1A: MATCH/AA1A: NO BASELINE (0 FAIL*)/' > "$d/results"
    check "a MATCH quietly becoming NO BASELINE is caught" 1 "UNEXPECTED: AA1A expected MATCH"
    # (3) ⭐ THE PB750 ARM. A plain `divergent` row is compared like a green one: its golden is the ISO-conforming
    #     output, so a DIFF there is a regression, and an exemption verdict (the retired legacy runner's word) is
    #     an unrecognized verdict, never a pass.
    control | sed 's/AA2A: MATCH/AA2A: DIFF — REGRESSION!/' > "$d/results"
    check "a DIFF on a divergent program is caught" 1 "UNEXPECTED: AA2A expected MATCH"
    control | sed 's/AA2A: MATCH/AA2A: LEGACY DIVERGENT (golden = ISO baseline)/' > "$d/results"
    check "an exemption verdict on a divergent program is caught" 1 "UNRECOGNIZED VERDICT: AA2A"
    # (3) ⭐ THE PB1955 ARM. A TERMINATES row must END on its declared exception. Each wrong shape below is what a
    #     guard that read every `divergent` row one way — or a runner that ignored the marker — would produce.
    control | sed 's/AA5A: TERMINATES.*/AA5A: RUN NO-VERDICT (produced no report — nothing was observed) — NOT SCORED/' > "$d/results"
    check "cobol scoring a TERMINATES row as a lost report (CI run 37217227958) is caught" 1 "NO-VERDICT: AA5A"
    control | sed 's/AA5A: TERMINATES.*/AA5A: DIFF — REGRESSION! (ran to a normal end; corpus.tsv declares it TERMINATES on EC-I-O-LOGIC-ERROR)/' > "$d/results"
    check "a TERMINATES row that ran to a normal end is caught" 1 "UNEXPECTED: AA5A expected TERMINATES EC-I-O-LOGIC-ERROR"
    control | sed 's/AA5A: TERMINATES EC-I-O-LOGIC-ERROR/AA5A: TERMINATES EC-I-O-PERMANENT-ERROR/' > "$d/results"
    check "ending on a different exception than the declared one is caught" 1 "UNEXPECTED: AA5A expected TERMINATES EC-I-O-LOGIC-ERROR"
    control | sed 's/AA5A: TERMINATES.*/AA5A: MATCH/' > "$d/results"
    check "cobol matching the continuing golden of a TERMINATES row is caught" 1 "UNEXPECTED: AA5A expected TERMINATES"
    # (4) an evidence-free outcome must never be scored in EITHER direction.
    control | sed 's/AA1A: MATCH/AA1A: RUN NO-VERDICT (timeout after 120s, rc=124) — NOT SCORED/' > "$d/results"
    check "a NO-VERDICT is a finding, not a pass" 1 "NO-VERDICT: AA1A"
    # (5) a verdict word the audit does not know — a runner that grows vocabulary must teach this file.
    control | sed 's/AA1A: MATCH/AA1A: PROBABLY FINE/' > "$d/results"
    check "an unrecognized verdict word is caught" 1 "UNRECOGNIZED VERDICT: AA1A"

    if [ "$rc" -eq 0 ]; then
        echo "=== guard-nist-audit --self-test: ALL GREEN (every check proven able to fail) ==="
    else
        echo "=== guard-nist-audit --self-test: FAILED ==="
    fi
    return $rc
}

if [ "${1:-}" = "--self-test" ]; then
    self_test
    exit $?
fi

if [ $# -ne 2 ]; then
    echo "usage: $0 <results-file> <tests-file>   |   $0 --self-test" >&2
    exit 2
fi
CORPUS="$ROOT/tests/nist/corpus.tsv"
audit "$1" "$2"
exit $?
