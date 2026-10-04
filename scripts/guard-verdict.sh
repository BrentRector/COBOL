#!/bin/bash
# THE NIST VERDICT-EVIDENCE RULES — ONE implementation, SOURCED by both guards. Not executable on its own.
#
#   . "$(dirname "$0")/guard-verdict.sh"
#
# Callers: scripts/guard.sh (serial authority) and scripts/guard-run-group.sh (the parallel guard's per-group
# worker). They differ in WHERE the files live and HOW a verdict is recorded; the rules that turn evidence into
# a verdict are identical, so they live here once (`feedback_one_rule_one_place`).
#
# ⛔ THE EVIDENCE RULES (plan §11 A12b; DESIGN-test-build-ci.md §3.10). A verdict about the COMPILER is produced
# ONLY from an observation the harness actually made. The defect this closes is one sentence:
# A MISSING OBSERVATION WAS BEING READ AS A NEGATIVE OBSERVATION.
#   · COMPILE — a missing .dll is not itself a verdict. FAILED needs a non-zero rc AND diagnostic text; anything
#     else is COMPILE NO-VERDICT.
#   · RUN — a program that was killed, timed out, or exited non-zero with NO diagnostic did not finish. Its
#     report is a lost result, not a wrong answer: RUN NO-VERDICT.
#   · COMPARE — ⭐ THE THIRD ARM, added 2026-09-04 (kb/Work/PB473). The compare used to read EVERY non-zero
#     `diff` exit as "not this file", so four different ways of failing to OBSERVE anything — an absent report,
#     `diff` itself failing (exit 2), a normalization that did not complete, a process substitution delivering
#     short data — all arrived at `DIFF — REGRESSION!` beside the one case that is a genuine wrong answer.
#     Battery #43's only red, IF141A, was manufactured exactly this way: the report was byte-identical to its
#     golden and `cmp` proved it in the same shell the three `diff <(normalize …)` comparisons had just failed
#     in. Reproduced 1 in 640 under the battery's own -P32 fan-out.
# A NO-VERDICT is never MATCH and never REGRESSION — it is an explicit statement that the run learned nothing.
# scripts/guard-nist-audit.sh treats it as a finding, and scripts/guard-fast.sh step 3b RE-OBSERVES it serially,
# which is the one and only retry mechanism (re-taking a measurement that was never made discards no result).
#
# ⭐ AND THE COMPARE'S ROOT CAUSE IS REMOVED, NOT MERELY DETECTED (CLAUDE.md rule 4). The old compare read both
# sides through PROCESS SUBSTITUTIONS — `diff <(normalize A) <(normalize B)` — whose data is delivered by a
# forked child over /dev/fd/N. Under the battery's concurrent `dotnet` load that delivery can come up short, and
# a short read is indistinguishable from a difference: `diff` prints NOTHING on stderr and reports a difference
# from input that has none. The compare alone, hammered 3000 times at -P32 with no other load, never failed;
# the same loop under the battery's load failed 1 in 640. So the normalized text is now MATERIALIZED INTO REAL
# FILES whose writes complete before `diff` opens them, and the pipeline's own exit status is checked. The
# exit-status discrimination below is defence in depth on top of that, not the fix.
#
# Every function sets shell options FUNCTION-LOCALLY (`local -`) and never aborts its caller: guard.sh runs
# under `set -e`, and a scoring routine that can kill the guard is not a scoring routine.
#
# Outputs (globals, so nothing runs in a subshell and the caller keeps its own recording/counting):
#   GUARD_VERDICT  the verdict TEXT, without the "NAME: " prefix — exactly what both guards print
#   GUARD_CLASS    match | regression | no-verdict — what the caller scores it as (a declared TERMINATES that
#                  happened is a `match`: it is the outcome the manifest predicts)
#   GUARD_ACTUAL   the candidate file that matched (empty unless GUARD_CLASS came from a match)
# Vocabulary note: any new verdict WORD must be taught to scripts/guard-nist-audit.sh (actual_class) and to
# scripts/guard-verify.sh (VERDICT_WORDS); both refuse to classify a word they do not know, loudly.

