*>--- conformance-case
*> id: pb51_compute_figurative_zero
*> rules: [GR-8.3.3.6.4-4, GR-14.9.8.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.1.1: "An arithmetic expression may be an identifier referencing
      *> a numeric data item, a numeric literal, THE FIGURATIVE CONSTANT ZERO
      *> (ZEROS, ZEROES), …" — so `COMPUTE <numeric> = ZERO` is arithmetic-
      *> expression-1 of 14.9.8 Format 1, and 8.3.3.6.4 GR4 makes the figurative
      *> "the numeric value '0' … depending on context", a numeric receiver being
      *> exactly that context.
      *>
      *> IT WAS REJECTED (fix-queue PB51) with "a boolean COMPUTE expression
      *> shall not consist solely of an ALL literal (14.9.8 Format 2 SR3)" — a
      *> diagnostic naming a construct the source does not contain. A bare ZERO
      *> is adjacent to no operator or paren, so ZeroTokenRewriter leaves it
      *> figurative and Format 1's arithmeticExpression cannot match it; the
      *> parser takes Format 2, whose valueOperand leaf admits the figurative,
      *> and BindBoolExpr then normalises it to the same node ALL B"0" produces.
      *>
      *> The fix is the MIRROR of the F1->F2 re-route the binder already carried:
      *> a Format-2 parse whose source is a bare ZERO and whose receiver is not
      *> boolean is Format 1. The test is on the PARSE TREE, because the bound
      *> node has already lost the distinction.
      *>
      *> ⚙ MEASURED SCOPE: every other arithmetic position already accepted a
      *> bare ZERO — ADD/SUBTRACT/MULTIPLY, the GIVING forms, IF and MOVE each
      *> have their own operand rules admitting the figurative. COMPUTE is the
      *> one verb whose RHS is a bare arithmeticExpression, which is why this is
      *> a targeted re-route and not a grammar change.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB51COMPZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4)V99 VALUE 7.
       01 I PIC S9(4) VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
      *> 1-3 — all three spellings of the Format-1 figurative (8.3.3.6.2).
           COMPUTE N = ZERO.
           DISPLAY "01-ZERO=" N.
           MOVE 7 TO N.
           COMPUTE N = ZEROS.
           DISPLAY "02-ZEROS=" N.
           MOVE 7 TO N.
           COMPUTE N = ZEROES.
           DISPLAY "03-ZEROES=" N.
      *> 4-5 — the re-route preserves the statement's other phrases.
           MOVE 7 TO N.
           COMPUTE N ROUNDED = ZERO.
           DISPLAY "04-ROUNDED=" N.
           MOVE 7 TO N.
           COMPUTE N = ZERO ON SIZE ERROR DISPLAY "SIZE" END-COMPUTE.
           DISPLAY "05-SIZEERR=" N.
      *> 6 — multiple receivers still take the value.
           MOVE 7 TO N.
           COMPUTE N I = ZERO.
           DISPLAY "06-MULTI=" N " " I.
      *> 7-9 — the CONTROLS. A bare ZERO already worked in every other
      *> arithmetic position, and must keep working.
           COMPUTE N = ZERO + 3.
           DISPLAY "07-ZERO-PLUS=" N.
           MOVE 7 TO N.
           ADD ZERO TO N.
           DISPLAY "08-ADD-ZERO=" N.
           MOVE ZERO TO N.
           DISPLAY "09-MOVE-ZERO=" N.
           STOP RUN.
