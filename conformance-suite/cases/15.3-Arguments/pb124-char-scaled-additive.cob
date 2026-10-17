*>--- conformance-case
*> id: pb124-char-scaled-additive
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 type 6's expression alternative. S (scale 1) as a free additive term makes consecutive
      *> results 0.1 apart, so a non-integer value always exists (kb/Work PB124, AR-15.3-6). The cancelling
      *> shapes stay legal - S - S, S * 10 - and are pinned compiling in pb124_always_integral_zoo.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 6.
       01 S PIC 9V9 VALUE 1.5.
       01 RS PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR(S + 1) TO RS
           STOP RUN.
