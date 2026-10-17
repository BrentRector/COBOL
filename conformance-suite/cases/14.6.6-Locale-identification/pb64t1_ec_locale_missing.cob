*>--- conformance-case
*> id: pb64t1_ec_locale_missing
*> rules: [GR-14.6.6-4, GR-14.9.39.4-24]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.4 GR24 — "If the locale specified by locale-name-1 is not available, the EC-LOCALE-MISSING
      *> exception condition is set to exist" — and 8.2.1 / 12.3.7.4 GR5 for a NAMED IS LOCALE collating sequence whose
      *> locale is not available at the point of use (DESIGN-locale-facility L1 item 4: availability is a RUN-TIME
      *> property — the compiler never resolves an external identification, so the program compiles). Table 13
      *> (14.6.13.1.6) makes EC-LOCALE-MISSING FATAL; with checking on the USE declarative observes it and RESUME AT
      *> NEXT STATEMENT continues (kb/Work PB64 T1).
      *>
      *> ⛔ THE POINT IS THAT THE CONDITION IS OBSERVED, not merely registered (DESIGN-locale-facility 4.10): a
      *> registered exception-name with no raise site is a zero-fan-out result that reads as coverage.
      *>
      *> What each line proves:
      *>   HANDLED #1 / SET  — SET LOCALE LC_COLLATE TO XX (an external identification no environment provides)
      *>                       raises; the statement is unsuccessful, so the current collation is still the root
      *>                       ("nz" > "ñu").
      *>   HANDLED #2 / NAMED — a relation under the PROGRAM COLLATING SEQUENCE bound to the missing locale (ALPHABET
      *>                       BAD IS LOCALE XX) raises AT USE; the IF statement is interrupted (14.6.13.1.3 #5 — the
      *>                       declarative runs, RESUME AT NEXT STATEMENT), so VERDICT keeps its initial "?" — displayed
      *>                       to prove the statement did not silently complete.
      *>   GOOD              — a declared locale that IS available (ES) switches without any condition.
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1MISS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS BAD.
       SPECIAL-NAMES.
           ALPHABET BAD IS LOCALE XX
           ALPHABET CUR IS LOCALE
           LOCALE XX IS "xx-NOWHERE"
           LOCALE ES IS "es-ES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       01  VERDICT         PIC X VALUE "?".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET LOCALE LC_COLLATE TO XX
           DISPLAY "SET=done"
           IF A < B MOVE "<" TO VERDICT ELSE MOVE ">" TO VERDICT END-IF
           DISPLAY "NAMED=" VERDICT
           SET LOCALE LC_COLLATE TO ES
           DISPLAY "GOOD=done"
           STOP RUN.
