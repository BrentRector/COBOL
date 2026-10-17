*>--- conformance-case
*> id: external_linage_externality_ok
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.8.4.2 conjunct 1 positive control: a SINGLE-program external file whose LINAGE data-name operand
      *> is IS EXTERNAL compiles clean at 2023 strict; a LITERAL LINAGE operand (WITH FOOTING AT 8) is exempt from
      *> the externality requirement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XLNOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata".
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL LINAGE IS EXT-LN LINES WITH FOOTING AT 8.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 EXT-LN IS EXTERNAL PIC 99 VALUE 10.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "LN-EXT-OK".
           STOP RUN.
