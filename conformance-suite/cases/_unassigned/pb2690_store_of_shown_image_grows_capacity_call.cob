*>--- conformance-case
*> id: pb2690_store_of_shown_image_grows_capacity_call
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - A BY REFERENCE FORMAL'S FIXED TABLE STORED PAST THE
      *> CAPACITY OF THE ARGUMENT'S DYNAMIC-CAPACITY TABLE RAISES THE CAPACITY
      *> WHATEVER IMAGE IT STORES: the INITIALIZED default, and SPACES into a
      *> table without the phrase, exactly the image the formal was shown.
      *>
      *> 14.2.3 GR8: the formal "operates as if the formal parameter occupies
      *> the same storage area as the argument", so a store through occurrence
      *> N of the formal's fixed table is a receiving reference to occurrence N
      *> of the argument's dynamic-capacity table, and 8.5.1.9.3: "When a data
      *> item in a dynamic-capacity table is referenced as a receiving item and
      *> the value of the subscript exceeds the current capacity of the table,
      *> a new element is automatically created and the capacity of the table
      *> is increased to the value given by the subscript". The value stored
      *> does not enter the rule. 8.5.1.9.5 initializes the items of an
      *> INITIALIZED table's new elements that the statement does not store.
      *>
      *> EXPECTED VALUES, DERIVED (each table starts with ONE element; the
      *> INITIALIZED element is "ab"):
      *>   GA  the callee stores FE (3) := "a", the default     = ab ab ab, 3
      *>   DA  the same store made in the caller                = ab ab ab, 3
      *>   GS  no INITIALIZED (PIC X elements "q"); the callee
      *>       stores SPACE into TS (3)                          = q, 3
      *>   G2  the callee stores the default into FE (2)       = ab ab, 2
      *>   GR  the callee only reads FE (3): nothing created    = 1
      *>   GV  a variable-length formal (its own DYNAMIC LENGTH
      *>       item after the fixed table); it stores the
      *>       default into VE (3)                               = ab ab ab, 3
      *>   GN  elements holding an INITIALIZED inner dynamic
      *>       table (ND "d", NI "i" FROM 1); the callee stores
      *>       the default "d" into LND (3)                      = d d d, 3
      *>   GC  BY CONTENT (14.2.3 GR9: a record that does "not
      *>       occupy the same storage area"): nothing created  = 1
      *>   GK  the formal passed on BY REFERENCE to a program of
      *>       the same description, which stores the default
      *>       into FE (3): one storage area throughout         = 3
      *>   GE  the formal passed on BY REFERENCE to a program
      *>       whose formal is ELEMENTARY (PIC X(7)), which
      *>       stores "Q" into its position 2 (FE (1), inside the
      *>       capacity): no occurrence past the capacity is
      *>       referenced, so none is created                   = Qb, 1
      *>   GQ  an OCCURS DEPENDING formal (count 3 from the
      *>       argument) passed on BY REFERENCE to a program
      *>       whose formal has a DYNAMIC-CAPACITY table (another
      *>       shape), which stores nothing: no statement
      *>       references an occurrence past the capacity       = 1
      *>   GP  GQ, but the middle program first stores the
      *>       default "a" into its FE (3)                       = ab ab ab, 3
      *>   GZ  the callee's four ON SIZE ERROR stores into
      *>       occurrence 3, one per resultant category (fixed
      *>       numeric, numeric-edited, floating-point edited,
      *>       COMP-1), each too large for its receiver: 14.7.5
      *>       SIZE ERROR rule 2) "the content of that resultant
      *>       identifier is not changed from the content that
      *>       existed at the start of the execution of the
      *>       arithmetic statement". No store, so no receiving
      *>       reference is recorded and no element is created:
      *>       every phrase runs (QEGF)                          = QEGF, 1
      *>   DZ  the same four statements made in the caller     = QEGF, 1
      *>   GW  a formal whose OWN table is dynamic, of elements
      *>       (FY, FI OCCURS 3), opposite elements (WY, WI
      *>       DYNAMIC FROM 1): the callee stores SPACE into FE
      *>       (1, 3), a receiving reference to WI (3) of WO (1),
      *>       so WI's capacity in WO (1) becomes 3; the outer
      *>       capacity stays 1. Shown: capacity, then FUNCTION
      *>       LENGTH (WO (1)) = 1 + 3                           = 1 4
      *>   GI  GW with the inner table INITIALIZED (IE "i"); the
      *>       callee stores the default "i" into FE (1, 3)    = iii, 1 4
      *>   GX  GW, but the callee stores "Q" into FE (2, 1): the
      *>       formal's own table creates occurrence 2, so the
      *>       argument's does (8.5.1.9.3), and only XI (1) of it
      *>       is referenced: XI's capacity there stays 1, never
      *>       the formal's fixed count 3. Shown: XE (2, 1),
      *>       capacity, LENGTH (XO (2)) = 1 + 1               = Q, 2 2
      *>   GF  GX through a formal whose OUTER table is fixed
      *>       (OCCURS 2): the same reference, the same result  = Q, 2 2
      *>   GD  a third level: dynamic of dynamic of (FY, FI
      *>       OCCURS 3) opposite dynamic of dynamic of (DY, DI
      *>       DYNAMIC); SPACE into FE (1, 1, 3). Shown: LENGTH
      *>       (DP (1, 1)) = 1 + 3                               = 4
      *>   DW  the stores of GW and GX made in the caller.
      *>       Shown: capacity, LENGTH (DWO (1)), LENGTH
      *>       (DWO (2))                                         = 2 4 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 GA.
          05 AX PIC X VALUE "x".
          05 ATB OCCURS DYNAMIC CAPACITY IN CA FROM 1 INITIALIZED.
             10 AE PIC X VALUE "a".
             10 AF PIC X VALUE "b".
       01 DA.
          05 DX PIC X VALUE "x".
          05 DTB OCCURS DYNAMIC CAPACITY IN CDA FROM 1 INITIALIZED.
             10 DQ PIC X VALUE "a".
             10 DR PIC X VALUE "b".
       01 GS.
          05 SX PIC X VALUE "x".
          05 STB PIC X OCCURS DYNAMIC CAPACITY IN CS FROM 1 VALUE "q".
       01 G2.
          05 BX PIC X VALUE "x".
          05 BTB OCCURS DYNAMIC CAPACITY IN CB FROM 1 INITIALIZED.
             10 BE PIC X VALUE "a".
             10 BF PIC X VALUE "b".
       01 GR.
          05 RX PIC X VALUE "x".
          05 RTB OCCURS DYNAMIC CAPACITY IN CR FROM 1 INITIALIZED.
             10 RQE PIC X VALUE "a".
             10 RQF PIC X VALUE "b".
       01 GV.
          05 VX PIC X VALUE "x".
          05 VTB OCCURS DYNAMIC CAPACITY IN CV FROM 1 INITIALIZED.
             10 VE PIC X VALUE "a".
             10 VF PIC X VALUE "b".
          05 VDL PIC X DYNAMIC LENGTH LIMIT 5.
       01 GN.
          05 NH PIC X VALUE "h".
          05 NT OCCURS DYNAMIC CAPACITY IN CN FROM 1 INITIALIZED.
             10 ND PIC X VALUE "d".
             10 NI PIC X OCCURS DYNAMIC CAPACITY IN CNI FROM 1
                INITIALIZED VALUE "i".
       01 GC.
          05 CX PIC X VALUE "x".
          05 CTB OCCURS DYNAMIC CAPACITY IN CC FROM 1 INITIALIZED.
             10 CQE PIC X VALUE "a".
             10 CQF PIC X VALUE "b".
       01 GK.
          05 KX PIC X VALUE "x".
          05 KTB OCCURS DYNAMIC CAPACITY IN CK FROM 1 INITIALIZED.
             10 KE PIC X VALUE "a".
             10 KF PIC X VALUE "b".
       01 GE.
          05 EX PIC X VALUE "x".
          05 ETB OCCURS DYNAMIC CAPACITY IN CE FROM 1 INITIALIZED.
             10 EE PIC X VALUE "a".
             10 EF PIC X VALUE "b".
       01 GQ.
          05 ON1 PIC 9 VALUE 3.
          05 OTB OCCURS DYNAMIC CAPACITY IN COO FROM 1 INITIALIZED.
             10 OQE PIC X VALUE "a".
             10 OQF PIC X VALUE "b".
       01 GP.
          05 PN1 PIC 9 VALUE 3.
          05 PTB OCCURS DYNAMIC CAPACITY IN CPP FROM 1 INITIALIZED.
             10 PQE PIC X VALUE "a".
             10 PQF PIC X VALUE "b".
       01 WS-SE PIC X(4).
       01 GZ.
          05 ZX PIC X VALUE "x".
          05 ZTB OCCURS DYNAMIC CAPACITY IN CZ FROM 1 INITIALIZED.
             10 ZQ PIC 9 VALUE 9.
             10 ZE PIC Z9 VALUE " 7".
             10 ZG PIC +9.9E+99 VALUE "+1.0E+00".
             10 ZF USAGE COMP-1 VALUE 0.
       01 DZ.
          05 DZX PIC X VALUE "x".
          05 DZTB OCCURS DYNAMIC CAPACITY IN CDZ FROM 1 INITIALIZED.
             10 DZQ PIC 9 VALUE 9.
             10 DZE PIC Z9 VALUE " 7".
             10 DZG PIC +9.9E+99 VALUE "+1.0E+00".
             10 DZF USAGE COMP-1 VALUE 0.
       01 GW.
          05 WX PIC X VALUE "x".
          05 WO OCCURS DYNAMIC CAPACITY IN CWO FROM 1.
             10 WY PIC X.
             10 WI OCCURS DYNAMIC CAPACITY IN CWI FROM 1.
                15 WE PIC X.
       01 GI.
          05 IX PIC X VALUE "x".
          05 IO OCCURS DYNAMIC CAPACITY IN CIO FROM 1.
             10 IY PIC X.
             10 II OCCURS DYNAMIC CAPACITY IN CII FROM 1
                INITIALIZED.
                15 IE PIC X VALUE "i".
       01 GX.
          05 XX PIC X VALUE "x".
          05 XO OCCURS DYNAMIC CAPACITY IN CXO FROM 1.
             10 XY PIC X.
             10 XI OCCURS DYNAMIC CAPACITY IN CXI FROM 1.
                15 XE PIC X.
       01 GF.
          05 FFX PIC X VALUE "x".
          05 FFO OCCURS DYNAMIC CAPACITY IN CFFO FROM 1.
             10 FFY PIC X.
             10 FFI OCCURS DYNAMIC CAPACITY IN CFFI FROM 1.
                15 FFE PIC X.
       01 GD.
          05 DDX PIC X VALUE "x".
          05 DDO OCCURS DYNAMIC CAPACITY IN CDDO FROM 1.
             10 DP OCCURS DYNAMIC CAPACITY IN CDP FROM 1.
                15 DDY PIC X.
                15 DDI OCCURS DYNAMIC CAPACITY IN CDDI FROM 1.
                   20 DDE PIC X.
       01 DW.
          05 DWX PIC X VALUE "x".
          05 DWO OCCURS DYNAMIC CAPACITY IN CDWO FROM 1.
             10 DWY PIC X.
             10 DWI OCCURS DYNAMIC CAPACITY IN CDWI FROM 1.
                15 DWE PIC X.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB2690SA" AS NESTED USING GA
           MOVE CA TO WS-C
           DISPLAY "GA=[" AE (1) AF (1) " " AE (2) AF (2) " "
               AE (3) AF (3) "] " WS-C
           MOVE "a" TO DQ (3)
           MOVE CDA TO WS-C
           DISPLAY "DA=[" DQ (1) DR (1) " " DQ (2) DR (2) " "
               DQ (3) DR (3) "] " WS-C
           CALL "PB2690SS" AS NESTED USING GS
           MOVE CS TO WS-C
           DISPLAY "GS=[" STB (1) "] " WS-C
           CALL "PB2690S2" AS NESTED USING G2
           MOVE CB TO WS-C
           DISPLAY "G2=[" BE (1) BF (1) " " BE (2) BF (2) "] " WS-C
           CALL "PB2690SR" AS NESTED USING GR
           MOVE CR TO WS-C
           DISPLAY "GR=" WS-C
           MOVE "dd" TO VDL
           CALL "PB2690SV" AS NESTED USING GV
           MOVE CV TO WS-C
           DISPLAY "GV=[" VE (1) VF (1) " " VE (2) VF (2) " "
               VE (3) VF (3) "] " WS-C
           CALL "PB2690SN" AS NESTED USING GN
           MOVE CN TO WS-C
           DISPLAY "GN=[" ND (1) " " ND (2) " " ND (3) "] " WS-C
           CALL "PB2690SA" AS NESTED USING BY CONTENT GC
           MOVE CC TO WS-C
           DISPLAY "GC=" WS-C
           CALL "PB2690SK" AS NESTED USING GK
           MOVE CK TO WS-C
           DISPLAY "GK=" WS-C
           CALL "PB2690SE" AS NESTED USING GE
           MOVE CE TO WS-C
           DISPLAY "GE=[" EE (1) EF (1) "] " WS-C
           CALL "PB2690SO" AS NESTED USING GQ
           MOVE COO TO WS-C
           DISPLAY "GQ=" WS-C
           CALL "PB2690SP" AS NESTED USING GP
           MOVE CPP TO WS-C
           DISPLAY "GP=[" PQE (1) PQF (1) " " PQE (2) PQF (2) " "
               PQE (3) PQF (3) "] " WS-C
           MOVE "...." TO WS-SE
           CALL "PB2690SZ" AS NESTED USING GZ WS-SE
           MOVE CZ TO WS-C
           DISPLAY "GZ=[" WS-SE "] " WS-C
           MOVE "...." TO WS-SE
           ADD 5 TO DZQ (3) ON SIZE ERROR MOVE "Q" TO WS-SE (1:1)
           END-ADD
           ADD 500 TO 1 GIVING DZE (3)
               ON SIZE ERROR MOVE "E" TO WS-SE (2:1)
           END-ADD
           COMPUTE DZG (3) = 1.0E300
               ON SIZE ERROR MOVE "G" TO WS-SE (3:1)
           END-COMPUTE
           COMPUTE DZF (3) = 1.0E300
               ON SIZE ERROR MOVE "F" TO WS-SE (4:1)
           END-COMPUTE
           MOVE CDZ TO WS-C
           DISPLAY "DZ=[" WS-SE "] " WS-C
           CALL "PB2690W1" AS NESTED USING GW
           MOVE CWO TO WS-C
           DISPLAY "GW=" WS-C " " FUNCTION LENGTH (WO (1))
           CALL "PB2690W2" AS NESTED USING GI
           MOVE CIO TO WS-C
           DISPLAY "GI=[" IE (1, 1) IE (1, 2) IE (1, 3) "] " WS-C
               " " FUNCTION LENGTH (IO (1))
           CALL "PB2690W3" AS NESTED USING GX
           MOVE CXO TO WS-C
           DISPLAY "GX=[" XE (2, 1) "] " WS-C
               " " FUNCTION LENGTH (XO (2))
           CALL "PB2690W4" AS NESTED USING GF
           MOVE CFFO TO WS-C
           DISPLAY "GF=[" FFE (2, 1) "] " WS-C
               " " FUNCTION LENGTH (FFO (2))
           CALL "PB2690W5" AS NESTED USING GD
           DISPLAY "GD=" FUNCTION LENGTH (DP (1, 1))
           MOVE SPACE TO DWE (1, 3)
           MOVE "Q" TO DWE (2, 1)
           MOVE CDWO TO WS-C
           DISPLAY "DW=" WS-C " " FUNCTION LENGTH (DWO (1))
               " " FUNCTION LENGTH (DWO (2))
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SA IS COMMON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA.
          05 LAX PIC X.
          05 TF OCCURS 3.
             10 FE PIC X.
             10 FF PIC X.
       PROCEDURE DIVISION USING LA.
       MAIN.
           MOVE "a" TO FE (3)
           GOBACK.
       END PROGRAM PB2690SA.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS.
          05 LSX PIC X.
          05 TS PIC X OCCURS 3.
       PROCEDURE DIVISION USING LS.
       MAIN.
           MOVE SPACE TO TS (3)
           GOBACK.
       END PROGRAM PB2690SS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690S2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L2.
          05 L2X PIC X.
          05 T2 OCCURS 3.
             10 E2 PIC X.
             10 F2 PIC X.
       PROCEDURE DIVISION USING L2.
       MAIN.
           MOVE "a" TO E2 (2)
           GOBACK.
       END PROGRAM PB2690S2.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E PIC X.
       LINKAGE SECTION.
       01 LR.
          05 LRX PIC X.
          05 TR OCCURS 3.
             10 ER PIC X.
             10 FR PIC X.
       PROCEDURE DIVISION USING LR.
       MAIN.
           MOVE ER (3) TO W-E
           GOBACK.
       END PROGRAM PB2690SR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV.
          05 LVX PIC X.
          05 TV OCCURS 3.
             10 LVE PIC X.
             10 LVF PIC X.
          05 LDL PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LV.
       MAIN.
           MOVE "a" TO LVE (3)
           GOBACK.
       END PROGRAM PB2690SV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN.
          05 LNH PIC X.
          05 LNT OCCURS 3.
             10 LND PIC X.
             10 LNI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LN.
       MAIN.
           MOVE "d" TO LND (3)
           GOBACK.
       END PROGRAM PB2690SN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK.
          05 LKX PIC X.
          05 TK OCCURS 3.
             10 KE2 PIC X.
             10 KF2 PIC X.
       PROCEDURE DIVISION USING LK.
       MAIN.
           CALL "PB2690SA" USING LK
           GOBACK.
       END PROGRAM PB2690SK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE.
          05 LEX PIC X.
          05 TE OCCURS 3.
             10 LEE PIC X.
             10 LEF PIC X.
       PROCEDURE DIVISION USING LE.
       MAIN.
           CALL "PB2690SX" USING LE
           GOBACK.
       END PROGRAM PB2690SE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SX IS COMMON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX7 PIC X(7).
       PROCEDURE DIVISION USING LX7.
       MAIN.
           MOVE "Q" TO LX7 (2:1)
           GOBACK.
       END PROGRAM PB2690SX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO.
          05 LON PIC 9.
          05 TO1 OCCURS 1 TO 3 DEPENDING ON LON.
             10 LOE PIC X.
             10 LOF PIC X.
       PROCEDURE DIVISION USING LO.
       MAIN.
           CALL "PB2690SD" USING LO
           GOBACK.
       END PROGRAM PB2690SO.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP.
          05 LPN PIC 9.
          05 TP1 OCCURS 1 TO 3 DEPENDING ON LPN.
             10 LPE PIC X.
             10 LPF PIC X.
       PROCEDURE DIVISION USING LP.
       MAIN.
           MOVE "a" TO LPE (3)
           CALL "PB2690SD" USING LP
           GOBACK.
       END PROGRAM PB2690SP.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SD IS COMMON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD.
          05 LDN PIC 9.
          05 TD OCCURS DYNAMIC CAPACITY IN CDD FROM 1.
             10 LDE PIC X.
             10 LDF PIC X.
       PROCEDURE DIVISION USING LD.
       MAIN.
           GOBACK.
       END PROGRAM PB2690SD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SZ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LZ.
          05 LZX PIC X.
          05 TZ OCCURS 3.
             10 LZQ PIC 9.
             10 LZE PIC Z9.
             10 LZG PIC +9.9E+99.
             10 LZF USAGE COMP-1.
       01 LSE PIC X(4).
       PROCEDURE DIVISION USING LZ LSE.
       MAIN.
           ADD 5 TO LZQ (3) ON SIZE ERROR MOVE "Q" TO LSE (1:1)
           END-ADD
           ADD 500 TO 1 GIVING LZE (3)
               ON SIZE ERROR MOVE "E" TO LSE (2:1)
           END-ADD
           COMPUTE LZG (3) = 1.0E300
               ON SIZE ERROR MOVE "G" TO LSE (3:1)
           END-COMPUTE
           COMPUTE LZF (3) = 1.0E300
               ON SIZE ERROR MOVE "F" TO LSE (4:1)
           END-COMPUTE
           GOBACK.
       END PROGRAM PB2690SZ.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690W1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LWX PIC X.
          05 LWO OCCURS DYNAMIC CAPACITY IN CLWO FROM 1.
             10 LWY PIC X.
             10 LWI OCCURS 3.
                15 LWE PIC X.
       PROCEDURE DIVISION USING LW.
       MAIN.
           MOVE SPACE TO LWE (1, 3)
           GOBACK.
       END PROGRAM PB2690W1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690W2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LWX PIC X.
          05 LWO OCCURS DYNAMIC CAPACITY IN CLWO FROM 1.
             10 LWY PIC X.
             10 LWI OCCURS 3.
                15 LWE PIC X.
       PROCEDURE DIVISION USING LW.
       MAIN.
           MOVE "i" TO LWE (1, 3)
           GOBACK.
       END PROGRAM PB2690W2.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690W3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LWX PIC X.
          05 LWO OCCURS DYNAMIC CAPACITY IN CLWO FROM 1.
             10 LWY PIC X.
             10 LWI OCCURS 3.
                15 LWE PIC X.
       PROCEDURE DIVISION USING LW.
       MAIN.
           MOVE "Q" TO LWE (2, 1)
           GOBACK.
       END PROGRAM PB2690W3.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690W4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LWX PIC X.
          05 LWO OCCURS 2.
             10 LWY PIC X.
             10 LWI OCCURS 3.
                15 LWE PIC X.
       PROCEDURE DIVISION USING LW.
       MAIN.
           MOVE "Q" TO LWE (2, 1)
           GOBACK.
       END PROGRAM PB2690W4.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690W5.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LWX PIC X.
          05 LWO OCCURS DYNAMIC CAPACITY IN CLWO FROM 1.
             10 LWP OCCURS DYNAMIC CAPACITY IN CLWP FROM 1.
                15 LWY PIC X.
                15 LWI OCCURS 3.
                   20 LWE PIC X.
       PROCEDURE DIVISION USING LW.
       MAIN.
           MOVE SPACE TO LWE (1, 1, 3)
           GOBACK.
       END PROGRAM PB2690W5.
       END PROGRAM PB2690SG.
