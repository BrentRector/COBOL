*>--- conformance-case
*> id: pb137_commit_inert
*> rules: [GR-14.9.7.4-1, GR-14.9.36.4-1, GR-14.9.36.4-7, FMT-14.9.36.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB137 - the COMMIT/ROLLBACK identity. The two words leave the user-word space at the
      *> editions where 8.9 reserves them (ONE cobolWord predicate), so no operand list absorbs a
      *> following bare facility verb: `DISPLAY "OK" COMMIT` inside IF, a bare ROLLBACK, and COMMIT as
      *> the first statement of an EVALUATE WHEN arm - the position CONFORMANCE.md documented as the
      *> warning's dead spot - all parse as statements, draw the 4.2.6 named warning (COBOLNET1579), and
      *> execute as CONTINUE (14.9.7.4 GR1 / 14.9.36.4 GR1 - no APPLY COMMIT clause can exist under the
      *> documented A.3 non-support). Derived: OK then DONE, nothing else.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF X = 1
               DISPLAY "OK" COMMIT
           END-IF
           ROLLBACK
           EVALUATE X
               WHEN 1 COMMIT
           END-EVALUATE
           DISPLAY "DONE"
           STOP RUN.
