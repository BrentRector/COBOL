*>--- conformance-case
*> id: smallest_algebraic_below_2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.83 SMALLEST-ALGEBRAIC is NEW-IN-2023 (Annex E.3 item 29 "has been added";
      *> PHASE-11-scout-notes.md spec:concat-smallest). At --std 2014 the D8 catalog window rejects the
      *> reference BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11SAW2014.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X-INT PIC S999.
       01 N-3   PIC S9(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-3 = FUNCTION SMALLEST-ALGEBRAIC(X-INT)
           STOP RUN.
