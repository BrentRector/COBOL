      *>--- conformance-case
      *> id: l1_perform_varying_six_after
      *> rules: [SR-14.9.28.3-9, GR-14.9.28.4-13]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR9 — "At least six AFTER phrases shall be
      *> permitted in varying-phrase."
      *>
      *> A MINIMUM-CAPACITY rule: the only way to discharge it is to
      *> write the sixth AFTER phrase and run it. But "it parses" is
      *> not what the rule buys — a sixth AFTER that parsed and then
      *> nested or reset wrongly would satisfy a parse-only witness and
      *> still be broken, so this program asserts the SEQUENCE that
      *> §14.9.28.4 GR13 e) prescribes for seven levels.
      *>
      *> Seven induction variables, each FROM 1 BY 1 UNTIL x > 2, so
      *> each admits exactly the values 1 and 2:
      *>
      *>   COUNT — GR13 e) runs the body once per combination of the
      *>   admitted values (step 2's "otherwise b." is the only arm
      *>   that executes it), so 2 ** 7 = 128.
      *>
      *>   SNAP2 — the tuple at the SECOND body execution. GR13 e) 2.
      *>   "otherwise b." increments the induction variable associated
      *>   with the CURRENT condition, and the current condition at the
      *>   moment the body runs is always the rightmost, so the seventh
      *>   variable is the one that varies fastest: 1111112, not
      *>   2111111. This is what a wrongly-NESTED seventh level breaks.
      *>
      *>   FINAL — the values left behind, derived by walking GR13 e)
      *>   2.'s true-branch on the last unwind: I7 reaches 3, so a. sets
      *>   it to its initialization value 1, b. makes condition-6
      *>   current and c. increments I6 to 3; the same three steps then
      *>   repeat leftwards until d. reaches condition-1, which is
      *>   incremented to 3 and, being condition-1, sends execution to
      *>   step 1, where 3 > 2 transfers control to the end. So the
      *>   first variable is left at 3 and every other at its
      *>   initialization value 1: 3111111. This is what a missing
      *>   RESET of an inner level breaks.
      *>
      *> The rule is worded identically in COBOL-85/2002/2014/2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFAF09.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I1  PIC 9 VALUE 0.
       01 I2  PIC 9 VALUE 0.
       01 I3  PIC 9 VALUE 0.
       01 I4  PIC 9 VALUE 0.
       01 I5  PIC 9 VALUE 0.
       01 I6  PIC 9 VALUE 0.
       01 I7  PIC 9 VALUE 0.
       01 CNT PIC 9(3) VALUE 0.
       01 SNAP2.
          05 P1 PIC 9 VALUE 0.
          05 P2 PIC 9 VALUE 0.
          05 P3 PIC 9 VALUE 0.
          05 P4 PIC 9 VALUE 0.
          05 P5 PIC 9 VALUE 0.
          05 P6 PIC 9 VALUE 0.
          05 P7 PIC 9 VALUE 0.
       01 FINALT.
          05 Q1 PIC 9 VALUE 0.
          05 Q2 PIC 9 VALUE 0.
          05 Q3 PIC 9 VALUE 0.
          05 Q4 PIC 9 VALUE 0.
          05 Q5 PIC 9 VALUE 0.
          05 Q6 PIC 9 VALUE 0.
          05 Q7 PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING I1 FROM 1 BY 1 UNTIL I1 > 2
                   AFTER I2 FROM 1 BY 1 UNTIL I2 > 2
                   AFTER I3 FROM 1 BY 1 UNTIL I3 > 2
                   AFTER I4 FROM 1 BY 1 UNTIL I4 > 2
                   AFTER I5 FROM 1 BY 1 UNTIL I5 > 2
                   AFTER I6 FROM 1 BY 1 UNTIL I6 > 2
                   AFTER I7 FROM 1 BY 1 UNTIL I7 > 2
               ADD 1 TO CNT
               IF CNT = 2
                   MOVE I1 TO P1
                   MOVE I2 TO P2
                   MOVE I3 TO P3
                   MOVE I4 TO P4
                   MOVE I5 TO P5
                   MOVE I6 TO P6
                   MOVE I7 TO P7
               END-IF
           END-PERFORM.
           MOVE I1 TO Q1.
           MOVE I2 TO Q2.
           MOVE I3 TO Q3.
           MOVE I4 TO Q4.
           MOVE I5 TO Q5.
           MOVE I6 TO Q6.
           MOVE I7 TO Q7.
           DISPLAY "SR9-COUNT=" CNT.
           DISPLAY "SR9-SNAP2=" SNAP2.
           DISPLAY "SR9-FINAL=" FINALT.
           STOP RUN.
