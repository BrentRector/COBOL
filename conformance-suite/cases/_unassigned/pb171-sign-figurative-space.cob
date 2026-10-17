*>--- conformance-case
*> id: pb171-sign-figurative-space
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
      *> 8.3.3.6.3 syntax rule 1 a): "If the literal is restricted to a numeric
      *> literal, the only figurative constant permitted is ZERO (ZEROS, ZEROES)
      *> without the ALL phrase." 8.3.3.6.4 GR5 gives SPACE a CHARACTER reading
      *> only - never a numeric one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171N3.
       PROCEDURE DIVISION.
       MAIN.
           IF SPACE IS NEGATIVE
               DISPLAY "T"
           END-IF
           STOP RUN.
