      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1404 - 8.3.2.2 list item 2 externalizes "data-names, file-names, and record-names of items described
      *>   with the EXTERNAL attribute", and "all instances of a given name that is externalized to the operating
      *>   environment shall identify the same kind of entity or item". KCF1404 is an EXTERNAL file in PB1404N2 and an
      *>   EXTERNAL data item in KC1404B: two kinds under one externalized name. COBOLNET2213.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1404N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KCF1404 ASSIGN TO "pb1404n2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD KCF1404 IS EXTERNAL.
       01 KCF1404-REC PIC X(4).
       PROCEDURE DIVISION.
           CALL "KC1404B"
           STOP RUN.
       END PROGRAM PB1404N2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KC1404B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KCF1404 PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM KC1404B.
