*>--- conformance-case
*> id: pb238-by-value-nonnumeric-literal
*> rules: [SR-14.9.4.3-23]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.4.3 SR23 (kb/Work PB238; COBOLNET1762's witness): "If literal-2 or its corresponding formal
*> parameter is specified with the BY VALUE phrase, literal-2 shall be a numeric literal." The verdict was
*> already right and it named no rule: `callByValue : BY VALUE arithmeticExpression` and the expression
*> spine bottoms out at `numericLiteral | ZERO_ARITH | functionCall | dataReference | ( ... )`, so this
*> source died at a raw ANTLR "no viable alternative" with no COBOLNET code and no citation. The grammar
*> now admits the `literal` alternative the printed 14.9.4.2 Format-2 figure prints as literal-2, purely so
*> the compiler delivers SR23's verdict itself. Rejected at 2002+ because BY VALUE is a COBOL-2002
*> introduction: below it the CALL BY VALUE phrase draws its own edition gate (COBOLNET0900) instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238N1.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB238N1S" AS NESTED USING BY VALUE "ABC"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238N1S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC S9(4).
       PROCEDURE DIVISION USING BY VALUE LX.
       M1.
           GOBACK.
       END PROGRAM PB238N1S.
       END PROGRAM PB238N1.
