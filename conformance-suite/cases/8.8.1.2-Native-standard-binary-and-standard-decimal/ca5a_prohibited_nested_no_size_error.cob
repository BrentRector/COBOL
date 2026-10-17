*>--- conformance-case
*> id: ca5a_prohibited_nested_no_size_error
*> rules: [GR-8.8.1.2-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> CA5 (CONFORMANCE-FIX-QUEUE) part A: ROUNDED MODE IS PROHIBITED tests only THE RESULTANT IDENTIFIER
      *> (ISO 14.7.4.3 GR7); it must NOT be inherited by a NESTED intermediate division (14.7.7 rule 3 NOTE 1 —
      *> ROUNDED applies only to the final transfer). The arithmetic value of (1/3)*0 is 0 for any implementor-
      *> defined intermediate precision (8.8.1.3), and 0 is exactly representable in PIC 99V99, so PROHIBITED does
      *> NOT raise -> NOT ON SIZE ERROR runs, X = 0. Pre-fix the nested 1/3 inherited PROHIBITED and DivideOrThrow
      *> threw a SPURIOUS size error on the inexact intermediate quotient (ON SIZE ERROR ran, X left at 7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA5A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 99V99 VALUE 7.
       PROCEDURE DIVISION.
           COMPUTE X ROUNDED MODE IS PROHIBITED = (1 / 3) * 0
               ON SIZE ERROR DISPLAY "SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "OK".
           DISPLAY X.
           STOP RUN.
