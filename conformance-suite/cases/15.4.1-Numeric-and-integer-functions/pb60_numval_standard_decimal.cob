*>--- conformance-case
*> id: pb60_numval_standard_decimal
*> rules: [RV-15.4.1-L2.2, AR-15.67.3-4, RV-15.67.4-1, RV-15.69.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 (RV-15.67.4-1a) - the NUMVAL family under ARITHMETIC IS STANDARD-DECIMAL returns the SDIDI
      *> exactly at the parsed scale. 15.4.1: under a standard mode "the returned value for numeric and
      *> integer functions is contained in a temporary standard data item in the intermediate form defined
      *> for the arithmetic mode in effect" - the SDIDI of 8.8.1.5.2 (34 digits); 15.67.4 r1 / 15.68.4 r1
      *> name that value outright ("the numeric value represented by argument-1") and 15.69.4 r3 says it
      *> for NUMVAL-F ("If standard-decimal arithmetic is in effect, the returned value is the numeric
      *> value represented by argument-1" - r2 grants NATIVE arithmetic only an approximation).
      *> RAW/RAWC/RAWF: the receiver-less DISPLAY channel - before this landing the standard-mode value
      *>        rode the native Int128 projection at the item-92 working scale (1.234567 / 1234.567890 /
      *>        0.001234567); the item-92 text form renders the SDIDI at its own scale.
      *> N34:   a 34-digit argument is legal under SD (15.67.3 r4) and exact (8.8.1.5.2's 34 digits) - the
      *>        native projection saturated its Int128 rescale and printed 170141183460469231731687303715884.105727.
      *> F40:   NUMVAL-F's E-exponent lifts through the ONE 8.8.1.5.2 r2 range check; 1E+40 is in range.
      *> SC:    the SDIDI keeps the parsed scale (1.500), the item-92 form.
      *> NG/SP: 15.67.4 r2 - CR or a minus sign negates; leading/embedded-before-digit spaces ignored (r2).
      *> MM/C35: the reject projections are the native twins' - "-12-" conforms to neither r1 format and a
      *>        35th digit exceeds the r4 cap; both are EC-ARGUMENT-FUNCTION, checking off -> the 15.3 default 0.
      *> REL/MV20/CMF20/MAX/SUM/SUB/MVX/MVED/EVAL: the SDIDI value flows through every channel - a
      *>        relation, MOVE and COMPUTE at 20 fraction digits, as an argument (MAX routes to its Dec body),
      *>        in an SDIDI addition, as a subscript, MOVEd to alphanumeric (GR6a de-signed) and to a
      *>        numeric-edited receiver (MOVE truncation), and as an EVALUATE subject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60SDNUMVAL.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9   PIC S9(9)V9(9).
       01 E9   PIC -(9)9.9(9).
       01 R20  PIC S9V9(20).
       01 E20  PIC -9.9(20).
       01 A20  PIC X(20).
       01 ED   PIC $$$,$$9.99.
       01 TBL.
          05 FILLER PIC X(5) VALUE "ABCDE".
       01 TBR REDEFINES TBL.
          05 EL PIC X OCCURS 5.
       PROCEDURE DIVISION.
           DISPLAY "RAW=" FUNCTION NUMVAL("1.2345678").
           DISPLAY "RAWC=" FUNCTION NUMVAL-C("$1,234.5678901234", "$").
           DISPLAY "RAWF=" FUNCTION NUMVAL-F("1.23456789012345E-3").
           DISPLAY "N34=" FUNCTION NUMVAL("1234567890123456789012345678901234").
           DISPLAY "F40=" FUNCTION NUMVAL-F("1E+40").
           DISPLAY "SC=" FUNCTION NUMVAL("1.500").
           DISPLAY "NG=" FUNCTION NUMVAL("0.5CR").
           DISPLAY "SP=" FUNCTION NUMVAL(" - 0.5 ").
           DISPLAY "MM=" FUNCTION NUMVAL("-12-").
           DISPLAY "C35=" FUNCTION NUMVAL("12345678901234567890123456789012345").
           IF FUNCTION NUMVAL("1.2345678") = 1.2345678
               DISPLAY "REL=EQ"
           ELSE
               DISPLAY "REL=NE"
           END-IF.
           MOVE FUNCTION NUMVAL("0.12345678901234567890") TO R20.
           MOVE R20 TO E20.
           DISPLAY "MV20=" E20.
           COMPUTE R20 = FUNCTION NUMVAL-F("12345678901234567890E-20").
           MOVE R20 TO E20.
           DISPLAY "CMF20=" E20.
           COMPUTE R9 = FUNCTION MAX(FUNCTION NUMVAL("1.5"), 2, 1.75).
           MOVE R9 TO E9.
           DISPLAY "MAX=" E9.
           COMPUTE R9 = FUNCTION NUMVAL("2.5") + FUNCTION NUMVAL-F("1.5E-8").
           MOVE R9 TO E9.
           DISPLAY "SUM=" E9.
           DISPLAY "SUB=" EL(FUNCTION NUMVAL("3")).
      *> (a MOVE of the NUMERIC function into A20 stood here until 2026-08-18: Table 16 makes a numeric function
      *> the Noninteger sender, kb/Work PB73 - the exact SDIDI value's text form shows through DISPLAY instead)
           DISPLAY "MVX=[" FUNCTION NUMVAL("-1.2345678") "]".
           MOVE FUNCTION NUMVAL-C("$1,234.567", "$") TO ED.
           DISPLAY "MVED=" ED.
           EVALUATE FUNCTION NUMVAL("0.5")
               WHEN 0.5 DISPLAY "EVAL=HALF"
               WHEN OTHER DISPLAY "EVAL=OTHER"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM PB60SDNUMVAL.
