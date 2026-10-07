      *> kb/Work PB1545 - the hexadecimal alphanumeric literal X"..." at its introducing edition, COBOL-2002
      *> (VCR row 7.29; constructs.json hex-alphanumeric-literal-2002; below 2002 it is COBOLNET0900, negative
      *> pb1545-hex-alphanumeric-literal-below-2002).
      *>   cite.py --check 8.3.3.2.2 "Format 2 (hexadecimal-alphanumeric)" -> OK §8.3.3.2.2
      *>   cite.py --check 8.3.3.2.3 "Each hex-character-sequence-1 shall consist of the number of hexadecimal
      *>     digits" -> OK §8.3.3.2.3 6)  (two per character here, DOC-A.1-9)
      *>   cite.py --check 8.3.3.2.4 "each of which has the bit configuration specified by one occurrence of
      *>     hex-character-sequence-1" -> OK §8.3.3.2.4 4)
      *> The literal is written in every position the format allows a literal: a VALUE clause of an
      *> alphanumeric item (no data-category gate sees it there - the gate is on the TOKEN), a DISPLAY operand,
      *> a relation operand, and an UNSTRING delimiter (the hexadecimal neighbour moved here from the COBOL-85
      *> golden 85/pb1181_string_unstring_screen_controls). Both quotation symbols and both digit cases.
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES (each digit pair is one character whose code is the pair's
      *> value, DOC-A.1-9; 41 42 43 44 are A B C D, 2C is the comma):
      *>   V=[AB]       the VALUE X"4142"
      *>   D=[CD]       DISPLAY X'4344'
      *>   EQ           W = x"4a4B" after MOVE "JK" TO W
      *>   U=[AB][CD]   UNSTRING "AB,CD" DELIMITED BY X"2C"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1545H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC XX VALUE X"4142".
       01 W PIC XX.
       01 S PIC X(5) VALUE "AB,CD".
       01 A PIC XX.
       01 B PIC XX.
       PROCEDURE DIVISION.
           DISPLAY "V=[" V "]"
           DISPLAY "D=[" X'4344' "]"
           MOVE "JK" TO W
           IF W = x"4a4B"
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           UNSTRING S DELIMITED BY X"2C" INTO A B
           DISPLAY "U=[" A "][" B "]"
           STOP RUN.
