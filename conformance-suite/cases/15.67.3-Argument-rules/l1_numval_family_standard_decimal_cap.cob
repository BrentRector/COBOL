*>--- conformance-case
*> id: l1_numval_family_standard_decimal_cap
*> rules: [AR-15.67.3-4, AR-15.68.3-7, AR-15.69.3-3]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.67.3 r4 (NUMVAL), §15.68.3 r7 (NUMVAL-C) and §15.69.3 r3 (NUMVAL-F) — the STANDARD-DECIMAL
      *> half of the digit cap, measured as a MATCHED PAIR one digit apart on each of the three functions.
      *> Each rule is two sentences; this fixture is about the second of them:
      *>   §15.67.3 r4 / §15.68.3 r7 "... If standard-decimal arithmetic is in effect, the total number of
      *>       digits in argument-1 shall not exceed 34."
      *>   §15.69.3 r3       "... If standard-decimal arithmetic is in effect, the total number of digits in
      *>       the significand of argument-1 shall not exceed 34."
      *> (cite.py --check 15.67.3 / 15.68.3 / 15.69.3 on those sentences -> OK at rules 4, 7 and 3.)
      *> The FIRST sentence of each rule bounds STANDARD-BINARY arithmetic at 35; that mode is declined on
      *> §4.2.6 processor-dependence (kb/Work PB198, owner decision D-A; docs/CONFORMANCE.md §1/§2), so no
      *> compilation can put it in effect and the 35 is unreachable BY CONSTRUCTION rather than unwritten —
      *> ArithmeticModes.NumvalDigitCap records it anyway so the table has no hole.
      *>
      *> STANDARD-DECIMAL arithmetic is in effect by the OPTIONS paragraph (§11.9.5). Checking is DISABLED
      *> (no >>TURN), so §15.3's closing sentence supplies the observable for the reject half: "If the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist and checking for EC-ARGUMENT-FUNCTION is
      *> not enabled, the implementor defines the result of the function reference" — 0, documented in
      *> docs/CONFORMANCE.md. That is the same convention pb33_numval_family_digit_cap and
      *> l1_numval_family_cap_discriminators run under, and 0 is a value no legal argument here produces.
      *>
      *> ⛔ WHY THIS EXISTS BESIDE THE TWO NATIVE-CAP FIXTURES. pb33 and l1_numval_family_cap_discriminators
      *> pin the FIRST sentence of the sibling rules (§15.67.3 r3 / §15.68.3 r6 / §15.69.3 r2 — 31 under
      *> native), and conformance:cases/15.4.1-Numeric-and-integer-functions/pb60_numval_standard_decimal pins the 34/35 boundary for NUMVAL
      *> alone. Nothing measured the standard-decimal boundary on NUMVAL-C or on NUMVAL-F, and a cap is a
      *> BOUNDARY: only a matched pair astride it, on the function whose own rule is being measured, can
      *> tell 34 from 31 or from 35. The NUMVAL pair is carried here too so the three rules are measured in
      *> one mode, in one run, one digit apart.
      *>
      *> Hand-derived. The standard intermediate data item for standard-decimal arithmetic holds 34 digits
      *> (§8.8.1.5.2), so a 34-digit argument is exact and its value comes back unrounded through the
      *> receiver-less DISPLAY channel:
      *>   NV34   "1234567890123456789012345.678901234"   25 + 9 = 34 digits  -> the value
      *>   NV35   "1234567890123456789012345.6789012345"  25 + 10 = 35        -> §15.3 default 0
      *>   NVC34  "$1234567890123456789012345.678901234"  the currency string is not a digit (§15.68.4 r2),
      *>                                                  so this is 34      -> the value
      *>   NVC35  one more fraction digit = 35            -> 0
      *>   NVF34  "1234567890123456789012345.678901234"   34 significand digits, no exponent -> the value
      *>   NVFS34 "123456789012345678901234567.8901234E+1" 27 + 7 = 34 SIGNIFICAND digits inside a string
      *>          carrying 35 digits once the exponent's own digit is counted. Rule 3 bounds the
      *>          significand, so this argument is REQUIRED to be accepted; a whole-string cap would answer
      *>          0 here. Value = 123456789012345678901234567.8901234 x 10 = 1234567890123456789012345678.901234
      *>   NVF35  "12345678901234567890123456.789012345E+1" 26 + 9 = 35 significand digits -> 0
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NVSDCAP.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "NV34=" FUNCTION NUMVAL(
               "1234567890123456789012345.678901234").
           DISPLAY "NV35=" FUNCTION NUMVAL(
               "1234567890123456789012345.6789012345").
           DISPLAY "NVC34=" FUNCTION NUMVAL-C(
               "$1234567890123456789012345.678901234").
           DISPLAY "NVC35=" FUNCTION NUMVAL-C(
               "$1234567890123456789012345.6789012345").
           DISPLAY "NVF34=" FUNCTION NUMVAL-F(
               "1234567890123456789012345.678901234").
           DISPLAY "NVFS34=" FUNCTION NUMVAL-F(
               "123456789012345678901234567.8901234E+1").
           DISPLAY "NVF35=" FUNCTION NUMVAL-F(
               "12345678901234567890123456.789012345E+1").
           STOP RUN.
       END PROGRAM L1NVSDCAP.
