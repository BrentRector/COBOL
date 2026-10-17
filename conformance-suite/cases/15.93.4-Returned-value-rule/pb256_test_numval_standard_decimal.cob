*>--- conformance-case
*> id: pb256_test_numval_standard_decimal
*> rules: [RV-15.93.4-1, RV-15.95.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB256 — ⛔ THE COMPILER-SIDE ARM, MEASURED END TO END. The §15.93.4 / §15.95.4 digit-cap
      *> sub-notes are ARITHMETIC-MODE conditioned, and the mode is read at COMPILE time: the OPTIONS
      *> paragraph -> ArithmeticModes.NumvalDigitCap -> IntrinsicRenderer.DigitCapFlag -> the emitted
      *> `digitCap:` named argument -> NvScan/NvfScan. Before this fixture the ONLY coverage of those
      *> sub-notes for the TEST- twins passed the cap as a LITERAL from a unit test, so every arm left of
      *> the runtime scan was unmeasured and a mode that resolved to the wrong cap would have gone unseen.
      *>
      *> THE CAP RULES (both re-derived; cite.py --check OK on each sentence):
      *>   §15.67.3 r4  "If standard-decimal arithmetic is in effect, the total number of digits in
      *>                argument-1 shall not exceed 34."          (NUMVAL, imported by §15.93.4 r1 a)
      *>   §15.69.3 r3  "If standard-decimal arithmetic is in effect, the total number of digits in the
      *>                significand of argument-1 shall not exceed 34."                      (NUMVAL-F)
      *>   §15.93.4 r1 b) 4  "If standard-decimal arithmetic is in effect, and the argument has more than
      *>                34 digits, the returned value is the position of the 35th digit if no prior error
      *>                has been found."
      *>   §15.95.4 r1 b) 4  "... the returned value is the position of the 35th digit of the significand
      *>                because the character in error for a significand longer than 34 digits is the 35th
      *>                digit."
      *>   §15.95.4 r1 b) 5  "If standard-decimal arithmetic, or standard-binary arithmetic is in effect,
      *>                and the exponent in the argument contains more than four significant digits, the
      *>                returned value is the position of the fifth digit of the exponent."
      *> The standard-binary halves of r4/r3 (35) are UNREACHABLE: the mode is declined on §4.2.6
      *> processor-dependence (kb/Work PB198; docs/CONFORMANCE.md §1/§2), pinned by
      *> conformance:cases/15.93.4-Returned-value-rule/standard-binary-test-numval-2014. ArithmeticModes.NumvalDigitCap records the
      *> 35 anyway so the table has no hole.
      *>
      *> Hand-derived, with STANDARD-DECIMAL arithmetic in effect by the OPTIONS paragraph (§11.9.5):
      *>   NV32  32 '1's                    -> 0   ⛔ THE DISCRIMINATOR: 32 digits are legal under the
      *>                                          34 cap. Under NATIVE (§15.67.3 r3's 31) this same
      *>                                          argument answers 32, so a golden that saw only the
      *>                                          35-digit case could not tell a live arm from a dead one.
      *>   NV34  34 '1's                    -> 0   the boundary itself
      *>   NV35  35 '1's                    -> 35  r1 b) 4 — the 35th digit's own position
      *>   NVDOT "1234567890123456789012345.6789012345"  (25 + 10 = 35 digits)
      *>                                    -> 36  the position of the 35th DIGIT, which the decimal
      *>                                          separator at character 26 pushes to character 36 — the
      *>                                          rule names a digit ordinal, not a character count
      *>   NF32  32-digit significand E+1   -> 0   §15.69.3 r3 bounds the SIGNIFICAND; native gives 32
      *>   NF34  34-digit significand E+1   -> 0   the boundary
      *>   NF35  35-digit significand E+1   -> 35  §15.95.4 r1 b) 4
      *>   NFEXP "1E+12345"                 -> 8   §15.95.4 r1 b) 5 under the mode the rule names — the
      *>                                          fifth exponent digit's own position
      *>   NC32  "$" + 32 '1's              -> 0   §15.68.3 r7's standard-decimal 34 (via §15.94.3 r1). The
      *>                                          currency string is not a digit (§15.68.4 r2), so the 32
      *>                                          digits start at character 2 and native would answer 33
      *>   NC35  "$" + 35 '1's              -> 36  §15.94.4 r1 b) 4 — the 35th DIGIT sits at character 36
      *>                                          because the currency occupies character 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB256SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "12345678901234567890123456789012")
           IF R = 0 DISPLAY "NV32 OK" ELSE DISPLAY "NV32 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "1234567890123456789012345678901234")
           IF R = 0 DISPLAY "NV34 OK" ELSE DISPLAY "NV34 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "12345678901234567890123456789012345")
           IF R = 35 DISPLAY "NV35 OK" ELSE DISPLAY "NV35 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "1234567890123456789012345.6789012345")
           IF R = 36 DISPLAY "NVDOT OK" ELSE DISPLAY "NVDOT BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(
               "12345678901234567890123456789012E+1")
           IF R = 0 DISPLAY "NF32 OK" ELSE DISPLAY "NF32 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(
               "1234567890123456789012345678901234E+1")
           IF R = 0 DISPLAY "NF34 OK" ELSE DISPLAY "NF34 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(
               "12345678901234567890123456789012345E+1")
           IF R = 35 DISPLAY "NF35 OK" ELSE DISPLAY "NF35 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+12345")
           IF R = 8 DISPLAY "NFEXP OK" ELSE DISPLAY "NFEXP BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-C(
               "$12345678901234567890123456789012")
           IF R = 0 DISPLAY "NC32 OK" ELSE DISPLAY "NC32 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-C(
               "$12345678901234567890123456789012345")
           IF R = 36 DISPLAY "NC35 OK" ELSE DISPLAY "NC35 BAD " R END-IF
           STOP RUN.
       END PROGRAM PB256SD.
