*>--- conformance-case
*> id: pb128_resultant_categories
*> rules: [SR-14.9.2.3-2, SR-14.9.8.3-1, SR-14.9.12.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB128 — the arithmetic resultant categories the syntax rules ADMIT, pinned from the legal
      *> side: DIVIDE's REMAINDER identifier-4 admits a NUMERIC-EDITED item (ISO 14.9.12.3 SR2 — the same
      *> rule that admits the GIVING quotient's editing), and ADD GIVING admits one (14.9.2.3 SR4). The
      *> negatives pb128-* pin the reject side (in-place receivers are numeric-only per SR2/SR1; alphanumeric
      *> receivers are nothing at all). Hand-derived: 10/3 -> Q=0003 R=1 edited as '   1'; 1+2 edited '  3'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB128RC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9(4) VALUE 10.
       01 Q PIC 9(4).
       01 RE PIC ZZZ9.
       01 GE PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE D BY 3 GIVING Q REMAINDER RE
           DISPLAY "Q=" Q " R=" RE
           ADD 1 2 GIVING GE
           DISPLAY "G=" GE
           STOP RUN.
