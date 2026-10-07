#!/usr/bin/env bash
# linux-gate.sh — run CI's LINUX test legs locally, under WSL, before a push spends a CI run on them.
#
#   From Windows (the normal use), through the PowerShell tool:
#     wsl -d Ubuntu --cd <worktree> -- bash -lc 'bash scripts/linux-gate.sh [--legs unit,characterization,conformance,guard] [--nice]'
#   On a Linux host (a cloud session): bash scripts/linux-gate.sh …
#
# ⛔ WHY IT EXISTS (kb/Work PB1732). The implementer and lander gates ran on Windows only, so CI's Linux jobs were the
# first Linux run a change ever got. On 2026-09-28 train 71b's first push went RED in `Greenfield unit +
# characterization (Linux)`: group F's new GateLegDriftTests.Arm5 planted the Windows root `E:\COBOL-wt\battery87`,
# which `Path.GetFullPath` treats as RELATIVE on Linux. The unit leg reproduces exactly that one red; the CI round
# trip cost ~30 min and a dropped cluster. MANDATORY-PRACTICES I8 (implementers) and L10 (landers) require this
# script, check_practices.py refuses a brief without it, and LinuxGateDriftTests keeps its legs equal to what CI's
# Linux jobs run: every test project they `dotnet test`, and every repository script they `bash` (the `guard` leg,
# CI's guard job, added 2026-10-04 by kb/Work PB1955 after that job's red reached CI unseen).
#
# HOW: a LINUX CLONE OF THE COMMIT, never the Windows tree. The tree's committed HEAD is cloned (`--shared`: it
# borrows the Windows repository's object store read-only, so it copies nothing) into ~/linux-gate/<tree> on the
# Linux filesystem, built there with the Linux SDK, and tested there — the shape of CI's ubuntu jobs.
# Two earlier designs failed, each measured:
#  1. Running the Windows-built binaries: the Conformance tests find their goldens through [CallerFilePath], which a
#     Windows build bakes in as `E:\…` paths, relative on Linux — 363 false reds.
#  2. Running in the Windows tree with GIT_DIR/GIT_WORK_TREE exported so Linux git could read a Windows worktree
#     (whose `.git` file names an `E:/` gitdir): the variables reached EVERY test process, and a test that builds its
#     own scratch repository (gate_slot.py's self-test: git init, commit, worktree add) wrote into the REAL
#     repository instead — `core.worktree` landed in the shared E:\COBOL\.git\config and broke git for every
#     checkout until the owner removed it (2026-09-29, wave 72). So NOTHING here is exported: the only git calls on
#     the Windows repository are this script's own two reads (HEAD, and the uncommitted-change count), with their
#     settings passed inline, and the tests run in a clone that is an ordinary Linux repository.
# The clone builds COMMITTED HEAD: commit before running it. Uncommitted tracked changes are counted and reported.
#
# Output: TestResults/linux-gate/<leg>.log (+ .trx) and build logs in the tree, one `leg <name>: GREEN|RED|NOT RUN`
# line each, and ONE verdict:
#   === LINUX GATE: GREEN (legs …) ===   |   === LINUX GATE: RED (…) ===   |   === LINUX GATE: NOT RUN (…) ===
# Exit 0 only on GREEN.
set -u

legs="unit,characterization,conformance,guard"
nice_prefix=()
while [ $# -gt 0 ]; do
  case "$1" in
    --legs) legs="$2"; shift 2 ;;
    --nice) nice_prefix=(nice -n 10); shift ;;   # an implementer's run yields the cores to the lander's
    *) echo "linux-gate: unknown argument '$1'"; echo "=== LINUX GATE: NOT RUN (bad arguments) ==="; exit 2 ;;
  esac
done

[ -d "$HOME/.dotnet" ] && export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if ! command -v dotnet >/dev/null 2>&1; then
  echo "=== LINUX GATE: NOT RUN (no .NET SDK in this Linux environment — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
py="$(command -v python3 || command -v python)"
if [ -z "$py" ]; then
  echo "=== LINUX GATE: NOT RUN (no python in this Linux environment — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
