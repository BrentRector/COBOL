      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB3233 - ISO 14.9.22.3 SR6: literal-1 and literal-3
      *> "shall be the same size except when literal-3 is a
      *> figurative constant". SPACE as literal-1 is "an implicit one
      *> character data item" (SR3), and "QR" is two characters.
      *> It compiled clean and left X unchanged.
      *> Expected: COBOLNET0846 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG3233R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "A B C ".
       PROCEDURE DIVISION.
           INSPECT X REPLACING ALL SPACE BY "QR".
           DISPLAY X.
           STOP RUN.
