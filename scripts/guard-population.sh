#!/bin/bash
# guard-population.sh — THE populations the NIST guards run, DERIVED from tests/nist/corpus.tsv.
#
# ⛔ WHY THIS FILE EXISTS (kb/Work PB898). `scripts/guard.sh` carried the divergent set as a hand-written string
# and `scripts/guard-fast.sh` `sed`-extracted THAT string, so the manifest — which the header of corpus.tsv
# itself calls the ONE source of truth, and which says it "folds … scripts/guard.sh LEGACY_DIVERGENT" — had a
# second copy nobody compared it to. The copy had drifted: the manifest declared THIRTEEN divergent programs and
# the string named TWELVE. `SQ212A` was the missing one, so under `GUARD_DIVERGENT=1` its EXPECTED legacy
# difference was scored as a REGRESSION.
#
# ⭐ And the tie-break needs no new evidence, because a THIRD reader already derives this very set from the
# manifest: `scripts/guard-nist-audit.sh` computes `expect[name] = (status == "divergent" && compiler ==
# "legacy") ? "LEGACY DIVERGENT" : …` straight out of corpus.tsv. The runner and its auditor disagreed about
# SQ212A while reading the same fact from two places. Deriving here makes them one reader, which is the fix
# CLAUDE.md rule 5 asks for — never a hand-maintained list where a structure belongs.
#
# ⛔ AND A `divergent` ROW IS ONE OF TWO KINDS (kb/Work PB1955), told apart by its note:
#   · a plain divergent row: the golden is the ISO-conforming output, which WiseOwl COBOL reproduces byte-exact and
#     the LEGACY legitimately differs from (the legacy run is exempt: LEGACY DIVERGENT);
#   · a TERMINATES row, whose note begins `TERMINATES EC-<name>`: the golden records a run that CONTINUED past a
#     fatal I-O status nothing covers, which WiseOwl COBOL's documented choice under ISO §9.1.13.1 ("The implementor
#     may either continue or terminate the execution of the run unit"; Annex A.1 item 103, docs/CONFORMANCE.md
#     DOC-A.1-103) ENDS. Both readings invert: WiseOwl COBOL must terminate on that EC, and the legacy, which
#     continues, is compared with the golden like any green program.
# Reading every `divergent` row the first way turned CI's guard red on PB322's two TERMINATES rows (CI run
# 37217227958). The marker is written down here ONCE for the shell (the two runners and the audit all ask this
# file) and once for C# (CorpusRow.ExpectedTermination in tests/Cobol.Net.Tests.Conformance/CorpusManifest.cs);
# CorpusManifestTests holds the two to the same grammar.
#
# Sourced, never executed, except for `--self-test`:
#     . "$(dirname "$0")/guard-population.sh"
#     LEGACY_DIVERGENT="$(guard_legacy_divergent)" || exit 1
#     GUARD_TERMINATES="$(guard_terminating)" || exit 1
#
# `GUARD_CORPUS_TSV` overrides the manifest path — for the self-test, and for nothing else.

GUARD_POPULATION_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GUARD_CORPUS_TSV="${GUARD_CORPUS_TSV:-$GUARD_POPULATION_ROOT/tests/nist/corpus.tsv}"

# The TERMINATES marker, as an awk ERE over the note column (column 6). The C# reader's pattern is the same
# grammar (CorpusRow.ExpectedTermination), and CorpusManifestTests asserts this literal against it.
GUARD_TERMINATES_MARKER='^TERMINATES EC-[A-Z0-9-]+'

# guard_manifest_present corpus.tsv — loud when the manifest is missing; every reader below starts here.
guard_manifest_present() {
    if [ ! -f "$1" ]; then
        echo "guard-population: the NIST manifest is missing: $1" >&2
        echo "guard-population: the guard's populations are DERIVED from it and cannot be guessed." >&2
        return 1
    fi
}

