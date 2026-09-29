      *> A SIGN ABUTTING ITS DIGITS IS PART OF THE LITERAL; A SEPARATED SIGN IN AN
      *> EXPRESSION IS A UNARY OPERATOR (kb/Work PB1445).
      *> ISO/IEC 1989:2023 §8.3.3.3.2 2): "A literal shall not contain more than one sign
      *> character. If a sign is used, it shall appear as the leftmost character of the
      *> literal." So VALUE -5 / VALUE +4 and MOVE -3 / ADD -2 carry one-sign literals and
      *> must stay legal after the separated-sign screen (COBOLNET2155) was widened to every
      *> literal slot; `- 5` inside an arithmetic expression (a relation operand, COMPUTE)
      *> is the unary minus of §8.8.1 applied to 5 and must also stay legal.
      *> Expected, by the rules: B = -5, C = +4; MOVE -3 TO A gives -3.00; ADD -2 gives
      *> -5.00; A = - 5 is TRUE (EQ); COMPUTE A = - 5 * 2 gives -10.00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445SL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(5)V99.
       01 B PIC S9 VALUE -5.
       01 C PIC S9 VALUE +4.
       01 E PIC -(5)9.99.
       01 F PIC -9.
       PROCEDURE DIVISION.
           MOVE B TO F
           DISPLAY F
           MOVE C TO F
           DISPLAY F
           MOVE -3 TO A
           MOVE A TO E
           DISPLAY E
           ADD -2 TO A
           MOVE A TO E
           DISPLAY E
           IF A = - 5
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           COMPUTE A = - 5 * 2
           MOVE A TO E
           DISPLAY E
           STOP RUN.
