*>--- conformance-case
*> id: pb65_boolean_wide
*> rules: [AR-15.13.3-1, AR-15.13.3-2, RV-15.13.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB65 / RV-15.13.4-1 D1 — BOOLEAN-OF-INTEGER's argument-1 rides the WIDE (Int128) bridge.
      *> ISO §15.13.4 r1: the returned value "has the same bit configuration as the binary
      *> representation of the value of argument-1" — a mathematical configuration (its NOTE), so
      *> 2^63 and 2^64+5 are legal values with bits 63 / 64 set; §15.13.3 r1 admits any positive
      *> integer. The previous long carrier EC'd values at/above 2^63 to the checking-off default 0
      *> (seventy zeros, silently). 544 at length 6 is the §15.13.4 r1 left-truncation branch
      *> (544 mod 2^6 = 100000b — Annex D.10's worked example).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65BOOLWIDE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B70   PIC 1(70) USAGE BIT.
       01 B6    PIC 1(6)  USAGE BIT.
       01 V62   PIC 9(25) VALUE 4611686018427387904.
       01 V63M  PIC 9(25) VALUE 9223372036854775807.
       01 V63   PIC 9(25) VALUE 9223372036854775808.
       01 V64   PIC 9(25) VALUE 18446744073709551621.
       PROCEDURE DIVISION.
           MOVE FUNCTION BOOLEAN-OF-INTEGER(V62, 70) TO B70
           DISPLAY "P62 =" B70
           MOVE FUNCTION BOOLEAN-OF-INTEGER(V63M, 70) TO B70
           DISPLAY "P63M=" B70
           MOVE FUNCTION BOOLEAN-OF-INTEGER(V63, 70) TO B70
           DISPLAY "P63 =" B70
           MOVE FUNCTION BOOLEAN-OF-INTEGER(V64, 70) TO B70
           DISPLAY "P64 =" B70
           MOVE FUNCTION BOOLEAN-OF-INTEGER(544, 6) TO B6
           DISPLAY "T544=" B6
           STOP RUN.
