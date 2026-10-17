      *>--- conformance-case
      *> id: l1_log10_format
      *> rules: [FMT-15.56.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.56.2 general format — FUNCTION LOG10 ( argument-1 )
      *> The transcribed format is "<u>FUNCTION</u> <u>LOG10</u> (
      *> argument-1 )": BOTH words underlined (required), ONE argument
      *> inside required parentheses, no bracketed part anywhere - so
      *> there is no optional element to under- or over-implement, and
      *> the whole content of the rule is "this shape, and no other".
      *>
      *> WHAT argument-1 MAY BE is ISO 15.3 type 10, "An arithmetic
      *> expression or a numeric data item shall be specified", so the
      *> format is written here over every shape that position admits:
      *> a numeric literal, a numeric data item, an arithmetic
      *> expression, a nested function-identifier, and the SIGNED
      *> literal forms - integer and floating-point with a sign on the
      *> significand AND on the exponent. The signed forms are the
      *> ones a lexer can get wrong: ISO 8.3.3.3.2 r2 makes the sign
      *> the leftmost CHARACTER of the literal while 8.7.1 requires a
      *> space on both sides of an arithmetic operator, so "(+1000)"
      *> is ONE argument and never an operator plus a literal.
      *>
      *> EVERY EXPECTED VALUE IS EXACT, not an approximation pinned at
      *> some chosen precision. ISO 15.56.4 r1: "The returned value is
      *> the approximation of the logarithm to the base 10 of
      *> argument-1", and log10 of an integral power of ten is that
      *> integer exactly - log10(1000) = 3, log10(100) = 2. Native
      *> arithmetic is in effect (ISO 8.8.1.3: "Native arithmetic is
      *> in effect when the ARITHMETIC IS NATIVE clause is specified
      *> in the OPTIONS paragraph or no ARITHMETIC clause is
      *> specified" - none is written here), so 15.4.1 leaves the
      *> REPRESENTATION to the implementor; the receiver therefore
      *> ROUNDS at scale 9, which pins the exact mathematical value
      *> and not the last bit of one particular approximation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1L10FMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9  PIC S9(3)V9(9) SIGN LEADING SEPARATE.
       01 X-K PIC 9(4) VALUE 1000.
       01 X-H PIC 9(4) VALUE 100.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R9 ROUNDED = FUNCTION LOG10(1000)
           DISPLAY "LIT=" R9
           COMPUTE R9 ROUNDED = FUNCTION LOG10(X-K)
           DISPLAY "ITEM=" R9
           COMPUTE R9 ROUNDED = FUNCTION LOG10(X-K / 10)
           DISPLAY "EXPR=" R9
           COMPUTE R9 ROUNDED = FUNCTION LOG10(FUNCTION ABS(X-H))
           DISPLAY "NEST=" R9
           COMPUTE R9 ROUNDED = FUNCTION LOG10(+1000)
           DISPLAY "SLIT=" R9
           COMPUTE R9 ROUNDED = FUNCTION LOG10(+1.0E+3)
           DISPLAY "SFLOAT=" R9
           STOP RUN.
