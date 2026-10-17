*>--- conformance-case
*> id: pb70-refmod-group-noninteger-numeric-sender
*> rules: [GR-8.4.3.3.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.4 GR6: the unique data item a reference modification creates over a GROUP is an
      *> ELEMENTARY item of category alphanumeric - so 14.9.25.3 Table 16 governs a MOVE into it, and a
      *> noninteger numeric sender does not move to an alphanumeric receiver (SR10). kb/Work PB70: the
      *> receiver-category reader answered "group" for a group ref-mod (a conversion-free GR4 copy, no
      *> Table-16 check); the ref-mod view now answers alphanumeric first - COBOLNET0819.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70NTABLE16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GP.
          05 GA PIC X(2).
          05 GB PIC 9(3).
       PROCEDURE DIVISION.
           MOVE 1.5 TO GP(1:2).
           STOP RUN.
