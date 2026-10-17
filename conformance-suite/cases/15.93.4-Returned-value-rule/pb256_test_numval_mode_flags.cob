*>--- conformance-case
*> id: pb256_test_numval_mode_flags
*> rules: [RV-15.93.4-1, RV-15.95.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB256 — ⛔ THE TWO COMPILER-SIDE FLAGS THE TEST- SCANNERS TAKE AS PARAMETERS, MEASURED FROM
      *> THE SOURCE CONSTRUCT THAT SETS THEM. NvScan/NvfScan receive `commaMode` and `digitCap` as arguments;
      *> the arms that COMPUTE them live in the compiler (IntrinsicRenderer.CommaFlag reads
      *> ctx.Data.DecimalPointIsComma; IntrinsicRenderer.DigitCapFlag reads ArithmeticModes.NumvalDigitCap of
      *> the OPTIONS paragraph's mode). Every existing test supplied both as LITERALS, so those arms had no
      *> coverage of any kind for TEST-NUMVAL. Three compilation units, one per state of the two flags —
      *> and each flag is measured in BOTH directions, because a dead arm and a live one differ only on the
      *> case the other state would answer differently.
      *>
      *> §15.67.3 r5 (imported into TEST-NUMVAL by §15.93.4 r1 a): "The character period in argument-1
      *> represents the decimal separator. When the DECIMAL-POINT IS COMMA clause is specified, the
      *> character comma shall be used in argument-1 instead of the character period to represent the
      *> decimal separator" — cite.py --check 15.67.3 -> OK, rule 5. §15.69.3 r4 says it for NUMVAL-F
      *> (§15.95.4 r1 a) — same sentence, rule 4.
      *> §15.93.4 r1 b) 2 / 4 and §15.95.4 r1 b) 2 / 4 condition the digit cap on the arithmetic mode:
      *> 31 native, 34 standard-decimal. ARITHMETIC IS STANDARD — the COBOL-2002 mode, obsolete 2014 and
      *> removed 2023, so 2002 is where it can be measured — is named by NONE of the sub-notes; COBOL.NET's
      *> recorded determination is that it takes 34 because its standard intermediate data item IS the
      *> standard DECIMAL one and it routes to the same engine (ArithmeticModes.NumvalDigitCap /
      *> ArithmeticModes' SDIDI remark, kb/Work PB194/PB198). This fixture is what MEASURES that
      *> determination; it was carried as reasoning alone until now.
      *>
      *> Hand-derived. Unit PB256NAT (no SPECIAL-NAMES, no OPTIONS — period separator, native 31 cap):
      *>   P1 TEST-NUMVAL("1.5")        -> 0   the period IS the separator
      *>   P2 TEST-NUMVAL("1,5")        -> 2   a comma is not a NUMVAL character; the ',' is first in error
      *>   P3 TEST-NUMVAL-F("1.5E+1")   -> 0
      *>   P4 TEST-NUMVAL-F("1,5E+1")   -> 2
      *>   P5 TEST-NUMVAL(32 '1's)      -> 32  §15.93.4 r1 b) 2 — the native 31 cap's 32nd digit
      *>   P6 TEST-NUMVAL-C("$1,234.56") -> 0  §15.68.3 r4 d — period decimals, comma groups
      *>   P7 TEST-NUMVAL-C("$1.234,56") -> 7  the SWAPPED spelling: the '.' takes the decimal role, so the
      *>                                       ',' at position 7 is a grouping separator AFTER it, which
      *>                                       r4 a)'s format does not admit
      *>   P8 NUMVAL-C("$1,234.56")     -> 1234.56  the value twin of P6
      *> Unit PB256DPC (DECIMAL-POINT IS COMMA) — every verdict is the MIRROR of PB256NAT's. ⛔ NUMVAL-C is
      *> here because §15.68.3 r4 d swaps BOTH roles, not just one, so its mirror pair is the only shape that
      *> can tell a live compiler arm from a dead one:
      *>   D1 TEST-NUMVAL("1,5")        -> 0   the comma IS the separator now
      *>   D2 TEST-NUMVAL("1.5")        -> 2   and the period is no longer a NUMVAL character at all
      *>   D3 TEST-NUMVAL-F("1,5E+1")   -> 0
      *>   D4 TEST-NUMVAL-F("1.5E+1")   -> 2
      *>   D5 TEST-NUMVAL-C("$1.234,56") -> 0  r4 d — "the character comma shall be used in argument-1 to
      *>                                       represent the decimal separator and the character period
      *>                                       shall be used to represent the grouping separator"
      *>   D6 TEST-NUMVAL-C("$1,234.56") -> 7  the exact string P6 accepts, now in error at the '.'
      *>   D7 NUMVAL-C("$1.234,56")     -> 1234,56  the value twin (the COBOL literal takes the comma too)
      *> Unit PB256STD (OPTIONS. ARITHMETIC IS STANDARD.) — the cap moves and nothing else does:
      *>   S1 TEST-NUMVAL(32 '1's)      -> 0   ⛔ the discriminator against PB256NAT's P5
      *>   S2 TEST-NUMVAL(35 '1's)      -> 35  the 34 cap's 35th digit
      *>   S3 TEST-NUMVAL-F(32-digit significand E+1) -> 0
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB256NAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       01 V PIC S9(9)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL("1.5")
           IF R = 0 DISPLAY "P1 OK" ELSE DISPLAY "P1 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL("1,5")
           IF R = 2 DISPLAY "P2 OK" ELSE DISPLAY "P2 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1.5E+1")
           IF R = 0 DISPLAY "P3 OK" ELSE DISPLAY "P3 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1,5E+1")
           IF R = 2 DISPLAY "P4 OK" ELSE DISPLAY "P4 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "12345678901234567890123456789012")
           IF R = 32 DISPLAY "P5 OK" ELSE DISPLAY "P5 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-C("$1,234.56")
           IF R = 0 DISPLAY "P6 OK" ELSE DISPLAY "P6 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-C("$1.234,56")
           IF R = 7 DISPLAY "P7 OK" ELSE DISPLAY "P7 BAD " R END-IF
           COMPUTE V = FUNCTION NUMVAL-C("$1,234.56")
           IF V = 1234.56 DISPLAY "P8 OK" ELSE DISPLAY "P8 BAD " V END-IF
           CALL "PB256DPC"
           CALL "PB256STD"
           STOP RUN.
       END PROGRAM PB256NAT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB256DPC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       01 V PIC S9(9)V99.
       PROCEDURE DIVISION.
       DPC.
           COMPUTE R = FUNCTION TEST-NUMVAL("1,5")
           IF R = 0 DISPLAY "D1 OK" ELSE DISPLAY "D1 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL("1.5")
           IF R = 2 DISPLAY "D2 OK" ELSE DISPLAY "D2 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1,5E+1")
           IF R = 0 DISPLAY "D3 OK" ELSE DISPLAY "D3 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1.5E+1")
           IF R = 2 DISPLAY "D4 OK" ELSE DISPLAY "D4 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-C("$1.234,56")
           IF R = 0 DISPLAY "D5 OK" ELSE DISPLAY "D5 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-C("$1,234.56")
           IF R = 7 DISPLAY "D6 OK" ELSE DISPLAY "D6 BAD " R END-IF
           COMPUTE V = FUNCTION NUMVAL-C("$1.234,56")
           IF V = 1234,56 DISPLAY "D7 OK" ELSE DISPLAY "D7 BAD " V END-IF
           GOBACK.
       END PROGRAM PB256DPC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB256STD.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       STD.
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "12345678901234567890123456789012")
           IF R = 0 DISPLAY "S1 OK" ELSE DISPLAY "S1 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(
               "12345678901234567890123456789012345")
           IF R = 35 DISPLAY "S2 OK" ELSE DISPLAY "S2 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(
               "12345678901234567890123456789012E+1")
           IF R = 0 DISPLAY "S3 OK" ELSE DISPLAY "S3 BAD " R END-IF
           GOBACK.
       END PROGRAM PB256STD.
