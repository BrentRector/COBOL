*>--- conformance-case
*> id: pb2690_fixed_table_formal_grows_dynamic_argument_invoke
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - A FIXED TABLE OF THE FORMAL STORED PAST THE CAPACITY OF THE
      *> ARGUMENT'S DYNAMIC-CAPACITY TABLE, ACROSS AN INVOKE. The INVOKE twin of
      *> pb2690_fixed_table_formal_grows_dynamic_argument_call and of
      *> pb2690_fixed_table_formal_grows_vlg_element_table_call.
      *>
      *> 14.2.3 GR8: the BY REFERENCE formal "operates as if the formal parameter
      *> occupies the same storage area as the argument", so a store to the
      *> formal's occurrence 3 is a receiving reference to the argument's
      *> dynamic-capacity table whose subscript "exceeds the current capacity of
      *> the table", and 8.5.1.9.3 creates the element and raises the capacity
      *> "to the value given by the subscript"; a method that stores nothing past
      *> the capacity creates nothing. The descriptions are compatible
      *> (8.5.1.12.3: the dynamic-capacity table is "considered to be the same
      *> length as the corresponding table").
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   SC  scalar elements: VT = "a", capacity 1; the method stores
      *>       LT (2) := "q" and LT (3) := "r"                  = aqr, 3
      *>   ST  the same, the method storing LH only             = a, 1
      *>   NE  VG = [a b c d] [e f], capacity 2; the method stores LD (3) := "Y"
      *>       and LI (3, 1) := "X"; the first two elements keep every occurrence
      *>                                                        = abcd, ef, YX, 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690GRI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2690G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2690G.
       01 WS-C PIC 9.
       01 GS.
          05 SH PIC X VALUE "x".
          05 ST1 PIC X OCCURS DYNAMIC CAPACITY IN CS FROM 1.
       01 GT.
          05 TH PIC X VALUE "x".
          05 TT1 PIC X OCCURS DYNAMIC CAPACITY IN CT FROM 1.
       01 VG.
          05 VH PIC X VALUE "h".
          05 VT OCCURS DYNAMIC CAPACITY IN CV FROM 1.
             10 VD PIC X.
             10 VI PIC X OCCURS DYNAMIC CAPACITY IN CVI FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2690G "NEW" RETURNING O
           MOVE "a" TO ST1 (1)
           INVOKE O "STORESC" USING GS
           MOVE CS TO WS-C
           DISPLAY "SC=" ST1 (1) ST1 (2) ST1 (3) " " WS-C
           MOVE "a" TO TT1 (1)
           INVOKE O "READSC" USING GT
           MOVE CT TO WS-C
           DISPLAY "ST=" TT1 (1) " " WS-C
           MOVE "a" TO VD (1)
           MOVE "b" TO VI (1, 1)
           MOVE "c" TO VI (1, 2)
           MOVE "d" TO VI (1, 3)
           MOVE "e" TO VD (2)
           MOVE "f" TO VI (2, 1)
           INVOKE O "STORENE" USING VG
           MOVE CV TO WS-C
           DISPLAY "NE=" WS-C
           DISPLAY "N1=" VD (1) VI (1, 1) VI (1, 2) VI (1, 3)
           DISPLAY "N2=" VD (2) VI (2, 1)
           DISPLAY "N3=" VD (3) VI (3, 1)
           STOP RUN.
       END PROGRAM PB2690GRI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2690G INHERITS FROM BASE.
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
          05 LT PIC X OCCURS 3.
       PROCEDURE DIVISION USING LS.
       MAIN-P.
           MOVE "q" TO LT (2)
           MOVE "r" TO LT (3).
       END METHOD STORESC.

       METHOD-ID. READSC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR.
          05 LRH PIC X.
          05 LRT PIC X OCCURS 3.
       PROCEDURE DIVISION USING LR.
       MAIN-P.
           MOVE "Z" TO LRH.
       END METHOD READSC.

       METHOD-ID. STORENE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF.
          05 LFH PIC X.
          05 LTF OCCURS 3.
             10 LD PIC X.
             10 LI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LF.
       MAIN-P.
           MOVE "Y" TO LD (3)
           MOVE "X" TO LI (3, 1).
       END METHOD STORENE.

       END OBJECT.
       END CLASS PB2690G.
