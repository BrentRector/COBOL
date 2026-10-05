*> reject-at: 85 2002 2014 2023
      *> kb/Work PB543 - 14.9.48.3 SR5: identifier-8 (TALLYING IN) "shall reference integer data items"; an index
      *> data item is class index (8.5.2.1 Table 2) and 13.18.60.3 SR10 lists no UNSTRING statement among the contexts
      *> that may reference one. The COUNT IN and POINTER operands take the same screen (14.9.48.3 SR5, SR6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB543UNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       01 S PIC X(10) VALUE "A B C".
       01 R PIC X(10).
       PROCEDURE DIVISION.
           UNSTRING S DELIMITED BY ALL SPACE INTO R TALLYING IN IX
           STOP RUN.
