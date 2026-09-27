#!/bin/bash
# THE COMPILER UNDER TEST for the NIST guard legs — ONE place that answers "which compiler is this gate
# measuring?", SOURCED by scripts/guard.sh, scripts/guard-fast.sh and scripts/run-suite.sh.
#
#   . "$(dirname "$0")/guard-compiler.sh"
#   guard_select_compiler          # sets GUARD_COMPILER + the four paths below, and ASSERTS the identity
#
# scripts/guard-run-group.sh and scripts/guard-compile.sh do NOT source it: they are per-group / per-program
# workers spawned by a guard that has already made the selection, so they READ the exported GUARD_RUNTIME_DLL /
# GUARD_CLI_DLL + GUARD_COMPILER instead. A worker that re-resolved would be a second place the answer lives.
#
# ⛔ WHY THIS EXISTS (kb/Work/PB750). Until 2026-09-06 both guards hard-coded
#     CLI="src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll"
# and drove the whole 376-program NIST compile-and-run leg — plus its audit and its forensics — through it.
# That binary is the LEGACY byte engine: its dependency closure is CobolSharp.Compiler -> Cobol.Net.Frontend,
# and `Cobol.Net.Compiler` (the Roslyn code generator that IS WiseOwl COBOL) is not in it. So every battery's
# headline `guard NIST: 353 MATCH, 0 REGRESSION(S)` was a true statement about the ORACLE and said nothing
# whatever about the compiler this project ships. Battery #58 proved it the hard way: `NC215A` printed a
# wrong answer (PB741) that the Conformance assembly's NistDifferentialTests partitions caught and the guard's
# NIST leg — 353 MATCH, audit CLEAN — could not see, because the two runs drove two different compilers.
# CLAUDE.md rule 1 names the legacy a regression net with known holes, never authority; a gate pointed at it
# is `feedback_green_gates_arent_evidence` in its purest form.
#
# THE MODEL. The guard drives the compiler this project ships, `cobol` (src/Cobol.Net.Cli), by default. The
# legacy compiler is still runnable through the SAME leg, but only through the project's existing opt-in
# differential switch:
#
#   bash scripts/guard-fast.sh                          -> WiseOwl COBOL  (the gate)
#   COBOLSHARP_LEGACY_DIFFERENTIAL=1 bash scripts/guard-fast.sh   -> the legacy oracle (a differential run)
#
# and every verdict line NAMES the compiler it drove, so a pasted `NIST (cobol): ...` line can never again be
# read as evidence about the other one.
#
# ⛔ AND THE SELECTION IS ASSERTED AGAINST THE BINARY, NOT TRUSTED. `guard_assert_compiler_identity` reads the
# resolved CLI's own `.deps.json` — the build's record of its project graph — and refuses to run when the
# closure does not match the compiler the guard says it is driving. A path typo, a stale bin directory or a
# future project rename can therefore no longer silently re-point this gate at the other compiler.
# `bash scripts/guard-compiler.sh --self-test` proves BOTH directions of that refusal actually fire.

