*>--- conformance-case
*> id: boolean_conv_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.13 BOOLEAN-OF-INTEGER / §15.45 INTEGER-OF-BOOLEAN are COBOL-2002 introductions (the
      *> boolean-data amendment; PHASE-11-scout-notes.md spec:boolean). Below 2002 the D8 catalog window
      *> rejects each reference BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11BOOLW85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-5 PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-5 = FUNCTION INTEGER-OF-BOOLEAN(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8))
           STOP RUN.
