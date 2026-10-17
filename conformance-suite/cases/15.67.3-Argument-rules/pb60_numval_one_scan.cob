*>--- conformance-case
*> id: pb60_numval_one_scan
*> rules: [AR-15.67.3-1, AR-15.67.3-2, RV-15.67.4-1, RV-15.67.4-2, AR-15.68.3-4, RV-15.68.4-1, RV-15.68.4-2, RV-15.68.4-3, AR-15.69.3-1, AR-15.69.3-5, RV-15.93.4-1, RV-15.95.4-1, FMT-15.68.2, FMT-15.95.2, DOC-A.1-92]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 - the NUMVAL family's ONE-SCAN discipline: each family's TEST- validator and value function
      *> are projections of one positional format scan, so they can never disagree about what conforms.
      *> RC/DC: 15.68.4 r1/r3 - a currency letter shared with CR/DB ("R"/"D") is consumed at its ONE r4a
      *>        position, and the trailing CR/DB still negates: -123.45. The old unanchored Replace ate the
      *>        R of CR and valued a CONFORMING string as 0 while TEST-NUMVAL-C certified it good.
      *> SG/GP: control rows - sign-before-currency and grouping separators still value exactly.
      *> MD:    a currency where r4a forbids it (mid-digits) raises EC; checking off -> the 15.3 default 0.
      *> TB/TT: 15.67.3 r2 - the ignorable character is the SPACE only; a TAB-led argument is malformed
      *>        (EC default 0) and TEST-NUMVAL reports position 1 - the two projections agree.
      *> MM/TM: "-12-" conforms to NEITHER 15.67.3 r1 format (the formats are alternatives, never a
      *>        toggle) - EC default 0, TEST-NUMVAL position 4. 15.67.4 r2's containment sign holds by
      *>        construction on the conforming forms: TS ("12.5-") and CV ("12.5CR") value -12.5.
      *> ES/ET: 15.69.3 r5's except-clause - a space between significand digits is ILLEGAL (EC default 0,
      *>        TEST-NUMVAL-F position 3); LG (" - 35E+0 ") holds every LEGAL space placement at once.
      *> CM/MV/EQ: 15.68.4 r1 carries no channel qualification - COMPUTE, MOVE (the sender renders under the
      *>        RECEIVER's scale, MoveEmitter#SenderContext) and a relation (each side renders knowing the
      *>        OTHER side's static scale) all see 0.123456789. Before the threading, MOVE stored
      *>        0.123456000 and the relation agreed with the truncated value - four channels, three answers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60ONESCAN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XTAB PIC X(4) VALUE X"09313233".
       01 WS-A9 PIC X(14) VALUE "$0.123456789".
       01 WS-R9 PIC S9(4)V9(9).
       01 WS-E9 PIC -(4)9.9(9).
       01 R    PIC S9(12)V99.
       01 RE   PIC -(12)9.99.
       01 F    PIC S9(9)V9(6).
       01 FE   PIC -(9)9.9(6).
       01 T    PIC S9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C("R123.45CR", "R").
           MOVE R TO RE.
           DISPLAY "RC=" RE.
           COMPUTE R = FUNCTION NUMVAL-C("D123.45DB", "D").
           MOVE R TO RE.
           DISPLAY "DC=" RE.
           COMPUTE R = FUNCTION NUMVAL-C("- $ 890.05", "$").
           MOVE R TO RE.
           DISPLAY "SG=" RE.
           COMPUTE R = FUNCTION NUMVAL-C("$1,234.56", "$").
           MOVE R TO RE.
           DISPLAY "GP=" RE.
           COMPUTE R = FUNCTION NUMVAL-C("1$234.56", "$").
           MOVE R TO RE.
           DISPLAY "MD=" RE.
           COMPUTE F = FUNCTION NUMVAL(XTAB).
           MOVE F TO FE.
           DISPLAY "TB=" FE.
           COMPUTE T = FUNCTION TEST-NUMVAL(XTAB).
           DISPLAY "TT=" T.
           COMPUTE F = FUNCTION NUMVAL("-12-").
           MOVE F TO FE.
           DISPLAY "MM=" FE.
           COMPUTE T = FUNCTION TEST-NUMVAL("-12-").
           DISPLAY "TM=" T.
           COMPUTE F = FUNCTION NUMVAL("12.5-").
           MOVE F TO FE.
           DISPLAY "TS=" FE.
           COMPUTE F = FUNCTION NUMVAL("12.5CR").
           MOVE F TO FE.
           DISPLAY "CV=" FE.
           COMPUTE F = FUNCTION NUMVAL-F("1 2").
           MOVE F TO FE.
           DISPLAY "ES=" FE.
           COMPUTE T = FUNCTION TEST-NUMVAL-F("1 2").
           DISPLAY "ET=" T.
           COMPUTE F = FUNCTION NUMVAL-F(" - 35E+0 ").
           MOVE F TO FE.
           DISPLAY "LG=" FE.
           COMPUTE WS-R9 = FUNCTION NUMVAL-C(WS-A9, "$").
           MOVE WS-R9 TO WS-E9.
           DISPLAY "CM=" WS-E9.
           MOVE FUNCTION NUMVAL-C(WS-A9, "$") TO WS-R9.
           MOVE WS-R9 TO WS-E9.
           DISPLAY "MV=" WS-E9.
           IF FUNCTION NUMVAL-C(WS-A9, "$") = 0.123456789
               DISPLAY "EQ=YES"
           ELSE
               DISPLAY "EQ=NO"
           END-IF.
           STOP RUN.
       END PROGRAM PB60ONESCAN.
