#!/usr/bin/env bash
# linux-gate.sh — run CI's LINUX test legs locally, under WSL, before a push spends a CI run on them.
#
#   From any shell, at the tree's root:  bash scripts/linux-gate.sh [--legs unit,characterization,conformance,guard] [--nice]
#   On a host that is not Linux (Git Bash, PowerShell's bash) it runs ITSELF under WSL, in the same tree with the same
#   arguments, and its verdict is that run's (run_on_linux, kb/Work PB2999).
#   python scripts/self_tests.py runs `bash scripts/linux-gate.sh --self-test` (the host hand-off, with stand-ins).
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
#     the Windows repository are this script's own READS (`wgit`: HEAD, the git directory, the uncommitted-change
#     count, the tripwire), by the tree's own git and without optional locks (kb/Work PB2880), and the tests run in a
#     clone that is an ordinary Linux repository.
# The clone builds COMMITTED HEAD: commit before running it. Uncommitted tracked changes are counted and reported.
#
# Output: TestResults/linux-gate/<leg>.log (+ .trx) and build logs in the tree, one `leg <name>: GREEN|RED|NOT RUN`
# line each, and ONE verdict:
#   === LINUX GATE: GREEN (legs …) ===   |   === LINUX GATE: RED (…) ===   |   === LINUX GATE: NOT RUN (…) ===
# Exit 0 only on GREEN.
set -u

# ⛔ THE GATE PUTS ITSELF ON ITS HOST (kb/Work PB2999; owner 2026-10-11: "Why not simply require it to invoke using the
# proper host rather than an after the fact check that the correct host was invoked"). Started from Git Bash — the Bash
# tool every session has, and the bare command the gate skill gives — this script ran on WINDOWS: it cloned into the
# Windows home, built with the Windows SDK and printed `LINUX GATE: RED (... build ...)`, a verdict about no Linux leg at
# all. So on a host that is not Linux it runs itself under WSL (`-d Ubuntu`, the distribution scripts/wsl/ provisions),
# in this tree with these arguments, and exits with that run's status; the one refusal left is a host with no WSL.
# MSYS_NO_PATHCONV: Git Bash rewrites an argument that looks like a POSIX path before a Windows program sees it
# (`/mnt/e/...` became `C:/Program Files/Git/mnt/e/...`, DEVLOG), and the hand-off passes the arguments verbatim.
# `--exec`, not `--`: after `--`, wsl.exe hands the command line to the distribution's default shell, which parses it
# again — measured 2026-10-11, `-- bash -lc '... "$@"' linux-gate --legs unit --nice` reached bash as `[/bin/bash][]`,
# every argument lost; `--exec` gives bash the arguments as they are: `[linux-gate][--legs][unit][--nice]`.
run_on_linux() {  # the script's own arguments
  local host here
  host="$(uname -s)"
  [ "$host" = "Linux" ] && return 0
  if ! command -v wsl.exe >/dev/null 2>&1; then
    echo "=== LINUX GATE: NOT RUN (this host is $host, not Linux, and has no WSL to run the gate on) ==="; exit 2
  fi
  here="$(cygpath -w "$(pwd)" 2>/dev/null || pwd)"
  echo "linux-gate: this host is $host, not Linux: running the gate under WSL (Ubuntu) in $here"
  MSYS_NO_PATHCONV=1 wsl.exe -d Ubuntu --cd "$here" --exec bash -lc 'bash scripts/linux-gate.sh "$@"' linux-gate "$@"
  exit $?
}

# Every --legs name ($legs) is one of the selectable legs, into `wanted`, checked before anything is cloned or built: an
# unknown name used to reach the leg dispatch, where `selftests` (which always runs) wrote "unknown leg" over the real
# leg's result file and was reported twice (2026-10-11, PB2999's first live run).
check_legs() {
  local leg
  IFS=',' read -r -a wanted <<< "$legs"
  for leg in "${wanted[@]}"; do
    case "$leg" in
      unit|characterization|conformance|guard) ;;
      *) echo "linux-gate: unknown leg '$leg' (the selectable legs are unit, characterization, conformance and guard;" \
              "selftests always runs, and selftests-built with unit or conformance)"
         echo "=== LINUX GATE: NOT RUN (bad arguments) ==="; exit 2 ;;
    esac
  done
}