# ── Selection ─────────────────────────────────────────────────────────────────────────────────────────────
# Outputs (globals):
#   GUARD_COMPILER      cobol | legacy — the name that goes in every verdict line
#   GUARD_CLI_PROJECT   the .csproj the guard builds
#   GUARD_CLI_BIN       the built output directory (guard.sh snapshots this whole directory)
#   GUARD_CLI_DLL       the managed entry point the guard invokes as `dotnet "$GUARD_CLI_DLL" ...`
#   GUARD_RUNTIME_DLL   the COBOL runtime the compiled programs bind to (copied next to a program before it runs)
#   GUARD_DIVERGENT     1 iff the LEGACY_DIVERGENT exemption list applies (legacy only — see below)
#
# TWO WAYS TO ASK FOR THE LEGACY ARM, and they are not equivalent — say which you mean:
#   GUARD_COMPILER=legacy bash scripts/guard-fast.sh          changes ONLY this NIST leg's compiler
#   COBOLSHARP_LEGACY_DIFFERENTIAL=1 bash scripts/guard-fast.sh   ALSO switches CobolSharp.Tests.Integration's
#       `ConformanceTests` into their opt-in legacy-differential corpus (that assembly reads the same variable —
#       ConformanceTests.cs:41), so the guard's Integration leg then runs a DIFFERENT, much larger population
#       against the byte engine. That is a separate gate with its own verdict; do not read its result as
#       anything about the NIST leg. Measured: it turns ~1276 Integration cases into ~1289 with ~483 failures,
#       because the greenfield conformance corpus has grown post-85 cases the frozen engine cannot compile.
guard_select_compiler() {
    local want="${GUARD_COMPILER:-}"
    if [ -z "$want" ] && [ "${COBOLSHARP_LEGACY_DIFFERENTIAL:-0}" = "1" ]; then want="legacy"; fi
    case "$want" in
        ""|cobol) : ;;
        legacy)   : ;;
        *) echo "⛔ GUARD: GUARD_COMPILER='$want' is not a compiler this gate knows (use cobol|legacy)" >&2
           return 1 ;;
    esac
    if [ "$want" = "legacy" ]; then
        GUARD_COMPILER="legacy"
        GUARD_CLI_PROJECT="src/CobolSharp.CLI/CobolSharp.CLI.csproj"
        GUARD_CLI_BIN="src/CobolSharp.CLI/bin/Debug/net10.0"
        GUARD_CLI_DLL="$GUARD_CLI_BIN/cobolsharp.dll"
        GUARD_RUNTIME_DLL="src/CobolSharp.Runtime/bin/Debug/net10.0/CobolSharp.Runtime.dll"
        # ⭐ THE EXEMPTION LIST IS THE LEGACY'S, AND ONLY THE LEGACY'S. The LEGACY_DIVERGENT programs are the
        # `divergent` rows of tests/nist/corpus.tsv, derived there by scripts/guard-population.sh (kb/Work
        # PB898 — guard.sh used to carry a hand copy that had drifted a program behind it). Their goldens were
        # RE-BASELINED to the ISO-conforming output because the legacy is non-conforming there, and the
        # manifest's note column carries each one's § citation. Under `cobol` those goldens are exactly what
        # the compiler must reproduce — NistDifferentialTests locks them byte-exact — so applying the exemption
        # would skip the comparison on the programs most likely to catch a codegen regression.
        GUARD_DIVERGENT=1
    else
        GUARD_COMPILER="cobol"
        GUARD_CLI_PROJECT="src/Cobol.Net.Cli/Cobol.Net.Cli.csproj"
        GUARD_CLI_BIN="src/Cobol.Net.Cli/bin/Debug/net10.0"
        GUARD_CLI_DLL="$GUARD_CLI_BIN/cobol.dll"
        GUARD_RUNTIME_DLL="src/Cobol.Net.Runtime/bin/Debug/net10.0/Cobol.Net.Runtime.dll"
        GUARD_DIVERGENT=0
    fi
}

# ── The identity watchdog ─────────────────────────────────────────────────────────────────────────────────
# guard_cli_closure_has <cli-dll> <assembly-simple-name>
#   0 = the CLI's dependency closure contains that assembly, 1 = it does not, 2 = the question could not be
#   answered (no .deps.json). A .deps.json library key is always "<name>/<version>", so the "/" anchors the
#   match to a library entry and cannot be satisfied by a type or namespace name inside some other string.
guard_cli_closure_has() {
    local cli="$1" asm="$2" deps
    deps="${cli%.dll}.deps.json"
    [ -f "$deps" ] || return 2
    grep -q "\"${asm//./\\.}/" "$deps"
}

# guard_assert_compiler_identity <cli-dll> <cobol|legacy>
# Refuses (rc 1) unless the binary about to be driven really is the compiler the guard claims. Loud on stderr.
guard_assert_compiler_identity() {
    local cli="$1" want="$2" rc
    if [ ! -f "$cli" ]; then
        echo "⛔ GUARD: the compiler under test does not exist: $cli" >&2
        echo "   Build it first (the guard builds it itself; a missing binary here means the build failed)." >&2
        return 1
    fi
    # ⛔ ASKED AS A CONDITION, NEVER AS A BARE COMMAND. scripts/guard.sh runs under `set -e`, where a bare
    # `guard_cli_closure_has …; rc=$?` KILLS THE CALLER the moment the answer is "no" — which is the NORMAL
    # answer in legacy mode. Written that way, this watchdog aborted the serial guard's legacy arm silently
    # after the build (found by smoking the second arm; `feedback_two_arm_dispatch`).
    if guard_cli_closure_has "$cli" "Cobol.Net.Compiler"; then rc=0; else rc=$?; fi
    if [ "$rc" -eq 2 ]; then
        echo "⛔ GUARD: no dependency manifest beside $cli — the compiler's identity cannot be established." >&2
        echo "   Expected ${cli%.dll}.deps.json. A gate that cannot say WHICH compiler it drove is not a gate (PB750)." >&2
        return 1
    fi
    case "$want" in
        cobol)
            if [ "$rc" -ne 0 ]; then
                echo "⛔ GUARD REFUSES TO RUN: $cli does NOT reference Cobol.Net.Compiler." >&2
                echo "   The guard was asked to measure WiseOwl COBOL ($want) but resolved a binary whose project" >&2
                echo "   graph contains no code generator — i.e. the LEGACY byte engine, or a stale bin dir." >&2
                echo "   This is exactly kb/Work/PB750: every 'NIST: NNN MATCH' line such a run printed measured" >&2
                echo "   the oracle, not the compiler. Fix the path or rebuild; do not silence this check." >&2
                return 1
            fi ;;
        legacy)
            if [ "$rc" -eq 0 ]; then
                echo "⛔ GUARD REFUSES TO RUN: $cli DOES reference Cobol.Net.Compiler." >&2
                echo "   COBOLSHARP_LEGACY_DIFFERENTIAL=1 asks for the legacy ORACLE, whose whole value is that it" >&2
                echo "   shares no code generator with WiseOwl COBOL. A differential against yourself proves nothing." >&2
                return 1
            fi ;;
        *)  echo "⛔ GUARD: guard_assert_compiler_identity — unknown compiler name '$want'" >&2; return 1 ;;
    esac
    return 0
}

