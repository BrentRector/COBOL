      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1261 - ISO 13.18.38.3 SR18: "If the OCCURS clause is specified in an entry
      *> subordinate to one containing the GLOBAL clause, data-name-1, if specified, shall be a global
      *> name". N is a local name, so a contained program could see T but not the count that sizes it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1261N18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 G GLOBAL.
           05 T PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
