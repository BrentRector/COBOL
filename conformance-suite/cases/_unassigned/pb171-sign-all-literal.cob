*>--- conformance-case
*> id: pb171-sign-all-literal
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
      *> SR1 a)'s "WITHOUT the ALL phrase" is the load-bearing half: ALL "5"
      *> LOOKS numeric and is not. It carries its literal's own class, which
      *> 8.5.2.1 Table 2 makes alphanumeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171N4.
       PROCEDURE DIVISION.
       MAIN.
           IF ALL "5" IS POSITIVE
               DISPLAY "T"
           END-IF
           STOP RUN.