# The hand-off, measured with stand-ins for uname, wsl.exe and cygpath (shell functions, so no fake program and no
# setting is needed, and it runs the same on every host): a Windows host hands every argument to WSL verbatim and
# returns WSL's status; a host with no WSL is NOT RUN, never RED; a Linux host does not hand off.
if [ "${1:-}" = "--self-test" ]; then
  fails=0
  check() { if [ "$2" = "$3" ]; then echo "  ok    $1"; else echo "  FAIL  $1"; echo "        want: $3"; echo "        got:  $2"; fails=$((fails + 1)); fi; }
  got="$( uname() { echo MINGW64_NT-10.0-26300; }; cygpath() { echo 'E:\tree'; }
          wsl.exe() { printf '%s|' "$@"; echo; echo "MSYS_NO_PATHCONV=${MSYS_NO_PATHCONV:-unset}"; return 7; }
          run_on_linux --legs unit --nice; )"
  rc=$?
  check "a Windows host hands its arguments to WSL verbatim, in this tree" "$(echo "$got" | sed -n 2p)" \
        "-d|Ubuntu|--cd|E:\\tree|--exec|bash|-lc|bash scripts/linux-gate.sh \"\$@\"|linux-gate|--legs|unit|--nice|"
  check "the hand-off turns Git Bash's path rewriting off" "$(echo "$got" | sed -n 3p)" "MSYS_NO_PATHCONV=1"
  check "the hand-off's status is WSL's" "$rc" "7"
  got="$( uname() { echo Darwin; }; PATH=/nonexistent; run_on_linux --nice; )"
  rc=$?
  check "a host with no WSL is NOT RUN" "$got" "=== LINUX GATE: NOT RUN (this host is Darwin, not Linux, and has no WSL to run the gate on) ==="
  check "a host with no WSL exits 2, as every NOT RUN does" "$rc" "2"
  got="$( uname() { echo Linux; }; wsl.exe() { echo "handed off"; }; run_on_linux --nice; echo "stayed" )"
  check "a Linux host runs the gate itself" "$got" "stayed"
  got="$( legs=unit,selftests; check_legs; echo "accepted" )"
  rc=$?
  check "an unknown --legs name is NOT RUN (bad arguments) before anything runs" "$(echo "$got" | tail -1)" \
        "=== LINUX GATE: NOT RUN (bad arguments) ==="
  check "an unknown --legs name exits 2" "$rc" "2"
  got="$( legs=unit,characterization,conformance,guard; check_legs; echo "accepted ${wanted[*]}" )"
  check "the four selectable legs are accepted, in order" "$got" "accepted unit characterization conformance guard"
  if [ "$fails" -eq 0 ]; then echo "=== LINUX-GATE SELF-TEST: GREEN ==="; exit 0; fi
  echo "=== LINUX-GATE SELF-TEST: RED ($fails) ==="; exit 1
fi

run_on_linux "$@"

legs="unit,characterization,conformance,guard"
nice_prefix=()
while [ $# -gt 0 ]; do
  case "$1" in
    --legs) legs="$2"; shift 2 ;;
    --nice) nice_prefix=(nice -n 10); shift ;;   # an implementer's run yields the cores to the lander's
    *) echo "linux-gate: unknown argument '$1'"; echo "=== LINUX GATE: NOT RUN (bad arguments) ==="; exit 2 ;;
  esac
done
check_legs

[ -d "$HOME/.dotnet" ] && export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if ! command -v dotnet >/dev/null 2>&1; then
  echo "=== LINUX GATE: NOT RUN (no .NET SDK in this Linux environment — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
py="$(command -v python3 || command -v python)"
if [ -z "$py" ]; then
  echo "=== LINUX GATE: NOT RUN (no python in this Linux environment — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
if [ ! -f Cobol.Net.sln ]; then
  echo "=== LINUX GATE: NOT RUN (not at a repository root: $(pwd)) ==="; exit 2
fi
tree="$(pwd)"

