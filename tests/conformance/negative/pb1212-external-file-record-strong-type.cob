      *> reject-at: 2002 2014 2023
      *> kb/Work PB1212 - ISO 13.18.22.3 SR5: "When a record description is an external item, any associated type declaration that is strongly typed shall also be external."
      *> cite.py: OK  13.18.22.3 5)  (Syntax rules).  The NOTE under it: "This covers the situation where a file is declared as external and the associated record
      *> descriptions have TYPE clauses."  R is external by 13.18.22.4 GR4 b) (the record of an EXTERNAL file) though it writes no EXTERNAL clause, and ST is a strong,
      *> non-external type declaration.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1212EXTFILESTRONG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB1212EXTFILESTRONG.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F IS EXTERNAL.
       01  R TYPE ST.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05  A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
