      *>--- conformance-case
      *> id: pb412_go_to_general_formats
      *> rules: [FMT-14.9.17.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB412 — EVERY SPELLING §14.9.17.2's TWO GENERAL FORMATS ADMIT, end to end. The grammar rule
      *> `goToStatement` used to be the UNION of both formats with every element optional, so narrowing it to one
      *> alternative per printed format could have taken legal spellings with it; this is the over-rejection
      *> guard for that narrowing. Rendered from the canonical PDF page 660 / printed folio 630:
      *>   Format 1 (unconditional):  GO TO procedure-name-1
      *>   Format 2 (depending):      GO TO { procedure-name-1 } ... DEPENDING ON identifier-1
      *> Only GO and DEPENDING are underlined, so TO and ON are OPTIONAL words (ISO 5.2.3) and each is omitted at
      *> least once below. Exercised here: TO written and omitted; ON omitted; Format 2 with THREE
      *> procedure-names and with ONE; a SECTION target (14.9.17.4 GR1 — control enters its first paragraph and
      *> falls through into the next); a QUALIFIED procedure-name resolving past an in-section homonym (8.4.2.2);
      *> and 14.9.17.4 GR2's fall-through — "the value of identifier-1 is anything other than the positive or
      *> unsigned integers 1, 2, ... , n, then no transfer occurs and control passes to the next statement in the
      *> normal sequence for execution". The sentence after that Format-2 GO TO is legal precisely because
      *> 14.9.17.3 SR2's last-in-the-sequence rule names FORMAT 1 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB412FMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-SEL  PIC 9 VALUE 3.
       01 W-ONE  PIC 9 VALUE 1.
       01 W-OVER PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           DISPLAY "START".
           GO P-NOTO.
       P-NOTO.
           DISPLAY "NOTO".
           GO TO P-ONE P-TWO DEPENDING ON W-OVER.
           DISPLAY "GR2-FALLTHROUGH".
           GO TO P-SEL.
       P-SEL.
           GO TO P-ONE P-TWO P-THREE DEPENDING W-SEL.
       P-ONE.
           DISPLAY "ONE-WRONG".
           STOP RUN.
       P-TWO.
           DISPLAY "TWO-WRONG".
           STOP RUN.
       P-THREE.
           DISPLAY "THREE".
           GO TO P-SOLO DEPENDING ON W-ONE.
           DISPLAY "SOLO-NOT-TAKEN-WRONG".
           STOP RUN.
       P-SOLO.
           DISPLAY "SOLO".
           GO TO S-TWO.
       S-TWO SECTION.
       S-TWO-FIRST.
           DISPLAY "S-TWO-FIRST".
       S-TWO-SECOND.
           DISPLAY "S-TWO-SECOND".
           GO TO DUP-P IN S-THREE.
       DUP-P.
           DISPLAY "S-TWO-DUP-WRONG".
           STOP RUN.
       S-THREE SECTION.
       DUP-P.
           DISPLAY "S-THREE-DUP".
           STOP RUN.
