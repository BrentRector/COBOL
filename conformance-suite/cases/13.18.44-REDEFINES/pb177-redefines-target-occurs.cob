*>--- conformance-case
*> id: pb177-redefines-target-occurs
*> rules: [SR-13.18.44.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR5, SENTENCE 1: "The data description entry for data-name-2 shall not contain an OCCURS
      *> clause." SR5 has FOUR sentences and this repo read only the fourth ("Neither the original definition nor
      *> the redefinition shall include an occurs-depending table", which is COBOLNET0855's rule), concluded the
      *> whole rule was about something else, and shipped a comment saying the OCCURS-bearing data-name-2 case
      *> "NO syntax rule literally names". Sentence 1 names it outright. Measured before the screen: this program
      *> compiled CLEAN and ran, laying R over one occurrence's worth of storage.
      *> Sentence 2 - "However, data-name-2 may be subordinate to an item whose data description entry contains
      *> an OCCURS clause" - is the LIMIT of the screen: the test is on data-name-2's OWN entry, never an
      *> ancestor walk, or it would reject legal source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A.
          05 T PIC X(3) OCCURS 4.
          05 R REDEFINES T PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.
