*>--- conformance-case
*> id: smallest_algebraic
*> rules: [RV-15.83.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.83 SMALLEST-ALGEBRAIC — the smallest positive value that can increment argument-1 = 10^(-scale),
      *> a compile-time PICTURE fold independent of digit count / sign / container. S999→+1; S9PP→+100 (scale -2);
      *> 99V9(3)→+.001; S9(4) COMP→+1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SMALGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M-S999    PIC S999.
       01 M-S9PP    PIC S9PP.
       01 M-99V999  PIC 99V9(3).
       01 M-S94B    PIC S9(4) COMP.
       01 MR        PIC +99999.999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SMALLEST-ALGEBRAIC(M-S999)   TO MR.
           DISPLAY MR.
           MOVE FUNCTION SMALLEST-ALGEBRAIC(M-S9PP)   TO MR.
           DISPLAY MR.
           MOVE FUNCTION SMALLEST-ALGEBRAIC(M-99V999) TO MR.
           DISPLAY MR.
           MOVE FUNCTION SMALLEST-ALGEBRAIC(M-S94B)   TO MR.
           DISPLAY MR.
           STOP RUN.
       END PROGRAM SMALGB.
