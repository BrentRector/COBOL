*>--- conformance-case
*> id: pb99-floating-literal-form
*> rules: [SR-8.3.3.3.3-2, SR-8.3.3.3.3-3, SR-8.3.3.3.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.3.3.3.3 SR2 (the significand shall be from 1 to 36 digits), SR3 (the exponent shall have a
      *> maximum of four digits), SR4 (a zero significand requires a zero exponent and no negative sign in either
      *> part): each entry below violates one rule and is COBOLNET1661 (kb/Work PB99 - the five-digit exponent used to
      *> reach Roslyn as an overflowing C# double literal, CS0594; the others compiled silently). The form is checked
      *> at the ONE numeric-literal normalizer, so the VALUE clause, a level-88 and a statement all report it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99NF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE FLOAT-LONG VALUE 1.234567890123456789012345678901234567890E+2.
       01 F2 USAGE FLOAT-LONG VALUE 1.0E12345.
       01 F3 USAGE FLOAT-LONG VALUE 0.0E+5.
       01 F4 USAGE FLOAT-LONG VALUE -0.0E+0.
       01 F5 USAGE FLOAT-LONG VALUE 0.0E-0.
       01 F6 USAGE FLOAT-LONG.
          88 F6-BIG VALUE 1.0E12345.
       PROCEDURE DIVISION.
           MOVE 0.0E+3 TO F6
           COMPUTE F6 = 1.0E00001 + 1
           STOP RUN.
