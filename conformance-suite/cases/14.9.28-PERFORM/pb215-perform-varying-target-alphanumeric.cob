*>--- conformance-case
*> id: pb215-perform-varying-target-alphanumeric
*> rules: [SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB215 (sibling sweep). 14.9.28.3 SR2: "Each identifier shall
      *> reference a numeric elementary item" - the induction variable too. The
      *> SET-receiver resolution applied no class screen, so a PIC X induction
      *> variable compiled clean and died at run time (NotImplemented: arithmetic
      *> into a non-fixed-point target) where 4.2.2 wants a compile-time error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AX PIC X VALUE "0".
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING AX FROM 1 BY 1 UNTIL AX > "3"
               DISPLAY "AX=" AX
           END-PERFORM
           STOP RUN.
