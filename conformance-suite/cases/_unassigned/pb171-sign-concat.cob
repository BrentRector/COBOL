*>--- conformance-case
*> id: pb171-sign-concat
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
      *> 8.8.3.2 syntax rule 1 makes a concatenation expression of class
      *> alphanumeric, boolean or national - never numeric. (The concatenation
      *> operator is a COBOL-2002 introduction, hence 2002+.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171N6.
       PROCEDURE DIVISION.
       MAIN.
           IF "1" & "2" IS POSITIVE
               DISPLAY "T"
           END-IF
           STOP RUN.
