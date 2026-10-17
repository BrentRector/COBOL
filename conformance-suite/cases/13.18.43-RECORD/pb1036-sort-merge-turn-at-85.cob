*>--- conformance-case
*> id: pb1036-sort-merge-turn-at-85
*> rules: [GR-13.18.43.4-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1036 -- the checked half of ISO 14.9.40.4 GR9 / GR10 / GR12 b) and
      *> 14.9.24.4 GR6 / GR7 / GR8 / GR12 below the edition that introduced the
      *> exception-checking model: the TURN directive (7.3.25) and the EC-SORT-MERGE
      *> exception-names are COBOL-2002, so a COBOL-85 program cannot enable them; the
      *> refusal is the directive's own edition gate.
      >>TURN EC-SORT-MERGE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1036N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G-OUT ASSIGN TO "pb1036n85.dat".
           SELECT SF ASSIGN TO "pb1036n85.srt".
       DATA DIVISION.
       FILE SECTION.
       FD G-OUT.
       01 G-REC PIC X(3).
       SD SF.
       01 SF-REC PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT G-OUT.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS FEED GIVING G-OUT.
           STOP RUN.
       FEED.
           MOVE "QQQ" TO SF-REC.
           RELEASE SF-REC.