# `local -` (function-local shell options) needs bash 4.4+. Fail LOUDLY rather than mis-score under `set -e`.
if [ "${BASH_VERSINFO[0]:-0}" -lt 4 ] || { [ "${BASH_VERSINFO[0]:-0}" -eq 4 ] && [ "${BASH_VERSINFO[1]:-0}" -lt 4 ]; }; then
    echo "guard-verdict.sh: bash 4.4+ required (found ${BASH_VERSION:-unknown}) — 'local -' is load-bearing" >&2
    exit 90
fi

# Where a non-MATCH's evidence is kept. Both guards set a RUN-SCOPED directory; this default is for a standalone
# guard-run-group.sh invocation. Nothing is written here on a green run.
GUARD_FORENSICS="${GUARD_FORENSICS:-${TMPDIR:-/tmp}/nist-forensics}"

# One line of a log/diagnostic file, flattened for a verdict line. A verdict line is PARSED (by the audit and by
# guard-verify), so it must stay one line and stay short.
guard_first_line() {
    local -
    set +e
    [ -f "$1" ] || return 0
    head -1 "$1" 2>/dev/null | tr -d '\r' | cut -c1-110
    return 0
}

# The comparison normalization, written down ONCE.
#   tr -d '\r' FIRST (CRLF golden vs LF program output on Linux); it must precede 's/ *$//', which is a no-op
#   while a trailing \r remains. Idempotent on Windows. Then the time-dependent COMPUTED= values are masked.
# $1 = source file, $2 = destination file. Returns 0 only if the source existed AND the whole pipeline
# completed — a normalization that did not finish is a LOST observation, never an empty one.
guard_normalize_into() {
    local -
    set +e -o pipefail
    [ -f "$1" ] || return 2
    tr -d '\r' < "$1" | sed 's/ *$//; s/COMPUTED=  [0-9]*/COMPUTED=  XXXXXXXXX/' > "$2"
}

