      *> kb/Work PB1643. ISO 14.9.11.4 GR1: "Any conversion of data
      *> required between literal-1 or the data item referenced by
      *> identifier-1 and the device is defined by the implementor", so
      *> the character image of a numeric literal is the implementor's
      *> (CONFORMANCE.md DOC-A.1-56): the literal AS WRITTEN, which is
      *> also GnuCOBOL's image outside its vendor configurations that set
      *> pretty-display: no. 12.3.7.4 GR14 a): under DECIMAL-POINT IS
      *> COMMA "the character written in numeric literals to represent
      *> the decimal separator shall be the comma", so the image carries
      *> the comma. 12.3.4 GR1: the nested program inherits that clause
      *> (it may not restate it, 12.3.3 SR1), so it writes and displays
      *> its literals the same way. Before the fix every line below
      *> printed its separator as a period ('1.5').
      *> EXPECTED, by the rule: the literal as written, one line each:
      *>   1,5  -1,5  +0,25  1,50  7 (an integer has no separator)
      *> then the nested program's 9,75.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1643A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
           DISPLAY 1,5
           DISPLAY -1,5
           DISPLAY +0,25
           DISPLAY 1,50
           DISPLAY 7
           CALL "PB1643B"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1643B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
           DISPLAY 9,75
           EXIT PROGRAM.
       END PROGRAM PB1643B.
       END PROGRAM PB1643A.
