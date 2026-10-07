      *> kb/Work PB895 - the zero-length literal at its introducing edition, COBOL-2014 (VCR row 7.30;
      *> constructs.json zero-length-literal-2014; below 2014 it is COBOLNET0900, negative
      *> pb895-zero-length-literal-below-2014).
      *>   cite.py --check 8.3.3.1 "If the opening and closing delimiters are contiguous, the length of the
      *>     literal is zero, and it is known as a zero-length literal" -> OK §8.3.3.1
      *>   cite.py --check 8.3.3.2.4 "each of which has the bit configuration specified by one occurrence of
      *>     hex-character-sequence-1" -> OK §8.3.3.2.4 4)  (whose first sentence makes X"" zero-length too)
      *>   cite.py --check 14.9.25.4 "If literal-1 is an alphanumeric or national zero-length literal and the
      *>     receiving operand is other than a dynamic-length elementary item, literal-1 is treated as if it
      *>     were the figurative constant SPACE" -> OK §14.9.25.4 2)
      *>   cite.py --check 14.9.11.4 "If an operand is a zero-length data item or a zero-length literal, no
      *>     data is transferred for that operand" -> OK §14.9.11.4 1)
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES: each MOVE of a zero-length literal (both quotation symbols,
      *> and the hexadecimal format) fills X with SPACE, so X=[   ] three times; the DISPLAY of a zero-length
      *> literal between two brackets transfers nothing for it, so D=[].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB895Z.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
           MOVE "" TO X
           DISPLAY "X=[" X "]"
           MOVE "xyz" TO X
           MOVE '' TO X
           DISPLAY "X=[" X "]"
           MOVE "xyz" TO X
           MOVE X"" TO X
           DISPLAY "X=[" X "]"
           DISPLAY "D=[" "" "]"
           STOP RUN.
