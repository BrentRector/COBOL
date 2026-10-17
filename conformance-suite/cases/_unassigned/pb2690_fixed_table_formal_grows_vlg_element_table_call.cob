*>--- conformance-case
*> id: pb2690_fixed_table_formal_grows_vlg_element_table_call
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - A FIXED-LENGTH GROUP FORMAL WHOSE TABLE OF FIXED-LENGTH
      *> ELEMENTS IS STORED PAST THE CAPACITY OF THE ARGUMENT'S DYNAMIC-CAPACITY
      *> TABLE OF VARIABLE-LENGTH ELEMENTS, ACROSS A FORMAT-2 CALL.
      *>
      *> 14.2.3 GR8: the BY REFERENCE formal "operates as if the formal parameter
      *> occupies the same storage area as the argument", so a store to the
      *> formal's third element is a receiving reference to the argument's
      *> dynamic-capacity table with a subscript that exceeds its current
      *> capacity, and 8.5.1.9.3 creates the element and raises the capacity "to
      *> the value given by the subscript". The elements correspond (8.5.1.12.3
      *> sentence 2: both are 1 + 2 characters, the dynamic-capacity VI being
      *> "considered to be the same length" as FI) and 14.6.9.2 moves
      *> correspondingly numbered elements by the rules of the MOVE statement.
      *>
      *> EXPECTED VALUES, DERIVED (VG starts as h, [a b c d] [e f], capacity 2):
      *>   S1  the fixed formal sees VG's two elements, each IL cut to two
      *>       occurrences ("d" superfluous) and the third element missing,
      *>       space filled                                       = habcef + four spaces
      *>   R1  the callee stores DF (3) := "Y" and FI (3, 1) := "X": VG has three
      *>       elements, the third IL's first occurrence is "X"; the first two
      *>       keep every occurrence, "d" included                = Y, X, 3
      *>   S2  a callee that reads the third element and stores nothing past the
      *>       capacity (it changes DF (1) only) leaves the capacity as it was
      *>                                                          = Q, 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690NES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 VG.
          05 VH PIC X VALUE "h".
          05 VT OCCURS DYNAMIC CAPACITY IN CV FROM 1.
             10 VD PIC X.
             10 VI PIC X OCCURS DYNAMIC CAPACITY IN CVI FROM 1.
       01 VH2.
          05 WH PIC X VALUE "h".
          05 WT OCCURS DYNAMIC CAPACITY IN CW FROM 1.
             10 WD PIC X.
             10 WI PIC X OCCURS DYNAMIC CAPACITY IN CWI FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "a" TO VD (1)
           MOVE "b" TO VI (1, 1)
           MOVE "c" TO VI (1, 2)
           MOVE "d" TO VI (1, 3)
           MOVE "e" TO VD (2)
           MOVE "f" TO VI (2, 1)
           CALL "PB2690NGR" AS NESTED USING VG
           MOVE CV TO WS-C
           DISPLAY "R1=" WS-C
           DISPLAY "E1=" VD (1) VI (1, 1) VI (1, 2) VI (1, 3)
           DISPLAY "E2=" VD (2) VI (2, 1)
           DISPLAY "E3=" VD (3) VI (3, 1)
           MOVE "a" TO WD (1)
           MOVE "b" TO WI (1, 1)
           MOVE "e" TO WD (2)
           MOVE "f" TO WI (2, 1)
           CALL "PB2690NRD" AS NESTED USING VH2
           MOVE CW TO WS-C
           DISPLAY "S2=" WS-C
           DISPLAY "D1=" WD (1)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690NGR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FA.
          05 FH PIC X.
          05 TF OCCURS 3.
             10 DF PIC X.
             10 FI PIC X OCCURS 2.
       PROCEDURE DIVISION USING FA.
       MAIN.
           DISPLAY "S1=[" FA "]"
           MOVE "Y" TO DF (3)
           MOVE "X" TO FI (3, 1)
           GOBACK.
       END PROGRAM PB2690NGR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690NRD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FB.
          05 FBH PIC X.
          05 TB OCCURS 3.
             10 DB PIC X.
             10 FIB PIC X OCCURS 2.
       PROCEDURE DIVISION USING FB.
       MAIN.
           MOVE "Q" TO DB (1)
           GOBACK.
       END PROGRAM PB2690NRD.
       END PROGRAM PB2690NES.
