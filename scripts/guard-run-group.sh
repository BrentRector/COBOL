#!/bin/bash
# Run ONE NIST group serially in an isolated scratch directory, emitting one "TEST: VERDICT" line per test to
# stdout. Used by scripts/guard-fast.sh to parallelize the NIST suite across cores with airtight isolation.
#
#   $1 = repo root (absolute)
#   $2 = group id (informational)
#   $3 = ordered, space-separated test names that must share a dir + run order (a producer/consumer chain, or a
#        single self-contained test)
#
# Mirrors scripts/guard.sh's per-test run+compare logic EXACTLY (same outfile/print-file/stdout resolution, same
# FAIL*/footer rules) so a verdict here is identical to the serial guard's — only the working directory differs.
# ⭐ THAT IS NO LONGER A PROMISE KEPT BY HAND: both guards call scripts/guard-verdict.sh, the ONE implementation
# of the evidence rules (`feedback_one_rule_one_place`). The prose used to say the two blocks were kept
# "character-for-character in step", and they had already drifted (this file's normalize() carried a `[ -f ]`
# guard guard.sh's did not) while sharing every hole kb/Work/PB473 found.
# The dir starts clean (mktemp) and is NOT cleaned between tests within the group, so a chain's shared TF### data
# files accumulate in declaration order exactly as they do in the serial guard's shared dir.
#
# ⛔ THE EVIDENCE RULES (plan §11 A12b/A12d; DESIGN-test-build-ci.md §3.10) live in scripts/guard-verdict.sh and
# cover all THREE arms — compile, run and compare. Read that file's header for what each one requires and why.
set -u

ROOT="$1"; GROUP="$2"; TESTS="$3"
OUT="$ROOT/tests/nist/output"
# The COBOL runtime the compiled programs bind to — the compiler under test decides which one (PB750):
# WiseOwl COBOL's `Cobol.Net.Runtime` by default, `CobolSharp.Runtime` under COBOLSHARP_LEGACY_DIFFERENTIAL=1.
# guard-fast.sh exports GUARD_RUNTIME_DLL (a repo-relative path); the default keeps a standalone invocation —
# and scripts/guard-verify.sh's witnesses — working without the caller.
RUNTIME="${GUARD_RUNTIME_DLL:-src/Cobol.Net.Runtime/bin/Debug/net10.0/Cobol.Net.Runtime.dll}"
case "$RUNTIME" in /*|[A-Za-z]:[\\/]*) ;; *) RUNTIME="$ROOT/$RUNTIME" ;; esac
# A hung program used to block its whole group forever; the timeout makes that a reported NO-VERDICT instead.
# `timeout` reports 124 on expiry and 137 after the -k KILL. A COBOL program may itself STOP RUN with status
# 124 (§14.9.42.4 GR5), but that ambiguity only arises on a NON-MATCHING run, where declining to score is the
# conservative direction — and it is loud, so it gets read rather than absorbed.
RUN_TIMEOUT="${GUARD_RUN_TIMEOUT:-120}"

# THE evidence rules (compile · run · compare · termination), shared with scripts/guard.sh, and the manifest's
# one shell reader (guard_declared_termination).
. "$(cd "$(dirname "$0")" && pwd)/guard-verdict.sh"
. "$(cd "$(dirname "$0")" && pwd)/guard-population.sh"

WORK="$(mktemp -d)"
# The comparison's scratch — DELIBERATELY OUTSIDE $WORK. The normalized copies must never become inputs to the
# next test in a chain (a group's dir is shared by design and IX216A/IX217A are canaries for any stray file).
CMP="$WORK.cmp"
mkdir -p "$CMP"
trap 'rm -rf "$WORK" "$CMP"' EXIT
cp "$RUNTIME" "$WORK/"
export COBOL_SWITCH_1=ON

for test in $TESTS; do
    dll="$OUT/$test.dll"
    if [ ! -f "$dll" ]; then
        # EVIDENCE RULE (compile): "no .dll" is not a compiler verdict on its own — see guard-verdict.sh.
        clog="$OUT/$test.compile.log"
        crc="$(cat "$OUT/$test.compile.rc" 2>/dev/null || echo "")"
        guard_compile_verdict "$test" "$crc" "$clog"
        echo "$test: $GUARD_VERDICT"
        continue
    fi
    cp "$dll" "$WORK/"
    [ -f "$OUT/$test.runtimeconfig.json" ] && cp "$OUT/$test.runtimeconfig.json" "$WORK/"

    outfile="$(echo "$test" | tr '[:upper:]' '[:lower:]').txt"
    stdoutfile="$WORK/${test}-stdout.txt"
    errfile="$WORK/${test}-stderr.txt"
    datafile="$ROOT/tests/nist/data/$test.dat"
    # EVIDENCE RULE (run): keep the exit status and stderr instead of discarding both with `|| true`. They are
    # what distinguishes "the program produced the wrong answer" from "the program never finished".
    rrc=0
    if [ -f "$datafile" ]; then
        (cd "$WORK" && timeout -k 5 "$RUN_TIMEOUT" dotnet "$test.dll" 2>"$errfile") < "$datafile" > "$stdoutfile" || rrc=$?
    else
        (cd "$WORK" && timeout -k 5 "$RUN_TIMEOUT" dotnet "$test.dll" 2>"$errfile") > "$stdoutfile" || rrc=$?
    fi

    validfile="$ROOT/tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        fc=$(grep -c "FAIL\*" "$WORK/$outfile" 2>/dev/null || true); fc=${fc:-0}
        echo "$test: NO BASELINE (${fc} FAIL* — pending fix)"
        continue
    fi

    # A row corpus.tsv declares TERMINATES (kb/Work PB1955): the run unit's end on the named exception is the
    # observation, not the report. guard-fast.sh exports GUARD_TERMINATES for WiseOwl COBOL only.
    ec="$(guard_declared_termination "$test")"
    if [ -n "$ec" ]; then
        guard_termination_verdict "$test" "$ec" "$rrc" "$errfile" "$RUN_TIMEOUT"
        echo "$test: $GUARD_VERDICT"
        if [ "$GUARD_CLASS" != "match" ]; then
            guard_preserve "$test" "$GUARD_VERDICT" "$WORK/$outfile" "$WORK/print-file.txt" "$stdoutfile" "$errfile"
        fi
        continue
    fi

    # An ISO-re-baselined golden the legacy legitimately diverges from (derived from corpus.tsv by
    # guard-population.sh; guard-fast exports it): compiled and ran above; the diff is expected — never a regression.
    case " ${LEGACY_DIVERGENT:-} " in *" $test "*)
        echo "$test: LEGACY DIVERGENT (golden = ISO-conforming baseline; expected diff)"
        continue ;;
    esac

    # RUN + COMPARE arms, scored by the shared evidence rules (candidates in preference order).
    guard_output_verdict "$test" "$validfile" "$rrc" "$errfile" "$RUN_TIMEOUT" "$CMP" \
        "$WORK/$outfile" "$WORK/print-file.txt" "$stdoutfile"
    echo "$test: $GUARD_VERDICT"
    if [ "$GUARD_CLASS" != "match" ]; then
        # Anything but a MATCH is worth a post-mortem, and this dir dies with the group. Keep the evidence.
        guard_preserve "$test" "$GUARD_VERDICT" \
            "$WORK/$outfile" "$WORK/print-file.txt" "$stdoutfile" "$errfile" \
            "$CMP/expected.norm" "$CMP/actual.norm"
    fi
done
