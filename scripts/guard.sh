#!/bin/bash
# Guardrail script — run after every meaningful change set.
# Exit on first failure.
#
# ⛔ WHICH COMPILER THIS MEASURES (kb/Work/PB750). The NIST leg drives `cobol` (src/Cobol.Net.Cli) — the compiler
# this project ships — and refuses to start unless the resolved binary really references Cobol.Net.Compiler.
# Until 2026-09-06 it hard-coded the LEGACY `cobolsharp.dll`, whose project graph contains no code generator, so
# its headline MATCH count was a true statement about the ORACLE and no statement at all about WiseOwl COBOL.
# `GUARD_COMPILER=legacy` runs the identical leg through the legacy oracle instead — a differential observation,
# never the gate (`COBOLSHARP_LEGACY_DIFFERENTIAL=1` does too, and ALSO flips the Integration suite's opt-in
# differential corpus; see scripts/guard-compiler.sh). Every summary line names the compiler it drove.
set -e

# WHICH COMPILER, asserted against the binary's own dependency graph (scripts/guard-compiler.sh).
. "$(dirname "$0")/guard-compiler.sh"
guard_select_compiler
guard_announce_compiler

echo "=== Building ==="
dotnet build "$GUARD_CLI_PROJECT" -v quiet
guard_assert_compiler_identity "$GUARD_CLI_DLL" "$GUARD_COMPILER"

# SCRUBBED (kb/Work PB1718): no gate leg handshake or VSTest*/RunSettingsFilePath property reaches a test host.
. "$(dirname "$0")/python-resolve.sh"
echo "=== Unit tests ==="
"$PY" scripts/test_population.py scrubbed dotnet test tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj --verbosity quiet

echo "=== Integration tests ==="
"$PY" scripts/test_population.py scrubbed dotnet test tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj --verbosity quiet

echo "=== NIST regression ==="

# ── RUN ISOLATION (2026-07-19) ────────────────────────────────────────────────────────────────────────────
# The NIST leg used to compile and run straight out of the live tree, which made it fragile in two ways:
#   1. It EXECUTES the binary it just built. Any rebuild during the ~20-minute loop (a concurrent
#      `dotnet build`, an IDE, a second guard) swaps the compiler / runtime assemblies MID-RUN, so
#      early tests use compiler A and later ones compiler B — the "phantom regression" class.
#   2. tests/nist/output/ is SHARED MUTABLE state. Two overlapping runs (including one whose children
#      outlived a kill) interleave their .txt writes. The absent-OPTIONAL-file tests IX216A/IX217A are the
#      canaries: they expect status 05 ("not present, created"), so ANY stray file makes them fail.
# Both are fixed by snapshotting the built compiler and the NIST inputs into a per-run directory and doing
# all work there. Cost: a ~40 MB copy (a few seconds). Benefit: the tree is only frozen for the BUILD phase,
# not the whole run, and concurrent runs can no longer corrupt each other.
# Set GUARD_WORK to a fixed path to keep the snapshot for post-mortem; default is a fresh temp dir.
GUARD_WORK="${GUARD_WORK:-$(mktemp -d -t nistguard.XXXXXX)}"
GUARD_KEEP="${GUARD_KEEP:-0}"
echo "  run-scoped work dir: $GUARD_WORK"
mkdir -p "$GUARD_WORK/output"
cp -r tests/nist/programs "$GUARD_WORK/programs"
cp -r tests/nist/valid    "$GUARD_WORK/valid"
[ -d tests/nist/copylib ] && cp -r tests/nist/copylib "$GUARD_WORK/copylib"
[ -d tests/nist/data ]    && cp -r tests/nist/data    "$GUARD_WORK/data"
cp -r "$GUARD_CLI_BIN" "$GUARD_WORK/cli"
cp "$GUARD_RUNTIME_DLL" "$GUARD_WORK/output/"
# The SNAPSHOT is what runs — that is the whole point of the run-scoped work dir — so GUARD_CLI_DLL is
# re-pointed at it (and re-asserted: a snapshot copy is still a binary whose identity must hold).
CLI="$GUARD_WORK/cli/$(basename "$GUARD_CLI_DLL")"
export GUARD_CLI_DLL="$CLI" GUARD_COMPILER
guard_assert_compiler_identity "$CLI" "$GUARD_COMPILER"
NIST_OUT="$GUARD_WORK/output"
NIST_PROGS="$GUARD_WORK/programs"
NIST_VALID="$GUARD_WORK/valid"
NIST_DATA="$GUARD_WORK/data"
cleanup_guard_work() { if [ "$GUARD_KEEP" != "1" ]; then rm -rf "$GUARD_WORK"; fi; }
trap cleanup_guard_work EXIT

