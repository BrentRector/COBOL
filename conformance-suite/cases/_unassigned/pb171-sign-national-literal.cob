*>--- conformance-case
*> id: pb171-sign-national-literal
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
      *> Run at 2002+ only, so the CLASS rejection is what is pinned: below 2002
      *> the N"..." literal's own introduction gate (COBOLNET0900) would answer
      *> first and this fixture would assert the wrong rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171N5.
       PROCEDURE DIVISION.
       MAIN.
           IF N"12" IS POSITIVE
               DISPLAY "T"
           END-IF
           STOP RUN.