# THIS SCRIPT'S OWN READS OF THE TREE (never exported) go through `wgit`: the TREE'S OWN git, and never one that
# writes. kb/Work PB2880 measured the alternative: Linux git reading a Windows tree through /mnt took 34 s for the one
# `status` below (every run, at ~0.5 of one core: the gate's first serial valley). The index's stat data is Windows
# git's, so Linux git re-read and re-hashed every tracked file across the 9P boundary, and then WROTE the refreshed
# index back into the real worktree (an "optional lock"), which Windows git rewrote again on its next command. So:
#  - a tree on a Windows drive (WSL; `wslpath -w` names a drive letter) is read by Windows git (`git.exe` through WSL
#    interop) with its Windows path, against the stat data it wrote itself: 0.1-1.2 s;
#  - a tree on a Linux filesystem (a cloud session, a CI-like host) is read by Linux git, as any Linux checkout;
#  - both pass `--no-optional-locks`, so a read never refreshes the index of the repository it reads.
# Each arm reports git's paths in this shell's form (`wpath`): the clone below reads the object store through it.
winroot="$(wslpath -w "$tree" 2>/dev/null)"
case "$winroot" in
  [A-Za-z]:\\*)
    if ! command -v git.exe >/dev/null 2>&1; then
      echo "=== LINUX GATE: NOT RUN (the tree $winroot is on a Windows drive and Windows git (git.exe) is not reachable" \
           "from WSL: enable WSL interop, or Linux git re-hashes the whole tree across /mnt — kb/Work PB2880) ==="; exit 2
    fi
    wgit() { git.exe --no-optional-locks -C "$winroot" "$@"; }
    wpath() { wslpath -u "$(printf '%s' "$1" | tr -d '\r')"; } ;;
  *)
    wgit() { git -c safe.directory='*' --no-optional-locks -C "$tree" "$@"; }
    wpath() { printf '%s' "$1"; } ;;
esac
head="$(wgit rev-parse HEAD 2>/dev/null | tr -d '\r')"
common="$(wgit rev-parse --path-format=absolute --git-common-dir 2>/dev/null)"
[ -n "$common" ] && common="$(wpath "$common")"
if [ -z "$head" ] || [ -z "$common" ]; then
  echo "=== LINUX GATE: NOT RUN (cannot read this tree's HEAD and git directory: $tree) ==="; exit 2
fi
dirty="$(wgit status --porcelain --untracked-files=no 2>/dev/null | tr -d '\r' | grep -v ' STATUS.md$' | wc -l)"
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
# it by design (PB209). Copy the tree's (no network), then apply THE one corpus rule the Windows gate and the battery
# apply (scripts/external_corpus.py, kb/Work PB2611): fetch when still absent, and a failed fetch is a named red.
corpus_red=""
if [ -d tests/external/gnucobol ]; then
  mkdir -p "$snap/tests/external" && cp -r tests/external/gnucobol "$snap/tests/external/"
fi
if ! "$py" "$snap/scripts/external_corpus.py" ensure --repo "$snap" > "$out/corpus-fetch.log" 2>&1; then
  echo "linux-gate: EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED (TestResults/linux-gate/corpus-fetch.log); the two ExternalCorpusPopulationDriftTests reds in the unit leg are attributable to it"
  corpus_red=" corpus-fetch"
fi
# ⛔ THE GATE'S OWN PYTHON (kb/Work PB3097, branch review R3-1): every leg runs with a virtual environment holding
# EXACTLY the clone's scripts/requirements-gate.txt, first on PATH (the self-tests, the guard and every Python
# instrument a test runs). PB3097 added PyYAML to CI and to setup-wsl.sh, a script run by hand, and this host's
# interpreter never got it: tooling_check.py's self-test went RED (6) here and would have reddened every landing.
# python_deps.py builds the environment the first time the declaration (or the base interpreter) changes and reuses it
# after, so a new dependency is provisioned by the commit that declares it, with no hand step; and EXACTLY, so a package
# only the developer's interpreter carries (PyMuPDF, SciPy) cannot pass here and fail in CI. A build that fails is
# NOT RUN, with pip's output, never a run over a missing package.
if ! vpy="$("$py" "$snap/scripts/python_deps.py" venv "$HOME/linux-gate/.python-env" 2> "$out/python-deps.log")" \
   || [ ! -x "$vpy" ]; then
  sed 's/^/    /' "$out/python-deps.log"
  echo "=== LINUX GATE: NOT RUN (could not build the gate's Python environment from scripts/requirements-gate.txt;" \
       "see TestResults/linux-gate/python-deps.log) ==="; exit 2
