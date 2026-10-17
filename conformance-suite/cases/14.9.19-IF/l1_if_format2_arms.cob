      *>--- conformance-case
      *> id: l1_if_format2_arms
      *> rules: [GR-14.9.19.4-3, GR-14.9.19.4-5, GR-14.9.19.4-7]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.19.4 GR3, GR5 and GR7 — the FORMAT 2 (historic)
      *> rules. Format 2 has NO END-IF (§14.9.19.2): the IF statement is
      *> terminated by the terminal separator period of its sentence,
      *> and that is the only thing separating these three rules from
      *> their Format 1 twins — so no IF in this program is written
      *> with an END-IF.
      *> GR3 — "If condition-1 is true and statement-1 is specified,
      *> control is transferred to the first statement of statement-1
      *> ... The ELSE phrase, if specified, is ignored."
      *> GR5 — "If condition-1 is false and statement-2 is specified,
      *> the THEN phrase is ignored, control is transferred to the
      *> first statement of statement-2, and execution continues
      *> according to the rules for each statement specified in
      *> statement-2."
      *> GR7 — "If condition-1 is false and the ELSE phrase is not
      *> specified, the THEN phrase is ignored."  Derived expectation:
      *> the statement is a complete no-op — nothing of the THEN phrase
      *> runs, nothing is substituted for the absent ELSE, and the next
      *> sentence runs normally.
      *> W-C makes the ignored arms checkable: each would have added 5.
      *> GR3-T1/GR3-C=1 pin GR3; GR5-E1/GR5-E2/GR5-C=1 pin GR5; a
      *> GR7-C=0 with no GR7-T1 line pins GR7. The optional word THEN
      *> is written on the GR3 and GR5 sentences and omitted on the
      *> GR7 one (§5.2.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IFF02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC 9 VALUE 0.
       01 W-C PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO W-X.
           MOVE 0 TO W-C.
           IF W-X = 1 THEN
               DISPLAY "GR3-T1"
               ADD 1 TO W-C
           ELSE
               DISPLAY "GR3-E1"
               ADD 5 TO W-C.
           DISPLAY "GR3-C=" W-C.
           MOVE 0 TO W-X.
           MOVE 0 TO W-C.
           IF W-X = 1 THEN
               DISPLAY "GR5-T1"
               ADD 5 TO W-C
           ELSE
               DISPLAY "GR5-E1"
               ADD 1 TO W-C
               DISPLAY "GR5-E2".
           DISPLAY "GR5-C=" W-C.
           MOVE 0 TO W-X.
           MOVE 0 TO W-C.
           IF W-X = 1
               DISPLAY "GR7-T1"
               ADD 5 TO W-C.
           DISPLAY "GR7-C=" W-C.
           STOP RUN.
