*>--- conformance-case
*> id: pb124-trim-keyword-after-arg2
*> rules: [AR-15.3-7, FMT-15.96.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.96.2 fixes the order: argument-1 [LEADING|TRAILING] [argument-2]... - the keyword group
      *> precedes every argument-2. The old order-free walk took it anywhere (kb/Work PB124, AR-15.3-7 /
      *> FMT-15.96.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TRIM("xx" "y" LEADING) TO RS
           STOP RUN.
