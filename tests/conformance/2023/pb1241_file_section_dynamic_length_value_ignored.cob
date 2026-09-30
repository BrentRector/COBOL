      *> PB1241 - ISO 13.4.4 GR1: "A data-item format or table format
      *>   VALUE clause specified in the file section is ignored except in
      *>   the execution of the INITIALIZE statement", and "The initial
      *>   length of a dynamic-length elementary item is zero."
      *> cite.py --check 13.4.4 "A data-item format or table format VALUE
      *>   clause specified in the file section is ignored" -> OK
      *>   13.4.4 1)
      *> Derivation: before any INITIALIZE both dynamic-length items in the
      *>   file record have length zero, VALUE or not: LEN-D=0 LEN-E=0.
      *>   INITIALIZE R ALL TO VALUE then applies the clause: D takes its
      *>   VALUE "HELLO" (length 5); E has no VALUE and stays empty (0).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1241.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO "pb1241.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD OUTF.
       01 R.
          05 DL PIC X DYNAMIC LENGTH VALUE "HELLO".
          05 EL PIC X DYNAMIC LENGTH.
       WORKING-STORAGE SECTION.
       01 LD PIC 9.
       01 LE PIC 9.
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(DL) TO LD
           MOVE FUNCTION LENGTH(EL) TO LE
           DISPLAY "LEN-D=" LD " LEN-E=" LE
           INITIALIZE R ALL TO VALUE
           MOVE FUNCTION LENGTH(DL) TO LD
           MOVE FUNCTION LENGTH(EL) TO LE
           DISPLAY "LEN-D=" LD " D=[" DL "] LEN-E=" LE
           STOP RUN.
