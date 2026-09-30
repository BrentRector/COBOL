      *> reject-at: 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR9: "When both literal-4 and literal-5 are specified they shall be the same size except when literal-5 is a figurative
      *> constant" - the two-character constant KAB against the one-character "Q" (13.10.3 SR2). It used to draw
      *> COBOLNET1639 instead of the SR9 violation.
      *> Expected: COBOLNET0846 (the INSPECT size rule) at 2023, the first edition with constant entries.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1127H9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 KA CONSTANT AS "A".
       01 KAB CONSTANT AS "AB".
       PROCEDURE DIVISION.
           INSPECT X CONVERTING KAB TO "Q"
           DISPLAY X
           STOP RUN.
