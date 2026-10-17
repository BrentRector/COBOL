*>--- conformance-case
*> id: stat-mean-arg-class-numeric
*> rules: [AR-15.60.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §15.60.3 rule 1 (MEAN): "Argument-1 shall be of class numeric." It is the
*> ONLY argument rule §15.60.3 has, so any argument diagnostic this program draws
*> is that rule and no other.
*>
*> §15.60.2's general format is `FUNCTION MEAN ( { argument-1 } ... )`: every
*> written position IS argument-1, so the rule governs the whole variadic list.
*> This fixture puts the non-numeric operand at POSITION 1 (its sibling fixtures
*> in this batch take positions 2, 3 and 4, which is how the per-position schema
*> is shown to screen every position rather than only the first).
*>
*> WHY A PIC X(3) ITEM HOLDING "100" IS NOT CLASS NUMERIC. §8.5.2.1 Table 2 puts
*> category alphanumeric in class ALPHANUMERIC; the rule is on the argument's
*> CLASS, never on the digits its VALUE happens to hold. §15.3 argument type 10
*> (Numeric) admits "an arithmetic expression or a numeric data item", and
*> §8.8.1.1 admits as an arithmetic expression "an identifier referencing a
*> numeric data item" — which an alphanumeric item is not.
*>
*> The legal complement is 2023/pb62_standard_decimal_summing_family (its MEAN=
*> line takes two class-numeric arguments) and must keep compiling: this screen
*> may only ever reject what §15.60.3 r1 excludes.
*>
*> ⛔ THE EDITION WINDOW IS 2014 2023 AND MUST NOT BE WIDENED BACK. AR-15.60.3-1's
*> own adjudication records the wider "85, 2002, 2014, 2023" as REFUTED, because
*> it was taken from the CODE UNDER REVIEW — IntrinsicCatalog's `Add(new("MEAN",
*> ..., 85))` row plus `--std` probes of this compiler — and the 2023 standard
*> cannot establish when MEAN became available. Annex E covers only 2014→2023,
*> §8.11 lists intrinsic NAMES without edition data, and the repo holds no 2002
*> or 2014 text (ratified decision #1; docs/VERSION_CHANGE_REFERENCE.md row 7.19).
*> An 85 or 2002 leg here would freeze the catalog's own window as conformance.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1MEAN01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "100".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION MEAN(A, 5).
    STOP RUN.