fi
py="$vpy"
export PATH="$(dirname "$vpy"):$PATH"
echo "linux-gate: $("$py" --version 2>&1) holding exactly scripts/requirements-gate.txt ($vpy)"
echo "linux-gate: Linux clone of HEAD ${head:0:9} at $snap"
[ "$dirty" -gt 0 ] && echo "linux-gate: NOTE — $dirty uncommitted tracked change(s) are NOT tested (the clone is HEAD)"

bad="$corpus_red"; ran=""
# PB2879 (owner 2026-10-09): the legs ran one after another and used about 10 % of a 32-CPU WSL VM (351 s alone). The
# clone is now built ONCE (`dotnet build Cobol.Net.sln`) and every leg runs in PARALLEL against that build, each to its
# own log. A background leg cannot set this shell's variables, so each writes its report lines to <leg>.result and
# GREEN|RED|NOT RUN to <leg>.status; after `wait` they are printed in a fixed order and folded into the verdict.
# `selftests` needs no build, so it starts beside the build. Every script self-test runs on Linux through the ONE runner
# CI runs (kb/Work PB2563): `selftests` is `python3 scripts/self_tests.py`, `selftests-built` its `--built` half, which
# reads the built test assemblies. They are path-sensitive: a drive-letter path that Linux reads as relative turned CI
# red on 2026-10-07 after a green Windows run (kb/Work PB2142).
leg_status() { printf '%s\n' "$2" > "$out/$1.status"; }   # <leg> <GREEN|RED|NOT RUN>
selftests_leg() {  # <leg name> [runner arguments…]
  local name="$1"; shift
  local start rc secs verdict
  start=$(date +%s)
  ( cd "$snap" && "${nice_prefix[@]}" "$py" scripts/self_tests.py "$@" ) > "$out/$name.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  verdict="$(grep -E '^=== SELF-TESTS: ' "$out/$name.log" | tail -1)"
  if [ $rc -eq 0 ] && [[ "$verdict" == "=== SELF-TESTS: GREEN"* ]]; then
    leg_status "$name" GREEN; echo "leg $name: GREEN in ${secs}s — $verdict"
  else
    # A red runner prints its WHOLE log less the self-tests that passed: trim what passed, never what failed (PB1573).
    leg_status "$name" RED
    echo "leg $name: RED (rc=$rc) in ${secs}s — ${verdict:-no self-test verdict; see TestResults/linux-gate/$name.log}"
    grep -vE '^self-test .*: GREEN in ' "$out/$name.log" | sed 's/^/    /'
  fi
}
guard_leg() {
  # CI's `guard` job (kb/Work PB1955, PB1957 row 40): the NIST suite through the `cobol` CLI from bash, the manifest
  # audit, the baseline check and the guard's own self-tests — the one CI Linux job that runs a SCRIPT rather than
  # `dotnet test`, so it was the one no local gate ran. Train 1013's CI red on PB322's TERMINATES rows was invisible to
  # every local leg. Its scratch is private to this clone (the guard writes fixed file names under TMPDIR, and
  # implementers run this gate concurrently). Its own `dotnet build` of the CLI is up to date after the ONE build.
  # ⛔ The scratch lies BESIDE the clone, never inside it (kb/Work PB2989): `selftests-built` runs at the same time and
  # keys its inputs by every untracked file of the clone (scripts/self_test_inputs.py), so a guard temp file listed by
  # `git ls-files -o` and deleted before `git hash-object` read it made that leg NOT RUN (train 1056b, 2026-10-11).
  local start rc secs summary gtmp="$snap.guard-tmp"
  start=$(date +%s)
  rm -rf "$gtmp"; mkdir -p "$gtmp"
  ( cd "$snap" && TMPDIR="$gtmp" "${nice_prefix[@]}" bash scripts/guard-fast.sh ) > "$out/guard.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  summary="$(grep -E '^=== (NIST \(|NIST AUDIT: population|ALL GREEN|FAILURES)' "$out/guard.log" | tr '\n' ' ')"
  if [ $rc -eq 0 ] && grep -qx '=== ALL GREEN ===' "$out/guard.log"; then
    leg_status guard GREEN; echo "leg guard: GREEN in ${secs}s — $summary"
  else
    # A red guard prints its WHOLE log less the per-program verdicts that came out as predicted: trim what passed,
    # never what failed (kb/Work PB1573). The audit's findings and every non-matching verdict's evidence stay.
    leg_status guard RED
    echo "leg guard: RED (rc=$rc) in ${secs}s — ${summary:-no guard verdict; see TestResults/linux-gate/guard.log}"
    grep -vE '^ *[A-Z][A-Z0-9]+: (MATCH|TERMINATES EC-|NO BASELINE)' "$out/guard.log" | sed 's/^/    /'
  fi
}
test_leg() {  # <leg> <project> — `dotnet test --no-build` against the ONE build
  local leg="$1" proj="$2" start rc secs summary
  start=$(date +%s)
  # Scrubbed (kb/Work PB1718): a gate-leg handshake or VSTest* variable inherited from the caller would narrow the run
  # to PART of its assembly while it exits 0. The one statement of that rule is test_population.py.
  ( cd "$snap" && "${nice_prefix[@]}" "$py" "$snap/scripts/test_population.py" scrubbed dotnet test "$proj" \
      --no-build --no-restore --logger "trx;LogFileName=$leg.trx" --results-directory "$out" ) > "$out/$leg.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  summary="$(grep -E '^(Passed!|Failed!)' "$out/$leg.log" | tail -1)"
  if [ $rc -eq 0 ] && grep -qE '^Passed!' "$out/$leg.log"; then
    leg_status "$leg" GREEN; echo "leg $leg: GREEN in ${secs}s — $summary"
  else
    leg_status "$leg" RED
    echo "leg $leg: RED (rc=$rc) in ${secs}s — ${summary:-no test summary; see TestResults/linux-gate/$leg.log}"
    grep -E '^\s+Failed ' "$out/$leg.log" | head -20 | sed 's/^/    /'
  fi
}

