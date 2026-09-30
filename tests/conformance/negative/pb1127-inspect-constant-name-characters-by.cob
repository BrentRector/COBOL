      *> reject-at: 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR7: "When the CHARACTERS phrase is specified, literal-3 shall be one character in length" - the two-character constant
      *> KAB is literal-3 here (13.10.3 SR2). It used to draw COBOLNET1639 instead of the SR7 violation.
      *> Expected: COBOLNET0846 (the INSPECT size rule) at 2023, the first edition with constant entries.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1127H7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 KA CONSTANT AS "A".
       01 KAB CONSTANT AS "AB".
       PROCEDURE DIVISION.
           INSPECT X REPLACING CHARACTERS BY KAB
           DISPLAY X
           STOP RUN.
