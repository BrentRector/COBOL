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

       END OBJECT.
       END CLASS PB2690SK3.
