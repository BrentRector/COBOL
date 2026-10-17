*>--- conformance-case
*> id: pb66-float-edited-picture-symbols
*> rules: [SR-13.18.40.3-15, SR-13.18.40.3-23, GR-13.18.40.4-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.40.4 GR13 b): the significand of a floating-point numeric-edited picture is a numeric
      *> or fixed-point numeric-edited character-string with NEITHER floating insertion NOR zero suppression, and
      *> 13.18.40.6 Table 10 (row E) admits only B 0 / , . a leading + or - and 9 before the E: V, P, S, Z, *, CR,
      *> DB and the currency symbol are illegal in the significand; the exponent is exactly +9{1..4} (a sign is
      *> required, at most four digits, no other symbol); one E only; the EDITING phrase applies to a fixed-point
      *> result (SR8); a SIGN clause needs an S (13.18.52.3 SR1); the significand holds 1..36 digits (13.18.40.3 SR15) and
      *> at least one. Every entry below is COBOLNET1658 (kb/Work PB66) except BE, whose SIGN clause is the
      *> general 13.18.52.3 SR1 subject screen, COBOLNET2422 (kb/Work PB537).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66NSYM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 9V99E+99.
       01 B2 PIC ZZ9.99E+99.
       01 B3 PIC **9.99E+99.
       01 B4 PIC 9PPE+99.
       01 B5 PIC S9.99E+99.
       01 B6 PIC 9.99CRE+99.
       01 B7 PIC 9.99DBE+99.
       01 B8 PIC $9.99E+99.
       01 B9 PIC 9.99E99.
       01 BA PIC 9.99E+9(5).
       01 BB PIC 9.99E-99.
       01 BC PIC 9.99E+99E+99.
       01 BD PIC +++9.99E+99.
       01 BE PIC 9.99E+99 SIGN IS LEADING SEPARATE.
       01 BF PIC ---.--E+99.
       01 BG PIC 9(37)E+99.
       01 BH PIC E+99.
       PROCEDURE DIVISION.
           STOP RUN.