order=(selftests)
selftests_leg selftests > "$out/selftests.result" 2>&1 &
start=$(date +%s)
( cd "$snap" && "${nice_prefix[@]}" dotnet build Cobol.Net.sln -c Debug ) > "$out/build.log" 2>&1
brc=$?
echo "linux-gate: ONE build of Cobol.Net.sln in $(( $(date +%s) - start ))s (rc=$brc); the legs run in parallel against it"
if [ $brc -ne 0 ]; then
  echo "linux-gate: the Linux BUILD failed; see TestResults/linux-gate/build.log"
  grep -E ' error ' "$out/build.log" | sort -u | head -10 | sed 's/^/    /'
fi
for leg in "${wanted[@]}"; do
  order+=("$leg")
  case "$leg" in
    unit)             proj="tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj" ;;
    characterization) proj="tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj" ;;
    conformance)      proj="tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj" ;;
    guard)            guard_leg > "$out/guard.result" 2>&1 & continue ;;
  esac
  if [ $brc -ne 0 ]; then
    leg_status "$leg" RED; echo "leg $leg: RED — the Linux BUILD failed (rc=$brc)" > "$out/$leg.result"; continue
  fi
  test_leg "$leg" "$proj" > "$out/$leg.result" 2>&1 &
done
# selftests-built reads the unit and conformance assemblies, which the ONE build made; it needs no leg to finish first.
order+=(selftests-built)
case ",$legs," in
  *,unit,*conformance,*|*,conformance,*unit,*)
    if [ $brc -eq 0 ]; then
      selftests_leg selftests-built --built > "$out/selftests-built.result" 2>&1 &
    else
      leg_status selftests-built "NOT RUN"
      echo "leg selftests-built: NOT RUN (the Linux build failed)" > "$out/selftests-built.result"
    fi ;;
  *)
    # Not run because the legs asked for build neither unit nor conformance: said, never silent.
    leg_status selftests-built "NOT RUN"
    echo "leg selftests-built: NOT RUN (it reads the unit and conformance assemblies, and --legs '$legs' asks for" \
         "neither of them)" > "$out/selftests-built.result" ;;
esac
wait
for leg in "${order[@]}"; do
  cat "$out/$leg.result"
  st="$(cat "$out/$leg.status" 2>/dev/null)"
  case "$st" in
    GREEN) ran="$ran $leg" ;;
    "NOT RUN") [ $brc -ne 0 ] && bad="$bad $leg" ;;   # only selftests-built is ever NOT RUN (every --legs name was checked)
    *) bad="$bad $leg" ;;
  esac
done
[ $brc -ne 0 ] && bad="$bad build"

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