# COMPILE ARM.  guard_compile_verdict TEST CRC CLOG
# Called when the .dll the compile claimed to produce is NOT there. CRC may be empty (guard-run-group reads it
# from a file that a lost compile never wrote); guard.sh always has one.
guard_compile_verdict() {
    local -
    set +e
    local test="$1" crc="$2" clog="$3"
    GUARD_VERDICT=""; GUARD_CLASS=""; GUARD_ACTUAL=""
    if [ -n "$crc" ] && [ "$crc" != "0" ] && [ -s "$clog" ]; then
        GUARD_CLASS="regression"
        GUARD_VERDICT="COMPILE FAILED — REGRESSION! (rc=$crc: $(guard_first_line "$clog"))"
    elif [ -z "$crc" ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="COMPILE NO-VERDICT (the compile never reported an exit status) — NOT SCORED"
    elif [ "$crc" = "0" ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="COMPILE NO-VERDICT (rc=0 but no .dll — contradictory evidence) — NOT SCORED"
    else
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="COMPILE NO-VERDICT (rc=$crc with NO diagnostic — a rejection with no reason is a lost result) — NOT SCORED"
    fi
    return 0
}

# RUN + COMPARE ARMS.
#   guard_output_verdict TEST VALIDFILE RRC ERRFILE RUN_TIMEOUT CMPDIR CANDIDATE...
# CMPDIR is a writable scratch directory OUTSIDE the program's run directory (the normalized copies must not
# become inputs to the next test in a chain). CANDIDATEs are tried in preference order.
guard_output_verdict() {
    local -
    set +e
    local test="$1" validfile="$2" rrc="$3" errfile="$4" timeout_s="$5" cmpdir="$6"
    shift 6
    GUARD_VERDICT=""; GUARD_CLASS=""; GUARD_ACTUAL=""
    local exp="$cmpdir/expected.norm" act="$cmpdir/actual.norm"
    local cand rc nrc derr note="" trouble=0 content=0

    mkdir -p "$cmpdir" 2>/dev/null
    guard_normalize_into "$validfile" "$exp"; nrc=$?
    if [ "$nrc" -ne 0 ]; then
        # The golden is committed and the baseline check proves it non-empty, so failing to read it is a
        # harness fault — never evidence about the compiler.
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="COMPARE NO-VERDICT (the golden could not be normalized, rc=$nrc — absent golden, or an unwritable compare scratch) — NOT SCORED"
        echo "  ⚠ $test: cannot normalize golden $validfile into $cmpdir (rc=$nrc)" >&2
        return 0
    fi

    for cand in "$@"; do
        # A candidate that does not exist is not a difference — most tests legitimately write only one of the
        # three. Whether ANY candidate had bytes is what separates "wrong answer" from "no answer".
        [ -f "$cand" ] || continue
        if [ -s "$cand" ]; then content=1; fi
        guard_normalize_into "$cand" "$act"; nrc=$?
        if [ "$nrc" -ne 0 ]; then
            trouble=1; note="normalizing $(basename "$cand") did not complete (rc=$nrc)"
            echo "  ⚠ $test: $note" >&2
            continue
        fi
        # diff's own exit status, read explicitly: 0 = same, 1 = a real content difference, ANYTHING ELSE =
        # trouble (it could not tell), and its stderr is KEPT instead of going to /dev/null.
        derr="$(diff "$exp" "$act" 2>&1 >/dev/null)"; rc=$?
        if [ "$rc" -eq 0 ]; then GUARD_ACTUAL="$cand"; break; fi
        if [ "$rc" -ne 1 ]; then
            derr="$(printf '%s' "$derr" | tr '\n\r' '  ' | cut -c1-110)"
            trouble=1; note="diff exited $rc on $(basename "$cand")${derr:+ — }$derr"
            echo "  ⚠ $test: $note" >&2
        fi
    done

    if [ -n "$GUARD_ACTUAL" ]; then
        # A FAIL* detail line is a real failure; the AUTHORITATIVE signal is the report footer total, which can
        # be non-zero with no FAIL* line at all (the IX108A false green). "NO TEST(S) FAILED" is not [0-9]+.
        # ⛔ THE ALLOWANCE COMES FROM THE GOLDEN, NOT FROM THE CONSTANT 0. This arm is reached only after a
        # candidate matched the golden byte-for-byte, so the output's footer IS the golden's footer: comparing
        # it to 0 was auditing the BASELINE, not the run. One golden legitimately carries a failure — NC201A,
        # whose PFM-TEST-F4-23 asserts a value §14.9.28.4 GR13 e) 2 contradicts (kb/Work PB436) — and scoring
        # that as a regression would turn a CONFORMING compiler red. Which goldens may carry one is not decided
        # here: tests/nist/corpus.tsv declares them CCVS-DEFECT and CorpusManifestTests asserts that the
        # declared set equals the goldens that actually carry a footer, in BOTH directions — so a golden cannot
        # quietly acquire a failure, and this comparison cannot quietly stop noticing one.
        local fc ff ffexp
        fc=$(grep -c "FAIL\*" "$GUARD_ACTUAL" 2>/dev/null); fc=${fc:-0}
        ff=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$GUARD_ACTUAL" 2>/dev/null | grep -oE "^[0-9]+" | head -1); ff=${ff:-0}
        ffexp=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$validfile" 2>/dev/null | grep -oE "^[0-9]+" | head -1); ffexp=${ffexp:-0}
        if [ "$ff" -gt "$ffexp" ] 2>/dev/null; then
            GUARD_CLASS="regression"; GUARD_VERDICT="FOOTER ${ff} TEST(S) FAILED, golden allows ${ffexp} — REGRESSION!"
        elif [ "$fc" -gt 0 ] 2>/dev/null; then
            GUARD_CLASS="match"; GUARD_VERDICT="MATCH (${fc} FAIL*, golden allows ${ffexp})"
        else
            GUARD_CLASS="match"; GUARD_VERDICT="MATCH"
        fi
        return 0
    fi

    # NOTHING MATCHED. Before calling that a REGRESSION, ask what was actually observed. The order is
    # strongest-evidence-first: a process that never finished explains everything after it.
    if [ "$rrc" -eq 124 ] || [ "$rrc" -eq 137 ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (timeout/kill after ${timeout_s}s, rc=$rrc) — NOT SCORED"
    elif [ "$rrc" -gt 128 ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (killed by signal $((rrc - 128))) — NOT SCORED"
    elif [ "$trouble" -ne 0 ]; then
        # The COMPARE arm's own rule: the harness could not carry out the comparison, so it observed nothing.
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="COMPARE NO-VERDICT (the comparison did not complete: $note) — NOT SCORED"
    elif [ "$content" -eq 0 ]; then
        # No candidate had a single byte. A wrong answer HAS bytes; this is a lost result.
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (produced no report — nothing was observed) — NOT SCORED"
    elif [ "$rrc" -ne 0 ] && [ ! -s "$errfile" ]; then
        # The RUN arm aligned with the COMPILE arm: a failure with no reason is a lost result on both or on
        # neither. This is the shape battery #43's iteration produced (rc=1, empty stderr, perfect report).
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (rc=$rrc with NO diagnostic — a failure with no reason is a lost result) — NOT SCORED"
    elif [ "$rrc" -ne 0 ]; then
        GUARD_CLASS="regression"
        GUARD_VERDICT="DIFF — REGRESSION! (run exited $rrc: $(guard_first_line "$errfile"))"
    else
        # The run finished, it produced bytes, and the comparison completed and said they differ. THAT is a
        # wrong answer. (A TRUNCATED report with rc=0 belongs here: the process ran to completion, so short
        # output is an answer, not a lost observation — and it is indistinguishable from one by design.)
        GUARD_CLASS="regression"
        GUARD_VERDICT="DIFF — REGRESSION!"
    fi
    return 0
}

# THE TERMINATION ARM (kb/Work PB1955).  guard_termination_verdict TEST EC RRC ERRFILE RUN_TIMEOUT
# For a row tests/nist/corpus.tsv declares `TERMINATES EC-…` (read through scripts/guard-population.sh), the
# observation that counts is the run unit's END, not its report: the golden records a run that continued past a
# fatal I-O status nothing covers, and WiseOwl COBOL's documented choice under ISO §9.1.13.1 ends the run unit
# there (Annex A.1 item 103). So the run must exit non-zero NAMING the declared exception — the same two facts
# NistDifferentialTests asserts for the row. The evidence rules hold here as on every other arm: a timeout, a
# signal, or a non-zero exit with nothing on stderr observed nothing, so none of them is scored.
guard_termination_verdict() {
    local -
    set +e
    local test="$1" ec="$2" rrc="$3" errfile="$4" timeout_s="$5"
    GUARD_VERDICT=""; GUARD_CLASS=""; GUARD_ACTUAL=""
    if [ "$rrc" -eq 124 ] || [ "$rrc" -eq 137 ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (timeout/kill after ${timeout_s}s, rc=$rrc) — NOT SCORED"
    elif [ "$rrc" -gt 128 ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (killed by signal $((rrc - 128))) — NOT SCORED"
    elif [ "$rrc" -eq 0 ]; then
        GUARD_CLASS="regression"
        GUARD_VERDICT="DIFF — REGRESSION! (ran to a normal end; corpus.tsv declares it TERMINATES on $ec)"
    elif [ ! -s "$errfile" ]; then
        GUARD_CLASS="no-verdict"
        GUARD_VERDICT="RUN NO-VERDICT (rc=$rrc with NO diagnostic — a failure with no reason is a lost result) — NOT SCORED"
    elif grep -qE "(^|[^A-Z0-9-])${ec}([^A-Z0-9-]|\$)" "$errfile"; then
        # Whole-name match: EC-I-O must not be satisfied by EC-I-O-LOGIC-ERROR, nor the reverse.
        GUARD_CLASS="match"
        GUARD_VERDICT="TERMINATES $ec (run exited $rrc, as corpus.tsv declares)"
    else
        GUARD_CLASS="regression"
        GUARD_VERDICT="DIFF — REGRESSION! (exited $rrc, but not on the declared $ec: $(guard_first_line "$errfile"))"
    fi
    return 0
}

# KEEP THE EVIDENCE on any non-MATCH (kb/Work/PB473 item 4). Attributing battery #43's red cost hours only
# because the group's working directory had already been deleted by its own EXIT trap; the one artefact that
# made attribution possible at all survived by accident. Nothing is copied on a green run.
#   guard_preserve TEST VERDICT FILE...
guard_preserve() {
    local -
    set +e
    local test="$1" verdict="$2"; shift 2
    local dest="$GUARD_FORENSICS/$test" f
    mkdir -p "$dest" 2>/dev/null || return 0
    printf '%s\n' "$verdict" > "$dest/VERDICT.txt" 2>/dev/null
    for f in "$@"; do
        if [ -f "$f" ]; then cp -p "$f" "$dest/" 2>/dev/null; fi
    done
    echo "  ⚑ $test: evidence preserved in $dest" >&2
    return 0
}
