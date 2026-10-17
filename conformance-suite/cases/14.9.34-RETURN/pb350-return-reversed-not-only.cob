*>--- conformance-case
*> id: pb350-return-reversed-not-only
*> rules: [FMT-14.9.34.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.34.2 with §14.9.34.3 SR4 — the REVERSED-ORDER arm, written with only its NOT AT END half.
      *> SR4 says "The AT END phrase and the NOT AT END phrase, when specified, may be written in reversed
      *> order": it permits REVERSING the two phrases, and "when specified" is a condition on the reversal,
      *> not a licence to omit either one. The AT END line of the general format is still unbracketed, so it
      *> is still mandatory (§5.2.6.2), and this program is rejected at every edition.
      *> ⛔ THIS IS THE SECOND GRAMMAR ARM, and the reason the screen lives in the binder rather than in
      *> returnAtEndPhrase: `NOT AT? END statementBlock (AT? END statementBlock)?` made the AT END HALF of the
      *> reversed pair optional too, so a screen that only asked whether the PHRASE NODE was present would
      *> have left this arm compiling (CLAUDE.md rule 4 — the two-arm dispatch with one arm fixed). The bind
      *> screen asks instead whether the AT END BLOCK is null after PhraseBlocks.Split has normalized position
      *> away, which is one test covering both arms.
      *> The legal complement — the SAME reversal with BOTH halves — is exercised by DRAIN-2 of
      *> conformance-suite/cases/14.9.34-RETURN/pb350_return_at_end_required.cob and must keep compiling.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB350N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb350n2-wk".
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
           RETURN SF NOT AT END DISPLAY "R=" S-T END-RETURN.
           ADD 1 TO WS-N.
           IF WS-N < 4 GO TO DRAIN-1.
