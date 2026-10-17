*>--- conformance-case
*> id: pb263_call_literal_arg_exact_value
*> rules: [GR-8.3.3.3.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB263 + PB264 — A NUMERIC LITERAL ARGUMENT CROSSES A CALL WITH ITS EXACT VALUE, in either
      *> notation and in either value-passing mode.
      *>
      *> ISO 8.3.3.3.2 rule 4: "The value of a fixed-point numeric literal is the algebraic quantity
      *> represented by the characters in the fixed-point numeric literal."
      *> ISO 8.3.3.3.3 rule 5: "The value of a floating-point numeric literal is the algebraic product of the
      *> value of its significand and the quantity derived by raising ten to the power of the exponent."
      *> So 1.2345678901234567890123E+3 and 1234.5678901234567890123 ARE THE SAME VALUE, and the four rows
      *> below (two notations x BY CONTENT / BY VALUE) shall all display it.
      *>
      *> ISO 14.2.3 GR9 gives the BY CONTENT argument its allocated record; GR10 gives BY VALUE one "of the
      *> same description as the formal parameter", filled as if by "a COMPUTE statement without the ROUNDED
      *> phrase". The formal PIC S9(5)V9(20) holds 4 integer and 19 fraction digits exactly, so no rule here
      *> rounds or truncates and every row's expected value is the literal itself.
      *>
      *> WHAT USED TO HAPPEN. The CALL argument lane rendered a literal through EmitText.UnscaledLit, whose
      *> contract is a scaled integer but which returned the source text as a binary64 double for the E-form:
      *> rows 2 and 4 were a RAW ROSLYN CS1503 ("cannot convert from 'double' to 'long'"), conforming source
      *> rejected with no COBOL diagnostic at all. Row 3 hard-wired a long cell and an unchecked (long) cast,
      *> so the 23-digit literal crossed as its MODULAR LOW-ORDER BITS (0.0000048071159228778590190), and the
      *> signed E-form of row 6 landed at the receiver-less working scale and lost its fraction after 6 digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB263LX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-DUMMY PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> The same value, written four ways.
           CALL "PB263CX" AS NESTED USING
               BY CONTENT 1234.5678901234567890123
           CALL "PB263CX" AS NESTED USING
               BY CONTENT 1.2345678901234567890123E+3
           CALL "PB263VX" AS NESTED USING
               BY VALUE 1234.5678901234567890123
           CALL "PB263VX" AS NESTED USING
               BY VALUE 1.2345678901234567890123E+3
      *> A narrow float literal (the long-carrier arm): 1.5E+3 is exactly 1500.
           CALL "PB263CX" AS NESTED USING BY CONTENT 1.5E+3
      *> A SIGNED float literal, both modes. Inside an arithmetic expression the leading sign is taken by the
      *> unary rule, so the BY VALUE spelling arrives negated rather than as one signed literal; both shall
      *> still deliver -0.00001234 exactly.
           CALL "PB263CX" AS NESTED USING BY CONTENT -1.234E-5
           CALL "PB263VX" AS NESTED USING BY VALUE -1.234E-5
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB263CX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC PIC S9(5)V9(20).
       PROCEDURE DIVISION USING LC.
       C1.
           DISPLAY "C=" LC
           GOBACK.
       END PROGRAM PB263CX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB263VX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC S9(5)V9(20).
       PROCEDURE DIVISION USING BY VALUE LV.
       V1.
           DISPLAY "V=" LV
           GOBACK.
       END PROGRAM PB263VX.
       END PROGRAM PB263LX.
