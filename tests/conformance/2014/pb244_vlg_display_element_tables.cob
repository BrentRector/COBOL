      *> kb/Work PB244 shape (b), the RUN-TIME MULTIPLICITY half - DISPLAY and
      *> FUNCTION LENGTH of a VARIABLE-LENGTH GROUP whose OCCURS DEPENDING or
      *> dynamic-capacity table has ELEMENTS that are themselves variable-length
      *> groups (a dynamic-length item inside each occurrence).
      *>
      *> 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the
      *> format in which its contents are displayed is defined by the implementor"
      *> - docs/CONFORMANCE.md DOC-A.1-57: the members' images in declaration
      *> order, each dynamic-length item at its CURRENT content. 8.5.1.12.1: a
      *> group with "at least one dynamic-length elementary item ... as a
      *> subordinate item" is variable-length, and a table element above the item
      *> does not exempt it, so EVERY OCCURRENCE is a variable-length group of
      *> its own, contributing its own current image in occurrence order. How many
      *> occurrences: an OCCURS DEPENDING table uses "only that part of the table
      *> area that is specified by the value of the data item referenced by
      *> data-name-1" (13.18.38.4 GR8), a dynamic-capacity table its current
      *> capacity (8.5.1.9.1). 15.50.4 rule 7 and 15.14.4 rule 6 make LENGTH and
      *> BYTE-LENGTH of the group the sum of its parts, so each EQUALS the
      *> displayed width (A.1 item 57), with no exception.
      *>
      *> EXPECTED VALUES, DERIVED (K = 2 unless a MOVE says otherwise):
      *>   A1 H "H" + D "abc" + P "pp" + (x,1) (yy,2)            = Habcppx1yy2   11
      *>   A2 the second occurrence alone: yy + 2                 = yy2            3
      *>   A3 the nested group SUB (P and the table)              = ppx1yy2
      *>   A4 K=3: the third occurrence (zzz,3) joins             = Habcppx1yy2zzz3 15
      *>   A5 K=1: only the first occurrence is used, BYTE-LENGTH = Habcppx1       8
      *>   B1 a group that holds ONLY the table: h + (ab,1) (c,2) = hab1c2          6
      *>   C1 dynamic-capacity table, capacity 1: h + (q,1)       = hq1             3
      *>   C2 a reference to occurrence 3 creates 2 and 3 (8.5.1.9.3): occurrence 2
      *>      holds a dynamic-length item of length zero (8.6.4: with no VALUE
      *>      clause its length in its initial state is zero) and FX2 "2",
      *>      occurrence 3 (rr,3)
      *>                                                         = hq12rr3         7
      *>      and the capacity register reads 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244ELM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 G1.
          05 H PIC X VALUE "H".
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB.
             10 P PIC XX VALUE "pp".
             10 TE OCCURS 1 TO 3 DEPENDING ON K.
                15 DD PIC X DYNAMIC LENGTH LIMIT 5.
                15 FX PIC X.
       01 G5.
          05 H5 PIC X VALUE "h".
          05 TE5 OCCURS 1 TO 3 DEPENDING ON K.
             10 DD5 PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX5 PIC X.
       01 G2.
          05 H2 PIC X VALUE "h".
          05 TD OCCURS DYNAMIC CAPACITY IN TDCAP FROM 1.
             10 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX2 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO D
           MOVE "x" TO DD OF G1(1)
           MOVE "yy" TO DD OF G1(2)
           MOVE "zzz" TO DD OF G1(3)
           MOVE "1" TO FX OF G1(1)
           MOVE "2" TO FX OF G1(2)
           MOVE "3" TO FX OF G1(3)
           DISPLAY "A1=[" G1 "] " FUNCTION LENGTH(G1)
           DISPLAY "A2=[" TE OF G1(2) "] " FUNCTION LENGTH(TE OF G1(2))
           DISPLAY "A3=[" SUB OF G1 "]"
           MOVE 3 TO K
           DISPLAY "A4=[" G1 "] " FUNCTION LENGTH(G1)
           MOVE 1 TO K
           DISPLAY "A5=[" G1 "] " FUNCTION BYTE-LENGTH(G1)
           MOVE 2 TO K
           MOVE "ab" TO DD5(1)
           MOVE "c" TO DD5(2)
           MOVE "1" TO FX5(1)
           MOVE "2" TO FX5(2)
           DISPLAY "B1=[" G5 "] " FUNCTION LENGTH(G5)
           MOVE "q" TO DD2(1)
           MOVE "1" TO FX2(1)
           DISPLAY "C1=[" G2 "] " FUNCTION LENGTH(G2)
           MOVE "rr" TO DD2(3)
           MOVE "2" TO FX2(2)
           MOVE "3" TO FX2(3)
           DISPLAY "C2=[" G2 "] " FUNCTION LENGTH(G2) " " TDCAP
           STOP RUN.
