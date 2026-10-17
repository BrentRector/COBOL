      *>--- conformance-case
      *> id: pb350_return_at_end_required_85
      *> rules: [FMT-14.9.34.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.34.2 at --std 85 — the SAME mandatory AT END phrase, at the OLDEST edition COBOL.NET
      *> compiles. The rule is a property of the printed general format, which brackets [ NOT AT END … ] and
      *> [ END-RETURN ] and leaves the AT END line unbracketed; §5.2.6.2 makes brackets the only convention
      *> that makes a portion omissible, so nothing about the rule is edition-dependent and COBOLNET1850
      *> screens it at every edition (kb/Work PB350). This is the 85 ACCEPT side; the reject side is
      *> tests/conformance/negative/pb350-return-without-at-end.cob, whose reject-at header names all four.
      *>
      *> ⚠ DELIBERATELY NORMAL ORDER ONLY. The 2023 twin (pb350_return_at_end_required.cob) carries the
      *> §14.9.34.3 SR4 reversed spelling; this one does not, so that an 85 red here can only mean the AT END
      *> screen misfired and never that the reversed order is being adjudicated at the same time.
      *>
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · §14.9.40.4 GR8 a): the ASCENDING key returns "the record containing the key data item with the
      *>    lower value … first" — released 3/CCC, 1/AAA, 2/BBB, so the drain returns AAA, BBB, CCC.
      *>  · §14.9.34.4 GR4: each successful return transfers control to imperative-statement-2 "if specified"
      *>    — it is, so A=AAA / A=BBB / A=CCC print from the NOT AT END arm.
      *>  · §14.9.34.4 GR3: the fourth execution sets the at end condition, transfers to imperative-statement-1
      *>    (which sets the flag), and on return from it transfers to the end of the RETURN statement, so the
      *>    PERFORM UNTIL re-tests the flag and the loop ends. The record area is undefined once the at end
      *>    condition exists, so nothing after the loop reads S1-T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB350A85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF1 ASSIGN TO "pb350a85-w1".
       DATA DIVISION.
       FILE SECTION.
       SD SF1.
       01 S1-REC.
          05 S1-K PIC 9.
          05 S1-T PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-EOF1 PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-1.
           SORT SF1 ON ASCENDING KEY S1-K
               INPUT PROCEDURE IS FEED-1
               OUTPUT PROCEDURE IS DRAIN-1.
           DISPLAY "DONE".
           STOP RUN.
       FEED-1.
           MOVE 3 TO S1-K MOVE "CCC" TO S1-T RELEASE S1-REC.
           MOVE 1 TO S1-K MOVE "AAA" TO S1-T RELEASE S1-REC.
           MOVE 2 TO S1-K MOVE "BBB" TO S1-T RELEASE S1-REC.
       DRAIN-1.
           PERFORM UNTIL WS-EOF1 = "Y"
               RETURN SF1 RECORD
                   AT END MOVE "Y" TO WS-EOF1
                   NOT AT END DISPLAY "A=" S1-T
               END-RETURN
           END-PERFORM.
