*>--- conformance-case
*> id: pb99_floating_literal_standard_decimal
*> rules: [SR-8.3.3.3.3-3, GR-8.3.3.3.3-5, GR-8.8.1.5.2-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB99 - under ARITHMETIC IS STANDARD-DECIMAL a floating-point literal is the EXACT decimal128 operand
      *> (8.8.1.5.2 r1 - CobolDec.FromParsed, the range-checking funnel NUMVAL-F uses): a 23-digit significand
      *> keeps every digit, a four-digit exponent that binary64 cannot spell evaluates (1.0E+400 / 1.0E+398 = 100),
      *> and the implementor-defined exponent range (8.3.3.3.3 r3) is decimal128's there (CONFORMANCE.md 7 item
      *> 82). Before PB99 the literal was a C# double: the 23 digits rounded to 17 and 1.0E+400 was Roslyn's CS0594.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9(5)V9(20).
       01 N2 PIC 9(3).
       01 X PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N1 = 1.2345678901234567890123E+3
           DISPLAY "N1=" N1.
           COMPUTE N2 = 1.0E+400 / 1.0E+398
           DISPLAY "N2=" N2.
           COMPUTE N1 = 1.5E+3 * 2
           DISPLAY "N1=" N1.
           MOVE 2.5E+3 TO X
           DISPLAY "X=" X.
           IF 1.5E+3 > 1499.9 DISPLAY "relation" END-IF
           STOP RUN.
