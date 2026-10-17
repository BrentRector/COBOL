      *>--- conformance-case
      *> id: l1_exit_paragraph_unnamed_paragraph
      *> rules: [SR-14.9.14.3-10]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.3 SR10 — "The EXIT statement with the PARAGRAPH
      *> phrase may be specified only in a paragraph."
      *> §14.4.3 defines a paragraph as "a paragraph-name followed by a
      *> separator period and by zero, one, or more successive sentences
      *> or, if the paragraph-name is omitted, one or more successive
      *> sentences following the procedure division header or a section
      *> header", so the UNNAMED sentences under a section header ARE a
      *> paragraph and SR10 admits EXIT PARAGRAPH among them. That is
      *> the one shape a too-narrow reading of SR10 would reject, and it
      *> is what the first half of this golden pins.
      *> Execution per §14.9.14.4 GR6: control passes to an implicit
      *> CONTINUE immediately following the last explicit statement of
      *> the CURRENT paragraph, so the rest of that paragraph is skipped
      *> while the next paragraph of the section still runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EXT02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-K PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       S1 SECTION.
      *> the paragraph-name-OMITTED paragraph of section S1 (§14.4.3)
           DISPLAY "UNNAMED-1".
           IF W-K = 1
               EXIT PARAGRAPH
           END-IF.
           DISPLAY "UNNAMED-2".
       P-NAMED.
           DISPLAY "NAMED-1".
           IF W-K = 1
               EXIT PARAGRAPH
           END-IF.
           DISPLAY "NAMED-2".
       P-LAST.
           DISPLAY "DONE".
           STOP RUN.