if [ ! -f CobolSharp.sln ]; then
  echo "=== LINUX GATE: NOT RUN (not at a repository root: $(pwd)) ==="; exit 2
fi
tree="$(pwd)"

# The Windows tree's git directory, for this script's own READS only (never exported). A linked worktree's `.git` is a
# file naming a Windows path; the main checkout's is a directory. /mnt trees trip git's ownership check and Git for
# Windows' core.autocrlf lives in its SYSTEM config, so both are passed inline.
gd="$tree/.git"
if [ -f .git ]; then
  gd="$(sed -n 's/^gitdir: //p' .git | tr -d '\r')"
  case "$gd" in [A-Za-z]:*) gd="$(wslpath -u "$gd")" ;; esac
fi
wgit() { git -c safe.directory='*' -c core.autocrlf=true --git-dir="$gd" --work-tree="$tree" "$@"; }
head="$(wgit rev-parse HEAD 2>/dev/null)"
common="$(wgit rev-parse --path-format=absolute --git-common-dir 2>/dev/null)"
if [ -z "$head" ] || [ -z "$common" ]; then
  echo "=== LINUX GATE: NOT RUN (cannot read this tree's HEAD from Linux: gitdir $gd) ==="; exit 2
fi
dirty="$(wgit status --porcelain --untracked-files=no 2>/dev/null | grep -v ' STATUS.md$' | wc -l)"
# TRIPWIRE (group G's, kb/Work PB1719): nothing below may write to the REAL repository. The clone makes that true by
# construction; this snapshot makes any future breach LOUD instead of silent. (Not the worktree list: other agents add
# and remove worktrees of the shared repository while this runs.)
repo_state() { printf '%s|%s' "$(wgit rev-parse HEAD 2>/dev/null)" "$(wgit config --get core.worktree 2>/dev/null)"; }
state_before="$(repo_state)"

out="$tree/TestResults/linux-gate"
rm -rf "$out"; mkdir -p "$out"
snap="$HOME/linux-gate/$(basename "$tree")"
rm -rf "$snap"
if ! git -c safe.directory='*' clone --quiet --shared --no-checkout "$common" "$snap" > "$out/clone.log" 2>&1 \
   || ! git -C "$snap" -c advice.detachedHead=false checkout --quiet "$head" >> "$out/clone.log" 2>&1; then
  echo "=== LINUX GATE: NOT RUN (could not clone HEAD ${head:0:9} into $snap; see TestResults/linux-gate/clone.log) ==="; exit 2
fi
# The GPL GnuCOBOL corpus is git-ignored, so a clone never has it; ExternalCorpusPopulationDriftTests is RED without
# it by design (PB209). Copy the tree's, or fetch it exactly as CI's Linux jobs do.
if [ -d tests/external/gnucobol ]; then
  mkdir -p "$snap/tests/external" && cp -r tests/external/gnucobol "$snap/tests/external/"
elif ! ( cd "$snap" && pwsh -NoProfile -File scripts/fetch-gnucobol-tests.ps1 ) > "$out/corpus-fetch.log" 2>&1; then
  echo "linux-gate: NOTE — the GnuCOBOL corpus fetch FAILED (TestResults/linux-gate/corpus-fetch.log); the two ExternalCorpusPopulationDriftTests reds in the unit leg are attributable to it"
fi
echo "linux-gate: Linux clone of HEAD ${head:0:9} at $snap"
[ "$dirty" -gt 0 ] && echo "linux-gate: NOTE — $dirty uncommitted tracked change(s) are NOT tested (the clone is HEAD)"

