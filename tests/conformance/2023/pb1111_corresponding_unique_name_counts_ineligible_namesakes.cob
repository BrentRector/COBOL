      *> PB1111 - ISO 14.7.6 rule 6: a pair corresponds only if "the name
      *>   of each data item that satisfies the above conditions is unique
      *>   after application of the implied qualifiers". Uniqueness is of
      *>   the REFERENCE, so a namesake that rule 4 excludes (OCCURS,
      *>   REDEFINES) or that sits under a FILLER (no qualifier) still
      *>   defeats it.
      *> cite.py --check 14.7.6 "is unique after application of the
      *>   implied qualifiers" -> OK  14.7.6 6)
      *> Derivation (G1 -> G2, MOVE CORRESPONDING; G2 starts 0000):
      *>   DU   G1 has DU (elementary) and DU OCCURS 2 -> two items named
      *>        DU OF G1: ambiguous, not moved.
      *>   FF   G1 has FF and, under a FILLER REDEFINES FF, another FF
      *>        (a FILLER contributes no qualifier): ambiguous, not moved.
      *>   BB CC unique on both sides: BB=5 and CC=4 move.
      *>   G2 is BB DU FF CC = 5 0 0 4 -> 5004.
      *> The same shape on the receiving side (G2 twins) and under ADD
      *>   CORRESPONDING follows: ADD sums only the unique names BB, CC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1111.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 DU PIC 9 VALUE 1.
          05 DU PIC 9 OCCURS 2 VALUE 2.
          05 FF PIC 9 VALUE 3.
          05 FILLER REDEFINES FF.
             10 FF PIC 9.
          05 CC PIC 9 VALUE 4.
          05 BB PIC 9 VALUE 5.
       01 G2.
          05 BB PIC 9 VALUE 0.
          05 DU PIC 9 VALUE 0.
          05 FF PIC 9 VALUE 0.
          05 CC PIC 9 VALUE 0.
       01 G3.
          05 BB PIC 9 VALUE 1.
          05 CC PIC 9 VALUE 1.
          05 CC PIC 9 OCCURS 2 VALUE 1.
       PROCEDURE DIVISION.
           MOVE CORRESPONDING G1 TO G2
           DISPLAY G2
           ADD CORRESPONDING G1 TO G3
           DISPLAY BB OF G3
           STOP RUN.
