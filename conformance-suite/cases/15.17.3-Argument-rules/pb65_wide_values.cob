*>--- conformance-case
*> id: pb65_wide_values
*> rules: [AR-15.17.3-1, AR-15.17.3-2, RV-15.45.4-1, RV-15.79.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.45.4, §15.17.3, §15.79.4 — the wide-value members (fix-queue PB65).
      *> INTEGER-OF-BOOLEAN r1b is "the unsigned binary value" of the bit configuration — a 64-one-bit
      *> item is 2^64−1 = 18446744073709551615 (the old signed-long accumulator returned the EC default
      *> 0 at its inherited 63-bit cap). COMBINED-DATETIME's fold now carries the same guard prologue
      *> as FORMATTED-DATETIME: argument-1 = 0 violates §15.17.3 r1 (§15.5.2 integer-date range) and
      *> argument-2 = 86400 violates r2 (standard numeric time form, LEAP-SECOND OFF) — EC default 0;
      *> the legal control is 143951 + 45296.3/100000. And SECONDS-FROM-FORMATTED-TIME at TEN
      *> fractional digits is exact (§15.79.4 r1) — the 32-bit subfield accumulator wrapped modulo
      *> 2^32 at the first width past nine.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65WIDE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B64 PIC 1(64) USAGE BIT VALUE
          B"1111111111111111111111111111111111111111111111111111111111111111".
       01 R20 PIC 9(20).
       01 CD  PIC 9(7)V9(5).
       01 SF  PIC S9(5)V9(10) SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R20 = FUNCTION INTEGER-OF-BOOLEAN(B64)
           DISPLAY "B64=" R20
           COMPUTE CD = FUNCTION COMBINED-DATETIME(0, 45296)
           DISPLAY "CDZ=" CD
           COMPUTE CD = FUNCTION COMBINED-DATETIME(143951, 86400)
           DISPLAY "CDH=" CD
           COMPUTE CD = FUNCTION COMBINED-DATETIME(143951, 45296.3)
           DISPLAY "CDO=" CD
           COMPUTE SF = FUNCTION SECONDS-FROM-FORMATTED-TIME(
               "hh:mm:ss.ssssssssss", "12:34:56.3000000000")
           DISPLAY "SF=" SF
           STOP RUN.
