*>--- conformance-case
*> id: rounded_mode_prohibited
*> rules: [GR-14.7.4.3-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.7.4 — ROUNDED MODE IS PROHIBITED (COBOL-2002). Rounding is NOT permitted, so a result that
      *> is not exactly representable at the receiver's scale raises the SIZE ERROR condition
      *> (EC-SIZE-TRUNCATION) and leaves the receiver UNCHANGED — it must NOT silently truncate.
      *> Regression test for the bug where PROHIBITED fell through to truncation and never signaled size error:
      *>   2.25 into PIC 9.9 (one fraction digit) is inexact -> SIZE ERROR, X stays 7.7.
      *>   2.20 into PIC 9.9 is exact                        -> no size error, X becomes 2.2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RNDPROH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC-INEXACT PIC 9V99 VALUE 2.25.
       01 SRC-EXACT   PIC 9V99 VALUE 2.20.
       01 X           PIC 9.9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7.7 TO X.
           COMPUTE X ROUNDED MODE IS PROHIBITED = SRC-INEXACT
               ON SIZE ERROR DISPLAY "INEXACT-SIZEERR"
               NOT ON SIZE ERROR DISPLAY "INEXACT-NOERR"
           END-COMPUTE.
           DISPLAY "AFTER-INEXACT=" X.
           MOVE 7.7 TO X.
           COMPUTE X ROUNDED MODE IS PROHIBITED = SRC-EXACT
               ON SIZE ERROR DISPLAY "EXACT-SIZEERR"
               NOT ON SIZE ERROR DISPLAY "EXACT-NOERR"
           END-COMPUTE.
           DISPLAY "AFTER-EXACT=" X.
           STOP RUN.
