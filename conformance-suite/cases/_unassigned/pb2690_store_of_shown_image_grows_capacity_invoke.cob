*>--- conformance-case
*> id: pb2690_store_of_shown_image_grows_capacity_invoke
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - A BY REFERENCE METHOD FORMAL'S FIXED TABLE STORED PAST
      *> THE CAPACITY OF THE ARGUMENT'S DYNAMIC-CAPACITY TABLE RAISES THE
      *> CAPACITY WHATEVER IMAGE IT STORES, ACROSS AN INVOKE: the typed lane,
      *> the universal lane (every argument BY REFERENCE, 14.9.23.3 SR6) and BY
      *> CONTENT. The INVOKE twin of
      *> pb2690_store_of_shown_image_grows_capacity_call.
      *>
      *> 14.2.3 GR8 makes the formal occupy the argument's storage, so a store
      *> through occurrence N of its fixed table is a receiving reference to
      *> occurrence N of the argument's dynamic-capacity table, and 8.5.1.9.3
      *> increases the capacity "to the value given by the subscript"; the value
      *> stored does not enter the rule. 8.5.1.9.5 initializes the INITIALIZED
      *> table's new items the statement does not store.
      *>
      *> EXPECTED VALUES, DERIVED (each table starts with ONE element; the
      *> INITIALIZED element is "ab"):
      *>   SA  the method stores LE1 (3) := "a", the default  = ab ab ab, 3
      *>   VF  a variable-length formal; it stores LVE (3)
      *>       := "a"                                          = ab ab ab, 3
      *>   UN  SA through a universal object reference        = ab ab ab, 3
      *>   SP  no INITIALIZED (PIC X elements "q"); the method
      *>       stores SPACE into LSP (3)                       = q, 3
      *>   UP  SP through a universal object reference        = q, 3
      *>   BC  SA BY CONTENT (14.2.3 GR9): nothing created    = 1
      *>   UO  a method whose formal has a fixed table passes it
      *>       on through a universal object reference to a
      *>       method whose formal has a DYNAMIC-CAPACITY table
      *>       (another shape), which stores nothing: no
      *>       statement references an occurrence past the
      *>       capacity                                        = 1
      *>   US  UO, but the first method stores the default "a"
      *>       into its LE (3) before passing it on            = ab ab ab, 3
      *>   TO  an OCCURS DEPENDING formal passed on to the same
      *>       method through the typed reference SELF          = 1
      *>   NW  a formal whose OWN table is dynamic, of elements
      *>       (FY, FI OCCURS 3), opposite elements (WY, WI
      *>       DYNAMIC FROM 1); the method stores SPACE into
      *>       LNE (1, 3), a receiving reference to WI (3) of
      *>       WO (1). Shown: capacity, LENGTH (WO (1)) = 1 + 3  = 1 4
      *>   NU  NW through a universal object reference         = 1 4
      *>   NX  NW, but the method stores "Q" into LNE (2, 1):
      *>       occurrence 2 is created and only XI (1) of it is
      *>       referenced. Shown: capacity, LENGTH (XO (2))     = 2 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SGI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2690SK3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2690SK3.
       01 U USAGE OBJECT REFERENCE.
       01 WS-C PIC 9.
       01 GS.
          05 SH PIC X VALUE "x".
          05 STB OCCURS DYNAMIC CAPACITY IN CS FROM 1 INITIALIZED.
             10 SE PIC X VALUE "a".
             10 SF PIC X VALUE "b".
       01 GV.
          05 VH PIC X VALUE "x".
          05 VTB OCCURS DYNAMIC CAPACITY IN CV FROM 1 INITIALIZED.
             10 VE PIC X VALUE "a".
             10 VF1 PIC X VALUE "b".
          05 VDL PIC X DYNAMIC LENGTH LIMIT 5.
       01 GU.
          05 UH PIC X VALUE "x".
          05 UTB OCCURS DYNAMIC CAPACITY IN CU FROM 1 INITIALIZED.
             10 UE PIC X VALUE "a".
             10 UF PIC X VALUE "b".
       01 GP.
          05 PH1 PIC X VALUE "x".
          05 PTB PIC X OCCURS DYNAMIC CAPACITY IN CPS FROM 1 VALUE "q".
       01 GPU.
          05 PUH PIC X VALUE "x".
          05 PUE PIC X OCCURS DYNAMIC CAPACITY IN CPU FROM 1 VALUE "q".
       01 GB.
          05 BH PIC X VALUE "x".
          05 BTB OCCURS DYNAMIC CAPACITY IN CB FROM 1 INITIALIZED.
             10 BE PIC X VALUE "a".
             10 BF PIC X VALUE "b".
       01 GUO.
          05 OH PIC X VALUE "x".
          05 OTB OCCURS DYNAMIC CAPACITY IN COU FROM 1 INITIALIZED.
             10 OQE PIC X VALUE "a".
             10 OQF PIC X VALUE "b".
       01 GUS.
          05 QH PIC X VALUE "x".
          05 QTB OCCURS DYNAMIC CAPACITY IN CQS FROM 1 INITIALIZED.
             10 QQE PIC X VALUE "a".
             10 QQF PIC X VALUE "b".
       01 GTO.
          05 TN1 PIC 9 VALUE 3.
          05 TTB OCCURS DYNAMIC CAPACITY IN CTO FROM 1 INITIALIZED.
             10 TQE PIC X VALUE "a".
             10 TQF PIC X VALUE "b".
       01 GNW.
          05 WX PIC X VALUE "x".
          05 WO OCCURS DYNAMIC CAPACITY IN CWO FROM 1.
             10 WY PIC X.
             10 WI OCCURS DYNAMIC CAPACITY IN CWI FROM 1.
                15 WE PIC X.
       01 GNU.
          05 NUX PIC X VALUE "x".
          05 NUO OCCURS DYNAMIC CAPACITY IN CNUO FROM 1.
             10 NUY PIC X.
             10 NUI OCCURS DYNAMIC CAPACITY IN CNUI FROM 1.
                15 NUE PIC X.
       01 GNX.
          05 XX PIC X VALUE "x".
          05 XO OCCURS DYNAMIC CAPACITY IN CXO FROM 1.
             10 XY PIC X.
             10 XI OCCURS DYNAMIC CAPACITY IN CXI FROM 1.
                15 XE PIC X.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2690SK3 "NEW" RETURNING O
           INVOKE O "STORESA" USING GS
           MOVE CS TO WS-C
           DISPLAY "SA=[" SE (1) SF (1) " " SE (2) SF (2) " "
               SE (3) SF (3) "] " WS-C
           MOVE "dd" TO VDL
           INVOKE O "STOREVF" USING GV
           MOVE CV TO WS-C
           DISPLAY "VF=[" VE (1) VF1 (1) " " VE (2) VF1 (2) " "
               VE (3) VF1 (3) "] " WS-C
           SET U TO O
           INVOKE U "STORESA" USING GU
           MOVE CU TO WS-C
           DISPLAY "UN=[" UE (1) UF (1) " " UE (2) UF (2) " "
               UE (3) UF (3) "] " WS-C
           INVOKE O "STORESP" USING GP
           MOVE CPS TO WS-C
           DISPLAY "SP=[" PTB (1) "] " WS-C
           INVOKE U "STORESP" USING GPU
           MOVE CPU TO WS-C
           DISPLAY "UP=[" PUE (1) "] " WS-C
           INVOKE O "STORESA" USING BY CONTENT GB
           MOVE CB TO WS-C
           DISPLAY "BC=" WS-C
           INVOKE O "PASSONU" USING GUO
           MOVE COU TO WS-C
           DISPLAY "UO=" WS-C
           INVOKE O "STOREPASSONU" USING GUS
           MOVE CQS TO WS-C
           DISPLAY "US=[" QQE (1) QQF (1) " " QQE (2) QQF (2) " "
               QQE (3) QQF (3) "] " WS-C
           INVOKE O "PASSONT" USING GTO
           MOVE CTO TO WS-C
           DISPLAY "TO=" WS-C
           INVOKE O "STORENW" USING GNW
           MOVE CWO TO WS-C
           DISPLAY "NW=" WS-C " " FUNCTION LENGTH (WO (1))
           INVOKE U "STORENW" USING GNU
           MOVE CNUO TO WS-C
           DISPLAY "NU=" WS-C " " FUNCTION LENGTH (NUO (1))
           INVOKE O "STORENX" USING GNX
           MOVE CXO TO WS-C
           DISPLAY "NX=" WS-C " " FUNCTION LENGTH (XO (2))
           STOP RUN.
       END PROGRAM PB2690SGI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2690SK3 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. STORESA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS.
          05 LSH PIC X.
          05 LT OCCURS 3.
             10 LE1 PIC X.
             10 LF1 PIC X.
       PROCEDURE DIVISION USING LS.
       MAIN-P.
           MOVE "a" TO LE1 (3).
       END METHOD STORESA.

       METHOD-ID. STOREVF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV.
          05 LVH PIC X.
          05 LVT OCCURS 3.
             10 LVE PIC X.
             10 LVF PIC X.
          05 LVD PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LV.
       MAIN-P.
           MOVE "a" TO LVE (3).
       END METHOD STOREVF.

       METHOD-ID. STORESP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP.
          05 LPH PIC X.
          05 LSP PIC X OCCURS 3.
       PROCEDURE DIVISION USING LP.
       MAIN-P.
           MOVE SPACE TO LSP (3).
       END METHOD STORESP.

       METHOD-ID. PASSONU.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 UL USAGE OBJECT REFERENCE.
       LINKAGE SECTION.
       01 LO.
          05 LOH PIC X.
          05 LOT OCCURS 3.
             10 LOE PIC X.
             10 LOF PIC X.
       PROCEDURE DIVISION USING LO.
       MAIN-P.
           SET UL TO SELF
           INVOKE UL "TAKEDYN" USING LO.
       END METHOD PASSONU.

       METHOD-ID. STOREPASSONU.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 UL USAGE OBJECT REFERENCE.
       LINKAGE SECTION.
       01 LQ.
          05 LQH PIC X.
          05 LQT OCCURS 3.
             10 LQE PIC X.
             10 LQF PIC X.
       PROCEDURE DIVISION USING LQ.
       MAIN-P.
           MOVE "a" TO LQE (3)
           SET UL TO SELF
           INVOKE UL "TAKEDYN" USING LQ.
       END METHOD STOREPASSONU.

       METHOD-ID. TAKEDYN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD.
          05 LDH PIC X.
          05 LDT OCCURS DYNAMIC CAPACITY IN CDT FROM 1.
             10 LDE PIC X.
             10 LDF PIC X.
       PROCEDURE DIVISION USING LD.
       MAIN-P.
           CONTINUE.
       END METHOD TAKEDYN.

       METHOD-ID. PASSONT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LT9.
          05 LTN PIC 9.
          05 LTT OCCURS 1 TO 3 DEPENDING ON LTN.
             10 LTE PIC X.
             10 LTF PIC X.
       PROCEDURE DIVISION USING LT9.
       MAIN-P.
           INVOKE SELF "TAKEDYN9" USING LT9.
       END METHOD PASSONT.

       METHOD-ID. TAKEDYN9.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD9.
          05 LD9N PIC 9.
          05 LD9T OCCURS DYNAMIC CAPACITY IN CD9 FROM 1.
             10 LD9E PIC X.
             10 LD9F PIC X.
       PROCEDURE DIVISION USING LD9.
       MAIN-P.
           CONTINUE.
       END METHOD TAKEDYN9.

       METHOD-ID. STORENW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN.
          05 LNX PIC X.
          05 LNO OCCURS DYNAMIC CAPACITY IN CSTORENW FROM 1.
             10 LNY PIC X.
             10 LNI OCCURS 3.
                15 LNE PIC X.
       PROCEDURE DIVISION USING LN.
       MAIN-P.
           MOVE SPACE TO LNE (1, 3).
       END METHOD STORENW.

       METHOD-ID. STORENX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN.
          05 LNX PIC X.
          05 LNO OCCURS DYNAMIC CAPACITY IN CSTORENX FROM 1.
             10 LNY PIC X.
             10 LNI OCCURS 3.
                15 LNE PIC X.
       PROCEDURE DIVISION USING LN.
       MAIN-P.
           MOVE "Q" TO LNE (2, 1).
       END METHOD STORENX.

       END OBJECT.
       END CLASS PB2690SK3.
