      *> reject-at: 2002 2014 2023
      *> kb/Work PB1427 (wave-69 finisher) - a constant entry whose AS
      *> operand is the predefined NULL. 13.10.2 prints the operand as
      *> literal-1 or arithmetic-expression-1, and 13.10.3 SR7 makes
      *> every operand of arithmetic-expression-1 a literal; NULL is
      *> neither - it is an IDENTIFIER (8.4.3.1.2 Format 8), and
      *> 8.4.3.10.3 SR1 lists no constant entry among the places it may
      *> be written ("it may be used only as a sending operand in an
      *> INITIALIZE or a SET statement; ..."). COBOLNET2576.
      *> MEASURED BEFORE: the compiler THREW (NullReferenceException in
      *> DataBinder.BindConstantStringLiteral) - the decoder took the
      *> last nonNumericLiteral arm to be a boolean literal.
      *> Below 2002 the constant entry itself is refused (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69YKNUL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS NULL.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X
           STOP RUN.