# guard_legacy_divergent [corpus.tsv] — the space-joined names of every PLAIN `divergent` row (a TERMINATES row is
# not one: the legacy reproduces its golden), on stdout.
#
# ⛔ AN EMPTY ANSWER IS A FAILURE, NOT A POPULATION. Returning "" for an unreadable or malformed manifest would
# silently un-exempt every divergent program and report a wall of regressions that mean nothing — a missing
# observation is not a negative one (DESIGN-test-build-ci.md §3.10). Both failure arms print to stderr and
# return non-zero, and every caller stops. The emptiness test is on the `divergent` COLUMN, before the TERMINATES
# rows are set aside: a manifest whose every divergent row terminated would legitimately exempt nothing.
guard_legacy_divergent() {
    local -
    set -o pipefail   # an awk that could not read the manifest is a failure, never an empty exemption set
    local corpus="${1:-$GUARD_CORPUS_TSV}"
    guard_manifest_present "$corpus" || return 1
    # Column 3 is status(green|divergent|pending); a leading # is a comment (corpus.tsv's own header).
    if ! awk -F'\t' '$1 !~ /^#/ && $3 == "divergent" { found = 1 } END { exit !found }' "$corpus"; then
        echo "guard-population: $corpus declares NO divergent rows." >&2
        echo "guard-population: that is either a broken manifest or a broken reader — either way the guard must" >&2
        echo "guard-population: not proceed as though every legacy divergence were a regression." >&2
        return 1
    fi
    awk -F'\t' -v m="$GUARD_TERMINATES_MARKER" '$1 !~ /^#/ && $3 == "divergent" && $6 !~ m { printf "%s ", $1 }' \
        "$corpus" | sed 's/[[:space:]]*$//' || return 1
    echo
}

# guard_terminating [corpus.tsv] — `NAME=EC-…` for every TERMINATES row, space-joined, on stdout. Empty is a real
# answer (no program is declared to terminate); a missing manifest is still loud. "TERMINATES " is 11 characters,
# so the exception-name starts at column 12 of the note and runs to the end of the marker's match.
guard_terminating() {
    local -
    set -o pipefail   # an awk that could not read the manifest is a failure, never "no TERMINATES rows"
    local corpus="${1:-$GUARD_CORPUS_TSV}"
    guard_manifest_present "$corpus" || return 1
    awk -F'\t' -v m="$GUARD_TERMINATES_MARKER" '
        $1 !~ /^#/ && $3 == "divergent" && match($6, m) { printf "%s=%s ", $1, substr($6, 12, RLENGTH - 11) }' \
        "$corpus" | sed 's/[[:space:]]*$//' || return 1
    echo
}

# guard_declared_termination NAME — the exception-name GUARD_TERMINATES declares NAME terminates on, or nothing.
# Each runner sets GUARD_TERMINATES from guard_terminating for WiseOwl COBOL only; under the legacy it is empty, so
# a TERMINATES row is compared with its golden there like any green row.
guard_declared_termination() {
    local pair
    for pair in ${GUARD_TERMINATES:-}; do
        if [ "${pair%%=*}" = "$1" ]; then printf '%s\n' "${pair#*=}"; return 0; fi
    done
    return 0
}

