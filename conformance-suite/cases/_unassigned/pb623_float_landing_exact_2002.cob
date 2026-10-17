*>--- conformance-case
*> id: pb623_float_landing_exact_2002
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB623 — the COBOL-2002 leg of the exact binary64 landing: a float sender reaches a
      *> fixed-point receiver as its EXACT value, identically through the MOVE channel and the arithmetic
      *> channel. ISO 14.6.8.2 rule 1 ("the value is treated as if it had been converted to a fixed-point
      *> value") + rule 4 ("truncation on either end"); rule 2 gives the FLOAT-LONG sender's conversion to
      *> the implementor and COBOL.NET's determination is that same exact conversion, at every edition.
      *> The ROUNDED MODE IS legs live in the 2014/2023 twins -- the phrase is COBOL-2014+ (COBOLNET0803).
      *> Expected values are the EXACT expansions of the binary64 significands, not observations:
      *>   16331239353195370 = 8165619676597685 x 2                          -- exact
      *>   0.1 -> 3602879701896397 x 2^-55 = 0.10000000000000000555111512312578270211815834045410156250
      *>          at scale 19 -> 1000000000000000055 truncated; at scale 9 -> 100000000
      *>   1000000000 is exact, and lands in a trailing-P receiver as 10000000 x 10^2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB623F2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE COMP-2 VALUE 16331239353195370.
       01 F2 USAGE COMP-2 VALUE 0.1.
       01 F3 USAGE COMP-2 VALUE 1000000000.
       01 F5 USAGE COMP-2 VALUE -0.1.
       01 R1 PIC 9(28)V99.
       01 R2 PIC V9(19).
       01 P1 PIC 9(9)PP.
       01 E1 PIC -9.9(9).
       PROCEDURE DIVISION.
       MAIN.
           MOVE F1 TO R1
           DISPLAY "A1=" R1
           COMPUTE R1 = F1
           DISPLAY "A2=" R1
           MOVE F2 TO R2
           DISPLAY "B1=" R2
           COMPUTE R2 = F2
           DISPLAY "B2=" R2
           MOVE F3 TO P1
           DISPLAY "C2=" P1
           MOVE F5 TO E1
           DISPLAY "C3=" E1
           STOP RUN.
