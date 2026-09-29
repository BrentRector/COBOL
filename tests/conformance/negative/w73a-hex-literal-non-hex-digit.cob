      *> reject-at: 2002 2014 2023
      *> kb/Work PB1394 - ISO 8.3.3.2.3 SR5: "Hex-character-sequence-1
      *>  shall be composed of hexadecimal digits". 8.3.5 rule 5 makes
      *>  X" an opening delimiter, so X"GG" can only be a malformed
      *>  hexadecimal literal. Before the fix the lexer's hex body
      *>  refused G, the literal re-lexed as the data-name X followed by
      *>  the literal "GG", and beside the item X below the program
      *>  compiled and printed ZZZGG. COBOLNET2630 at every edition with
      *>  the hexadecimal format (2002+).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73AHX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "ZZZ".
       PROCEDURE DIVISION.
           DISPLAY X"GG".
           STOP RUN.
