#!/bin/bash
# Run a NIST suite by program-name prefix: compile + run each program and report status.
# Usage: bash scripts/run-suite.sh <PREFIX>   (e.g. IF, SQ, IC, ST, IX, RL, SM)
#
# Per-test status: COMPILE_FAIL | NO_OUTPUT | RUNTIME(rc) | <N> FAIL* | CLEAN
# A CLEAN test (compiled, ran, produced a CCVS report with 0 FAIL*) is a candidate
# for a tests/nist/valid/ baseline. This script does NOT create baselines.
set -u

PREFIX="${1:?usage: run-suite.sh PREFIX}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

# WHICH COMPILER — the same selection the guards make (scripts/guard-compiler.sh): `cobol` by default, the
# legacy oracle only under COBOLSHARP_LEGACY_DIFFERENTIAL=1. This survey used to hard-code the legacy CLI, so a
# suite triaged with it reported the ORACLE's compile/run health, not WiseOwl COBOL's (kb/Work/PB750).
. "$(dirname "$0")/guard-compiler.sh"
guard_select_compiler
guard_announce_compiler
guard_assert_compiler_identity "$GUARD_CLI_DLL" "$GUARD_COMPILER" || exit 1
export GUARD_CLI_DLL GUARD_COMPILER
mkdir -p tests/nist/output
cp "$GUARD_RUNTIME_DLL" tests/nist/output/ 2>/dev/null

# NIST convention: SWITCH-1 ON, SWITCH-2 OFF
export COBOL_SWITCH_1=ON

total=0; clean=0; failstar=0; compfail=0; noout=0; runfail=0
for f in tests/nist/programs/${PREFIX}*.cob; do
    [ -f "$f" ] || continue
    test=$(basename "$f" .cob)
    total=$((total + 1))

    # The invocation itself lives in scripts/guard-compile.sh — the two compilers spell --nist differently and
    # that rule is written down once (`feedback_one_rule_one_place`).
    bash "$(dirname "$0")/guard-compile.sh" "$test" "$f" "tests/nist/output"
    if [ "$(cat "tests/nist/output/$test.compile.rc" 2>/dev/null)" != "0" ]; then
        echo "  $test: COMPILE_FAIL"; compfail=$((compfail + 1)); continue
    fi

    lc=$(echo "$test" | tr '[:upper:]' '[:lower:]')
    outfile="tests/nist/output/$lc.txt"
    stdoutfile="tests/nist/output/$lc-stdout.txt"
    datafile="tests/nist/data/$test.dat"
    rm -f "$outfile" tests/nist/output/print-file.txt

    rc=0
    if [ -f "$datafile" ]; then
        (cd tests/nist/output && timeout 30 dotnet "$test.dll" >"$lc-stdout.txt" 2>&1) < "$datafile" || rc=$?
    else
        (cd tests/nist/output && timeout 30 dotnet "$test.dll" >"$lc-stdout.txt" 2>&1) || rc=$?
    fi
    if [ "$rc" -eq 124 ]; then echo "  $test: RUNTIME(timeout)"; runfail=$((runfail + 1)); continue; fi

    # Locate the CCVS report: named file, print-file.txt, or stdout capture.
    report=""
    for cand in "$outfile" "tests/nist/output/print-file.txt" "$stdoutfile"; do
        if [ -f "$cand" ] && grep -q "TESTS WERE\|TEST(S) FAILED\|PARAGRAPH" "$cand" 2>/dev/null; then
            report="$cand"; break
        fi
    done
    if [ -z "$report" ]; then echo "  $test: NO_OUTPUT (rc=$rc)"; noout=$((noout + 1)); continue; fi

    fc=$(grep -c "FAIL\*" "$report" 2>/dev/null || true); fc=${fc:-0}
    if [ "$fc" -gt 0 ] 2>/dev/null; then
        echo "  $test: $fc FAIL*"; failstar=$((failstar + 1))
    else
        echo "  $test: CLEAN"; clean=$((clean + 1))
    fi
done

echo "=== $PREFIX: total=$total CLEAN=$clean FAIL*=$failstar COMPILE_FAIL=$compfail NO_OUTPUT=$noout RUNTIME=$runfail ==="
