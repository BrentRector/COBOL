*>--- conformance-case
*> id: constant_entry
*> rules: [SR-13.10.3-1, GR-13.10.4-1, GR-13.10.4-2, GR-13.10.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.10 constant entries (2002). A constant is a COMPILE-TIME
      *> substitution: §13.10.4 GR1/GR3 — references are "as if [the]
      *> literal were written where constant-name-1 is written"; the
      *> entry occupies NO storage. AS forms covered: a literal (GR1/GR2
      *> — incl. the §13.10.3 SR1 reclassification, so AS 0.25 keeps its
      *> non-integer value, and a §8.8.3 concatenation, which folds
      *> first per §8.8.3.3 GR3); an arithmetic expression (§13.10.4 GR4 —
      *> §7.3.6 compile-time arithmetic, result truncated to an
      *> integer), including a PRIOR constant as an operand (§13.10.3
      *> SR2/SR7); and LENGTH OF (GR6 — the §15.50 LENGTH value).
      *> Substitution positions exercised: an OCCURS bound + a PICTURE
      *> repetition count (§13.10.3 SR2), VALUE clauses (01 and
      *> level-88), a subscript, MOVE/DISPLAY operands, arithmetic, and
      *> relation conditions. §13.10.3 SR9: a duplicated constant-name
      *> with the SAME specification is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KONSTP10CT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-MAX   CONSTANT AS 5.
       01 K-WID   CONSTANT AS 4.
       01 K-EXPR  CONSTANT AS (2 + 3) * 4 - 6 / 2.
       01 K-FRAC  CONSTANT AS 0.25.
       01 K-STR   CONSTANT AS "NIL".
       01 K-CAT   CONSTANT AS "AB" & "CD".
       01 K-TWICE CONSTANT AS K-MAX * 2.
       01 K-MAX   CONSTANT AS 5.
       01 W-REC.
          05 W-A PIC X(3).
          05 W-B PIC 9(4).
       01 K-LEN   CONSTANT AS LENGTH OF W-REC.
       01 W-TXT   PIC X(K-WID) VALUE K-STR.
       01 W-TAB.
          05 T-ENT PIC 9 OCCURS K-MAX.
       01 W-SUM   PIC 9(3).
          88 SUM-FULL VALUE K-EXPR.
       01 W-I     PIC 9.
       01 W-FRAC  PIC 9V99.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "K-MAX=" K-MAX.
           DISPLAY "EXPR=" K-EXPR.
           DISPLAY "FRAC=" K-FRAC.
           DISPLAY "STR=" K-STR.
           DISPLAY "CAT=" K-CAT.
           DISPLAY "TWICE=" K-TWICE.
           DISPLAY "LEN=" K-LEN.
           DISPLAY "TXT=" W-TXT.
           MOVE 0 TO W-SUM.
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > K-MAX
               MOVE W-I TO T-ENT(W-I)
               ADD T-ENT(W-I) TO W-SUM
           END-PERFORM.
           DISPLAY "SUM=" W-SUM.
           DISPLAY "LAST=" T-ENT(K-MAX).
           MOVE K-EXPR TO W-SUM.
           IF SUM-FULL DISPLAY "88-HIT" END-IF.
           IF W-SUM > K-MAX DISPLAY "REL-GT" END-IF.
           COMPUTE W-FRAC = K-FRAC * 8.
           DISPLAY "FCOMP=" W-FRAC.
           STOP RUN.
