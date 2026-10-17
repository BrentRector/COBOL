*>--- conformance-case
*> id: pb350_return_at_end_required
*> rules: [FMT-14.9.34.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.34.2 — RETURN's AT END phrase is MANDATORY, and the two shapes that keep it so are legal.
      *> The general format (rendered from the printed page, folio 708) writes
      *>     RETURN file-name-1 RECORD [ INTO identifier-1 ]
      *>         AT END imperative-statement-1
      *>         [ NOT AT END imperative-statement-2 ]
      *>         [ END-RETURN ]
      *> — the AT END line carries NO brackets while the two lines under it do, and §5.2.6.2 makes brackets
      *> the only convention that lets a portion of a general format be omitted. This is the ACCEPT side of
      *> COBOLNET1850 (kb/Work PB350): both a bare AT END and the §14.9.34.3 SR4 REVERSED pair still compile,
      *> because the screen keys on the AT END phrase being ABSENT, never on the order it is written in.
      *>
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · §14.9.40.4 GR8 a): "If the contents of the corresponding key data items are not equal and the key
      *>    is associated with the ASCENDING phrase, the record containing the key data item with the lower
      *>    value is returned first" — released 3/CCC, 1/AAA, 2/BBB, so every drain returns AAA, BBB, CCC.
      *>  · §14.9.34.4 GR4: with no at end condition "control is transferred to imperative-statement-2, if
      *>    specified; otherwise, control is transferred to the end of the RETURN statement". DRAIN-1 writes
      *>    NO NOT AT END phrase, so its three successful returns fall to the end of the statement and the
      *>    guarded DISPLAY after END-RETURN prints A=AAA / A=BBB / A=CCC. DRAIN-2 writes one, in SR4's
      *>    reversed order, so its three successful returns transfer INTO it and print B=AAA / B=BBB / B=CCC.
      *>  · §14.9.34.4 GR3: on the fourth execution "the at end condition is set to exist and control is
      *>    transferred to imperative-statement-1 of the AT END phrase. If control is returned from
      *>    imperative-statement-1, control is then transferred to the end of the RETURN statement" — so each
      *>    AT END sets its flag, the PERFORM UNTIL re-tests it and the loop ends. The same rule says the
      *>    record area is UNDEFINED once the at end condition exists, which is why DRAIN-1's DISPLAY is
      *>    guarded by the flag and DRAIN-2 reads the record only from the NOT AT END arm.
      *>  · Six R-lines then DONE, in that order, because the two SORT statements execute in written order
      *>    (§14.9.40.4 GR4 — the output procedure runs to its end before the SORT statement completes).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB350AE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF1 ASSIGN TO "pb350ae-w1".
           SELECT SF2 ASSIGN TO "pb350ae-w2".
       DATA DIVISION.
       FILE SECTION.
       SD SF1.
       01 S1-REC.
          05 S1-K PIC 9.
          05 S1-T PIC X(3).
       SD SF2.
       01 S2-REC.
          05 S2-K PIC 9.
          05 S2-T PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-EOF1 PIC X VALUE "N".
       01 WS-EOF2 PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-1.
           SORT SF1 ON ASCENDING KEY S1-K
               INPUT PROCEDURE IS FEED-1
               OUTPUT PROCEDURE IS DRAIN-1.
           SORT SF2 ON ASCENDING KEY S2-K
               INPUT PROCEDURE IS FEED-2
               OUTPUT PROCEDURE IS DRAIN-2.
           DISPLAY "DONE".
           STOP RUN.
       FEED-1.
           MOVE 3 TO S1-K MOVE "CCC" TO S1-T RELEASE S1-REC.
           MOVE 1 TO S1-K MOVE "AAA" TO S1-T RELEASE S1-REC.
           MOVE 2 TO S1-K MOVE "BBB" TO S1-T RELEASE S1-REC.
       DRAIN-1.
      *> The MINIMAL legal form: AT END alone, no NOT AT END, no reversal.
           PERFORM UNTIL WS-EOF1 = "Y"
               RETURN SF1 RECORD
                   AT END MOVE "Y" TO WS-EOF1
               END-RETURN
               IF WS-EOF1 = "N"
                   DISPLAY "A=" S1-T
               END-IF
           END-PERFORM.
       FEED-2.
           MOVE 3 TO S2-K MOVE "CCC" TO S2-T RELEASE S2-REC.
           MOVE 1 TO S2-K MOVE "AAA" TO S2-T RELEASE S2-REC.
           MOVE 2 TO S2-K MOVE "BBB" TO S2-T RELEASE S2-REC.
       DRAIN-2.
      *> §14.9.34.3 SR4 — the AT END and NOT AT END phrases written in REVERSED order. The AT END phrase is
      *> present, so COBOLNET1850 must NOT fire: the screen reads the phrase's PRESENCE, not its position.
           PERFORM UNTIL WS-EOF2 = "Y"
               RETURN SF2 RECORD
                   NOT AT END DISPLAY "B=" S2-T
                   AT END MOVE "Y" TO WS-EOF2
               END-RETURN
           END-PERFORM.
