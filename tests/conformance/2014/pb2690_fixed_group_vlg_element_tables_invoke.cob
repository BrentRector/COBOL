      *> kb/Work PB2690 - A FIXED-LENGTH GROUP OPPOSITE A DYNAMIC-CAPACITY TABLE
      *> OF VARIABLE-LENGTH ELEMENTS, ACROSS AN INVOKE BOUNDARY, IN BOTH
      *> DIRECTIONS. The INVOKE twin of pb2690_fixed_group_vlg_element_tables_call.
      *>
      *> 14.8.2.2 2): "If either the formal parameter or the argument is a
      *> variable length group, the formal parameter and the argument shall be
      *> compatible, as described in 8.5.1.12", and 8.5.1.12.1 admits a
      *> fixed-length group on the other side. The elements of FA's fixed table
      *> and of the formal's dynamic-capacity table correspond (8.5.1.12.3
      *> sentence 2) and the fixed tables are treated as dynamic-capacity tables
      *> of their fixed occurrence counts (sentence 3). This used to stop the run
      *> with a not-implemented naming the flat span adapter, because the fixed
      *> group's element layout did not travel with its image.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   S1  the method sees FA whole, capacity 2               = habcdef, 2
      *>   R1  14.2.3 GR8: IL (2, 1) := "X" and DL (2) := "Y" are FA's
      *>       second element's first FI occurrence and its DF    = habcYXf
      *>   S2  the fixed formal sees VG's first two elements, each IL cut to
      *>       two occurrences (14.6.9.2: superfluous elements are not moved,
      *>       a missing occurrence is space filled)              = habcef_
      *>   R2  GR8 again: LD (1) := "Q" and LI (1, 1) := "R" are VG's first
      *>       element and its first IL occurrence; the others keep their
      *>       values and the capacity stays 3                    = hQRcdefghi, 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690VI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2690C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2690C.
       01 WS-C PIC 9.
       01 FA.
          05 FH PIC X VALUE "h".
          05 TF OCCURS 2.
             10 DF PIC X.
             10 FI PIC X OCCURS 2.
       01 VG.
          05 VH PIC X VALUE "h".
          05 VT OCCURS DYNAMIC CAPACITY IN CV FROM 1.
             10 VD PIC X.
             10 VI PIC X OCCURS DYNAMIC CAPACITY IN CVI FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "a" TO DF (1)
           MOVE "b" TO FI (1, 1)
           MOVE "c" TO FI (1, 2)
           MOVE "d" TO DF (2)
           MOVE "e" TO FI (2, 1)
           MOVE "f" TO FI (2, 2)
           INVOKE PB2690C "NEW" RETURNING O
           INVOKE O "TOVAR" USING FA
           DISPLAY "R1=[" FA "]"
           MOVE "a" TO VD (1)
           MOVE "b" TO VI (1, 1)
           MOVE "c" TO VI (1, 2)
           MOVE "d" TO VI (1, 3)
           MOVE "e" TO VD (2)
           MOVE "f" TO VI (2, 1)
           MOVE "g" TO VD (3)
           MOVE "h" TO VI (3, 1)
           MOVE "i" TO VI (3, 2)
           INVOKE O "TOFIXED" USING VG
           MOVE CV TO WS-C
           DISPLAY "R2=" WS-C " [" VG "]"
           STOP RUN.
       END PROGRAM PB2690VI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2690C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. TOVAR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LH PIC X.
          05 TL OCCURS DYNAMIC CAPACITY IN CL FROM 1.
             10 DL PIC X.
             10 IL PIC X OCCURS DYNAMIC CAPACITY IN CIL FROM 1.
       PROCEDURE DIVISION USING L.
       MAIN-P.
           DISPLAY "S1=" CL " [" L "]"
           MOVE "X" TO IL (2, 1)
           MOVE "Y" TO DL (2).
       END METHOD TOVAR.

       METHOD-ID. TOFIXED.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF.
          05 LH PIC X.
          05 LT OCCURS 2.
             10 LD PIC X.
             10 LI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LF.
       MAIN-P.
           DISPLAY "S2=[" LF "]"
           MOVE "Q" TO LD (1)
           MOVE "R" TO LI (1, 1).
       END METHOD TOFIXED.

       END OBJECT.
       END CLASS PB2690C.
