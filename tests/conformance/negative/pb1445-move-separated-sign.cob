      *> reject-at: 85 2002 2014 2023
      *> A SIGN SEPARATED FROM ITS DIGITS IN A PROCEDURE LITERAL SLOT (kb/Work PB1445).
      *> MOVE's sending operand is identifier-1 or literal-1 (ISO/IEC 1989:2023 §14.9.25.2),
      *> never an arithmetic expression, and §8.3.3.3.2 2) puts a sign INSIDE a literal only
      *> as its leftmost character — `- .5` separated by a space is not the literal -.5.
      *> The grammar's `signedNumericLiteral : (PLUS | MINUS)? numericLiteralCore` read it
      *> as -0.50 through GetText(); LiteralScreenPass now refuses it, COBOLNET2155.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445MV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(5)V99.
       PROCEDURE DIVISION.
           MOVE - .5 TO A
           DISPLAY A
           STOP RUN.
