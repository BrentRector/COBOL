      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1249 -- ISO 13.7.3 SR4 e) (cite.py --check 13.7.3 "It is a condition-name or
      *> index-name associated with a data item that satisfies one of the above conditions"
      *> -> OK 13.7.3 4)). L-IX is the index-name of the table L-TE, which sits in the record
      *> L-T; L-T is not a USING operand, not a redefinition and not BASED, so neither the table
      *> nor its index-name is referenceable: COBOLNET2746 at SET L-IX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1249N3C" USING A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N3C.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A     PIC 9(4).
       01  L-T.
           05  L-TE PIC X OCCURS 3 INDEXED BY L-IX.
       PROCEDURE DIVISION USING L-A.
       SUB-P.
           SET L-IX TO 2.
           EXIT PROGRAM.
       END PROGRAM PB1249N3C.
       END PROGRAM PB1249N3.
