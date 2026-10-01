      *> ISO 8.3.3.3.2 2): "A literal shall not contain more than one sign character. If a sign is
      *> used, it shall appear as the leftmost character of the literal." A literal is ONE
      *> character-string and a space is a separator (8.3.5 1)), so a sign separated from its digits,
      *> or a second sign, is not part of any literal (kb/Work PB1445).
      *> POSITIVE half: a sign ABUTTING its digits is the literal's leftmost character, in a VALUE
      *> clause (literal-1, 13.18.63.2), an 88 VALUE, and a statement literal slot (MOVE / ADD). A
      *> relation operand and a COMPUTE expression are ARITHMETIC EXPRESSIONS, where a separated sign is
      *> the unary operator (8.8.1.2 Table 3), so `IF X = - 3` and `- 5 * - 2` stay legal.
      *> Expected (derived): A = -5 -> -005; B = +7 -> 007 (the - editing symbol prints a space for a
      *> positive value); X = -5 + 2 = -3 -> -003 and the relation is true -> EQ; Y = (-5) * (-2) = 10
      *> -> 010; C = -3 satisfies C-NEG -> C-NEG. Each leg fails if the sign is dropped or doubled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(3) VALUE -5.
       01 B PIC S9(3) VALUE +7.
       01 C PIC S9 VALUE -3.
          88 C-NEG VALUE -3.
       01 X PIC S9(3).
       01 Y PIC S9(3).
       01 E PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE A TO E
           DISPLAY E
           MOVE B TO E
           DISPLAY E
           MOVE -5 TO X
           ADD +2 TO X
           MOVE X TO E
           DISPLAY E
           IF X = - 3
               DISPLAY "EQ"
           END-IF
           COMPUTE Y = - 5 * - 2
           MOVE Y TO E
           DISPLAY E
           IF C-NEG
               DISPLAY "C-NEG"
           END-IF
           STOP RUN.
