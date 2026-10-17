*>--- conformance-case
*> id: pb623_float_landing_exact_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB623 — the COBOL-2023 leg of the exact binary64 landing, with the ROUNDED MODE IS
      *> PROHIBITED gate that the 2002 twin cannot carry (the phrase is COBOL-2014+, COBOLNET0803).
      *> ISO 14.6.8.2 rule 1 ("the value is treated as if it had been converted to a fixed-point value")
      *> + rule 4 ("truncation on either end"); 14.7.4.3 item 7 for PROHIBITED ("the arithmetic value
      *> cannot be represented exactly in the resultant identifier"), asked of the EXACT value: the double
      *> nearest 0.1 is 0.10000000000000000555111512312578270211815834045410156250, so it is NOT
      *> representable in one fraction digit and the receiver keeps the value it had; 1.5 is exact and stores.
      *> D0 shows the same F2 landing that D1 then refuses -- the truncation and the exactness question are
      *> two readings of ONE exact expansion, never of a binary64 product.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB623F3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F2 USAGE COMP-2 VALUE 0.1.
       01 F4 USAGE COMP-2 VALUE 1.5.
       01 R1 PIC 9(28)V99.
       01 R3 PIC 9V9.
       01 SE PIC X(3).
       01 F1 USAGE COMP-2 VALUE 16331239353195370.
       PROCEDURE DIVISION.
       MAIN.
           MOVE F1 TO R1
           DISPLAY "A1=" R1
           COMPUTE R1 = F1
           DISPLAY "A2=" R1
           MOVE F2 TO R3
           DISPLAY "D0=" R3
           MOVE "no " TO SE
           COMPUTE R3 ROUNDED MODE IS PROHIBITED = F2
               ON SIZE ERROR MOVE "yes" TO SE
           END-COMPUTE
           DISPLAY "D1=" SE " " R3
           MOVE "no " TO SE
           COMPUTE R3 ROUNDED MODE IS PROHIBITED = F4
               ON SIZE ERROR MOVE "yes" TO SE
           END-COMPUTE
           DISPLAY "D2=" SE " " R3
           STOP RUN.
