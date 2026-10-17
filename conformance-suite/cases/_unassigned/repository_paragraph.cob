*>--- conformance-case
*> id: repository_paragraph
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.8 — the REPOSITORY paragraph is accepted (FUNCTION ALL INTRINSIC specifier).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REPTEST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "REPOK".
           STOP RUN.
