*>--- conformance-case
*> id: pb1146-sentence-before-section
*> rules: [FMT-14.2.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1146. ISO 14.2.1 Format 1 (with-sections) prints nothing between the procedure division
      *> header and the first section header, and Format 2 (without-sections) has no section at all - so a
      *> sentence written before section S1 matches neither format and is refused COBOLNET2797.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1146NB.
       PROCEDURE DIVISION.
           DISPLAY "A".
       S1 SECTION.
       P1.
           DISPLAY "B".
           STOP RUN.
