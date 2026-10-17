*>--- conformance-case
*> id: options_default_rounded
*> rules: [GR-11.9.6.3-1, GR-14.7.4.3-1, FMT-11.9.6.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.9.6 — COBOL-2002 OPTIONS paragraph DEFAULT ROUNDED MODE. It sets the rounding mode used by a
      *> *bare* ROUNDED phrase (one with no per-statement MODE IS). Here NEAREST-EVEN (banker's rounding) gives
      *> 2.5 -> 2, 3.5 -> 4, 0.5 -> 0 — visibly distinct from the ISO default NEAREST-AWAY-FROM-ZERO, which
      *> would give 3, 4, 1. (A per-statement ROUNDED MODE IS … still overrides this program default.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPTROUND.
       OPTIONS.
           DEFAULT ROUNDED MODE IS NEAREST-EVEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R ROUNDED = 25 / 10.
           DISPLAY "H25=" R.
           COMPUTE R ROUNDED = 35 / 10.
           DISPLAY "H35=" R.
           COMPUTE R ROUNDED = 5 / 10.
           DISPLAY "H05=" R.
      *> A per-statement MODE IS overrides the OPTIONS default: TRUNCATION of 2.5 -> 2.
           COMPUTE R ROUNDED MODE IS TRUNCATION = 29 / 10.
           DISPLAY "TRUNC=" R.
           STOP RUN.
