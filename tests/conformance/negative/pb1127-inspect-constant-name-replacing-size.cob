      *> reject-at: 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR6: "When both literal-1 and literal-3 are specified, they shall be the same size" - a constant-name is a literal here
      *> (13.10.3 SR2), so the one-character KA and the two-character "QQ" violate it. It used to draw COBOLNET1639 ("not
      *> defined") - the wrong rule - because the operand arm never substituted the constant.
      *> Expected: COBOLNET0846 (the INSPECT size rule) at 2023, the first edition with constant entries.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1127H6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 KA CONSTANT AS "A".
       01 KAB CONSTANT AS "AB".
       PROCEDURE DIVISION.
           INSPECT X REPLACING ALL KA BY "QQ"
           DISPLAY X
           STOP RUN.
