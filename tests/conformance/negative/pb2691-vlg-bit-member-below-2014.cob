      *> reject-at: 85 2002
      *> kb/Work PB2691 - variable-length groups with USAGE BIT members move
      *> and compare by their 8.5.1.12 correspondence (8.5.1.6.3 bit runs),
      *> but the DYNAMIC LENGTH and OCCURS DYNAMIC CAPACITY clauses are
      *> COBOL-2014 additions (8.5.1.9 / 8.5.1.10), so below 2014 the group
      *> is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2691OLD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G3.
          05 A3 PIC 1 USAGE BIT VALUE B"0".
          05 B3 PIC 1 USAGE BIT VALUE B"1".
          05 D3 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G4.
          05 A4 PIC 11 USAGE BIT VALUE B"10".
          05 D4 PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE G3 TO G4
           IF G3 = G4 DISPLAY "EQ" END-IF
           STOP RUN.
