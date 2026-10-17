*>--- conformance-case
*> id: pb171-sign-hex-literal
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
      *> 8.3.3.2.1 makes BOTH formats of the alphanumeric literal - quoted and
      *> hexadecimal - "of the class and category alphanumeric", so the hex form
      *> is the same case, not a separate species (DA3's lesson).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171N2.
       PROCEDURE DIVISION.
       MAIN.
           IF X"36" IS POSITIVE
               DISPLAY "T"
           END-IF
           STOP RUN.
