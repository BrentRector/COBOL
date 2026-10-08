      *> kb/Work PB244 - DISPLAY and FUNCTION LENGTH of a VARIABLE-LENGTH GROUP that
      *> lives in a CELL (an EXTERNAL record, an ADDRESS-OF-taken record) and holds a
      *> table whose ELEMENTS are variable-length groups: an OCCURS DEPENDING table,
      *> a dynamic-capacity table, and a dynamic-capacity table nested in another's
      *> element. This is the cell-backed twin of pb244_vlg_display_element_tables:
      *> the same source shapes, the same expected text, because one storage has one
      *> rendering whatever area holds it.
      *>
      *> 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the
      *> format in which its contents are displayed is defined by the implementor"
      *> - docs/CONFORMANCE.md DOC-A.1-57: the members' images in declaration
      *> order, each dynamic-length item at its CURRENT content. 8.5.1.12.1: a
      *> group with "at least one dynamic-length elementary item ... as a
      *> subordinate item" is variable-length, and a table element above the item
      *> does not exempt it, so EVERY OCCURRENCE is a variable-length group of its
      *> own and contributes its own current image in occurrence order. How many
      *> occurrences: an OCCURS DEPENDING table uses "only that part of the table
      *> area that is specified by the value of the data item referenced by
      *> data-name-1" (13.18.38.4 GR8), a dynamic-capacity table its current
      *> capacity (8.5.1.9.1). 15.50.4 rule 7 and 15.14.4 rule 6 make LENGTH and
      *> BYTE-LENGTH of the group the sum of its parts, so each EQUALS the
      *> displayed width (A.1 item 57).
      *>
      *> EXPECTED VALUES, DERIVED (K = 2 unless a MOVE says otherwise):
      *>   A1 H "H" + D "abc" + P "pp" + (x,1) (yy,2)            = Habcppx1yy2   11
      *>   A2 the second occurrence alone: yy + 2                 = yy2            3
      *>   A3 the nested group SUB (P and the table)              = ppx1yy2
      *>   A4 K=3: the third occurrence (zzz,3) joins             = Habcppx1yy2zzz3 15
      *>   A5 K=1: only the first occurrence is used, BYTE-LENGTH = Habcppx1       8
      *>   B1 dynamic-capacity table, capacity 1: h + (q,1)       = hq1             3
      *>   B2 a reference to occurrence 3 creates 2 and 3 (8.5.1.9.3): occurrence 2
      *>      holds a dynamic-length item of length zero (8.6.4: with no VALUE
      *>      clause its length in its initial state is zero) and FX2 "2",
      *>      occurrence 3 (rr,3)                                 = hq12rr3         7
      *>      and the capacity register reads 3.
      *>   C1 D "dd", then occurrence 1 of TD: DD9 "q", the nested table's occurrence
      *>      (a,b), FX9 "1"; then K=2 occurrences of TE: ("ee", f g) ("e2", h)
      *>                                                          = ddqab1eefge2h  13
      *>   C2 K=1: only the first TE occurrence                   = ddqab1eefg     10
      *>   D1 ADDRESS OF the declared group (the cell is the pointer's storage):
      *>      h + (ab,1) (c,2)                                    = hab1c2          6
      *>   D2 KA=1                                                = hab1            4
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244CEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 EXTERNAL.
       01 G1 EXTERNAL.
          05 H PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB.
             10 P PIC XX.
             10 TE OCCURS 1 TO 3 DEPENDING ON K.
                15 DD PIC X DYNAMIC LENGTH LIMIT 5.
                15 FX PIC X.
       01 G2 EXTERNAL.
          05 H2 PIC X.
          05 TD OCCURS DYNAMIC CAPACITY IN TDCAP FROM 1.
             10 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX2 PIC X.
       01 G3 EXTERNAL.
          05 D3 PIC X DYNAMIC LENGTH LIMIT 4.
          05 TD3 OCCURS DYNAMIC CAPACITY IN CAP3 FROM 1.
             10 DD9 PIC X DYNAMIC LENGTH LIMIT 5.
             10 INT3 OCCURS DYNAMIC CAPACITY IN CAPI FROM 1.
                15 DDI PIC X DYNAMIC LENGTH LIMIT 3.
                15 FXI PIC X.
             10 FX9 PIC X.
          05 TE3 OCCURS 1 TO 2 DEPENDING ON K.
             10 EDD PIC X DYNAMIC LENGTH LIMIT 5.
             10 EDT OCCURS DYNAMIC CAPACITY IN CAPE FROM 1.
                15 EFX PIC X.
       01 PA USAGE POINTER.
       01 KA PIC 9 VALUE 2.
       01 GA.
          05 HA PIC X VALUE "h".
          05 TA OCCURS 1 TO 3 DEPENDING ON KA.
             10 DA PIC X DYNAMIC LENGTH LIMIT 5.
             10 FA PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2 TO K
           MOVE "H" TO H
           MOVE "abc" TO D
           MOVE "pp" TO P
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
           MOVE "h" TO H2
           MOVE "q" TO DD2(1)
           MOVE "1" TO FX2(1)
           DISPLAY "B1=[" G2 "] " FUNCTION LENGTH(G2)
           MOVE "rr" TO DD2(3)
           MOVE "2" TO FX2(2)
           MOVE "3" TO FX2(3)
           DISPLAY "B2=[" G2 "] " FUNCTION LENGTH(G2) " " TDCAP
           MOVE 2 TO K
           MOVE "dd" TO D3
           MOVE "q" TO DD9(1)
           MOVE "1" TO FX9(1)
           MOVE "a" TO DDI(1, 1)
           MOVE "b" TO FXI(1, 1)
           MOVE "ee" TO EDD(1)
           MOVE "f" TO EFX(1, 1)
           MOVE "g" TO EFX(1, 2)
           MOVE "e2" TO EDD(2)
           MOVE "h" TO EFX(2, 1)
           DISPLAY "C1=[" G3 "] " FUNCTION LENGTH(G3)
           MOVE 1 TO K
           DISPLAY "C2=[" G3 "] " FUNCTION LENGTH(G3)
           SET PA TO ADDRESS OF GA
           MOVE "ab" TO DA(1)
           MOVE "c" TO DA(2)
           MOVE "1" TO FA(1)
           MOVE "2" TO FA(2)
           DISPLAY "D1=[" GA "] " FUNCTION LENGTH(GA)
           MOVE 1 TO KA
           DISPLAY "D2=[" GA "] " FUNCTION LENGTH(GA)
           STOP RUN.
