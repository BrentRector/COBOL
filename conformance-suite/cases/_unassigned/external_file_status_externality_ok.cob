*>--- conformance-case
*> id: external_file_status_externality_ok
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.8.4.2 conjunct 1 positive control: a SINGLE-program external file whose FILE STATUS names an
      *> IS EXTERNAL item compiles clean at 2023 strict (guards the dropped conns.Count<2 early-out against
      *> over-firing on a lone, correctly-external describer).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFSOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata" FILE STATUS IS EXT-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 EXT-ST IS EXTERNAL PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FS-EXT-OK".
           STOP RUN.
