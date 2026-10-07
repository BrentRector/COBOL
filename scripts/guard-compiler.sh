#!/bin/bash
# THE COMPILER UNDER TEST for the NIST guard legs — ONE place that answers "which binary is this gate
# measuring?", SOURCED by scripts/guard.sh and scripts/guard-fast.sh.
#
#   . "$(dirname "$0")/guard-compiler.sh"
#   guard_select_compiler          # sets GUARD_COMPILER + the four paths below
#
# scripts/guard-run-group.sh and scripts/guard-compile.sh do NOT source it: they are per-group / per-program
# workers spawned by a guard that has already made the selection, so they READ the exported GUARD_RUNTIME_DLL /
# GUARD_CLI_DLL instead. A worker that re-resolved would be a second place the answer lives.
#
# ⛔ WHY THIS EXISTS (kb/Work/PB750). Until 2026-09-06 both guards hard-coded the legacy byte engine's CLI and
# drove the whole NIST compile-and-run leg — plus its audit and its forensics — through it. That binary's
# dependency closure did not contain `Cobol.Net.Compiler` (the Roslyn code generator that IS WiseOwl COBOL), so
# every battery's headline `guard NIST: 353 MATCH, 0 REGRESSION(S)` said nothing about the compiler this project
# ships. Battery #58 proved it: `NC215A` printed a wrong answer (PB741) that NistDifferentialTests caught and the
# guard's NIST leg could not see. The legacy engine is retired from every gate (kb/Work R69, PB2109); the guard
# drives `cobol` (src/Cobol.Net.Cli) and nothing else, and every verdict line still NAMES it.
#
# ⛔ AND THE SELECTION IS ASSERTED AGAINST THE BINARY, NOT TRUSTED. `guard_assert_compiler_identity` reads the
# resolved CLI's own `.deps.json` — the build's record of its project graph — and refuses to run when the
# closure does not contain the code generator. A path typo, a stale bin directory or a future project rename can
# therefore never silently point this gate at a binary that is not WiseOwl COBOL.
# `bash scripts/guard-compiler.sh --self-test` proves the refusal actually fires.

