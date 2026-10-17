*>--- conformance-case
*> id: exit_perform_multilevel
*> rules: [GR-14.9.14.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> CA31 (CONFORMANCE-FIX-QUEUE): EXIT PERFORM (no CYCLE) inside a multi-level inline PERFORM VARYING
      *> leaves the WHOLE PERFORM (every AFTER level), ISO 14.9.14.4 GR5a. A multi-level VARYING lowers to
      *> nested loops, so a bare C# break would exit only the innermost — the buggy pre-fix output added the
      *> A=3 pass (11 12 13 21 31 32 33 DONE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA31.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9.
       01 B PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
               IF A = 2 AND B = 2
                   EXIT PERFORM
               END-IF
               DISPLAY A B
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
