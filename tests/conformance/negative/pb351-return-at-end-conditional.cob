      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB351 (row FMT-14.9.34.2). ISO 14.9.34.2 writes imperative-statement-1 after AT END, and
      *> 14.5.1: "Any statement with a conditional phrase that is not terminated by its explicit scope
      *> terminator is a conditional statement" - so the IF with no END-IF below is a conditional statement
      *> where only an imperative statement is admitted, although NOT AT END ends it for the parser. Refused
      *> COBOLNET2796 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB351NR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB351NR.TMP".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 S-REC.
          05 S-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           SORT SF ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       FEED SECTION.
       FEED-P.
           MOVE "ONE" TO S-KEY.
           RELEASE S-REC.
       DRAIN SECTION.
       DRAIN-P.
           RETURN SF
               AT END IF WS-EOF = "N" MOVE "Y" TO WS-EOF
               NOT AT END DISPLAY "R=" S-KEY
           END-RETURN.
