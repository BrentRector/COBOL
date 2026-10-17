*>--- conformance-case
*> id: pb83_standard_decimal_division_prescale
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB83 - SDIDI division (8.8.1.5.3) of a SHORT numerator by a LONG denominator. The quotient is formed
      *> with 34-36 significant digits by pre-scaling the numerator; that pre-scale can exceed 10^38 (34 +
      *> digits(den) - digits(num) + 1), and it used to be CAPPED at 10^38 while the exponent still subtracted
      *> the uncapped amount - every such quotient was wrong by 10^(scaleUp - 38): 100000 / D30 answered 0.
      *> Values are the exact decimal quotients rounded to 34 significant digits (8.8.1.5.2 NOTE 2), landed at
      *> the receiver with TRUNCATION (14.7.4 - the receiver's mode; INTERMEDIATE ROUNDING is 11.9.11's default
      *> NEAREST-AWAY-FROM-ZERO):
      *> R1: 100000 / 123456789012345678901234567890 = 8.10000007290000066339000603685715...E-25 -> 9V9(30):
      *>   0.000000000000000000000000810000
      *> R2: 5 / D30 * 10^30 = 40.5000003645000033169500301... -> 9(5)V9(25): 00040.5000003645000033169500301
      *> R3: 1 / D30 * D30 = 1 (the round trip lands within the SDIDI's 34 digits) -> 1.0000
      *> R4: 7 / 9999999999999999999999999999999 (31 nines) * 10^30 = 0.70000000000000000000000000000007 -> 9V9(9): 0.700000000
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB83SDDIV.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D30 PIC 9(30) VALUE 123456789012345678901234567890.
       01 D32 PIC 9(31) VALUE 9999999999999999999999999999999.
       01 N PIC 9(6) VALUE 100000.
       01 R1 PIC 9V9(30).
       01 R2 PIC 9(5)V9(25).
       01 R3 PIC 9V9(4).
       01 R4 PIC 9V9(9).
       PROCEDURE DIVISION.
           COMPUTE R1 = N / D30
           DISPLAY "R1=" R1
           COMPUTE R2 = 5 / D30 * 1000000000000000000000000000000
           DISPLAY "R2=" R2
           COMPUTE R3 = 1 / D30 * D30
           DISPLAY "R3=" R3
           COMPUTE R4 = 7 / D32 * 1000000000000000000000000000000
           DISPLAY "R4=" R4
           STOP RUN.
       END PROGRAM PB83SDDIV.
