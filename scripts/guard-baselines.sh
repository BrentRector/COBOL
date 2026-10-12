#!/usr/bin/env bash
# guard-baselines.sh — THE baseline-cleanliness audit over tests/nist/valid, in ONE place.
#
# usage: bash scripts/guard-baselines.sh [<valid-dir>] [<corpus.tsv>]
# exits with the NUMBER of failures (0 = clean), and prints one `=== ERROR: … ===` line per failure.
#
# ⛔ WHY IT IS ITS OWN FILE. `guard.sh` and `guard-fast.sh` each carried a byte-similar copy of this loop —
# `guard-fast.sh`'s comment even said "parity with guard.sh" — and when kb/Work PB436 gave ONE golden a
# declared, spec-derived CCVS failure it taught `guard-verdict.sh`'s comparison arm about the allowance and
# NEITHER copy of this one. The Windows gate never runs these bash legs, so the train was green locally and
# the CI Guard job was red on `NC201A.txt has 1 FAIL*` (landing train 36). One rule, one place.
#
# The three things a baseline must not be, and the one thing it may be:
#   (1) 0 bytes — it would match an empty or crashed run vacuously;
#   (2) carrying a `FAIL*` detail line;
#   (3) carrying a non-zero `NNN TEST(S) FAILED` footer (real even with no FAIL* line — the IX108A shape);
#   … UNLESS the program is DECLARED a CCVS defect in the corpus manifest — a row whose note carries the
#   `CCVS-DEFECT` marker, which `EveryDivergent_CitesSpec` forces to carry an ISO § because the declaration
#   must be `divergent`. That is the same rule, read from the same file, that
#   `CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects` states as a SET
#   EQUALITY — and this script measures the selector's COMPLEMENT for the same reason it does: a declaration
#   left behind after a golden is repaired is as much a defect as an undeclared failure.
set -u

VALID="${1:-tests/nist/valid}"
MANIFEST="${2:-tests/nist/corpus.tsv}"
MARKER="CCVS-DEFECT"
FAILS=0

# The declared allowance, read from the ONE manifest. Columns: name suite status chain-preds golden note.
# `tr -d '\r'`: corpus.tsv is LF (kb/Work PB3029), and a stray CR must never ride into a field.
DECLARED=""
if [ -f "$MANIFEST" ]; then
    DECLARED=$(tr -d '\r' < "$MANIFEST" | awk -F'\t' -v m="$MARKER" \
        '!/^#/ && NF >= 6 && index($6, m) { print $1 }')
    # A declaration is a spec adjudication, so it takes the `divergent` status whose note is citation-checked.
    NOT_DIV=$(tr -d '\r' < "$MANIFEST" | awk -F'\t' -v m="$MARKER" \
        '!/^#/ && NF >= 6 && index($6, m) && $3 != "divergent" { print $1 }')
    for p in $NOT_DIV; do
        echo "=== ERROR: $p is declared $MARKER but its corpus.tsv status is not \`divergent\` — the ISO citation is then unenforced ==="
        FAILS=$((FAILS + 1))
    done
fi

declared_p() {
    for d in $DECLARED; do [ "$d" = "$1" ] && return 0; done
    return 1
}

CARRYING=""
for f in "$VALID"/*.txt; do
    [ -e "$f" ] || continue
    base=$(basename "$f" .txt)
    if [ ! -s "$f" ]; then
        echo "=== ERROR: $(basename "$f") is EMPTY — a 0-byte baseline passes vacuously; remove from valid/ ==="
        FAILS=$((FAILS + 1))
        continue
    fi
    fc=$(grep -c "FAIL\*" "$f" 2>/dev/null || true); fc=${fc:-0}
    ff=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$f" 2>/dev/null | grep -oE "^[0-9]+" | head -1); ff=${ff:-0}
    if [ "$fc" -gt 0 ] 2>/dev/null || [ "$ff" -gt 0 ] 2>/dev/null; then
        CARRYING="$CARRYING $base"
        if declared_p "$base"; then
            echo "=== NOTE: $(basename "$f") carries ${fc} FAIL* / footer ${ff} — DECLARED $MARKER in $(basename "$MANIFEST") ==="
            continue
        fi
        [ "$fc" -gt 0 ] 2>/dev/null && {
            echo "=== ERROR: $(basename "$f") contains $fc FAIL* — repair the compiler, or declare it $MARKER in $(basename "$MANIFEST") ==="
            FAILS=$((FAILS + 1)); }
        [ "$ff" -gt 0 ] 2>/dev/null && {
            echo "=== ERROR: $(basename "$f") footer reports $ff TEST(S) FAILED — repair the compiler, or declare it $MARKER in $(basename "$MANIFEST") ==="
            FAILS=$((FAILS + 1)); }
    fi
done

# ⛔ THE COMPLEMENT. A declaration that no longer describes anything is how an allowance rots into a hole.
for d in $DECLARED; do
    found=0
    for c in $CARRYING; do [ "$c" = "$d" ] && found=1; done
    if [ "$found" -eq 0 ] && [ -f "$VALID/$d.txt" ]; then
        echo "=== ERROR: $d is declared $MARKER but its baseline carries no failure — drop the declaration ==="
        FAILS=$((FAILS + 1))
    fi
done

exit "$FAILS"