# ── Selection ─────────────────────────────────────────────────────────────────────────────────────────────
# Outputs (globals):
#   GUARD_COMPILER      the name that goes in every verdict line (`cobol`)
#   GUARD_CLI_PROJECT   the .csproj the guard builds
#   GUARD_CLI_BIN       the built output directory (guard.sh snapshots this whole directory)
#   GUARD_CLI_DLL       the managed entry point the guard invokes as `dotnet "$GUARD_CLI_DLL" ...`
#   GUARD_RUNTIME_DLL   the COBOL runtime the compiled programs bind to (copied next to a program before it runs)
guard_select_compiler() {
    GUARD_COMPILER="cobol"
    GUARD_CLI_PROJECT="src/Cobol.Net.Cli/Cobol.Net.Cli.csproj"
    GUARD_CLI_BIN="src/Cobol.Net.Cli/bin/Debug/net10.0"
    GUARD_CLI_DLL="$GUARD_CLI_BIN/cobol.dll"
    GUARD_RUNTIME_DLL="src/Cobol.Net.Runtime/bin/Debug/net10.0/Cobol.Net.Runtime.dll"
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

# guard_assert_compiler_identity <cli-dll>
# Refuses (rc 1) unless the binary about to be driven really is WiseOwl COBOL. Loud on stderr.
guard_assert_compiler_identity() {
    local cli="$1" rc
    if [ ! -f "$cli" ]; then
        echo "⛔ GUARD: the compiler under test does not exist: $cli" >&2
        echo "   Build it first (the guard builds it itself; a missing binary here means the build failed)." >&2
        return 1
    fi
    # ⛔ ASKED AS A CONDITION, NEVER AS A BARE COMMAND. scripts/guard.sh runs under `set -e`, where a bare
    # `guard_cli_closure_has …; rc=$?` KILLS THE CALLER the moment the answer is "no", before the refusal below
    # can say why.
    if guard_cli_closure_has "$cli" "Cobol.Net.Compiler"; then rc=0; else rc=$?; fi
    if [ "$rc" -eq 2 ]; then
        echo "⛔ GUARD: no dependency manifest beside $cli — the compiler's identity cannot be established." >&2
        echo "   Expected ${cli%.dll}.deps.json. A gate that cannot say WHICH compiler it drove is not a gate (PB750)." >&2
        return 1
    fi
    if [ "$rc" -ne 0 ]; then
        echo "⛔ GUARD REFUSES TO RUN: $cli does NOT reference Cobol.Net.Compiler." >&2
        echo "   The guard measures WiseOwl COBOL, but this binary's project graph contains no code generator —" >&2
        echo "   a stale bin dir or a wrong path. This is exactly kb/Work/PB750: every 'NIST: NNN MATCH' line such" >&2
        echo "   a run printed would measure something other than the compiler. Fix the path or rebuild." >&2
        return 1
    fi
    return 0
}

# guard_announce_compiler — the banner both guards print before doing any work.
guard_announce_compiler() {
    echo "=== COMPILER UNDER TEST: $GUARD_COMPILER ($GUARD_CLI_DLL) ==="
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

    # (S) THE SELECTION ITSELF — a watchdog that checks the binary is no use if the SELECTION points elsewhere.
    out=$( ( guard_select_compiler; echo "$GUARD_COMPILER $GUARD_CLI_DLL $GUARD_RUNTIME_DLL" ) )
    want "the selection is cobol" 0 0 "$out" "cobol src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll src/Cobol.Net.Runtime/"

    # (0) THE CONTROL. Without it the watchdog could refuse EVERYTHING and every case below would "pass".
    if [ -f "$GREEN" ]; then
        out=$(guard_assert_compiler_identity "$GREEN" 2>&1); want "the real cobol.dll is accepted" 0 $? "$out"
        # (0b) ⛔ UNDER `set -e`, WHICH IS HOW scripts/guard.sh CALLS IT. An accept must not abort the caller.
        #      ⚠ THE CALL MUST BE A BARE COMMAND HERE: a command on the LEFT of `&&` has `set -e` suppressed, and
        #      the suppression propagates into the function body, so `… && echo SURVIVED` could not see the defect.
        out=$( { set -e; guard_assert_compiler_identity "$GREEN"; echo SURVIVED; } 2>&1 )
        want "an accept does not abort a set -e caller" 0 $? "$out" "SURVIVED"
    else
        echo "  ⚠ SKIP control: $GREEN not built (build the solution to exercise it)"
    fi

    # (1) ⭐ THE PB750 CASE — a binary whose project graph has no code generator. It must be REFUSED, and under
    #     `set -e` the refusal must still be the reported one (the "no" answer is the case that killed the caller).
    printf 'stub\n' > "$d/nogen.dll"
    printf '{"libraries":{"Cobol.Net.Frontend/1.0.0":{},"nogen/1.0.0":{}}}\n' > "$d/nogen.deps.json"
    out=$(guard_assert_compiler_identity "$d/nogen.dll" 2>&1)
    want "a CLI without the code generator is REFUSED" 1 $? "$out" "does NOT reference Cobol.Net.Compiler"
    out=$( { set -e; guard_assert_compiler_identity "$d/nogen.dll"; echo SURVIVED; } 2>&1 )
    want "a set -e caller hears the refusal" 1 $? "$out" "does NOT reference Cobol.Net.Compiler"

    # (2) A name that only CONTAINS the assembly name is not a library entry (the "/" anchor).
    printf 'stub\n' > "$d/lookalike.dll"
    printf '{"libraries":{"My.Cobol.Net.Compiler.Shim/1.0.0":{}},"x":"Cobol.Net.Compiler"}\n' > "$d/lookalike.deps.json"
    out=$(guard_assert_compiler_identity "$d/lookalike.dll" 2>&1)
    want "a manifest that only mentions the name is REFUSED" 1 $? "$out" "does NOT reference Cobol.Net.Compiler"

    # (3) An unbuilt / mistyped path is a refusal, never a pass-by-absence.
    out=$(guard_assert_compiler_identity "$d/nowhere.dll" 2>&1)
    want "a nonexistent CLI is REFUSED" 1 $? "$out" "does not exist"

    # (4) A binary with NO dependency manifest cannot prove its identity — refuse rather than guess.
    printf 'not really a dll\n' > "$d/mystery.dll"
    out=$(guard_assert_compiler_identity "$d/mystery.dll" 2>&1)
    want "a CLI with no .deps.json is REFUSED" 1 $? "$out" "cannot be established"

    # (5) And a manifest that names the code generator is accepted on its content alone (the check reads the
    #     project graph, not the file name — a renamed or copied CLI is judged by what it references).
    printf 'stub\n' > "$d/renamed.dll"
    printf '{"libraries":{"Cobol.Net.Compiler/1.0.0":{},"cobol/1.0.0":{}}}\n' > "$d/renamed.deps.json"
    out=$(guard_assert_compiler_identity "$d/renamed.dll" 2>&1)
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
