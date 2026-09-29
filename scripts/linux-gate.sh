#!/usr/bin/env bash
# linux-gate.sh — run CI's LINUX test legs locally, under WSL, before a push spends a CI run on them.
#
#   From Windows (the normal use), through the PowerShell tool:
#     wsl -d Ubuntu --cd <worktree> -- bash -lc 'bash scripts/linux-gate.sh [--legs unit,characterization,conformance] [--nice]'
#   On a Linux host (a cloud session): bash scripts/linux-gate.sh …
#
# ⛔ WHY IT EXISTS (kb/Work PB1732). The implementer and lander gates ran on Windows only, so CI's Linux jobs were the
# first Linux run a change ever got. On 2026-09-28 train 71b's first push went RED in `Greenfield unit +
# characterization (Linux)`. Group F's new GateLegDriftTests.Arm5 planted the Windows root `E:\COBOL-wt\battery87`,
# which `Path.GetFullPath` treats as RELATIVE on Linux, so the audit matched 0 of 3 planted offenders. The unit leg
# below reproduces exactly that one red in ~2.5 min; the CI round trip cost ~30 min and a dropped cluster. The
# WSL-repro practice had lived only in a memory note, so no brief carried it and no agent ran it.
# MANDATORY-PRACTICES I8 (implementers) and L10 (landers) now require it, and check_practices.py refuses a brief
# without it. LinuxGateDriftTests keeps the legs equal to the test projects CI's Linux jobs run.
#
# WHAT IT RUNS — CI's Linux test legs (.github/workflows/build-and-test.yml):
#   unit             tests/Cobol.Net.Tests.Unit              (job "Greenfield unit + characterization (Linux)")
#   characterization tests/Cobol.Net.Tests.Characterization  (same job)
#   conformance      tests/Cobol.Net.Tests.Conformance       (job "Greenfield conformance (Linux, sharded)"; the
#                                                             shards' union is the whole assembly, so it runs whole)
# unit and characterization run `--no-build` on the binaries the Windows gate built in this tree: IL is portable, and
# that reproduces CI's Linux unit job exactly (measured on train 71b's red). Run them AFTER that gate, on the same
# commit. A leg whose assembly is not built is NOT RUN, and NOT RUN is never green.
# conformance CANNOT reuse the Windows binaries: its tests locate their goldens through [CallerFilePath], which the
# Windows compiler bakes in as `E:\…` paths that are relative on Linux (measured: 363 false reds, "differential
# golden not baked: /mnt/e/…/E:\COBOL\…"). So it BUILDS the committed HEAD on Linux, as CI does, in a snapshot on
# the Linux filesystem (`git archive HEAD` into ~/linux-gate/<tree>, plus the git-ignored GnuCOBOL corpus). The
# Windows tree's bin/obj are never touched. Uncommitted changes are NOT in that snapshot, and the leg says so.
#
# Output: TestResults/linux-gate/<leg>.log (+ .trx), one `leg <name>: GREEN|RED|NOT RUN` line each, and ONE verdict:
#   === LINUX GATE: GREEN (legs …) ===   |   === LINUX GATE: RED (…) ===   |   === LINUX GATE: NOT RUN (…) ===
# Exit 0 only on GREEN.
set -u

legs="unit,characterization,conformance"
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
  echo "linux-gate: no dotnet on PATH — run scripts/wsl/setup-wsl.sh once"
  echo "=== LINUX GATE: NOT RUN (no .NET SDK in this Linux environment) ==="; exit 2
fi
py="$(command -v python3 || command -v python)"
if [ -z "$py" ]; then
  echo "=== LINUX GATE: NOT RUN (no python in this Linux environment — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
if [ ! -f CobolSharp.sln ]; then
  echo "=== LINUX GATE: NOT RUN (not at a repository root: $(pwd)) ==="; exit 2
fi
tree="$(pwd)"

# A tree checked out by WINDOWS git is not readable by Linux git as it stands, and several tests shell out to git:
#  - a linked worktree's `.git` is a FILE naming a Windows path (`gitdir: E:/COBOL/.git/worktrees/<name>`), which
#    Linux git cannot open ("fatal: not a git repository");
#  - the tree is owned by another uid under /mnt, which git refuses as "dubious ownership".
# So this process (and every test host it starts) gets GIT_DIR translated to its Linux path, and safe.directory,
# through git's environment config. Nothing on disk changes, and a native Linux clone needs none of it.
if [ -f .git ] && command -v wslpath >/dev/null 2>&1; then
  gitdir="$(sed -n 's/^gitdir: //p' .git | tr -d '\r')"
  case "$gitdir" in
    [A-Za-z]:*) export GIT_DIR="$(wslpath -u "$gitdir")" GIT_WORK_TREE="$tree" ;;
  esac
