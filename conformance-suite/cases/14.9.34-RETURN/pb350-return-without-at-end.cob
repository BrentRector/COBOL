*>--- conformance-case
*> id: pb350-return-without-at-end
*> rules: [FMT-14.9.34.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.34.2 — the AT END phrase OMITTED ENTIRELY. The general format prints
      *>     AT END imperative-statement-1
      *> on its own line with NO brackets, between the bracketed [ NOT AT END … ] and [ END-RETURN ]; per
      *> §5.2.6.2 brackets are the only convention that makes a portion of a general format omissible and per
      *> §5.2.2 an underlined keyword is required subject to those conventions, so the phrase is MANDATORY.
      *> Nothing in the rule is edition-dependent — it is a property of the printed format, not of a feature
      *> introduction — hence all four reject-at years.
      *> Until kb/Work PB350 this compiled clean at every edition and produced a WRONG ANSWER: §14.9.34.4 GR3
      *> makes imperative-statement-1 the only defined destination for control at end of data, so with no AT
      *> END phrase control fell THROUGH the statement onto a record area the same rule leaves undefined, and
      *> a loop written on RETURN could never terminate from the statement — this program printed the
      *> previous record again instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB350N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb350n1-wk".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 S-REC.
          05 S-K PIC 9.
          05 S-T PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-1.
           SORT SF ON ASCENDING KEY S-K
               INPUT PROCEDURE IS FEED-1
               OUTPUT PROCEDURE IS DRAIN-1.
           STOP RUN.
       FEED-1.
           MOVE 2 TO S-K MOVE "BBB" TO S-T RELEASE S-REC.
           MOVE 1 TO S-K MOVE "AAA" TO S-T RELEASE S-REC.
       DRAIN-1.
           RETURN SF RECORD END-RETURN.
           DISPLAY "R=" S-T.
           ADD 1 TO WS-N.
           IF WS-N < 4 GO TO DRAIN-1.
