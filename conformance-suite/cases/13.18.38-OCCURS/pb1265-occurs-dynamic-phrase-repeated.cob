*>--- conformance-case
*> id: pb1265-occurs-dynamic-phrase-repeated
*> rules: [FMT-13.18.38.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1265 - ISO 13.18.38.2 Format 4 prints [ CAPACITY IN data-name-3 ] [ FROM
      *> integer-4 ] [ TO integer-5 ] [ INITIALIZED ], each phrase once. FROM 1 FROM 4 used to bind
      *> the last value in silence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1265NR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T PIC X OCCURS DYNAMIC CAPACITY IN C FROM 1 FROM 4.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
