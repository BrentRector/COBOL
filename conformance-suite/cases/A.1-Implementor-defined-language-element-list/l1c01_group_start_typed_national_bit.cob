      *>--- conformance-case
      *> id: l1c01_group_start_typed_national_bit
      *> rules: [DOC-A.1-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.1.6.5 — strongly-typed, national-first and bit-item
      *> groups start at their first item (A.1 item 6)
      *> THE RULE (implementor-defined, A.1 item 6; documented choice:
      *> docs/CONFORMANCE.md row DOC-A.1-6: every group, alphanumeric
      *> or strongly-typed, is coincident with its first item and no
      *> slack is inserted; the only implicit filler is the filler bits
      *> that reach the next byte after a bit item):
      *>   cite.py --check 8.5.1.6.5 "The alignment of the start of a
      *>     strongly-typed group item relative to the first item within
      *>     that group is defined by the implementor" -> OK §8.5.1.6.5
      *>   cite.py --check 8.5.1.6.1 "The alignment of the start of an
      *>     alphanumeric group item relative to the first item within
      *>     that group is defined by the implementor" -> OK §8.5.1.6.1
      *>   cite.py --check 8.5.1.6.1 "Alignment of alphanumeric groups
      *>     and of data items of usage display is at a natural
      *>     alphanumeric character boundary and is coincident with a
      *>     byte boundary" -> OK §8.5.1.6.1 (so a PIC X after a bit
      *>     item starts a new byte)
      *> 2002 dir: TYPEDEF STRONG, national items, USAGE BIT and
      *> BYTE-LENGTH are 2002. A national character position is 2 bytes
      *> (UTF-16, docs/CONFORMANCE.md item 33).
      *> DERIVATION of every .out line:
      *>   SG strongly-typed (N, X): 2 + 1, no slack      -> SG=3
      *>   K  alphanumeric   (N, X): 2 + 1                -> K=3
      *>   BG (1(3) BIT, X): B1 in byte 1, the display
      *>      item B2 on the next byte boundary            -> BG=2
      *>   DG strongly-typed (X(2), X(3)): 2 + 3          -> DG=5
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF STRONG.
          05 S1 PIC N.
          05 S2 PIC X.
       01 DT TYPEDEF STRONG.
          05 T1 PIC X(2).
          05 T2 PIC X(3).
       01 SG TYPE ST.
       01 DG TYPE DT.
       01 K.
          05 K1 PIC N.
          05 K2 PIC X.
       01 BG.
          05 B1 PIC 1(3) USAGE BIT.
          05 B2 PIC X.
       01 N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION BYTE-LENGTH(SG) TO N
           DISPLAY "SG=" N
           MOVE FUNCTION BYTE-LENGTH(K) TO N
           DISPLAY "K=" N
           MOVE FUNCTION BYTE-LENGTH(BG) TO N
           DISPLAY "BG=" N
           MOVE FUNCTION BYTE-LENGTH(DG) TO N
           DISPLAY "DG=" N
           STOP RUN.