bad=""; ran=""
IFS=',' read -r -a wanted <<< "$legs"
for leg in "${wanted[@]}"; do
  start=$(date +%s)
  case "$leg" in
    unit)             proj="tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj" ;;
    characterization) proj="tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj" ;;
    conformance)      proj="tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj" ;;
    guard)
      # CI's `guard` job (kb/Work PB1955, PB1957 row 40): the NIST suite through the `cobol` CLI from bash, the
      # manifest audit, the baseline check and the guard's own self-tests — the one CI Linux job that runs a SCRIPT
      # rather than `dotnet test`, so it was the one no local gate ran. Train 1013's CI red on
      # PB322's TERMINATES rows was invisible to every local leg. Its scratch is private to this clone (the guard
      # writes fixed file names under TMPDIR, and implementers run this gate concurrently).
      mkdir -p "$snap/.guard-tmp"
      ( cd "$snap" && TMPDIR="$snap/.guard-tmp" "${nice_prefix[@]}" bash scripts/guard-fast.sh ) > "$out/guard.log" 2>&1
      rc=$?
      secs=$(( $(date +%s) - start ))
      summary="$(grep -E '^=== (NIST \(|NIST AUDIT: population|ALL GREEN|FAILURES)' "$out/guard.log" | tr '\n' ' ')"
      if [ $rc -eq 0 ] && grep -qx '=== ALL GREEN ===' "$out/guard.log"; then
        echo "leg guard: GREEN in ${secs}s — $summary"; ran="$ran guard"
      else
        # A red guard prints its WHOLE log less the per-program verdicts that came out as predicted: trim what passed,
        # never what failed (kb/Work PB1573). The audit's findings and every non-matching verdict's evidence stay.
        echo "leg guard: RED (rc=$rc) in ${secs}s — ${summary:-no guard verdict; see TestResults/linux-gate/guard.log}"
        grep -vE '^ *[A-Z][A-Z0-9]+: (MATCH|TERMINATES EC-|NO BASELINE)' "$out/guard.log" | sed 's/^/    /'
        bad="$bad guard"
      fi
      continue ;;
    *) echo "leg $leg: NOT RUN (unknown leg)"; bad="$bad $leg:unknown"; continue ;;
  esac
  ( cd "$snap" && "${nice_prefix[@]}" dotnet build "$proj" -c Debug ) > "$out/$leg-build.log" 2>&1
  brc=$?
  if [ $brc -ne 0 ]; then
    echo "leg $leg: RED — the Linux BUILD failed (rc=$brc); see TestResults/linux-gate/$leg-build.log"
    grep -E ' error ' "$out/$leg-build.log" | sort -u | head -10 | sed 's/^/    /'
    bad="$bad $leg:build"; continue
  fi
  bsecs=$(( $(date +%s) - start ))
  # Scrubbed (kb/Work PB1718): a gate-leg handshake or VSTest* variable inherited from the caller would narrow the run
  # to PART of its assembly while it exits 0. The one statement of that rule is test_population.py.
  ( cd "$snap" && "${nice_prefix[@]}" "$py" "$snap/scripts/test_population.py" scrubbed dotnet test "$proj" \
      --no-build --no-restore --logger "trx;LogFileName=$leg.trx" --results-directory "$out" ) > "$out/$leg.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  summary="$(grep -E '^(Passed!|Failed!)' "$out/$leg.log" | tail -1)"
  if [ $rc -eq 0 ] && grep -qE '^Passed!' "$out/$leg.log"; then
    echo "leg $leg: GREEN in ${secs}s (build ${bsecs}s) — $summary"; ran="$ran $leg"
  else
    echo "leg $leg: RED (rc=$rc) in ${secs}s — ${summary:-no test summary; see TestResults/linux-gate/$leg.log}"
    grep -E '^\s+Failed ' "$out/$leg.log" | head -20 | sed 's/^/    /'
    bad="$bad $leg"
  fi
done

state_after="$(repo_state)"
if [ "$state_after" != "$state_before" ]; then
  echo "repository: RED — something wrote to the REAL repository (HEAD|core.worktree before: $state_before," \
       "after: $state_after). Undo it, then find what escaped the clone."
  bad="$bad repository-written"
fi

if [ -z "$bad" ]; then
  echo "=== LINUX GATE: GREEN (legs$ran; HEAD ${head:0:9}) ==="; exit 0
fi
echo "=== LINUX GATE: RED (failed or not run:$bad; HEAD ${head:0:9}; logs in TestResults/linux-gate/) ==="; exit 1
