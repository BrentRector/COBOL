      *> kb/Work PB2690 - THE OCCURRENCES A BY REFERENCE FORMAL'S FIXED TABLE
      *> CREATES IN AN INITIALIZED DYNAMIC-CAPACITY TABLE OF THE ARGUMENT, ACROSS
      *> AN INVOKE: the typed lane, the universal lane (every argument BY
      *> REFERENCE, 14.9.23.3 SR6) and BY CONTENT. The INVOKE twin of
      *> pb2690_initialized_created_occurrences_call.
      *>
      *> 14.2.3 GR8 makes the formal occupy the argument's storage, so a store
      *> past the capacity creates the element (8.5.1.9.3), and 8.5.1.9.5 makes
      *> every item the statement does not store, and every intermediate
      *> occurrence, the INITIALIZED default ("as though they had been the
      *> subject of a statement of the form INITIALIZE ... WITH FILLER ALL TO
      *> VALUE THEN TO DEFAULT"). A method that only reads such an occurrence
      *> reads the initialized element and creates nothing (determination
      *> D-VGB); BY CONTENT (14.2.3 GR9) its fixed table holds the MOVE's space
      *> fill past the sending capacity (14.6.9.2 2).
      *>
      *> EXPECTED VALUES, DERIVED (each table starts with the one element "ab"):
      *>   SC  the method stores LE1 (3) := "r"           = ab, ab, rb, 3
      *>   RD  the method only reads LT (2): it sees ab   = ab, 1
      *>   VF  a variable-length formal; the method stores LF2 (3) := "z"
      *>                                                  = ab, ab, az, 3
      *>   UN  SC through a universal object reference    = ab, ab, rb, 3
      *>   BC  BY CONTENT: the method sees spaces         = ab, 1
      *>   NE  elements holding an INITIALIZED inner dynamic table (ND "d", NI
      *>       "i" FROM 1); the method stores LI (3, 2) := "X"
      *>                                                  = di, diX, 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690ICI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2690IK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2690IK.
       01 U USAGE OBJECT REFERENCE.
       01 WS-C PIC 9.
       01 GS.
          05 SH PIC X VALUE "x".
          05 STB OCCURS DYNAMIC CAPACITY IN CS FROM 1 INITIALIZED.
             10 SE PIC X VALUE "a".
             10 SF PIC X VALUE "b".
       01 GR.
          05 QH PIC X VALUE "x".
          05 RTB OCCURS DYNAMIC CAPACITY IN CR1 FROM 1 INITIALIZED.
             10 QE PIC X VALUE "a".
             10 QF PIC X VALUE "b".
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
       01 GB.
          05 BH PIC X VALUE "x".
          05 BTB OCCURS DYNAMIC CAPACITY IN CB FROM 1 INITIALIZED.
             10 BE PIC X VALUE "a".
             10 BF PIC X VALUE "b".
       01 GN.
          05 NH PIC X VALUE "h".
          05 NT OCCURS DYNAMIC CAPACITY IN CN FROM 1 INITIALIZED.
             10 ND PIC X VALUE "d".
             10 NI PIC X OCCURS DYNAMIC CAPACITY IN CNI FROM 1
                INITIALIZED VALUE "i".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2690IK "NEW" RETURNING O
           INVOKE O "STORESC" USING GS
           MOVE CS TO WS-C
           DISPLAY "SC=[" SE (1) SF (1) "|" SE (2) SF (2) "|"
               SE (3) SF (3) "] " WS-C
           INVOKE O "READRD" USING GR
           MOVE CR1 TO WS-C
           DISPLAY "RD=[" QE (1) QF (1) "] " WS-C
           MOVE "dd" TO VDL
           INVOKE O "STOREVF" USING GV
           MOVE CV TO WS-C
           DISPLAY "VF=[" VE (1) VF1 (1) "|" VE (2) VF1 (2) "|"
               VE (3) VF1 (3) "] " WS-C
           SET U TO O
           INVOKE U "STORESC" USING GU
           MOVE CU TO WS-C
           DISPLAY "UN=[" UE (1) UF (1) "|" UE (2) UF (2) "|"
               UE (3) UF (3) "] " WS-C
           INVOKE O "READRD" USING BY CONTENT GB
           MOVE CB TO WS-C
           DISPLAY "BC=[" BE (1) BF (1) "] " WS-C
           INVOKE O "STORENE" USING GN
           MOVE CN TO WS-C
           DISPLAY "NE=[" ND (2) NI (2, 1) "|" ND (3) NI (3, 1)
               NI (3, 2) "] " WS-C
           STOP RUN.
       END PROGRAM PB2690ICI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2690IK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. STORESC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS.
          05 LSH PIC X.
          05 LT OCCURS 3.
             10 LE1 PIC X.
             10 LF1 PIC X.
       PROCEDURE DIVISION USING LS.
       MAIN-P.
           MOVE "r" TO LE1 (3).
       END METHOD STORESC.

       METHOD-ID. READRD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR.
          05 LRH PIC X.
          05 LRT OCCURS 3.
             10 LRE PIC X.
             10 LRF PIC X.
       PROCEDURE DIVISION USING LR.
       MAIN-P.
           DISPLAY "sees [" LRE (2) LRF (2) "]".
       END METHOD READRD.

       METHOD-ID. STOREVF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV.
          05 LVH PIC X.
          05 LVT OCCURS 3.
             10 LE2 PIC X.
             10 LF2 PIC X.
          05 LVD PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LV.
       MAIN-P.
           MOVE "z" TO LF2 (3).
       END METHOD STOREVF.

       METHOD-ID. STORENE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN.
          05 LNH PIC X.
          05 LTF OCCURS 3.
             10 LD1 PIC X.
             10 LI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LN.
       MAIN-P.
           MOVE "X" TO LI (3, 2).
       END METHOD STORENE.

       END OBJECT.
       END CLASS PB2690IK.
