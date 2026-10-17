      *>--- conformance-case
      *> id: pb3233-inspect-figurative-literal4-size
      *> rules: [SR-14.9.22.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3233 - ISO 14.9.22.3 SR9: literal-4 and literal-5
      *> "shall be the same size except when literal-5 is a figurative
      *> constant". SPACE as literal-4 is "an implicit one character
      *> data item" (SR3), and "QR" is two characters. It compiled
      *> clean and converted each space to Q.
      *> Expected: COBOLNET0846 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG3233C4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X(6) VALUE "A B C ".
       PROCEDURE DIVISION.
           INSPECT Y CONVERTING SPACE TO "QR".
           DISPLAY Y.
           STOP RUN.
