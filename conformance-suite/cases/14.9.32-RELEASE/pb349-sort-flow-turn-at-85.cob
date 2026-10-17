*>--- conformance-case
*> id: pb349-sort-flow-turn-at-85
*> rules: [GR-14.9.32.4-1, GR-14.9.34.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB349 -- the checked half of ISO 14.9.32.4 GR1 / 14.9.34.4 GR1 + GR3 below the
      *> edition that introduced the exception-checking model: the TURN directive (7.3.25) and
      *> the EC-FLOW-RELEASE / EC-FLOW-RETURN / EC-SORT-MERGE-RETURN names are COBOL-2002, so a
      *> COBOL-85 program cannot enable them; the refusal is the directive's own edition gate.
      >>TURN EC-FLOW-RELEASE EC-FLOW-RETURN EC-SORT-MERGE-RETURN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB349N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb349n85.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SRT-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ZZZ" TO SRT-REC.
           RELEASE SRT-REC.
           STOP RUN.
