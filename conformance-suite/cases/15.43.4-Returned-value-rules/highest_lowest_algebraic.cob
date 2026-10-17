*>--- conformance-case
*> id: highest_lowest_algebraic
*> rules: [RV-15.43.4-2, RV-15.58.4-2, FMT-15.43.2, FMT-15.58.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43 HIGHEST-ALGEBRAIC / §15.58 LOWEST-ALGEBRAIC — the greatest / lowest algebraic value the
      *> argument's PICTURE can represent, a compile-time fold. S99→+99/-99; S9(4)→+9999; 99V9(3)→+99.999/0
      *> (unsigned ⇒ LOWEST 0, Annex D.32); $**,**9.99 (numeric-edited)→+99999.99/0 (no sign in the mask).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HILOALGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-S99      PIC S99.
       01 N-S9K      PIC S9(4).
       01 N-U99V999  PIC 99V9(3).
       01 N-EDIT     PIC $**,**9.99.
       01 SR         PIC +99999.999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-S99)     TO SR.
           DISPLAY SR.
           MOVE FUNCTION LOWEST-ALGEBRAIC (N-S99)     TO SR.
           DISPLAY SR.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-S9K)     TO SR.
           DISPLAY SR.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-U99V999) TO SR.
           DISPLAY SR.
           MOVE FUNCTION LOWEST-ALGEBRAIC (N-U99V999) TO SR.
           DISPLAY SR.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-EDIT)    TO SR.
           DISPLAY SR.
           MOVE FUNCTION LOWEST-ALGEBRAIC (N-EDIT)    TO SR.
           DISPLAY SR.
           STOP RUN.
       END PROGRAM HILOALGB.