# Start each run from a clean DATA-file state so the guard is deterministic and reproducible from any prior
# state. Producer/consumer chains (RL/SQ/IX) rebuild WITHIN this run because the producer runs before the
# consumer in NIST_TESTS order (the loop still does not clean BETWEEN tests, so TF### carries over once
# created). Critically, absent-file tests (e.g. IX216A/217A OPEN I-O/EXTEND of an OPTIONAL file, expecting
# status 05 = "not present, created") must NOT see the file they themselves created on a previous invocation;
# without this start-clean such a test passes once then fails forever. Only data/report .txt files are
# removed — the compiled .dll/.runtimeconfig.json stay.
# (`*.cbattr` used to go with them — the fixed-file-attribute catalog sidecar, whose orphan would have described
# a file that no longer existed, exactly the state the absent-OPTIONAL-file canaries test. It no longer exists:
# owner decision 2026-09-07, kb/Work PB802, put a keyed file's §9.1.6 attributes in the store's own header.)
rm -f $NIST_OUT/*.txt

# All NIST tests currently at 100% — must stay green
# (93 NC + 42 IF + 15 SM + 18 IC + 83 SQ + 28 RL + 39 IX + 29 ST + 3 OBSQ = 350 tests).
# (SQ212A baselined DEVLOG 311–312: variable-length WRITE/REWRITE boundary -> status 44, plus a USE-
#  procedure declaratives-return fix so its USE handler returns at the section's exit instead of falling
#  through into the CLOSE-FILES/footer/STOP-RUN termination tail. Still removed (genuine remaining work):
#  NC303M 0-byte flag module; IX108A footer 001 TEST(S) FAILED.)
#
# Out-of-scope exclusion class (DEVLOG 301 scope decision — NOT baselined, NOT implemented; like the
# flagging-"M" modules these are documented-excluded for modern-COBOL "operational" conformance):
#   CM (Communication, OBSOLETE) · SG (Segmentation, OBSOLETE) · OBNC/OBIC (obsolete variants) · EXEC85.
# Deferred OPTIONAL stretch (in-scope only as future enhancements, NOT required for operational):
#   DB (Debug) · RW (Report Writer).
# ST chains: ST101A (builds the SORT output file TF002) -> ST102A (updates it; NO_OUTPUT producer, runs but
# is not baselined) -> ST103A (verifies). ST104A (builds TF001) -> ST105A (verifies). ST112M (builds a
# 3-reel file; emits only a 000-of-000 builder comment, so NOT baselined) -> ST113M (sorts; NO_OUTPUT) ->
# ST114M (verifies, 10/10). ST109A (builds a 40-variable-length-record file; 000-of-000 builder, NOT
# baselined) -> ST110A (variable-length SORT, NO_OUTPUT) -> ST111A (verifies, 7/7). ST122A -> ST123A ->
# ST124A (the second variable-length SORT chain). ST115A (builds a 204-record file — XXXXX065 "number of
# records" is substituted to 204 = 51*4; 000-of-000 builder, NOT baselined) -> ST116A (the BIG-SORT,
# NO_OUTPUT) -> ST117A (verifies the native-collating sort of all 204 records, 1/1). Keep each chain's
# members consecutive, producers ahead of consumers. ST119A (SORTs, writes TF001 via XXXXP001 — baselined)
# -> ST120A (SORT USING TF001 GIVING TF002, the USING/GIVING feature; NO_OUTPUT producer) -> ST121A
# (verifies the TF002 sort, 9/9). These three MUST stay consecutive and ahead of ST122A, which re-creates
# TF002 in a variable-length format.
# ST137A is a self-contained SORT over a FORMAT-2 variable-length record (FD RECORD CONTAINS 148 TO 1435
# CHARACTERS + an embedded OCCURS DEPENDING ON); its 6 tests verify the native collating order of the
# sorted keys against the XXXXX063 collating-sequence literal. ST147A (MERGE) re-baselined the same
# session: substituting XXXXX063 turned its previously-blank NATIVE COLL.SEQUENCE checks into real ones.
# RL206A→RL207A are a producer/consumer pair over TF021 with VARIABLE-LENGTH records (RECORD IS
# VARYING): RL206A creates the file (each slot stores its own length, persisted length-prefixed), RL207A
# verifies/updates it. They MUST stay consecutive, AND a fixed-format TF021 producer must follow (RL209A
# opens OUTPUT, re-creating TF021 in the fixed format) so the later fixed TF021 readers are unaffected —
# each producer re-creates the file in its own format, so consecutive chains are self-sufficient.
# RL105A/RL118A are self-contained (each opens its relative file OUTPUT, populates it, and verifies in
# one run), so they carry no chain dependency. RL108A→RL109A→RL110A are a producer/updater/verifier
# chain over the shared relative file TF061 (XXXXX061): RL108A creates 500 records (ACCESS SEQUENTIAL),
# RL109A REWRITEs every 5th record by COMP relative key (ACCESS RANDOM), RL110A verifies (ACCESS
# SEQUENTIAL); they MUST stay consecutive. RL107A→RL117A is a producer/consumer pair over TF022
# (XXXXX022): RL107A creates it, RL117A verifies it — they MUST stay consecutive, with no other TF022
# writer (e.g. RL118A) between them. The XXXXX### ASSIGN targets of RELATIVE/INDEXED files are mapped to
# a shared "TF###" literal so these chains share one on-disk file across run units; SEQUENTIAL files
# keep program-id-qualified isolation (DEVLOG 244). Several of these compile only under the L1/L2 dialect
# leniencies (INVALID KEY / RELATIVE KEY with KEY omitted, accepted in Default/--nist) — see
# docs/dialect-strictness.md.
# RL101A→RL102A→RL103A are a producer/updater/verifier chain over the shared relative file TF021
# (XXXX[PD]021): RL101A creates 500 records, RL102A REWRITEs 100, RL103A verifies. They MUST stay
# consecutive in this list (the loop does not clean data files between tests, so TF021 carries over),
# and ahead of any other TF021 producer (e.g. RL201A).
# RL201A→RL202A→RL203A are a second producer/updater/verifier chain over TF021 (DYNAMIC access,
# COMP relative keys): RL201A creates, RL202A randomly REWRITE/DELETEs, RL203A verifies. They too
# MUST stay consecutive. RL201A opens OUTPUT (recreating TF021), so it is safe to run after the
# RL1xx chain; RL209A (also XXXXP021) likewise opens OUTPUT, so it is safe after RL203A.
# RL210A/RL211A are self-contained format-3 variable-record tests (RECORD IS VARYING with an OCCURS
# DEPENDING table inside the record, and mixed 120/140-byte 01 formats). They write 200×120 + 300×140
# records and verify the ODO content round-trips. They had a long history of vacuous/failing baselines
# (the no-op secondary-record WRITE, then 300/500 genuine failures); now genuinely pass once the READ
# buffer for a varying record uses the record's MAXIMUM length (ResolveReadRecordLocation resolves the
# largest 01 as a receiving operand) — see DEVLOG 257. They open OUTPUT (self-contained), so they leave
# a varying-format TF021 that the following fixed-format producer (RL209A) re-creates.
# IF401M/402M/403M are flagging-conformance modules: they emit no CCVS report by
# design, so they are intentionally NOT guarded (nothing to compare).
NIST_TESTS="
NC101A NC102A NC103A NC104A NC105A NC106A NC107A NC108M NC109M NC110M
NC111A NC112A NC113M NC114M NC115A NC116A NC117A NC118A NC119A NC120A NC121M
NC122A NC123A NC124A NC125A NC126A NC127A
NC131A NC132A NC133A NC134A NC135A NC136A NC137A NC138A NC139A NC140A NC141A
NC170A NC171A NC172A NC173A NC174A NC175A NC176A NC177A
NC201A NC202A NC203A NC204M NC205A NC206A NC207A NC208A NC209A NC210A NC211A NC215A NC216A NC217A NC218A NC219A NC220M NC221A NC222A NC223A NC224A
NC225A NC231A NC232A NC233A NC234A NC235A NC236A NC237A NC238A NC245A NC246A NC247A
NC239A NC240A NC250A NC241A NC242A NC243A NC244A NC248A NC251A NC252A NC253A NC254A
NC302M
NC401M
IF101A IF102A IF103A IF104A IF105A IF106A IF107A IF108A IF109A IF110A
IF111A IF112A IF113A IF114A IF115A IF116A IF117A IF118A IF119A IF120A
IF121A IF122A IF123A IF124A IF125A IF126A IF127A IF128A IF129A IF130A
IF131A IF132A IF133A IF134A IF135A IF136A IF137A IF138A IF139A IF140A
IF141A IF142A
SM101A SM102A SM103A SM104A SM106A SM107A SM201A SM202A SM203A SM204A SM206A
SM207A SM208A SM105A SM205A
IC101A IC103A IC106A IC108A IC112A IC114A IC201A IC203A IC207A IC209A IC213A IC216A IC222A IC223A IC224A IC225A IC226A IC227A IC228A IC233A IC234A IC235A IC237A
SQ101M SQ102A SQ104A SQ105A SQ108A SQ111A SQ112A SQ113A SQ114A SQ116A SQ117A SQ121A SQ126A SQ127A
SQ133A SQ136A SQ144A SQ141A SQ142A SQ106A SQ107A SQ109M SQ110M SQ124A SQ128A SQ130A SQ131A SQ143A SQ146A SQ149A SQ150A SQ154A SQ155A SQ156A SQ202A SQ203A SQ204A SQ206A SQ207M SQ211A SQ212A SQ213A
SQ214A SQ216A SQ217A SQ218A SQ219A SQ220A SQ221A SQ222A SQ223A SQ224A SQ230A SQ227A SQ228A SQ201M SQ208M SQ209M SQ210M SQ302M
SQ103A SQ115A SQ122A SQ123A SQ125A SQ129A SQ132A SQ134A SQ135A SQ137A SQ138A SQ139A SQ140A SQ147A SQ148A SQ151A SQ152A SQ153A SQ205A SQ215A SQ225A SQ226A SQ229A
RL105A RL118A RL108A RL109A RL110A RL101A RL102A RL103A RL107A RL117A RL201A RL202A RL203A RL206A RL207A RL208A RL210A RL211A RL209A RL104A RL112A RL113A RL114A RL115A RL116A RL204A RL205A RL111A RL106A RL119A RL302M RL212A RL213A
IX101A IX102A IX103A IX104A IX105A IX106A IX107A IX108A IX109A IX110A IX111A IX112A IX113A IX114A IX115A IX116A IX117A IX118A IX119A IX120A IX121A IX201A IX202A IX203A IX204A IX205A IX206A IX207A IX208A IX209A IX210A IX211A IX212A IX213A IX214A IX215A IX216A IX217A IX218A IX302M
ST101A ST102A ST103A ST104A ST105A ST106A ST107A ST108A ST109A ST110A ST111A ST112M ST113M ST114M ST115A ST116A ST117A ST118A ST119A ST120A ST121A ST122A ST123A ST124A ST125A ST126A ST127A ST131A ST132A ST133A ST134A ST135A ST136A ST137A ST139A ST140A ST144A ST146A ST147A
OBSQ1A OBSQ3A OBSQ4A OBSQ5A
RW101A RW102A RW103A RW104A
"

# NIST convention: SWITCH-1 ON, SWITCH-2 OFF (default)
export COBOL_SWITCH_1=ON

# ── THE LEGACY-DIVERGENT EXEMPTION, DERIVED (kb/Work PB898) ───────────────────────────────────────────────
# Programs whose golden was RE-BASELINED to the ISO-conforming output (owner-approved, DEVLOG 569/570): the
# LEGACY's output legitimately differs — either a verified legacy NON-CONFORMANCE (process rule #1 — the ISO
# spec is authority; the legacy is a regression net with holes) or a different implementor choice of
# spec-UNDEFINED behavior. The guard still compiles and runs them, but the output diff is EXPECTED and is
# reported, never counted as a regression; the greenfield differential suite (NistDifferentialTests) locks the
# conforming goldens byte-exact.
#
# ⛔ THE SET AND ITS PER-PROGRAM RATIONALE LIVE IN tests/nist/corpus.tsv — status `divergent`, with the ISO
# citation in the note column, which CorpusManifestTests.EveryDivergent_CitesSpec enforces. This file used to
# carry BOTH a hand-written name list and a hand-written digest of those rationales, and the list had already
# drifted a program behind the manifest (THIRTEEN rows, TWELVE names; SQ212A missing), so its expected legacy
# difference scored as a REGRESSION. Read the set with its reasons:
#     awk -F'\t' '$3=="divergent"{print $1"\t"$6}' tests/nist/corpus.tsv
#
# ⭐ THE SET APPLIES TO THE LEGACY ONLY (kb/Work/PB750). Every divergence is one the LEGACY exhibits, so under
# the default compiler (`cobol`) these goldens are exactly what WiseOwl COBOL must reproduce —
# NistDifferentialTests already locks them byte-exact — and exempting them would blind the guard on the very
# programs a codegen regression is most likely to break. The variable is therefore emptied unless the run is
# the opt-in legacy differential. Both guards derive it from the ONE manifest, through the ONE reader in
# scripts/guard-population.sh, which is the same fact scripts/guard-nist-audit.sh already reads to decide what
# verdict each row is expected to produce.
#
# ⛔ A `TERMINATES` DIVERGENT ROW IS THE OTHER WAY ROUND (kb/Work PB1955): its golden records a run that continued
# past a fatal I-O status nothing covers, which WiseOwl COBOL's Annex A.1 item 103 choice ends. So GUARD_TERMINATES
# (`NAME=EC-…`, from the same reader) applies to `cobol` only: the run must end naming that exception. Under the
# legacy it is emptied and the row is compared with its golden like a green one; guard_legacy_divergent leaves
# TERMINATES rows out of the legacy exemption for the same reason.
. "$(dirname "$0")/guard-population.sh"
LEGACY_DIVERGENT="$(guard_legacy_divergent)" || exit 1
GUARD_TERMINATES="$(guard_terminating)" || exit 1
if [ "$GUARD_DIVERGENT" != "1" ]; then LEGACY_DIVERGENT=""; else GUARD_TERMINATES=""; fi

# ⛔ THE EVIDENCE RULES + THE VERDICT AUDIT (plan §11 A12b/A12c; DESIGN-test-build-ci.md §3.10). A verdict is
# produced only from an observation actually made: a compile is FAILED only with a non-zero rc AND diagnostic
# text; a non-matching run is a DIFF only if the process RAN TO COMPLETION and the COMPARISON itself completed
# over output that exists. Anything else is a NO-VERDICT — never MATCH, never REGRESSION.
# ⭐ THE RULES LIVE IN ONE PLACE: scripts/guard-verdict.sh, sourced by this guard AND by guard-run-group.sh. The
# comment here used to say the two blocks were kept "character-for-character in step" by hand; they had already
# drifted, and both carried the compare-arm hole kb/Work/PB473 found in battery #43.
# A non-MATCH's evidence outlives $GUARD_WORK, which the EXIT trap deletes (PB473 item 4). Set BEFORE the
# source: guard-verdict.sh installs its own standalone default, and `:-` would then keep that one.
GUARD_FORENSICS="${GUARD_FORENSICS:-${TMPDIR:-/tmp}/nist-forensics.$$}"
. "$(dirname "$0")/guard-verdict.sh"
RUN_TIMEOUT="${GUARD_RUN_TIMEOUT:-120}"
CMPDIR="$GUARD_WORK/compare"
VERDICTS="$GUARD_WORK/verdicts.txt"; : > "$VERDICTS"
v() { echo "  $1: $2"; echo "$1: $2" >> "$VERDICTS"; }     # one verdict line, echoed AND recorded

# GUARD_TESTS overrides the population with a subset — for exercising this loop's own branches, or for the
# owner's standing rule that a change should not re-run cases it cannot reach. The AUDIT still applies in full:
# it takes the population as an argument, so a subset run asserts a partition of the SUBSET and each member's
# verdict against the same committed manifest. ⚠ A subset run is NOT the regression gate; it proves the loop,
# not the corpus. The gate is the whole list (or scripts/guard-fast.sh, which is proven equivalent).
if [ -n "${GUARD_TESTS:-}" ]; then
    echo "  ⚠ GUARD_TESTS override in effect — running a SUBSET, not the regression gate: $GUARD_TESTS"
    NIST_TESTS="$GUARD_TESTS"
fi

FAILURES=0
for test in $NIST_TESTS; do
    # Compile — keep the diagnostics; they are what distinguishes a rejection from a lost result. The
    # invocation itself lives in scripts/guard-compile.sh (the two compilers spell --nist differently).
    clog="$NIST_OUT/$test.compile.log"
    bash "$(dirname "$0")/guard-compile.sh" "$test" "$NIST_PROGS/$test.cob" "$NIST_OUT"
    crc=$(cat "$NIST_OUT/$test.compile.rc" 2>/dev/null || echo "")
    if [ "$crc" != "0" ] || [ ! -f "$NIST_OUT/$test.dll" ]; then
        guard_compile_verdict "$test" "$crc" "$clog"
        v "$test" "$GUARD_VERDICT"
        if [ "$GUARD_CLASS" = "regression" ]; then FAILURES=$((FAILURES + 1)); fi
        continue
    fi

    # Run in the output directory; capture stdout for DISPLAY-only tests
    # Pipe NIST data file to stdin when available (for ACCEPT tests)
    outfile=$(echo "$test" | tr '[:upper:]' '[:lower:]').txt
    stdoutfile="$NIST_OUT/${test}-stdout.txt"
    errfile="$NIST_OUT/${test}-stderr.txt"
    datafile="$NIST_DATA/$test.dat"
    rrc=0
    if [ -f "$datafile" ]; then
        (cd $NIST_OUT && timeout -k 5 "$RUN_TIMEOUT" dotnet "$test.dll" 2>"$errfile") < "$datafile" > "$stdoutfile" || rrc=$?
    else
        (cd $NIST_OUT && timeout -k 5 "$RUN_TIMEOUT" dotnet "$test.dll" 2>"$errfile") > "$stdoutfile" || rrc=$?
    fi

    # Compare output (files written to $NIST_OUT/)
    validfile="$NIST_VALID/$test.txt"
    if [ ! -f "$validfile" ]; then
        # No baseline = test has known failures. Still compile/run but don't compare.
        fail_count=$(grep -c "FAIL\*" "$NIST_OUT/$outfile" 2>/dev/null || true)
        fail_count=${fail_count:-0}
        v "$test" "NO BASELINE (${fail_count} FAIL* — pending fix)"
        continue
    fi

    # A row corpus.tsv declares TERMINATES (kb/Work PB1955; see GUARD_TERMINATES above): the run unit's end on
    # the named exception is the observation, not the report.
    ec="$(guard_declared_termination "$test")"
    if [ -n "$ec" ]; then
        guard_termination_verdict "$test" "$ec" "$rrc" "$errfile" "$RUN_TIMEOUT"
        v "$test" "$GUARD_VERDICT"
        if [ "$GUARD_CLASS" = "regression" ]; then FAILURES=$((FAILURES + 1)); fi
        if [ "$GUARD_CLASS" != "match" ]; then
            guard_preserve "$test" "$GUARD_VERDICT" "$NIST_OUT/$outfile" "$NIST_OUT/print-file.txt" "$stdoutfile" "$errfile"
        fi
        continue
    fi

    # An ISO-re-baselined golden the legacy legitimately diverges from: compiled and ran above; the diff is
    # expected (see LEGACY_DIVERGENT) — never a regression.
    case " $LEGACY_DIVERGENT " in *" $test "*)
        v "$test" "LEGACY DIVERGENT (golden = ISO-conforming baseline; expected diff)"
        continue ;;
    esac

    # RUN + COMPARE arms — normalization, candidate resolution (outfile, print-file, stdout), the FAIL*/footer
    # rules and the evidence rules all live in scripts/guard-verdict.sh, which guard-run-group.sh calls too.
    guard_output_verdict "$test" "$validfile" "$rrc" "$errfile" "$RUN_TIMEOUT" "$CMPDIR" \
        "$NIST_OUT/$outfile" "$NIST_OUT/print-file.txt" "$stdoutfile"
    v "$test" "$GUARD_VERDICT"
    if [ "$GUARD_CLASS" = "regression" ]; then FAILURES=$((FAILURES + 1)); fi
    if [ "$GUARD_CLASS" != "match" ]; then
        guard_preserve "$test" "$GUARD_VERDICT" \
            "$NIST_OUT/$outfile" "$NIST_OUT/print-file.txt" "$stdoutfile" "$errfile" \
            "$CMPDIR/expected.norm" "$CMPDIR/actual.norm"
    fi