# guard_announce_compiler — the banner both guards print before doing any work.
guard_announce_compiler() {
    echo "=== COMPILER UNDER TEST: $GUARD_COMPILER ($GUARD_CLI_DLL) ==="
    if [ "$GUARD_COMPILER" = "legacy" ]; then
        echo "  ⚠ LEGACY ARM — this run measures the LEGACY ORACLE, not WiseOwl COBOL."
        echo "    Its verdicts are a differential observation only; they are NOT the project's NIST gate (PB750)."
        if [ "${COBOLSHARP_LEGACY_DIFFERENTIAL:-0}" = "1" ]; then
            echo "    ⚠ COBOLSHARP_LEGACY_DIFFERENTIAL=1 ALSO switches CobolSharp.Tests.Integration's"
            echo "      ConformanceTests into their opt-in legacy-differential corpus — a separate, much larger"
            echo "      gate with its own verdict. Use GUARD_COMPILER=legacy to change ONLY the NIST leg."
        fi
    fi
}

# ── Self-test: prove the watchdog REFUSES (feedback_green_gates_arent_evidence) ────────────────────────────
# A check that has never been shown to fail is not evidence — and this one exists precisely because a gate
# silently measured the wrong compiler for months. Run from scripts/guard-verify.sh's witness phase, which the
# battery runs (phase 2a) before it believes any guard output.
guard_compiler_self_test() {
    local root rc=0 d out
    root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
    d="$(mktemp -d -t guardcli.XXXXXX)"
    trap 'rm -rf "$d"' RETURN

    local GREEN="$root/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll"
    local LEGACY="$root/src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll"

    # want NAME EXPECTED-RC ACTUAL-RC OUTPUT [REQUIRED-SUBSTRING]
    # ⛔ The substring argument is the point: a refusal for some OTHER reason (a missing file, a typo in the
    # name) would otherwise "pass" a case that never exercised the check it claims to.
    want() {
        local name="$1" exp="$2" got="$3" text="$4" need="${5:-}"
        if [ "$got" -ne "$exp" ]; then
            echo "  SELF-TEST FAILED: $name — expected rc=$exp, got rc=$got"; echo "$text" | sed 's/^/      /'; rc=1
        elif [ -n "$need" ] && ! printf '%s' "$text" | grep -qF -- "$need"; then
            echo "  SELF-TEST FAILED: $name — rc=$got but for the WRONG reason (no '$need')"
            echo "$text" | sed 's/^/      /'; rc=1
        else
            echo "  ok: $name (rc=$got)"
        fi
    }

    echo "=== guard-compiler --self-test (the compiler-identity watchdog) ==="

    # (S) THE SELECTION ITSELF — the default must be WiseOwl COBOL, and each way of asking for the legacy arm must
    #     be honoured. A watchdog that checks the binary is no use if the SELECTION silently defaults wrong.
    sel() { ( unset GUARD_COMPILER COBOLSHARP_LEGACY_DIFFERENTIAL; "$@"; guard_select_compiler >/dev/null 2>&1 \
              && echo "$GUARD_COMPILER $GUARD_CLI_DLL $GUARD_DIVERGENT" || echo "REFUSED" ); }
    out=$(sel true);                                            want "the default selection is cobol" 0 0 "$out" "cobol src/Cobol.Net.Cli"
    out=$(sel true); case "$out" in *" 0") want "the default does not apply LEGACY_DIVERGENT" 0 0 "$out" ;;
                                    *) want "the default does not apply LEGACY_DIVERGENT" 0 1 "$out" ;; esac
    out=$(sel export GUARD_COMPILER=legacy);                    want "GUARD_COMPILER=legacy selects the oracle" 0 0 "$out" "legacy src/CobolSharp.CLI"
    out=$(sel export COBOLSHARP_LEGACY_DIFFERENTIAL=1);         want "the differential switch selects the oracle" 0 0 "$out" "legacy src/CobolSharp.CLI"
    out=$(sel export GUARD_COMPILER=gnucobol);                  want "an unknown GUARD_COMPILER is refused" 0 0 "$out" "REFUSED"

    # (0) THE CONTROLS. Without these the watchdog could refuse EVERYTHING and every case below would "pass".
    if [ -f "$GREEN" ]; then
        out=$(guard_assert_compiler_identity "$GREEN" cobol 2>&1); want "the real cobol.dll is accepted as cobol" 0 $? "$out"
    else
        echo "  ⚠ SKIP control: $GREEN not built (build the solution to exercise it)"
    fi
    if [ -f "$LEGACY" ]; then
        out=$(guard_assert_compiler_identity "$LEGACY" legacy 2>&1); want "the real cobolsharp.dll is accepted as legacy" 0 $? "$out"

        # (1) ⭐ THE PB750 CASE ITSELF — the guard, believing it is measuring WiseOwl COBOL, is handed the binary the
        #     guard actually used from 2026-07 to 2026-09-06. It must REFUSE.
        out=$(guard_assert_compiler_identity "$LEGACY" cobol 2>&1)
        want "the LEGACY dll is REFUSED when cobol is claimed" 1 $? "$out" "does NOT reference Cobol.Net.Compiler"
    else
        echo "  ⚠ SKIP: $LEGACY not built — the PB750 refusal case cannot be exercised"
    fi

    # (2) The mirror: an opt-in legacy differential handed the greenfield compiler is not a differential at all.
    if [ -f "$GREEN" ]; then
        out=$(guard_assert_compiler_identity "$GREEN" legacy 2>&1)
        want "the greenfield dll is REFUSED when legacy is claimed" 1 $? "$out" "DOES reference Cobol.Net.Compiler"
    fi

    # (2b) ⛔ UNDER `set -e`, WHICH IS HOW scripts/guard.sh CALLS IT. An ACCEPT must not abort the caller —
    #      and the legacy accept is the dangerous one, because its internal question ("does this reference
    #      Cobol.Net.Compiler?") answers NO. Asked as a bare command that is a non-zero status, and `set -e`
    #      killed the serial guard right after its build with no message at all. This case is that regression.
    #      ⚠ THE CALL MUST BE A BARE COMMAND HERE. `guard_assert_compiler_identity … && echo SURVIVED` cannot
    #      see the defect at all: a command on the LEFT of `&&` has `set -e` suppressed, and the suppression
    #      propagates into the function body — the first draft of this case passed against the BROKEN code.
    if [ -f "$LEGACY" ]; then
        out=$( { set -e; guard_assert_compiler_identity "$LEGACY" legacy; echo SURVIVED; } 2>&1 )
        want "an accept does not abort a set -e caller (legacy)" 0 $? "$out" "SURVIVED"
    fi
    if [ -f "$GREEN" ]; then
        out=$( { set -e; guard_assert_compiler_identity "$GREEN" cobol; echo SURVIVED; } 2>&1 )
        want "an accept does not abort a set -e caller (cobol)" 0 $? "$out" "SURVIVED"
    fi

    # (3) An unbuilt / mistyped path is a refusal, never a pass-by-absence.
    out=$(guard_assert_compiler_identity "$d/nowhere.dll" cobol 2>&1)
    want "a nonexistent CLI is REFUSED" 1 $? "$out" "does not exist"

    # (4) A binary with NO dependency manifest cannot prove its identity — refuse rather than guess.
    printf 'not really a dll\n' > "$d/mystery.dll"
    out=$(guard_assert_compiler_identity "$d/mystery.dll" cobol 2>&1)
    want "a CLI with no .deps.json is REFUSED" 1 $? "$out" "cannot be established"

    # (5) And a manifest that names the code generator is accepted on its content alone (the check reads the
    #     project graph, not the file name — a renamed or copied CLI is judged by what it references).
    printf 'stub\n' > "$d/renamed.dll"
    printf '{"libraries":{"Cobol.Net.Compiler/1.0.0":{},"cobol/1.0.0":{}}}\n' > "$d/renamed.deps.json"
    out=$(guard_assert_compiler_identity "$d/renamed.dll" cobol 2>&1)
    want "a renamed CLI whose closure has the code generator is accepted" 0 $? "$out"

    if [ "$rc" -eq 0 ]; then
        echo "=== guard-compiler --self-test: ALL GREEN (the watchdog was proven able to refuse) ==="
    else
        echo "=== guard-compiler --self-test: FAILED ==="
    fi
    return $rc
}

# Executed directly (not sourced): run the self-test.
if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    case "${1:-}" in
        --self-test) guard_compiler_self_test; exit $? ;;
        *) echo "usage: $0 --self-test   (otherwise this file is SOURCED by the guards)" >&2; exit 2 ;;
    esac
fi
