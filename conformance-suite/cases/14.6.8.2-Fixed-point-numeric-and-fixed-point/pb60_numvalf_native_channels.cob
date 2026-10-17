*>--- conformance-case
*> id: pb60_numvalf_native_channels
*> rules: [GR-14.6.8.2-1, RV-15.69.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 (RV-15.69.4-2) - NUMVAL-F under NATIVE arithmetic follows the FLOAT family's documented
      *> determination (CONFORMANCE.md item 92): 15.69.4 r2 makes the native returned value "an approximation
      *> of the numeric value represented by argument-1", and this compiler carries an approximated returned
      *> value as binary64 unless a FIXED-POINT arithmetic receiver quantizes it. The receiver-less channels
      *> rode the Int128 projection at the ws-9 floor before this landing, so DISPLAY of NUMVAL-F("5E+30")
      *> printed the saturation sentinel 170141183460469231731687303715.884105727, "5E+30" = "9E+30" was TRUE,
      *> a COMP-2 receiver got 1.7E+29, and NUMVAL-F("1.5E-12") was 0 in every one of them - the
      *> RenderFloat Receiverless/Real arm (PB13) had never been swept to this member.
      *> RAW/RAWS/RAW1: receiver-less DISPLAY renders the binary64 through the ONE CobolFloat.Display a COMP-2
      *>        item uses (shortest round-trip). EQ/GT: relations compare natively in binary64 (8.8.4.2.4).
      *> F8/F8S: a COMP-2 receiver keeps the full binary64. CM: a FIXED arithmetic receiver keeps the EXACT
      *>        Int128 parse at the receiver-capped working scale (1.5E-8 x 10**8 = 1.5, exact). MV/MVS/MV20: a
      *>        MOVE sender ALSO keeps the exact parse - the receiver's scale is known (ReceiverContext.MoveSender)
      *>        and MOVE lands the parsed decimal digit-for-digit: 1500, 0.000000015 (the binary64 route would
      *>        land 14 through a multiply-then-truncate), and all twenty digits of a 20-digit argument.
      *> ARG/SUB: as an argument to MAX (the real-argument route) and as a subscript.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60NVFNATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9   PIC S9(9)V9(9).
       01 E9   PIC -(9)9.9(9).
       01 X5   PIC 9(5) VALUE 12345.
       01 F8   COMP-2.
       01 R20  PIC S9V9(20).
       01 E20  PIC -9.9(20).
       01 TBL.
          05 FILLER PIC X(5) VALUE "ABCDE".
       01 TBR REDEFINES TBL.
          05 EL PIC X OCCURS 5.
       PROCEDURE DIVISION.
           DISPLAY "RAW=" FUNCTION NUMVAL-F("5E+30").
           DISPLAY "RAWS=" FUNCTION NUMVAL-F("1.5E-12").
           DISPLAY "RAW1=" FUNCTION NUMVAL-F(" 1.5 ").
           IF FUNCTION NUMVAL-F("5E+30") = FUNCTION NUMVAL-F("9E+30")
               DISPLAY "EQ=TRUE"
           ELSE
               DISPLAY "EQ=FALSE"
           END-IF.
           IF FUNCTION NUMVAL-F("1.5E-12") > 0
               DISPLAY "GT=TRUE"
           ELSE
               DISPLAY "GT=FALSE"
           END-IF.
           COMPUTE F8 = FUNCTION NUMVAL-F("5E+30").
           DISPLAY "F8=" F8.
           COMPUTE F8 = FUNCTION NUMVAL-F("1.5E-12").
           DISPLAY "F8S=" F8.
           COMPUTE R9 = FUNCTION NUMVAL-F("1.5E-8") * 100000000.
           MOVE R9 TO E9.
           DISPLAY "CM=" E9.
           MOVE FUNCTION NUMVAL-F("1.5E+3") TO X5.
           DISPLAY "MV=" X5.
           MOVE FUNCTION NUMVAL-F("1.5E-8") TO R9.
           MOVE R9 TO E9.
           DISPLAY "MVS=" E9.
           MOVE FUNCTION NUMVAL-F("12345678901234567890E-20") TO R20.
           MOVE R20 TO E20.
           DISPLAY "MV20=" E20.
           COMPUTE R9 = FUNCTION MAX(FUNCTION NUMVAL-F("2.5E+1"), 3).
           MOVE R9 TO E9.
           DISPLAY "ARG=" E9.
           DISPLAY "SUB=" EL(FUNCTION NUMVAL-F("4E+0")).
           STOP RUN.
       END PROGRAM PB60NVFNATIVE.
