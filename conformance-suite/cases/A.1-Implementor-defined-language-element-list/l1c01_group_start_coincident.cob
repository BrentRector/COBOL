      *>--- conformance-case
      *> id: l1c01_group_start_coincident
      *> rules: [DOC-A.1-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.1.6.1 — an alphanumeric group starts at its first
      *> item, with no slack bytes (A.1 item 6)
      *> THE RULE (implementor-defined, A.1 item 6; documented choice:
      *> docs/CONFORMANCE.md row DOC-A.1-6, "Always coincident", no
      *> slack bytes before or between items):
      *>   cite.py --check 8.5.1.6.1 "The alignment of the start of an
      *>     alphanumeric group item relative to the first item within
      *>     that group is defined by the implementor" -> OK §8.5.1.6.1
      *>   cite.py --check 8.5.1.6.1 "Alignment of alphanumeric groups
      *>     and of data items of usage display is at a natural
      *>     alphanumeric character boundary and is coincident with a
      *>     byte boundary" -> OK §8.5.1.6.1
      *> The documented layout: 05 G1 PIC 9(4) COMP then 05 G2 PIC X "is
      *> a 3-byte group whose first two bytes are G1". T REDEFINES G, so
      *> T(n:1) is byte n of G.
      *> DERIVATION of every .out line:
      *>   LENGTH(G) = 2 (G1) + 1 (G2), no slack         -> LEN-G=03
      *>   G1 := 0 is binary zero in either byte order, and
      *>   G1 is bytes 1-2 of G -> T(1:2) = LOW-VALUES   -> G1-AT-1
      *>   G2 := "Z" directly after G1 -> T(3:1) = "Z"   -> G2-AT-3
      *>   D (X(2), 9(3)) <- "XY123": D1 is bytes 1-2, D2 bytes 3-5
      *>                         -> D1=XY D2=123 LEN-D=05
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G1 PIC 9(4) COMP.
          05 G2 PIC X.
       01 T REDEFINES G PIC X(3).
       01 D.
          05 D1 PIC X(2).
          05 D2 PIC 9(3).
       01 N PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(G) TO N
           DISPLAY "LEN-G=" N
           MOVE 0 TO G1
           MOVE "Z" TO G2
           IF T(1:2) = LOW-VALUES
               DISPLAY "G1-AT-1"
           ELSE
               DISPLAY "G1-NOT-AT-1"
           END-IF
           IF T(3:1) = "Z"
               DISPLAY "G2-AT-3"
           ELSE
               DISPLAY "G2-NOT-AT-3"
           END-IF
           MOVE "XY123" TO D
           MOVE FUNCTION LENGTH(D) TO N
           DISPLAY "D1=" D1 " D2=" D2 " LEN-D=" N
           STOP RUN.