# ── SELF-TEST ─────────────────────────────────────────────────────────────────────────────────────────────
# A check that has never been observed failing is not evidence (feedback_green_gates_arent_evidence), and BOTH
# loud arms above exist precisely because their silent versions would look green.
# Only when EXECUTED: a sourcing script's own `--self-test` argument (guard-nist-audit.sh has one) is not this one's.
if [ "${BASH_SOURCE[0]}" = "$0" ] && [ "${1:-}" = "--self-test" ]; then
    gp_checks=0; gp_fail=0
    gp_assert() {   # gp_assert <what> <condition-rc>
        gp_checks=$((gp_checks + 1))
        if [ "$2" -eq 0 ]; then echo "  ok   $1"; else echo "  FAIL $1"; gp_fail=$((gp_fail + 1)); fi
    }
    gp_tmp="$(mktemp -d)"
    trap 'rm -rf "$gp_tmp"' EXIT

    # (1) The real manifest yields a non-empty set that matches an independent awk over the same column.
    real="$(guard_legacy_divergent)"; rc=$?
    gp_assert "the real manifest derives a divergent set" "$rc"
    independent=$(awk -F'\t' '$3=="divergent" && index($6, "TERMINATES EC-") != 1 {print $1}' "$GUARD_CORPUS_TSV" \
                  | tr '\n' ' ' | sed 's/[[:space:]]*$//')
    [ "$real" = "$independent" ]; gp_assert "the derived set equals the manifest's plain divergent rows" $?
    guard_terminating > /dev/null; gp_assert "the real manifest derives a TERMINATES set (empty is an answer)" $?

    # (2) ⛔ A MISSING MANIFEST IS LOUD, not an empty exemption list.
    guard_legacy_divergent "$gp_tmp/absent.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "a missing manifest returns non-zero" $?

    # (3) ⛔ A manifest with no divergent rows is LOUD too — the shape that would un-exempt everything.
    printf '# header\nNC101A\tNC\tgreen\t-\tvalid\t-\n' > "$gp_tmp/nodiv.tsv"
    guard_legacy_divergent "$gp_tmp/nodiv.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "a manifest with no divergent rows returns non-zero" $?

    # (4) A comment line whose first field would otherwise match is ignored.
    printf '# ZZ999A\tZZ\tdivergent\t-\tvalid\t-\nNC101A\tNC\tdivergent\t-\tvalid\t-\n' > "$gp_tmp/cmt.tsv"
    [ "$(guard_legacy_divergent "$gp_tmp/cmt.tsv")" = "NC101A" ]; gp_assert "comment rows are not population" $?

    # (5) ⛔ THE PB1955 SHAPE. A TERMINATES row leaves the legacy exemption and enters the TERMINATES set with its
    #     exception-name; a plain divergent row beside it is untouched; a note that only MENTIONS the word, or
    #     spells the marker wrong, is a plain divergent row (CorpusManifestTests refuses the misspelling).
    printf 'NC401M\tNC\tdivergent\t-\tvalid\tTERMINATES EC-I-O-PERMANENT-ERROR - ISO 9.1.13.1: the run unit ends\n' > "$gp_tmp/term.tsv"
    printf 'ST146A\tST\tdivergent\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - ISO 9.1.13.1: status 47\n' >> "$gp_tmp/term.tsv"
    printf 'NC201A\tNC\tdivergent\t-\tvalid\tISO 14.9.28.4 CCVS-DEFECT: the golden carries one failure\n' >> "$gp_tmp/term.tsv"
    printf 'SQ212A\tSQ\tdivergent\t-\tvalid\tISO 9.1.13.1: the legacy TERMINATES EC-I-O where this does not\n' >> "$gp_tmp/term.tsv"
    printf 'SQ213A\tSQ\tgreen\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - a green row is never a TERMINATES row\n' >> "$gp_tmp/term.tsv"
    [ "$(guard_legacy_divergent "$gp_tmp/term.tsv")" = "NC201A SQ212A" ]
    gp_assert "a TERMINATES row is not a legacy-divergent row" $?
    [ "$(guard_terminating "$gp_tmp/term.tsv")" = "NC401M=EC-I-O-PERMANENT-ERROR ST146A=EC-I-O-LOGIC-ERROR" ]
    gp_assert "guard_terminating names each TERMINATES row with its exception-name" $?
    [ -z "$(guard_terminating "$gp_tmp/nodiv.tsv")" ]; gp_assert "no TERMINATES row is an empty answer" $?
    guard_terminating "$gp_tmp/absent.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "guard_terminating on a missing manifest returns non-zero" $?
    [ "$(GUARD_TERMINATES="$(guard_terminating "$gp_tmp/term.tsv")" guard_declared_termination ST146A)" = "EC-I-O-LOGIC-ERROR" ]
    gp_assert "guard_declared_termination finds a declared program's exception-name" $?
    [ -z "$(GUARD_TERMINATES="$(guard_terminating "$gp_tmp/term.tsv")" guard_declared_termination NC201A)" ]
    gp_assert "guard_declared_termination says nothing for an undeclared program" $?
    [ -z "$(GUARD_TERMINATES="" guard_declared_termination NC401M)" ]
    gp_assert "an empty GUARD_TERMINATES (the legacy run) declares nothing" $?

    if [ "$gp_fail" -eq 0 ]; then
        echo "=== GUARD POPULATION SELF-TEST: PASS ($gp_checks checks) ==="; exit 0
    fi
    echo "=== GUARD POPULATION SELF-TEST: FAIL ($gp_fail of $gp_checks checks) ==="; exit 1
fi
