*>--- conformance-case
*> id: pb2690_fixed_table_formal_grows_dynamic_argument_call
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - A FIXED TABLE OF THE FORMAL STORED OVER A DYNAMIC-CAPACITY
      *> TABLE OF THE ARGUMENT, PAST THE ARGUMENT'S CURRENT CAPACITY, ACROSS A
      *> FORMAT-2 CALL.
      *>
      *> 14.2.3 GR8: "If the argument is passed by reference, the activated runtime
      *> element operates as if the formal parameter occupies the same storage area
      *> as the argument." The formal's fixed table TF therefore IS the argument's
      *> dynamic-capacity table T: a store to TF (2) is a store to an occurrence of
      *> T, and 8.5.1.9.3 says "When a data item in a dynamic-capacity table is
      *> referenced as a receiving item and the value of the subscript exceeds the
      *> current capacity of the table, a new element is automatically created and
      *> the capacity of the table is increased to the value given by the
      *> subscript". The two descriptions are compatible (8.5.1.12.3: the dynamic-
      *> capacity table is "considered to be the same length as the corresponding
      *> table"), so the call is legal; the callee reads a space-filled occurrence
      *> where the argument has none (14.6.9.2 2: the remaining elements are
      *> "space filled") and a callee that
      *> never stores past the capacity creates nothing (8.5.1.9.3 acts only on a
      *> receiving reference).
      *>
      *> EXPECTED VALUES, DERIVED (T starts with the one element "a"):
      *>   GA  the callee stores TF (2) := "q" and TF (3) := "r": capacity 3
      *>                                                          = aqr, 3
      *>   GB  the callee only reads TF (1): capacity stays 1     = a, 1
      *>   GC  BY CONTENT (14.2.3 GR9): the callee's stores never reach the
      *>       argument                                           = a, 1
      *>   GD  a fixed-length group formal (L1 X(2), LT X OCCURS 3, L3 X(2))
      *>       over V1 X(2), VT (capacity 1), V3 X(2): LT (3) := "z" creates
      *>       occurrences 2 and 3 of VT and V1 and V3 survive    = gh, kz, ij, 3
      *>   GE  a callee storing only into the dynamic-length item of a formal
      *>       whose table is longer than the capacity leaves the capacity as
      *>       it was                                             = Gp, a, 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GRO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 GA.
          05 AX PIC X VALUE "x".
          05 ATB OCCURS DYNAMIC CAPACITY IN CA FROM 1.
             10 AE PIC X VALUE "a".
          05 AD PIC X DYNAMIC LENGTH LIMIT 5.
       01 GB.
          05 BX PIC X VALUE "x".
          05 BT OCCURS DYNAMIC CAPACITY IN CB FROM 1.
             10 BE PIC X VALUE "a".
          05 BD PIC X DYNAMIC LENGTH LIMIT 5.
       01 GC.
          05 CX PIC X VALUE "x".
          05 CT OCCURS DYNAMIC CAPACITY IN CC FROM 1.
             10 CE PIC X VALUE "a".
          05 CD PIC X DYNAMIC LENGTH LIMIT 5.
       01 GD.
          05 V1 PIC X(2) VALUE "gh".
          05 VT PIC X OCCURS DYNAMIC CAPACITY IN CD1 FROM 1.
          05 V3 PIC X(2) VALUE "ij".
       01 GE.
          05 EX PIC X VALUE "x".
          05 ET OCCURS DYNAMIC CAPACITY IN CE1 FROM 1.
             10 EE PIC X VALUE "a".
          05 ED PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "dd" TO AD OF GA
           CALL "PB2690GA" AS NESTED USING GA
           MOVE CA TO WS-C
           DISPLAY "GA=[" AE (1) AE (2) AE (3) "] " WS-C
           MOVE "dd" TO BD OF GB
           CALL "PB2690GB" AS NESTED USING GB
           MOVE CB TO WS-C
           DISPLAY "GB=[" BE (1) "] " WS-C
           MOVE "dd" TO CD OF GC
           CALL "PB2690GC" AS NESTED USING BY CONTENT GC
           MOVE CC TO WS-C
           DISPLAY "GC=[" CE (1) "] " WS-C
           MOVE "k" TO VT (1)
           CALL "PB2690GD" AS NESTED USING GD
           MOVE CD1 TO WS-C
           DISPLAY "GD=[" V1 "][" VT (1) VT (3) "][" V3 "] " WS-C
           MOVE "dd" TO ED OF GE
           CALL "PB2690GE" AS NESTED USING GE
           MOVE CE1 TO WS-C
           DISPLAY "GE=[" ED OF GE "][" EE (1) "] " WS-C
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA.
          05 LAX PIC X.
          05 TF OCCURS 3.
             10 FE PIC X.
          05 LAD PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LA.
       MAIN.
           MOVE "q" TO FE (2)
           MOVE "r" TO FE (3)
           GOBACK.
       END PROGRAM PB2690GA.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB.
          05 LBX PIC X.
          05 TFB OCCURS 3.
             10 FEB PIC X.
          05 LBD PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LB.
       MAIN.
           DISPLAY "GB sees " FEB (1)
           GOBACK.
       END PROGRAM PB2690GB.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC.
          05 LCX PIC X.
          05 TFC OCCURS 3.
             10 FEC PIC X.
          05 LCD PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LC.
       MAIN.
           MOVE "q" TO FEC (2)
           MOVE "r" TO FEC (3)
           GOBACK.
       END PROGRAM PB2690GC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD.
          05 L1 PIC X(2).
          05 LT PIC X OCCURS 3.
          05 L3 PIC X(2).
       PROCEDURE DIVISION USING LD.
       MAIN.
           MOVE "z" TO LT (3)
           GOBACK.
       END PROGRAM PB2690GD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE.
          05 LEX PIC X.
          05 TFE OCCURS 3.
             10 FEE PIC X.
          05 LED PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LE.
       MAIN.
           MOVE "Gp" TO LED
           GOBACK.
       END PROGRAM PB2690GE.
       END PROGRAM PB2690GRO.
