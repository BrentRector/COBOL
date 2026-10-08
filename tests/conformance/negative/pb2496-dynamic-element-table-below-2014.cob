      *> reject-at: 85 2002
      *> kb/Work PB2496 - a dynamic-capacity table whose elements are
      *> variable-length groups moves and compares element by element (ISO
      *> 14.6.9.2, 14.6.9.3), but the OCCURS DYNAMIC CAPACITY clause and the
      *> DYNAMIC LENGTH clause are COBOL-2014 additions (ISO 8.5.1.9 /
      *> 8.5.1.10), so below 2014 the group is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2496NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2.
          05 TD OCCURS DYNAMIC CAPACITY IN CAP2 FROM 1.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       01 G3.
          05 TD OCCURS DYNAMIC CAPACITY IN CAP3 FROM 1.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ab" TO DD OF G2 (1)
           MOVE G2 TO G3
           IF G2 = G3 DISPLAY "EQ" END-IF
           STOP RUN.
