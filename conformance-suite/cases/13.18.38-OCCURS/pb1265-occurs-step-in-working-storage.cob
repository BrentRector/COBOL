*>--- conformance-case
*> id: pb1265-occurs-step-in-working-storage
*> rules: [FMT-13.18.38.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1265 - ISO 13.18.38.2: STEP integer-3 exists only in Format 3, the report-writer
      *> format of a report group description entry; a working-storage entry has no such phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1265NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T PIC X OCCURS 5 STEP 2.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
