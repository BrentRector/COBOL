*>--- conformance-case
*> id: pb251_numval_native_value_fixed
*> rules: [RV-15.4.1-L2.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB251 - NUMVAL and NUMVAL-C return the value their DEFINITION fixes, in EVERY arithmetic mode, so the
      *> NATIVE answers on this page are the SAME answers pb60_numval_standard_decimal prints for the same
      *> references. 15.4.1 grants an implementor-defined returned value only "unless otherwise specified in the
      *> function definition", and 15.67.4 r1 / 15.68.4 r1 are exactly such a specification, written once with no
      *> arithmetic-mode qualification: "The returned value is the numeric value represented by argument-1".
      *> Under native arithmetic 15.4.1 leaves the implementor "the characteristics and representation of the
      *> returned value" - never its VALUE - so the SDIDI is the representation chosen here: it is the only one
      *> that holds every 15.67.3 r3-conforming argument (31 digits native, 34 standard) without a working scale.
      *>
      *> MEASURED BEFORE THE FIX (the native lane materialized the value at max(receiver scale, 6) fraction
      *> digits, capped at the Int128 headroom - so a function answered differently depending on the SHAPE of the
      *> receiver it was written for, and lost digits wherever no receiver scale was there to inherit):
      *>     RAW  1.234567          OPD  001234560         I31  1234567890123456789012345678901.000000
      *>     RAWC 1234.567890       CV   0.123456
      *> No compile-time working scale can fix that: an argument with i integer digits needs i + ws digits in the
      *> Int128 carrier, and 15.67.3 r3 permits i = 31, so the only ws safe for EVERY conforming argument is 7 -
      *> while NUMVAL("0.123456789") needs 9. The carrier is the fix; a bigger floor is not.
      *>
      *> RAW/RAWC/CV: the receiver-less DISPLAY channel - the item-92 text form of the value at its own scale.
      *> OPD:   THE LEG A RECEIVER DOES NOT PROTECT. The receiver bounds the STATEMENT result, not an OPERAND's
      *>        precision: r1 fixes NUMVAL("0.1234567") at 1234567 x 10**-7, so x 10**7 is 1234567 exactly in a
      *>        PIC 9(9) receiver whose own scale is 0.
      *> I31/D31: 15.67.3 r3 admits 31 TOTAL digits under native, at either end of the decimal separator, and r1
      *>        fixes both values. These are the two ends no single working scale can serve at once: I31 needs
      *>        ws = 0 to fit the Int128 carrier and D31 needs ws = 31. D31 is written in the bare ". digit"
      *>        alternative of r1 because a leading zero is itself a digit - "0." + 31 digits is 32, which r3
      *>        rejects.
      *> C32:   the 32nd digit exceeds the r3 cap - EC-ARGUMENT-FUNCTION, checking off -> the 15.3 default 0.
      *>        (The cap is the digitCap the emitter derives from the arithmetic mode; the standard-decimal twin
      *>        takes 34 by 15.67.3 r4, which is why C32 is native-only and N34 lives in the SD golden.)
      *> SC/NG/SP/MM: the parsed scale is kept (1.500); r2 negates on CR and on a minus sign; r2 ignores leading
      *>        and pre-digit embedded spaces; "-12-" conforms to NEITHER r1 format -> the 15.3 default 0.
      *> REL/MV20/MAX/SUM/SUB/MVX/MVED/EVAL: the value flows through every channel at its own scale - a relation,
      *>        a MOVE at 20 fraction digits, an intrinsic argument, an addition, a subscript, a DISPLAY text
      *>        operand, a numeric-edited MOVE and an EVALUATE subject. Same lines as the SD golden prints.
      *> RAWF:  THE CONTRAST THAT MUST NOT MOVE. 15.69.4 r2 gives NUMVAL-F - and only NUMVAL-F - "an
      *>        approximation" under native arithmetic, which is why r3 has to state the standard-decimal value
      *>        separately. So NUMVAL-F keeps the float family's determination here (CONFORMANCE.md item 92) and
      *>        prints the binary64 shortest-round-trip image, where the SD golden prints 41 exact digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB251NVNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9   PIC S9(9)V9(9).
       01 E9   PIC -(9)9.9(9).
       01 R20  PIC S9V9(20).
       01 E20  PIC -9.9(20).
       01 RI   PIC 9(9).
       01 ED   PIC $$$,$$9.99.
       01 TBL.
          05 FILLER PIC X(5) VALUE "ABCDE".
       01 TBR REDEFINES TBL.
          05 EL PIC X OCCURS 5.
       PROCEDURE DIVISION.
           DISPLAY "RAW=" FUNCTION NUMVAL("1.2345678").
           DISPLAY "RAWC=" FUNCTION NUMVAL-C("$1,234.5678901234", "$").
           DISPLAY "CV=" FUNCTION NUMVAL-C("$0.1234567", "$").
           COMPUTE RI = FUNCTION NUMVAL("0.1234567") * 10000000.
           DISPLAY "OPD=" RI.
           DISPLAY "I31=" FUNCTION NUMVAL("1234567890123456789012345678901").
           DISPLAY "D31=" FUNCTION NUMVAL(".1234567890123456789012345678901").
           DISPLAY "C32=" FUNCTION NUMVAL("12345678901234567890123456789012").
           DISPLAY "SC=" FUNCTION NUMVAL("1.500").
           DISPLAY "NG=" FUNCTION NUMVAL("0.5CR").
           DISPLAY "SP=" FUNCTION NUMVAL(" - 0.5 ").
           DISPLAY "MM=" FUNCTION NUMVAL("-12-").
           IF FUNCTION NUMVAL("1.2345678") = 1.2345678
               DISPLAY "REL=EQ"
           ELSE
               DISPLAY "REL=NE"
           END-IF.
           MOVE FUNCTION NUMVAL("0.12345678901234567890") TO R20.
           MOVE R20 TO E20.
           DISPLAY "MV20=" E20.
           COMPUTE R9 = FUNCTION MAX(FUNCTION NUMVAL("1.5"), 2, 1.75).
           MOVE R9 TO E9.
           DISPLAY "MAX=" E9.
           COMPUTE R9 = FUNCTION NUMVAL("2.5") + FUNCTION NUMVAL("0.000000015").
           MOVE R9 TO E9.
           DISPLAY "SUM=" E9.
           DISPLAY "SUB=" EL(FUNCTION NUMVAL("3")).
           DISPLAY "MVX=[" FUNCTION NUMVAL("-1.2345678") "]".
           MOVE FUNCTION NUMVAL-C("$1,234.567", "$") TO ED.
           DISPLAY "MVED=" ED.
           EVALUATE FUNCTION NUMVAL("0.5")
               WHEN 0.5 DISPLAY "EVAL=HALF"
               WHEN OTHER DISPLAY "EVAL=OTHER"
           END-EVALUATE.
           DISPLAY "RAWF=" FUNCTION NUMVAL-F("1E+40").
           STOP RUN.
       END PROGRAM PB251NVNAT.
