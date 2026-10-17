*>--- conformance-case
*> id: rounded_modes
*> rules: [GR-14.7.4.3-10, GR-14.9.8.4-1, GR-14.9.8.4-2, FMT-14.7.4.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.7.4 — ROUNDED MODE (COBOL-2002), all eight modes + the bare-ROUNDED default.
      *> Rounding 2.25 / 2.21 / 2.20 to one fraction digit exposes how each mode resolves the
      *> dropped digits (the three NEAREST-* modes differ only on the 2.25 tie):
      *>   TRUNCATION              2.25 -> 2.2   (toward zero)
      *>   NEAREST-AWAY-FROM-ZERO  2.25 -> 2.3   (tie away)        = bare ROUNDED default
      *>   NEAREST-EVEN            2.25 -> 2.2   (tie to even)
      *>   NEAREST-TOWARD-ZERO     2.25 -> 2.2   (tie toward zero)
      *>   AWAY-FROM-ZERO          2.21 -> 2.3   (always up in magnitude)
      *>   TOWARD-GREATER          2.21 -> 2.3   (+inf / ceiling)
      *>   TOWARD-LESSER           2.21 -> 2.2   (-inf / floor)
      *>   PROHIBITED              2.20 -> 2.2   (exact: no rounding needed)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RNDMODES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC   PIC 9V99  VALUE 2.25.
       01 SRC2  PIC 9V99  VALUE 2.21.
       01 SRC3  PIC 9V99  VALUE 2.20.
       01 X     PIC 9.9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE X ROUNDED MODE IS TRUNCATION = SRC.
           DISPLAY "TRUNC=" X.
           COMPUTE X ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO = SRC.
           DISPLAY "NAWAY=" X.
           COMPUTE X ROUNDED MODE IS NEAREST-EVEN = SRC.
           DISPLAY "NEVEN=" X.
           COMPUTE X ROUNDED MODE IS NEAREST-TOWARD-ZERO = SRC.
           DISPLAY "NTOZ =" X.
           COMPUTE X ROUNDED MODE IS AWAY-FROM-ZERO = SRC2.
           DISPLAY "AWAY =" X.
           COMPUTE X ROUNDED MODE IS TOWARD-GREATER = SRC2.
           DISPLAY "GREAT=" X.
           COMPUTE X ROUNDED MODE IS TOWARD-LESSER = SRC2.
           DISPLAY "LESS =" X.
           COMPUTE X ROUNDED = SRC.
           DISPLAY "DEFLT=" X.
           COMPUTE X ROUNDED MODE IS PROHIBITED = SRC3.
           DISPLAY "PROH =" X.
           STOP RUN.
