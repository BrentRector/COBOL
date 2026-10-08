      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB845. ISO 8.3.2.1 5): intrinsic-function-names may be
      *> user-defined words "except for ... intrinsic function names
      *> LENGTH, RANDOM, SIGN, and SUM", and 8.9 reserves LENGTH at every
      *> edition, so a class-name LENGTH is COBOLNET0901 everywhere.
      *> Before the fix LENGTH was exempt from the reservation gate and
      *> `IF X LENGTH` compiled as a class condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845NL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS LENGTH IS "A" THRU "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "B".
       PROCEDURE DIVISION.
           IF X LENGTH
               DISPLAY "T"
           END-IF
           STOP RUN.
