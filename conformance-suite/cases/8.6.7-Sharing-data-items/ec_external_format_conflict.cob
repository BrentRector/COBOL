*>--- conformance-case
*> id: ec_external_format_conflict
*> rules: [GR-8.6.7-1, GR-13.18.22.4-6, GR-14.9.4.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-EXTERNAL-FORMAT-CONFLICT (ISO §14.8.4.3 / §13.18.22 GR6; raise point §14.9.4.4
      *> GR3e; VCR 15). MAIN describes external record XFC-SHARED as 4 bytes, the CALLed
      *> sub as 8 bytes — a GR6 byte-count conflict. Checking is enabled in BOTH elements
      *> (§14.8.4.1: the activating side at the CALL statement, the activated side before
      *> its Environment division), so at the CALL the activated element's registration
      *> detects the conflict, the condition is set (Fatal, Table 13), and the program
      *> call is NOT successful — GR3h #1: the ON EXCEPTION phrase takes control (the
      *> sub's MOVE never runs), then FUNCTION EXCEPTION-STATUS reports the level-3 name.
      *> The second CALL activates a CONFORMING describer (4 bytes) — no condition, the
      *> NOT ON EXCEPTION path runs, and the sub's store through the shared cell is
      *> visible in MAIN (one storage copy per run unit, §8.6.7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFCMAIN.
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XFC-SHARED IS EXTERNAL.
          05 XFC-A PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "XFCBAD"
               ON EXCEPTION
                   DISPLAY "BAD=[" FUNCTION EXCEPTION-STATUS "]"
           END-CALL
           DISPLAY "CELL1=[" XFC-A "]"
           SET LAST EXCEPTION TO OFF
           CALL "XFCGOOD"
               ON EXCEPTION
                   DISPLAY "UNEXPECTED"
               NOT ON EXCEPTION
                   DISPLAY "GOOD-OK"
           END-CALL
           DISPLAY "CELL2=[" XFC-A "]"
           STOP RUN.
       END PROGRAM XFCMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFCBAD.
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XFC-SHARED IS EXTERNAL.
          05 XFC-B PIC X(8).
       PROCEDURE DIVISION.
       BAD-PARA.
           MOVE "BADBADBA" TO XFC-B
           GOBACK.
       END PROGRAM XFCBAD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFCGOOD.
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XFC-SHARED IS EXTERNAL.
          05 XFC-C PIC X(4).
       PROCEDURE DIVISION.
       GOOD-PARA.
           MOVE "GOOD" TO XFC-C
           GOBACK.
       END PROGRAM XFCGOOD.
