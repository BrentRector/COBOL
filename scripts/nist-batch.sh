#!/usr/bin/env bash
# NIST batch runner for CobolSharp
# Features:
# - CSV summary
# - Failure log
# - Parallel execution
# - Stop on first failure
# - Group tests by category
# - Run only specified subset (via args)

set -u

ROOT="E:/COBOL"
CSV_SUMMARY="nist_summary.csv"
FAIL_LOG="nist_failures.log"

# Configuration
MAX_JOBS=4          # parallelism
STOP_ON_FIRST_FAIL=0  # 1 = stop on first failure, 0 = keep going

# Categories (extend as needed)
ARITH_TESTS="NC101A NC102A NC103A NC104A NC105A NC106A NC107A NC108A NC109A NC110A NC110M NC111A NC112A NC113A NC114A NC115A"
CTRL_TESTS="NC201A NC202A NC203A NC204A NC205A NC206A NC207A NC208A NC209A NC210A"
IO_TESTS="NC301A NC302A NC303A NC304A NC305A NC306A NC307A NC308A NC309A NC310A"
OTHER_TESTS="NC401A"

# Default full list (can be expanded)
ALL_TESTS="$ARITH_TESTS $CTRL_TESTS $IO_TESTS $OTHER_TESTS"

# Mode selection:
#   ALL      = run ALL_TESTS
#   ARITH    = arithmetic only
#   CTRL     = control-flow only
#   IO       = I/O only
#   CUSTOM   = tests from command-line args
MODE="ALL"

if [ "$#" -gt 0 ]; then
  MODE="CUSTOM"
fi

# Build TESTS list based on MODE
TESTS=""
if [ "$MODE" = "ALL" ]; then
  TESTS="$ALL_TESTS"
elif [ "$MODE" = "ARITH" ]; then
  TESTS="$ARITH_TESTS"
elif [ "$MODE" = "CTRL" ]; then
  TESTS="$CTRL_TESTS"
elif [ "$MODE" = "IO" ]; then
  TESTS="$IO_TESTS"
elif [ "$MODE" = "CUSTOM" ]; then
  for t in "$@"; do
    TESTS="$TESTS $t"
  done
fi

# Init outputs
echo "test,status,message" > "$CSV_SUMMARY"
: > "$FAIL_LOG"

cd "$ROOT" || exit 1

running_jobs=0
first_fail=0

run_one_test() {
  test="$1"

  # Default result
  status="SKIP"
  message="no source"

  if [ ! -f "tests/nist/programs/$test.cob" ]; then
    echo "$test: SKIP (no source)"
    echo "$test,$status,$message" >> "$CSV_SUMMARY"
    return 0
  fi

  echo "$test: compiling..."
  # COBOL.NET (`cobol`), not the legacy oracle — kb/Work/PB750. ⚠ THIS SCRIPT IS NOT A GATE and duplicates
  # what scripts/guard-fast.sh does properly (isolation, chains, the verdict-evidence rules, the audit); it is
  # kept only as an ad-hoc CSV survey. Use guard-fast.sh for anything whose answer will be quoted.
  dotnet run --project src/Cobol.Net.Cli -- --nist "$test" "tests/nist/programs/$test.cob" -o "tests/nist/output/$test.dll" 2>/dev/null
  rc=$?
  if [ "$rc" -ne 0 ]; then
    status="COMPILE_FAILED"
    message="compile failed (rc=$rc)"
    echo "$test: COMPILE FAILED"
    echo "$test,$status,$message" >> "$CSV_SUMMARY"
    echo "$test: $message" >> "$FAIL_LOG"
    return 1
  fi

  echo "$test: running..."
  dotnet "tests/nist/output/$test.dll" 2>/dev/null
  rc=$?
  if [ "$rc" -ne 0 ]; then
    status="RUNTIME_FAILED"
    message="runtime failed (rc=$rc)"
    echo "$test: RUNTIME FAILED"
    echo "$test,$status,$message" >> "$CSV_SUMMARY"
    echo "$test: $message" >> "$FAIL_LOG"
    return 1
  fi

  # Assume output file is TEST.txt (case-insensitive FS on Windows)
  outfile="$test.txt"
  if [ ! -f "$outfile" ]; then
    status="NO_OUTPUT"
    message="no output file"
    echo "$test: NO OUTPUT"
    echo "$test,$status,$message" >> "$CSV_SUMMARY"
    echo "$test: $message" >> "$FAIL_LOG"
    return 1
  fi

  summary_line=""
  while IFS= read -r line; do
    case "$line" in
      *"TESTS WERE"*)
        summary_line="$line"
        ;;
    esac
  done < "$outfile"

  if [ "$summary_line" = "" ]; then
    status="NO_SUMMARY"
    message="no summary line"
    echo "$test: NO SUMMARY"
    echo "$test,$status,$message" >> "$CSV_SUMMARY"
    echo "$test: $message" >> "$FAIL_LOG"
    return 1
  fi

  case "$summary_line" in
    *"TESTS WERE SUCCESSFUL"*|*"TESTS WERE OK"*)
      status="PASS"
      message="$summary_line"
      ;;
    *"TEST(S) FAILED"*|*"TESTS WERE NOT SUCCESSFUL"*)
      status="FAIL"
      message="$summary_line"
      ;;
    *)
      status="UNKNOWN"
      message="$summary_line"
      ;;
  esac

  echo "$test: $message"
  echo "$test,$status,\"$message\"" >> "$CSV_SUMMARY"

  if [ "$status" != "PASS" ]; then
    echo "$test: $message" >> "$FAIL_LOG"
    return 1
  fi

  return 0
}

for test in $TESTS; do
  if [ "$STOP_ON_FIRST_FAIL" -eq 1 ] && [ "$first_fail" -ne 0 ]; then
    echo "Stopping on first failure; skipping remaining tests."
    break
  fi

  # Parallel job control
  while [ "$running_jobs" -ge "$MAX_JOBS" ]; do
    wait -n 2>/dev/null || true
    running_jobs=`expr "$running_jobs" - 1`
  done

  run_one_test "$test" &
  pid=$!
  running_jobs=`expr "$running_jobs" + 1`

  # Track first failure (best-effort: check exit codes after wait)
  (
    wait "$pid"
    rc=$?
    if [ "$rc" -ne 0 ] && [ "$first_fail" -eq 0 ]; then
      first_fail=1
    fi
  ) &
done

# Wait for remaining jobs
wait 2>/dev/null || true

echo "NIST batch complete."
echo "CSV summary: $CSV_SUMMARY"
echo "Failures log: $FAIL_LOG"
