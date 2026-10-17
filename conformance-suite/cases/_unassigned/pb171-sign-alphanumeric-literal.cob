*>--- conformance-case
*> id: pb171-sign-alphanumeric-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.7.3 syntax rule 1: "Arithmetic-expression-1 shall be any
      *> single numeric data item described with a usage other than a standard
      *> floating-point usage, or any form of arithmetic expression." 8.8.1.1
      *> then admits only a numeric identifier, a numeric literal, or ZERO.
      *> The grammar's valueOperand : arithmeticExpression | nonNumericLiteral
      *> puts a BARE NonNumericLiteralContext under comparisonOperand - not a
      *> LiteralContext - so BindOperandExprCore's walk had no arm for it, the
      *> queue drained, and the fallback returned BoundNumLiteral("0").
      *> 
      *> Measured on 9a89fbd1: compiled clean and evaluated 0 > 0, printing the
      *> ELSE branch. A silent wrong answer, not a crash.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171N1.
       PROCEDURE DIVISION.
       MAIN.
           IF "ABC" IS POSITIVE
               DISPLAY "T"
           END-IF
           STOP RUN.
