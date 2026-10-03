      *> reject-at: 2002 2014
      *> kb/Work PB1412. ISO 1989:2023 8.8.2 rule 8: the boolean SHIFT operators are a COBOL-2023 introduction
      *> (Annex E.2), so a condition whose ONLY boolean operator is a shift - `IF A B-SHIFT-L 1 = B"1000"` - is
      *> refused below 2023 by the introduction gate (COBOLNET0900), as the COMPUTE form already was. The binder's
      *> discriminator now recognises the shift-only expression as the boolean expression it is, so the 2023
      *> positive pb1412_shift_only_boolean_conditions compiles and this program, at 2002 and 2014, is told which
      *> construct needs which edition. (Boolean data items need 2002, so 85 refuses it for other reasons.)
      *>   cite.py --check 8.8.2 "Boolean shift operations shall be performed without regard for the usage of
      *>     the first operand." -> OK 8)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       PROCEDURE DIVISION.
       MAIN.
           IF A B-SHIFT-L 1 = B"1000"
               DISPLAY "Y"
           ELSE
               DISPLAY "N"
           END-IF
           STOP RUN.
