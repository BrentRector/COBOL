      *>--- conformance-case
      *> id: pb260-accept-screen-at-phrase
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.1.3 SR5 — "Identifier-3 and identifier-4 shall be
      *> unsigned integer data items." Those two operands are the AT
      *> LINE NUMBER / COLUMN NUMBER positions of the ACCEPT statement's
      *> Format 3 (§14.9.1.2; the measured figure notes record that AT
      *> and NUMBER are optional words, that the LINE/COLUMN brace
      *> carries choice indicators, and that the whole AT phrase sits
      *> inside one outer bracket). This is the ONLY source shape that
      *> can carry them: an ACCEPT with the AT phrase OMITTED writes
      *> neither operand, so it measures nothing about SR5.
      *> WS-L is PIC S9(3) — SIGNED — so this source is exactly the
      *> shape SR5 forbids, a description-level violation SR5 would
      *> diagnose if the format existed.
      *> Format 3 is Annex A.4.2 item 1, an optional element this
      *> implementation DECLINES (docs/CONFORMANCE.md §4 item 4), with
      *> A.4.1 carrying the licence from the format to SR5; per §5's
      *> preamble a Not-claimed module's syntax is not accepted, so
      *> "a parse error or a named error is the conforming posture".
      *> WITNESS: the AT phrase parses (acceptStatement's screenTail)
      *> and the binder refuses Format 3 BY NAME, COBOLNET1707 "the
      *> ACCEPT statement's screen format 3" (ScreenFacility), so this
      *> source is REFUSED rather than silently reinterpreted as a
      *> Format-1 ACCEPT that drops the position operands. That refusal
      *> is what SR5's row closes on. ENABLED in the negative manifest;
      *> pb260-accept-screen-at-phrase.err holds the measured substring.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SCRW04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(4) VALUE "ABCD".
       01 WS-L PIC S9(3) VALUE 1.
       01 WS-C PIC 9(3) VALUE 1.
       SCREEN SECTION.
       01 SG.
          05 SI-1 LINE 1 COL 1 PIC X(4) TO WS-A.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT SG AT LINE NUMBER WS-L COLUMN NUMBER WS-C
             END-ACCEPT.
           STOP RUN.
