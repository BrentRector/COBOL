*>--- conformance-case
*> id: l1_move_corr_equivalence
*> rules: [SR-14.9.25.3-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.25.3 SR11 MOVE statement (FORMAT 2) — "The words CORR and CORRESPONDING are
      *> equivalent."
      *>   python scripts/spec/cite.py --check 14.9.25.3 "The words CORR and CORRESPONDING are
      *>   equivalent."  ->  OK  §14.9.25.3 11)  (Syntax rules)
      *>
      *> DERIVATION. SR11 makes the two words ONE word, so a Format-2 MOVE written with CORR and the
      *> same statement written with CORRESPONDING have the same meaning and §14.9.25.4 GR11 applies
      *> to both identically:
      *>   python scripts/spec/cite.py --check 14.9.25.4 "The results are the same as if the user
      *>   had referred to each pair of corresponding identifiers in separate MOVE statements"
      *>   ->  OK  §14.9.25.4 11)  (General rules)
      *> The pairs themselves come from §14.7.6, CORRESPONDING phrase: a pair corresponds when the
      *> two items "have the same data-name and the same qualifiers, if any, up to, but not
      *> including, D1 and D2" (rule 1), at least one is elementary and the resulting move is valid
      *> (rule 2), and neither carries OCCURS / REDEFINES / RENAMES (rule 4). So:
      *>   F-A and F-B correspond (same name, elementary in both, valid MOVEs);
      *>   F-C exists only in G1 and F-D only in G2 / G3 — neither is in a corresponding pair, so
      *>   F-D is NOT a receiving operand and keeps its VALUE.
      *>
      *> ⛔ WHY G3 EXISTS. Equivalence is a claim about the two WORDS, so the two statements have to
      *> start from identical states: G2 and G3 carry the same picture strings and the same VALUE
      *> clauses, and only the spelling of the Format-2 word differs between the two statements.
      *> A single receiver written twice would prove nothing — the second MOVE would find the work
      *> of the first already done and agree for free.
      *>
      *> EXPECTED OUTPUT, line by line, derived from GR11 + §14.7.6 (each pair is a separate
      *> elementary MOVE) and never from what the compiler emits:
      *>   line 1/2  F-A: alphanumeric to alphanumeric, §14.9.25.4 GR6a — equal sizes (3), so the
      *>             content transfers unchanged: XYZ. F-B: numeric to numeric, GR6d — the value 123
      *>             aligned by decimal point into PIC 9(3): 123. F-D: untouched, so still ??.
      *>             The two lines are therefore CHARACTER-IDENTICAL, which is SR11's whole content.
      *>   line 3    G2 and G3 are group items of equal length (3+3+2 = 8) holding the same
      *>             characters, so the alphanumeric comparison finds them equal: SAME=T.
      *>
      *> One golden, at 2023 only: SR11 carries no edition qualification, CORR and CORRESPONDING are
      *> reserved words in every edition this compiler targets, and no rule makes the equivalence
      *> behave differently at 85 / 2002 / 2014 — there is no per-edition branch to pin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1MVCORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
           05 F-A PIC X(3) VALUE "XYZ".
           05 F-B PIC 9(3) VALUE 123.
           05 F-C PIC X(2) VALUE "PQ".
       01 G2.
           05 F-A PIC X(3) VALUE "abc".
           05 F-B PIC 9(3) VALUE 7.
           05 F-D PIC X(2) VALUE "??".
       01 G3.
           05 F-A PIC X(3) VALUE "abc".
           05 F-B PIC 9(3) VALUE 7.
           05 F-D PIC X(2) VALUE "??".
       PROCEDURE DIVISION.
       MAIN.
           MOVE CORR G1 TO G2
           MOVE CORRESPONDING G1 TO G3
           DISPLAY "G2=[" F-A OF G2 "|" F-B OF G2 "|" F-D OF G2 "]"
           DISPLAY "G3=[" F-A OF G3 "|" F-B OF G3 "|" F-D OF G3 "]"
           IF G2 = G3 DISPLAY "SAME=T" ELSE DISPLAY "SAME=F" END-IF
           STOP RUN.
