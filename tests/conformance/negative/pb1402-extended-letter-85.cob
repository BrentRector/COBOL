      *> reject-at: 85
      *> ISO 8.3.2.1 / 8.1.3.2 GR4 (kb/Work PB1402): extended letters are a COBOL 2002 introduction - the
      *> COBOL-85 word is basic letters, digits and hyphen - so CAFE-acute is the named edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1402N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CAFÉ PIC X(5) VALUE "CREME".
       PROCEDURE DIVISION.
           DISPLAY CAFÉ.
           STOP RUN.
