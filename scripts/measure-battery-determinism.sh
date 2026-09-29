#!/bin/bash
# MEASURE the battery's determinism — plan §11 A12 / A12d ask for a DISTRIBUTION, not an anecdote.
#
# ⛔ WHY. Five `guard-fast` runs on ONE unchanged tree gave five outcomes across five different programs in
# three different failure modes, and two full Conformance runs on an identical tree gave 4159/4160 then
# 4160/4160. Until the variance is measured and shown to be gone, "every leg green" is not evidence for a
# conformance claim (§12 R-6). This script is what turns "assume it is fixed" into "here is the distribution".
#
# Usage:  bash scripts/measure-battery-determinism.sh <leg> <runs> [outdir]
#           leg  = guard | conformance | unit
#           runs = how many times to repeat on the UNCHANGED tree
#
# It changes nothing and builds nothing between runs — that is the point. Build once, first, yourself.
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT"
. scripts/python-resolve.sh   # "$PY" — every test run below is SCRUBBED through test_population.py (kb/Work PB1718)
LEG="${1:-guard}"; RUNS="${2:-5}"
OUT="${3:-${TMPDIR:-/tmp}/determinism-$LEG}"
mkdir -p "$OUT"

# The harness observation log is per-run so a retry or non-observation is attributed to the run that had it.
for i in $(seq 1 "$RUNS"); do
    echo "=== $LEG run $i/$RUNS ==="
    export COBOLNET_HARNESS_LOG="$OUT/run$i.observations.log"
    : > "$COBOLNET_HARNESS_LOG"
    case "$LEG" in
        guard)
            bash scripts/guard-fast.sh > "$OUT/run$i.log" 2>&1 ;;
        conformance)
            "$PY" scripts/test_population.py scrubbed dotnet test tests/Cobol.Net.Tests.Conformance --no-build --verbosity quiet \
                > "$OUT/run$i.log" 2>&1 ;;
        unit)
            "$PY" scripts/test_population.py scrubbed dotnet test tests/Cobol.Net.Tests.Unit --no-build --verbosity quiet \
                > "$OUT/run$i.log" 2>&1 ;;
        *) echo "unknown leg: $LEG" >&2; exit 2 ;;
    esac
    echo "  rc=$?  $(grep -cE 'RETRY|NON-OBSERVATION' "$COBOLNET_HARNESS_LOG" 2>/dev/null || echo 0) harness events"
done

echo
echo "=== DISTRIBUTION over $RUNS runs of '$LEG' (nothing rebuilt between them) ==="
case "$LEG" in
    guard)
        for i in $(seq 1 "$RUNS"); do
            printf 'run%-3s %s | %s\n' "$i" \
                "$(grep -E '^=== NIST: ' "$OUT/run$i.log" | tail -1)" \
                "$(grep -E '^=== (ALL GREEN|FAILURES)' "$OUT/run$i.log" | tail -1)"
        done
        echo "--- per-program verdicts that DIFFER between runs (the real signal) ---"
        for i in $(seq 1 "$RUNS"); do
            grep -E '^[A-Z][A-Z0-9]+: ' "$OUT/run$i.log" | sed 's/ (.*//' | sort > "$OUT/run$i.verdicts"
        done
        # A program is unstable if its verdict, or its PRESENCE, is not identical across every run.
        cat "$OUT"/run*.verdicts | awk -F': ' '{print $1}' | sort -u > "$OUT/all_programs.txt"
        unstable=0
        while read -r p; do
            sig=""
            for i in $(seq 1 "$RUNS"); do
                v=$(awk -F': ' -v n="$p" '$1==n{print $2; found=1} END{if(!found) print "<ABSENT>"}' "$OUT/run$i.verdicts")
                sig="$sig|$v"
            done
            if [ "$(printf '%s' "$sig" | tr '|' '\n' | grep . | sort -u | wc -l)" -gt 1 ]; then
                echo "  UNSTABLE  $p  $sig"; unstable=$((unstable+1))
            fi
        done < "$OUT/all_programs.txt"
        echo "  unstable programs: $unstable of $(wc -l < "$OUT/all_programs.txt")" ;;
    *)
        for i in $(seq 1 "$RUNS"); do
            printf 'run%-3s %s\n' "$i" "$(grep -E '^(Passed!|Failed!)' "$OUT/run$i.log" | tail -1)"
        done
        echo "--- named failures per run ---"
        for i in $(seq 1 "$RUNS"); do
            echo "  run$i: $(grep -cE '\[FAIL\]' "$OUT/run$i.log" 2>/dev/null || echo 0) FAIL lines"
            grep -E '\[FAIL\]' "$OUT/run$i.log" 2>/dev/null | sed 's/^/      /' | head -10
        done ;;
esac

echo "--- harness observation events (retries / non-observations) per run ---"
for i in $(seq 1 "$RUNS"); do
    r=$(grep -c 'RETRY' "$OUT/run$i.observations.log" 2>/dev/null || echo 0)
    v=$(grep -c 'RECOVERED' "$OUT/run$i.observations.log" 2>/dev/null || echo 0)
    n=$(grep -c 'NON-OBSERVATION' "$OUT/run$i.observations.log" 2>/dev/null || echo 0)
    echo "  run$i: retried=$r recovered=$v unobserved=$n"
done
echo "=== artifacts in $OUT ==="
