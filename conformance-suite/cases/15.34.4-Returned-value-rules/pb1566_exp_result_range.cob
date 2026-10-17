*>--- conformance-case
*> id: pb1566_exp_result_range
*> rules: [RV-15.34.4-1, RV-15.35.4-1, DOC-A.1-179]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1566 - EXP / EXP10 whose returned value lies outside
      *> the range of the native intermediate it is formed in.
      *> 15.34.1: "The EXP function returns an approximation of the value
      *> of e raised to the power of the argument"; 15.35.1 the same of
      *> 10. Under native arithmetic 15.4.1 makes "the characteristics
      *> and representation of the returned value" the implementor's,
      *> and COBOL.NET's is binary64 (CONFORMANCE.md DOC-A.1-92); 14.7.5
      *> case 5 then makes a value outside that range the size error
      *> condition (checked, DOC-A.1-179), EC-SIZE-OVERFLOW when
      *> "farther from zero" and EC-SIZE-UNDERFLOW when "nearer to zero
      *> than is allowed for the intermediate data item" (no-phrase
      *> rule 3). ON SIZE ERROR therefore runs and the receiver keeps its
      *> value (14.7.5 SIZE ERROR phrase rule 1).
      *> Expected values: Python decimal at 80 digits:
      *>   e**1000   = 1.97E+434  - past binary64 (was Infinity, NOT ON)
      *>   e**-1000  = 5.08E-435  - nonzero, rounds to 0 (was 0, NOT ON)
      *>   10**400   - past binary64 (float argument, the double arm)
      *>   10**-400  - rounds to 0 (float argument)
      *>   e**709    = 8.2184E+307 - inside the range: NOT ON SIZE ERROR
      *>   e**-740   = 4.19E-322  - a SUBNORMAL binary64, not an underflow
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1566EXPRANGE02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG  PIC 9(4)   VALUE 1000.
       01 NBIG PIC S9(4)  VALUE -1000.
       01 IN1  PIC 9(3)   VALUE 709.
       01 SUB  PIC S9(3)  VALUE -740.
       01 F400 COMP-2     VALUE 400.
       01 FN4  COMP-2     VALUE -400.
       01 F    COMP-2.
       01 R    PIC 9(3)V9(4).
       01 W    PIC 9.9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO R
           COMPUTE R = FUNCTION EXP (BIG)
               ON SIZE ERROR DISPLAY "EXP-BIG   SIZE ERROR R=" R
               NOT ON SIZE ERROR DISPLAY "EXP-BIG   NOT ON"
           END-COMPUTE
           MOVE 7 TO F
           COMPUTE F = FUNCTION EXP (NBIG)
               ON SIZE ERROR MOVE F TO W
                             DISPLAY "EXP-NBIG  SIZE ERROR F=" W
               NOT ON SIZE ERROR DISPLAY "EXP-NBIG  NOT ON"
           END-COMPUTE
           MOVE 7 TO F
           COMPUTE F = FUNCTION EXP10 (F400)
               ON SIZE ERROR MOVE F TO W
                             DISPLAY "E10-F400  SIZE ERROR F=" W
               NOT ON SIZE ERROR DISPLAY "E10-F400  NOT ON"
           END-COMPUTE
           MOVE 7 TO F
           COMPUTE F = FUNCTION EXP10 (FN4)
               ON SIZE ERROR MOVE F TO W
                             DISPLAY "E10-FN4   SIZE ERROR F=" W
               NOT ON SIZE ERROR DISPLAY "E10-FN4   NOT ON"
           END-COMPUTE
           COMPUTE F = FUNCTION EXP (IN1) / 1.0E307
               ON SIZE ERROR DISPLAY "EXP-709   SIZE ERROR"
               NOT ON SIZE ERROR MOVE F TO W
                                 DISPLAY "EXP-709   NOT ON /E307=" W
           END-COMPUTE
           COMPUTE F = FUNCTION EXP (SUB)
               ON SIZE ERROR DISPLAY "EXP-SUB   SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "EXP-SUB   NOT ON"
           END-COMPUTE
           IF F > 0 DISPLAY "EXP-SUB   POSITIVE" END-IF
           STOP RUN.
