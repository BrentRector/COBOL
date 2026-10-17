      *>--- conformance-case
      *> id: pb374-async-messaging-below-2023
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB374 - the RECEIVE and SEND formats of ISO 14.9.31.2
      *>   and 14.9.38.2 are the asynchronous messaging facility's, a
      *>   COBOL-2023 introduction (Annex E.3.2 item 1). Both verbs are
      *>   reserved at COBOL-85 (the '85 communication module) and again
      *>   at 2023, with a user-word gap at 2002/2014, so a reservation
      *>   test recognized these formats at --std 85 and compiled them
      *>   under the 2023 facility's warning. Each edition below 2023
      *>   answers the introduction gate COBOLNET0900 instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB374AM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG PIC X(8).
       01 WS-MSG PIC X(80).
       01 WS-LEN PIC 9(4).
       PROCEDURE DIVISION.
       M1.
           RECEIVE FROM WS-TAG GIVING WS-MSG WS-LEN
               CONTINUE AFTER 1 SECONDS
               ON EXCEPTION DISPLAY "EXC"
               NOT ON EXCEPTION DISPLAY "OK"
           END-RECEIVE.
           SEND TO WS-TAG FROM WS-MSG
               ON EXCEPTION DISPLAY "EXC"
           END-SEND.
           STOP RUN.