done

# ⛔ THE POPULATION + EXPECTATION AUDIT (plan §11 A12c). $FAILURES is computed from the tests that REPORTED, so
# it cannot see a program that produced nothing. The audit asserts the run is a partition of the declared
# population and that each verdict is the one tests/nist/corpus.tsv predicts.
echo "$NIST_TESTS" | tr ' ' '\n' | grep . | sort > "$GUARD_WORK/population.txt"
NIST_AUDIT=0
bash "$(dirname "$0")/guard-nist-audit.sh" "$VERDICTS" "$GUARD_WORK/population.txt" "$GUARD_COMPILER" || NIST_AUDIT=$?

if [ $FAILURES -gt 0 ] || [ $NIST_AUDIT -ne 0 ]; then
    echo "=== NIST ($GUARD_COMPILER): $FAILURES REGRESSION(S), audit rc=$NIST_AUDIT ==="
    if [ -d "$GUARD_FORENSICS" ]; then
        echo "=== EVIDENCE for every non-MATCH (report, stdout, stderr, both normalized sides): $GUARD_FORENSICS ==="
        ls "$GUARD_FORENSICS"
    fi
    exit 1
fi

# Verify every baseline is clean and non-vacuous — baselines must be 100% trustworthy. ONE implementation,
# shared with guard-fast.sh (scripts/guard-baselines.sh): a 0-byte baseline matches an empty or crashed run
# vacuously, a FAIL* detail line is a real failure, a non-zero "NNN TEST(S) FAILED" footer is a real failure
# even with no FAIL* line — unless the program is DECLARED a CCVS defect in tests/nist/corpus.tsv, the same
# rule and the same file CorpusManifestTests states as a set equality (kb/Work PB436).
bash "$(dirname "$0")/guard-baselines.sh" "$NIST_VALID" tests/nist/corpus.tsv
FAILURES=$((FAILURES + $?))
if [ $FAILURES -gt 0 ]; then
    echo "=== BASELINES ($GUARD_COMPILER): $FAILURES defect(s) above ==="
    exit 1
fi

# The verdict says what it measured; `=== ALL GREEN ===` itself stays byte-identical because callers gate on it.
echo "=== COMPILER UNDER TEST WAS: $GUARD_COMPILER ($GUARD_CLI_DLL) ==="
echo "=== ALL GREEN ==="
