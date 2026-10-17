      *>--- conformance-case
      *> id: l1_if_format1_arms
      *> rules: [GR-14.9.19.4-1, GR-14.9.19.4-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.19.4 GR1 and GR2 — the FORMAT 1 (delimited) rules.
      *> Format 1 is "IF condition-1 THEN statement-1 [ ELSE
      *> statement-2 ] END-IF" (§14.9.19.2), so every IF here carries
      *> its END-IF.
      *> GR1 — "If condition-1 is true, control is transferred to the
      *> first statement of statement-1 and execution continues
      *> according to the rules for each statement specified in
      *> statement-1. The ELSE phrase, if specified, is ignored."
      *> Derived expectation: BOTH statements of statement-1 run IN
      *> ORDER and NO part of statement-2 executes. W-C makes the
      *> second half checkable rather than merely invisible — the
      *> ignored arm would have added 5.
      *> GR2 — "If condition-1 is false, the THEN phrase is ignored. If
      *> the ELSE phrase is specified, control is transferred to the
      *> first statement of statement-2 and execution continues
      *> according to the rules for each statement specified in
      *> statement-2."  Derived expectation: nothing of statement-1
      *> runs and BOTH statements of statement-2 run in order.
      *> GR1-T1/GR1-T2/GR1-C=1 pin GR1; GR2-E1/GR2-E2/GR2-C=1 pin GR2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IFF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC 9 VALUE 0.
       01 W-C PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO W-X.
           MOVE 0 TO W-C.
           IF W-X = 1 THEN
               DISPLAY "GR1-T1"
               ADD 1 TO W-C
               DISPLAY "GR1-T2"
           ELSE
               DISPLAY "GR1-E1"
               ADD 5 TO W-C
           END-IF.
           DISPLAY "GR1-C=" W-C.
           MOVE 0 TO W-X.
           MOVE 0 TO W-C.
           IF W-X = 1 THEN
               DISPLAY "GR2-T1"
               ADD 5 TO W-C
           ELSE
               DISPLAY "GR2-E1"
               ADD 1 TO W-C
               DISPLAY "GR2-E2"
           END-IF.
           DISPLAY "GR2-C=" W-C.
           STOP RUN.