fi
# Windows git checks this tree out with core.autocrlf=true, set in Git for Windows' SYSTEM config, which Linux git never
# reads. Without it every CRLF-checked-out file reads as modified (measured: 9,543 "changes" on a clean tree).
if [ -n "${WSL_DISTRO_NAME:-}" ]; then
  export GIT_CONFIG_COUNT=2 GIT_CONFIG_KEY_0=safe.directory GIT_CONFIG_VALUE_0="*" \
         GIT_CONFIG_KEY_1=core.autocrlf GIT_CONFIG_VALUE_1=true
fi
if ! git ls-files --error-unmatch CobolSharp.sln >/dev/null 2>&1; then
  echo "=== LINUX GATE: NOT RUN (git cannot read this tree from Linux; tests that list tracked files would false-red) ==="
  exit 2
fi

out="$tree/TestResults/linux-gate"
rm -rf "$out"; mkdir -p "$out"
bad=""; ran=""

# run_leg <leg> <dir to run in> <project> — one dotnet test, --no-build, results into $out.
run_leg() {
  local leg="$1" dir="$2" proj="$3" start rc secs summary
  start=$(date +%s)
  # Scrubbed (kb/Work PB1718): a gate-leg handshake or VSTest* variable inherited from the caller would narrow the
  # run to PART of its assembly while it exits 0. The one statement of that rule is test_population.py.
  ( cd "$dir" && "${nice_prefix[@]}" "$py" "$tree/scripts/test_population.py" scrubbed dotnet test "$proj" \
      --no-build --no-restore --logger "trx;LogFileName=$leg.trx" --results-directory "$out" ) > "$out/$leg.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  summary="$(grep -E '^(Passed!|Failed!)' "$out/$leg.log" | tail -1)"
  if [ $rc -eq 0 ] && grep -qE '^Passed!' "$out/$leg.log"; then
    echo "leg $leg: GREEN in ${secs}s — $summary"; ran="$ran $leg"
  else
    echo "leg $leg: RED (rc=$rc) in ${secs}s — ${summary:-no test summary; see $out/$leg.log}"
    grep -E '^\s+Failed ' "$out/$leg.log" | head -20 | sed 's/^/    /'
    bad="$bad $leg"
  fi
}

IFS=',' read -r -a wanted <<< "$legs"
for leg in "${wanted[@]}"; do
  case "$leg" in
    unit)             proj="tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj" ;;
    characterization) proj="tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj" ;;
    conformance)      proj="tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj" ;;
    *) echo "leg $leg: NOT RUN (unknown leg)"; bad="$bad $leg:unknown"; continue ;;
  esac
  if [ "$leg" != conformance ]; then
    name="$(basename "$proj" .csproj)"
    if ! ls "$(dirname "$proj")"/bin/*/*/"$name".dll >/dev/null 2>&1; then
      echo "leg $leg: NOT RUN ($name.dll is not built in this tree — run the Windows gate first)"
      bad="$bad $leg:not-built"; continue
    fi
    run_leg "$leg" "$tree" "$proj"
    continue
  fi
  # conformance: a Linux build of the committed HEAD, in a snapshot on the Linux filesystem.
  snap="$HOME/linux-gate/$(basename "$tree")"
  dirty="$(git status --porcelain --untracked-files=no | grep -v ' STATUS.md$' | wc -l)"
  [ "$dirty" -gt 0 ] && echo "leg conformance: NOTE — $dirty uncommitted tracked change(s) are NOT in the Linux build (it builds HEAD)"
  rm -rf "$snap"; mkdir -p "$snap"
  if ! git archive HEAD | tar -x -C "$snap"; then
    echo "leg conformance: NOT RUN (git archive of HEAD failed)"; bad="$bad conformance:snapshot"; continue
  fi
  [ -d tests/external/gnucobol ] && mkdir -p "$snap/tests/external" && cp -r tests/external/gnucobol "$snap/tests/external/"
  start=$(date +%s)
  ( cd "$snap" && "${nice_prefix[@]}" dotnet build "$proj" -c Debug ) > "$out/conformance-build.log" 2>&1
  brc=$?
  if [ $brc -ne 0 ]; then
    echo "leg conformance: RED — the Linux BUILD failed (rc=$brc) in $(( $(date +%s) - start ))s; see $out/conformance-build.log"
    grep -E ' error ' "$out/conformance-build.log" | sort -u | head -10 | sed 's/^/    /'
    bad="$bad conformance:build"; continue
  fi
  echo "leg conformance: Linux build of HEAD $(git rev-parse --short HEAD) in $(( $(date +%s) - start ))s"
  run_leg conformance "$snap" "$proj"
done

if [ -z "$bad" ]; then
  echo "=== LINUX GATE: GREEN (legs$ran) ==="; exit 0
fi
echo "=== LINUX GATE: RED (failed or not run:$bad; logs in TestResults/linux-gate/) ==="; exit 1
