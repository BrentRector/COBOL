      *> kb/Work PB1283 - THE LEGAL RENAMES SHAPES NEXT TO THE ENTRY RULES (negatives pb1283-renames-*).
      *>   13.18.45.3 SR11: data-name-3 neither begins before nor ends at or before data-name-2, so G THRU C and A THRU C
      *>   are legal and the alias is the window from data-name-2's first character to data-name-3's last (13.18.45.4
      *>   GR1/GR2): X1 = "ABCDEF", X2 = G..C = "ABCDEF", X3 (no THRU) = B = "CD". SR2: the RENAMES entries immediately follow
      *>   the last entry of THEIR record (X4 follows S, not R). SR5: operands are items inside the record, never the record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 G.
             10 A PIC X(2) VALUE "AB".
             10 B PIC X(2) VALUE "CD".
          05 C PIC X(2) VALUE "EF".
          05 D PIC X(2) VALUE "GH".
       66 X1 RENAMES A THRU C.
       66 X2 RENAMES G THRU C.
       66 X3 RENAMES B.
       01 S.
          05 T PIC X(2) VALUE "ST".
       66 X4 RENAMES T.
       PROCEDURE DIVISION.
           DISPLAY X1 "|" X2 "|" X3 "|" X4
           STOP RUN.
