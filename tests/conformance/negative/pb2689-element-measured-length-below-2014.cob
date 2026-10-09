      *> reject-at: 85 2002
      *> kb/Work PB2689 - corresponding tables whose elements hold a
      *> dynamic-capacity table opposite a fixed one match (ISO 8.5.1.12.3),
      *> but the OCCURS DYNAMIC CAPACITY clause is a COBOL-2014 addition
      *> (ISO 8.5.1.9), so below 2014 the group is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2689NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 E1 OCCURS DYNAMIC CAPACITY IN C1 FROM 1.
             10 IA PIC X OCCURS DYNAMIC CAPACITY IN CA FROM 1.
             10 KA PIC X.
          05 Z1 PIC X(2) VALUE "z1".
       01 G2.
          05 E2 OCCURS DYNAMIC CAPACITY IN C2 FROM 1.
             10 IB PIC X OCCURS 2.
             10 KB PIC X.
          05 Z2 PIC X(2) VALUE "z2".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "p" TO IA (1, 1)
           MOVE G1 TO G2
           IF G1 = G2 DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           STOP RUN.
