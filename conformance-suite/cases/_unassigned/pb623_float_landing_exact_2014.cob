*>--- conformance-case
*> id: pb623_float_landing_exact_2014
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB623 — ONE binary64, ONE landing: a float sender reaches a fixed-point receiver as its EXACT
      *> value, and the MOVE channel and the arithmetic channel land it identically.
      *> ISO 14.6.8.2 rule 1: "If the sending operand is an intermediate data item or a data item described
      *> with a standard floating-point usage, the value is treated as if it had been converted to a
      *> fixed-point value" -- THE VALUE (a binary64 is a terminating decimal), then rule 4 "aligned by
      *> decimal point and ... transferred to the receiving digits with zero fill or truncation on either end".
      *> Rule 2 leaves a FLOAT-LONG sender's conversion to the implementor and COBOL.NET's determination is
      *> that same exact conversion. 15.4.1 then forbids the split: "the returned value is the same for all
      *> instances of a given function within a single execution of the runtime element".
      *> Every expected value below is the EXACT decimal expansion of the binary64, computed from its
      *> significand, never observed:
      *>   16331239353195370 = 8165619676597685 x 2      -- exact, so it survives whole at scale 2
      *>   the double nearest 0.1 = 3602879701896397 x 2^-55
      *>                          = 0.1000000000000000055511151231257827021181583404541015625
      *>       at scale 19 -> 1000000000000000055 (truncated), at scale 1 -> 1, at scale 9 -> 100000000
      *>   1.5 and 1000000000 are exact binary64 values.
      *> A1/A2 was the defect: 16331239353195368.96 through the MOVE against 16331239353195369.92 through
      *> COMPUTE, because each scaled by 10^scale IN binary64 first. C2 pins the trailing-P receiver, whose
      *> negative scale that same multiply turned into 10^0 and stored zero.
      *> D1/D2 are the ROUNDED MODE PROHIBITED gate (14.7.4.3 item 7 -- "the arithmetic value cannot be
      *> represented exactly in the resultant identifier"), asked of the exact value: 0.1 at one fraction
      *> digit raises and leaves the receiver unchanged; 1.5 is exact and stores.
      *> E1/E2 are the case rule 1 FORCES rather than leaves to the implementor -- a STANDARD floating-point
      *> usage (FLOAT-BINARY-64, 13.18.60.4 GR15), where the value that is transferred is the item's own
      *> algebraic value: the double nearest 8.2 is 8.19999999999999928945726423989981412887573242187500,
      *> so one fraction digit takes 8.1. The COMP-2 legs above take the SAME conversion under rule 2's
      *> latitude, because a COMPUTE from either one lands through the rule 1 intermediate anyway.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB623FL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE COMP-2 VALUE 16331239353195370.
       01 F2 USAGE COMP-2 VALUE 0.1.
       01 F3 USAGE COMP-2 VALUE 1000000000.
       01 F4 USAGE COMP-2 VALUE 1.5.
       01 F5 USAGE COMP-2 VALUE -0.1.
       01 F6 USAGE FLOAT-BINARY-64 VALUE 8.2.
       01 R1 PIC 9(28)V99.
       01 R2 PIC V9(19).
       01 R3 PIC 9V9.
       01 P1 PIC 9(9)PP.
       01 E1 PIC -9.9(9).
       01 SE PIC X(3).
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
           MOVE F2 TO R3
           DISPLAY "C1=" R3
           MOVE F3 TO P1
           DISPLAY "C2=" P1
           MOVE F5 TO E1
           DISPLAY "C3=" E1
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
           MOVE F6 TO R3
           DISPLAY "E1=" R3
           COMPUTE R3 = F6
           DISPLAY "E2=" R3
           STOP RUN.
