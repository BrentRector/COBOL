      *>--- conformance-case
      *> id: literal_concat
      *> rules: [GR-8.8.3.3-2, FMT-8.8.3.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.8.3 concatenation expressions — the & operator (2002).
      *> §8.8.3.3 GR3: a concatenation expression is EQUIVALENT to a
      *> literal of the same class and value and may be used anywhere a
      *> literal of that class may be used — folded at COMPILE time
      *> (ConcatFolder); no runtime operator exists. Covers every legal
      *> class pairing (§8.8.3.2 SR1): alphanumeric & alphanumeric
      *> (incl. the X"…" hex format, §8.3.3.2), national & national,
      *> boolean & boolean, and figurative-constant operands (one
      *> character each, §8.3.3.6.4 GR3a; both-figurative => class
      *> alphanumeric, §8.8.3.3 GR1b). Positions: VALUE clauses (01 +
      *> level-88), MOVE / DISPLAY / IF / EVALUATE operands, and
      *> FUNCTION LENGTH over a concatenated result (§15.50).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LITCONCATP10CC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-ALNUM  PIC X(6) VALUE "AB" & "CD".
       01 W-MOVE   PIC X(6).
          88 IS-WXYZ VALUE "WX" & "YZ".
       01 W-NAT    PIC N(4) VALUE N"AB" & N"CD".
       01 W-BOOL   PIC 1(4) VALUE B"01" & B"10".
       01 W-LEN    PIC 99.
       PROCEDURE DIVISION.
       MAIN.
      *> VALUE-clause folds: alphanumeric, national, boolean.
           DISPLAY "V-ALNUM=" W-ALNUM.
           DISPLAY "V-NAT=" W-NAT.
           DISPLAY "V-BOOL=" W-BOOL.
      *> Procedure-statement folds: MOVE source, DISPLAY operand, a
      *> level-88 VALUE concat proving the folded 88 comparison.
           MOVE "WX" & "YZ" TO W-MOVE.
           DISPLAY "MOVE=" W-MOVE.
           IF IS-WXYZ DISPLAY "88-TRUE" END-IF.
           DISPLAY "DISP=" "no" & "table".
      *> Figurative operands are ONE character each (§8.3.3.6.4 GR3a).
           DISPLAY "FIG=" "A" & SPACE & "B" & QUOTE & ZERO.
      *> X"…" is the hex FORMAT of the alphanumeric literal (§8.3.3.2)
      *> — it concatenates as class alphanumeric.
           MOVE X"4142" & "CD" TO W-MOVE.
           DISPLAY "HEX=" W-MOVE.
      *> Boolean concatenation in a relation (§8.8.2 — the folded
      *> result IS a boolean literal) and with figurative ZERO.
           IF W-BOOL = B"01" & B"10" DISPLAY "BOOL-EQ" END-IF.
           MOVE B"1" & ZERO TO W-BOOL.
           DISPLAY "BOOL-FIG=" W-BOOL.
      *> A multi-operand chain ({lit | concat-expr} & lit, §8.8.3.1).
           MOVE "A" & "B" & "C" & "D" & "E" TO W-MOVE.
           DISPLAY "CHAIN=" W-MOVE.
      *> FUNCTION LENGTH over the §8.8.3.3 GR3 equivalent literal.
           COMPUTE W-LEN = FUNCTION LENGTH("AB" & "CD").
           DISPLAY "LEN=" W-LEN.
      *> EVALUATE selection against a folded literal.
           EVALUATE W-ALNUM
               WHEN "AB" & "CD" DISPLAY "EVAL-HIT"
               WHEN OTHER DISPLAY "EVAL-MISS"
           END-EVALUATE.
           STOP RUN.
