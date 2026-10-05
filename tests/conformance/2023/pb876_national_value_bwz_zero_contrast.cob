      *> kb/Work PB876 - GR8's contrasts on a USAGE NATIONAL numeric-edited item (13.18.63.4 GR8: the BLANK WHEN
      *> ZERO clause has no effect on initialization when the VALUE literal is alphanumeric or national). The same
      *> picture initialized by a national literal N"0000" shows 0000, while VALUE ZERO and VALUE 0 are the numeric
      *> zero (13.18.63.3 SR6, a numeric literal for a numeric-edited item, COBOL-2023 - Annex E.3.3 item 43) and
      *> BLANK WHEN ZERO applies, giving spaces; VALUE 12 shows the mask is live.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB876ZER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL VALUE N"0000".
       01 E2 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL VALUE ZERO.
       01 E3 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL VALUE 0.
       01 E4 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL VALUE 12.
       PROCEDURE DIVISION.
           DISPLAY "E1=[" E1 "] E2=[" E2 "] E3=[" E3 "] E4=[" E4 "]".
           STOP RUN.
