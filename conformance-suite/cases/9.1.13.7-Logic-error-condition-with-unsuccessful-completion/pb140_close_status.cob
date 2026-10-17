      *>--- conformance-case
      *> id: pb140_close_status
      *> rules: [GR-9.1.13.7-2, GR-14.9.6.4-1, GR-14.9.6.4-4,
      *>    GR-14.9.6.4-6, GR-14.9.6.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140CS.
      *> kb/Work PB140 - ISO 14.9.6.4 GR1 ('42' not open), GR8 (a closed
      *> file reopens, never '41'), 9.1.13.2 item 6 (REEL/UNIT on a
      *> non-reel medium: successful '07'), and GR6: for an absent
      *> OPTIONAL input file NO unit processing is performed - CLOSE UNIT
      *> answers the plain-successful '00', and the FPI is unchanged.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb140cs.dat"
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL G ASSIGN TO "pb140csg.dat"
               FILE STATUS IS WS-GT.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(8).
       FD G.
       01 G-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-GT PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT S
           DISPLAY "OPENS=" WS-ST
           MOVE "DATA0001" TO S-REC
           WRITE S-REC
           CLOSE S
           DISPLAY "CLOSE1=" WS-ST
           CLOSE S
           DISPLAY "CLOSE2=" WS-ST
           OPEN INPUT S
           DISPLAY "REOPEN=" WS-ST
           CLOSE S UNIT
           DISPLAY "UNIT=" WS-ST
           CLOSE S
           DISPLAY "CLOSE3=" WS-ST
           OPEN INPUT G
           DISPLAY "OPTG=" WS-GT
           CLOSE G UNIT
           DISPLAY "GUNIT=" WS-GT
           CLOSE G
           DISPLAY "GCLOSE=" WS-GT
           STOP RUN.
