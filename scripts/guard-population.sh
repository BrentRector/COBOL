#!/bin/bash
# guard-population.sh — THE populations the NIST guards run, DERIVED from tests/nist/corpus.tsv.
#
# ⛔ WHY THIS FILE EXISTS (kb/Work PB898). A fact about the NIST rows that more than one guard script needs is read
# from the manifest HERE, once, never copied into a script as a hand-written name list: `scripts/guard.sh` once
# carried the divergent set as a string that `scripts/guard-fast.sh` `sed`-extracted, and the copy drifted a
# program (SQ212A) behind the manifest while the audit, reading the manifest itself, disagreed with both.
#
# ⛔ THE ONE FACT THE GUARDS STILL NEED: WHICH `divergent` ROWS TERMINATE (kb/Work PB1955). A `divergent` row whose
# note begins `TERMINATES EC-<name>` has a golden that records a run that CONTINUED past a fatal I-O status nothing
# covers, which WiseOwl COBOL's documented choice under ISO §9.1.13.1 ("The implementor may either continue or
# terminate the execution of the run unit"; Annex A.1 item 103, docs/CONFORMANCE.md DOC-A.1-103) ENDS, so the run
# must end naming that exception. Every other `divergent` row is compared with its golden like a green one (the
# legacy engine's exemption list for them is retired with the engine, kb/Work PB2109). The marker is written down
# here ONCE for the shell (the two runners and the audit all ask this file) and once for C#
# (CorpusRow.ExpectedTermination in tests/Cobol.Net.Tests.Conformance/CorpusManifest.cs); CorpusManifestTests holds
# the two to the same grammar.
#
# Sourced, never executed, except for `--self-test`:
#     . "$(dirname "$0")/guard-population.sh"
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
# Each runner sets GUARD_TERMINATES from guard_terminating before it scores any program.
guard_declared_termination() {
    local pair
    for pair in ${GUARD_TERMINATES:-}; do
        if [ "${pair%%=*}" = "$1" ]; then printf '%s\n' "${pair#*=}"; return 0; fi
    done
    return 0
}

# ── SELF-TEST ─────────────────────────────────────────────────────────────────────────────────────────────
# A check that has never been observed failing is not evidence (feedback_green_gates_arent_evidence), and the loud
# arm above exists precisely because its silent version would look green.
# Only when EXECUTED: a sourcing script's own `--self-test` argument (guard-nist-audit.sh has one) is not this one's.
if [ "${BASH_SOURCE[0]}" = "$0" ] && [ "${1:-}" = "--self-test" ]; then
    gp_checks=0; gp_fail=0
    gp_assert() {   # gp_assert <what> <condition-rc>
        gp_checks=$((gp_checks + 1))
        if [ "$2" -eq 0 ]; then echo "  ok   $1"; else echo "  FAIL $1"; gp_fail=$((gp_fail + 1)); fi
    }
    gp_tmp="$(mktemp -d)"
    trap 'rm -rf "$gp_tmp"' EXIT

    # (1) The real manifest yields the set an independent awk over the same column derives.
    real="$(guard_terminating)"; rc=$?
    gp_assert "the real manifest derives a TERMINATES set" "$rc"
    independent=$(awk -F'\t' '$1 !~ /^#/ && $3=="divergent" && index($6, "TERMINATES EC-") == 1 {
                      split($6, w, " "); printf "%s=%s ", $1, w[2] }' "$GUARD_CORPUS_TSV" | sed 's/[[:space:]]*$//')
    [ "$real" = "$independent" ]; gp_assert "the derived set equals the manifest's TERMINATES rows" $?

    # (2) A comment line whose fields would otherwise match is ignored.
    printf '# ZZ999A\tZZ\tdivergent\t-\tvalid\tTERMINATES EC-I-O\nNC101A\tNC\tdivergent\t-\tvalid\tTERMINATES EC-I-O\n' > "$gp_tmp/cmt.tsv"
    [ "$(guard_terminating "$gp_tmp/cmt.tsv")" = "NC101A=EC-I-O" ]; gp_assert "comment rows are not population" $?
    printf '# header\nNC101A\tNC\tgreen\t-\tvalid\t-\n' > "$gp_tmp/nodiv.tsv"

    # (3) ⛔ THE PB1955 SHAPE. A TERMINATES row enters the TERMINATES set with its exception-name; a plain divergent
    #     row beside it is untouched; a note that only MENTIONS the word, or spells the marker wrong, is a plain
    #     divergent row (CorpusManifestTests refuses the misspelling).
    printf 'NC401M\tNC\tdivergent\t-\tvalid\tTERMINATES EC-I-O-PERMANENT-ERROR - ISO 9.1.13.1: the run unit ends\n' > "$gp_tmp/term.tsv"
    printf 'ST146A\tST\tdivergent\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - ISO 9.1.13.1: status 47\n' >> "$gp_tmp/term.tsv"
    printf 'NC201A\tNC\tdivergent\t-\tvalid\tISO 14.9.28.4 CCVS-DEFECT: the golden carries one failure\n' >> "$gp_tmp/term.tsv"
    printf 'SQ212A\tSQ\tdivergent\t-\tvalid\tISO 9.1.13.1: another compiler TERMINATES EC-I-O where this does not\n' >> "$gp_tmp/term.tsv"
    printf 'SQ213A\tSQ\tgreen\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - a green row is never a TERMINATES row\n' >> "$gp_tmp/term.tsv"
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
    gp_assert "an empty GUARD_TERMINATES declares nothing" $?

    if [ "$gp_fail" -eq 0 ]; then
        echo "=== GUARD POPULATION SELF-TEST: PASS ($gp_checks checks) ==="; exit 0
    fi
    echo "=== GUARD POPULATION SELF-TEST: FAIL ($gp_fail of $gp_checks checks) ==="; exit 1
fi
