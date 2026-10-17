      *>--- conformance-case
      *> id: dns_validate_picture_locale_witness
      *> rules: [GR-13.18.40.4-19]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.40.4 GR19 (PICTURE format 2) — "The PICTURE clause
      *> takes effect during the format validation stage of the
      *> execution of a VALIDATE statement that refers directly or
      *> indirectly to the subject of the entry. The validation is
      *> carried out in accordance with the character classification and
      *> monetary specification in the locale."
      *> CITE GR19 BY BOTH SENTENCES: §13.18.40.4 rule 15 (FORMAT 1)
      *> opens with the same first sentence, so a shortened quote
      *> resolves to the Format-1 twin. The locale sentence is unique
      *> to rule 19, which sits under the printed FORMAT 2 heading.
      *> The rule's whole trigger is the VALIDATE statement, and the
      *> VALIDATE facility is DECLINED: OPTIONAL (§4.2.7, Annex A.4.14)
      *> and, at COBOL-2023, also OBSOLETE (§4.2.13, Annex F.2 item 5) —
      *> docs/CONFORMANCE.md §4 item 3. No format validation stage ever
      *> executes, so GR19 has no occasion to apply.
      *> WITNESS: the named COBOLNET1580 non-support warning, which is
      *> the WHOLE discriminating evidence (pinned by
      *> DocumentedNonSupportWitnessTests). The .out pins INERTNESS
      *> ONLY — control passes through the VALIDATE and N reaches 1.
      *> AN UNCHANGED SUBJECT IS NOT EVIDENCE THAT NO FORMAT VALIDATION
      *> STAGE RAN. §14.9.50.4 GR5 — "the execution of the VALIDATE
      *> statement does not terminate and the content of the invalid
      *> data item does not change" — and §13.18.17.4 GR1 — "The data
      *> item itself remains unchanged" — make an unchanged subject the
      *> CONFORMING outcome too, and §14.9.50.4 GR6 writes into an
      *> operand only through a DESTINATION clause (stage two) and a
      *> VALIDATE-STATUS item (stage five), neither of which this entry
      *> declares. A behavioural discriminator would have to be GR6
      *> e)'s EC-VALIDATE-FORMAT under >>TURN EC-VALIDATE CHECKING ON,
      *> never the subject's content. M is compared against its own
      *> saved image rather than displayed, so the expectation pins
      *> SHAPE-independence, not a locale's currency string.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VALW01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M      PIC $9 LOCALE IS US SIZE IS 2.
       01 M-SAVE PIC X(2).
       01 WS-N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO M.
           MOVE M TO M-SAVE.
           VALIDATE M.
           ADD 1 TO WS-N.
           IF M = M-SAVE
               DISPLAY "SUBJECT UNCHANGED"
           ELSE
               DISPLAY "SUBJECT CHANGED"
           END-IF.
           DISPLAY "N=" WS-N.
           STOP RUN.
