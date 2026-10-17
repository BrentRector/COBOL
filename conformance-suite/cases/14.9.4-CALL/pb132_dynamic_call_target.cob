*>--- conformance-case
*> id: pb132_dynamic_call_target
*> rules: [SR-14.9.4.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB132 - ISO 14.9.4.3 SR1's LEGAL side + 14.9.4.4 GR3b: identifier-1 is an alphanumeric
      *> data item and the activated program is the one whose name is its VALUE at execution time. The
      *> COBOLNET1681 screen must admit this (its job is the numeric/boolean/index targets only).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132DY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-PGM PIC X(8) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "PB132DS" TO W-PGM
           CALL W-PGM
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132DS.
       PROCEDURE DIVISION.
       P.
           DISPLAY "DYN=OK"
           GOBACK.
       END PROGRAM PB132DS.
       END PROGRAM PB132DY.
