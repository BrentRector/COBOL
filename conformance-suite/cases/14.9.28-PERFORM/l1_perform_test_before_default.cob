      *>--- conformance-case
      *> id: l1_perform_test_before_default
      *> rules: [SR-14.9.28.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR1 — "If neither the TEST BEFORE nor the TEST
      *> AFTER phrase is specified, the TEST BEFORE phrase is assumed."
      *>
      *> The rule is a DEFAULT, so it is observable only on a program
      *> shape that tells the two assumptions apart. Every loop below
      *> enters with its condition ALREADY TRUE and never changes it,
      *> which §14.9.28.4 GR10 splits exactly:
      *>   "If the condition is true when the PERFORM statement is
      *>   entered, and the TEST BEFORE phrase is specified or implied,
      *>   no transfer to the specified set of statements takes place"
      *>       -> the body runs ZERO times;
      *>   "If the TEST AFTER phrase is specified, the PERFORM statement
      *>   functions as if the TEST BEFORE phrase were specified except
      *>   that the condition is tested after the specified set of
      *>   statements has been executed"
      *>       -> the body runs EXACTLY ONCE.
      *> So SR1 holds iff DEF equals BEFORE (0) and differs from
      *> AFTER (1). All three counts are PRINTED on every line, so the
      *> assertion cannot pass vacuously: a carrier whose loops never
      *> ran at all would print 0 0 0, and one whose loops always ran
      *> once would print 1 1 1. Neither is the derived answer.
      *>
      *> SR1 is a FORMATS 1 AND 2 rule, so all THREE carriers of the
      *> optional WITH TEST phrase are measured — the §14.9.28.2
      *> until-phrase in its inline (Format 2) and out-of-line
      *> (Format 1) positions, and the varying-phrase, which prints the
      *> WITH TEST bracket of its own.
      *>
      *> The VARYING legs also print the induction variable.
      *> §14.9.28.4 GR13 a) sets it to 9 first; GR13 d) then leaves it
      *> with "the value it contained when condition-1 was evaluated"
      *> and GR13 b) with "the value it contained at the completion of
      *> the execution of the specified set of statements" — the body
      *> never touches it, so every leg ends at 009 and the difference
      *> between the arms is the COUNT alone.
      *>
      *> The rule is worded identically in COBOL-85/2002/2014/2023 and
      *> nothing here is edition-conditioned, so one 2023 program
      *> discharges the whole edition window.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFTB01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G       PIC 9    VALUE 1.
       01 CA-DEF  PIC 9    VALUE 0.
       01 CA-BEF  PIC 9    VALUE 0.
       01 CA-AFT  PIC 9    VALUE 0.
       01 CB-DEF  PIC 9    VALUE 0.
       01 CB-BEF  PIC 9    VALUE 0.
       01 CB-AFT  PIC 9    VALUE 0.
       01 CC-DEF  PIC 9    VALUE 0.
       01 CC-BEF  PIC 9    VALUE 0.
       01 CC-AFT  PIC 9    VALUE 0.
       01 I1      PIC 9(3) VALUE 0.
       01 I2      PIC 9(3) VALUE 0.
       01 I3      PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      *> Carrier A — the INLINE until-phrase (§14.9.28.2 Format 2).
      *> G is 1 and no body touches it, so G = 1 is true at entry.
           PERFORM UNTIL G = 1
               ADD 1 TO CA-DEF
           END-PERFORM.
           PERFORM WITH TEST BEFORE UNTIL G = 1
               ADD 1 TO CA-BEF
           END-PERFORM.
           PERFORM WITH TEST AFTER UNTIL G = 1
               ADD 1 TO CA-AFT
           END-PERFORM.
           DISPLAY "SR1-INLINE DEF=" CA-DEF " BEF=" CA-BEF
               " AFT=" CA-AFT.
      *> Carrier B — the OUT-OF-LINE until-phrase (§14.9.28.2
      *> Format 1). Each target is a paragraph of its own, so
      *> §14.9.28.4 GR5 a) returns after its single statement.
           PERFORM BUMP-B-DEF UNTIL G = 1.
           PERFORM BUMP-B-BEF WITH TEST BEFORE UNTIL G = 1.
           PERFORM BUMP-B-AFT WITH TEST AFTER UNTIL G = 1.
           DISPLAY "SR1-OUTLIN DEF=" CB-DEF " BEF=" CB-BEF
               " AFT=" CB-AFT.
      *> Carrier C — the varying-phrase. The induction variable starts
      *> at 9 and 9 > 5 is already true at entry, so the same
      *> 0 / 0 / 1 split applies.
           PERFORM VARYING I1 FROM 9 BY 1 UNTIL I1 > 5
               ADD 1 TO CC-DEF
           END-PERFORM.
           PERFORM WITH TEST BEFORE VARYING I2 FROM 9 BY 1
                   UNTIL I2 > 5
               ADD 1 TO CC-BEF
           END-PERFORM.
           PERFORM WITH TEST AFTER VARYING I3 FROM 9 BY 1
                   UNTIL I3 > 5
               ADD 1 TO CC-AFT
           END-PERFORM.
           DISPLAY "SR1-VARYNG DEF=" CC-DEF " BEF=" CC-BEF
               " AFT=" CC-AFT.
           DISPLAY "SR1-VARY-I DEF=" I1 " BEF=" I2 " AFT=" I3.
           STOP RUN.

       BUMP-B-DEF.
           ADD 1 TO CB-DEF.
       BUMP-B-BEF.
           ADD 1 TO CB-BEF.
       BUMP-B-AFT.
           ADD 1 TO CB-AFT.
