*>--- conformance-case
*> id: pb420_initialize_float_value_default_2002
*> rules: [GR-14.9.20.4-4, GR-14.9.20.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB420 — the SAME ISO 14.9.20.4 GR4 implicit MOVE into a floating-point receiver, over the
      *> phrases and usages COBOL-2002 introduced, which the '85 witness
      *> (tests/conformance/85/pb420_initialize_float_receiver_85) cannot express: the VALUE phrase
      *> (`ALL TO VALUE`), the DEFAULT phrase (`THEN TO DEFAULT`), and the FLOAT-SHORT / FLOAT-LONG /
      *> FLOAT-EXTENDED usage words of 13.18.60. The rule is edition-invariant — GR4 splits on the RECEIVER's
      *> category and a float item is category NUMERIC (8.5.2.4), so the implicit statement is
      *> "MOVE sending-operand TO receiving-operand" — but which items are receiving-operands, and what the
      *> sender is, is exactly what these phrases change, so each arm is measured separately.
      *>
      *> EXPECTED, DERIVED FROM THE RULE BEFORE THE RUN:
      *>   V1  ALL TO VALUE over a group whose three leaves all carry a data-item format VALUE clause.
      *>       GR5c1 qualifies each ("the VALUE phrase is specified, the category of the elementary data item
      *>       is one of the categories specified or implied in the VALUE phrase" — ALL implies all of them,
      *>       GR2 — "and … b. A data-item format VALUE clause is specified"). GR6a3 then makes the sender
      *>       "a literal that, when moved to the receiving-operand with a MOVE statement, produces the same
      *>       result as the initial value of the data item as produced by the application of the VALUE
      *>       clause" -> 1.5 into FLOAT-SHORT, 2.25 into FLOAT-LONG (both exact in binary32 and binary64),
      *>       and 008 into PIC 9(3).
      *>   V2  ALL TO VALUE over a group whose FLOAT-EXTENDED leaf has NO VALUE clause. GR5c1b's premise is
      *>       false for that leaf, so it is NOT a receiving-operand at all and 6.5 survives untouched, while
      *>       its VALUE-bearing sibling takes 3.25. This is the arm that proves the statement selects
      *>       operands by the rule rather than by the receiver's usage.
      *>   V3  THEN TO DEFAULT over the V1 group. GR5c3 ("The DEFAULT phrase is specified") makes every
      *>       possible receiving-operand one, but GR6a's premise — qualifying BECAUSE OF THE VALUE PHRASE —
      *>       is false when no VALUE phrase is written, and GR6b's is false with no REPLACING phrase, so
      *>       GR6c's table decides: a NUMERIC receiving operand takes "Figurative constant ZEROES". The
      *>       VALUE clauses are therefore NOT reinstated -> 0 / 0 / 000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB420FV02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 FS USAGE FLOAT-SHORT VALUE 1.5.
          05 FL USAGE FLOAT-LONG VALUE 2.25.
          05 N PIC 9(3) VALUE 8.
       01 H.
          05 HV USAGE FLOAT-LONG VALUE 3.25.
          05 HX USAGE FLOAT-EXTENDED.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 9.75 TO FS.
           MOVE 9.75 TO FL.
           MOVE 111 TO N.
           INITIALIZE G ALL TO VALUE.
           DISPLAY "V1=[" FS "][" FL "][" N "]".
           MOVE 9.75 TO HV.
           MOVE 6.5 TO HX.
           INITIALIZE H ALL TO VALUE.
           DISPLAY "V2=[" HV "][" HX "]".
           INITIALIZE G THEN TO DEFAULT.
           DISPLAY "V3=[" FS "][" FL "][" N "]".
           STOP RUN.
