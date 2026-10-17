      *> reject-at: 2002 2014 2023
      *> kb/Work PB3287 - ISO 8.4.3.11.3 SR1: identifier-1 "shall reference a data item defined in the file section,
      *> working-storage section, local-storage section, or linkage section".  IX is an index-name, which is not an
      *> identifier (8.4.3.1.2; 13.18.38.3 SR7 closes the list of contexts an index-name may appear in), so ADDRESS OF IX
      *> is refused for the CATEGORY of the name - COBOLNET1637, what `MOVE IX TO WS-N` already draws.  The ADDRESS OF
      *> entry used to say COBOLNET0869 that the operand "names none (an undeclared name ...)", false about a name
      *> INDEXED BY declares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC X OCCURS 3 INDEXED BY IX.
       01  WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET WS-P TO ADDRESS OF IX.
           STOP RUN.
