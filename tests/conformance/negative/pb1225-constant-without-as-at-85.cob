      *> reject-at: 85
      *> kb/Work PB1225 - the constant entry (13.10) is a COBOL-2002 introduction; spelled without its optional
      *> word AS (13.10.2 does not underline it, 5.2.3) it is the same entry, so below 2002 it takes the same
      *> edition gate (COBOLNET0900, construct constant-entry-2002) and never a parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1225NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT 8.
       PROCEDURE DIVISION.
           DISPLAY K
           STOP RUN.
