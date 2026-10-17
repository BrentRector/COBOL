*>--- conformance-case
*> id: pb97-value-edited-truncation
*> rules: [SR-13.18.63.3-3, SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.63.3 SR6: a numeric-edited item's numeric VALUE literal converts per the MOVE rules
      *> "such that no truncation of digits or sign is required", and SR3 admits a signed literal only for "a
      *> numeric-edited data item with a representation of a sign": 12345 does not fit ZZ9.99 (three integer
      *> positions), 123.456 loses a decimal, -5 has no sign position in ZZ9.99, and 12345 exceeds the scaled
      *> range of ZZZPP (COBOLNET1625 for each; kb/Work PB97 - before it every one compiled silently).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB97NVT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE1 PIC ZZ9.99 VALUE 12345.
       01 NE2 PIC ZZ9.99 VALUE 123.456.
       01 NE3 PIC ZZ9.99 VALUE -5.
       01 NE4 PIC ZZZPP VALUE 12345.
       PROCEDURE DIVISION.
           STOP RUN.
