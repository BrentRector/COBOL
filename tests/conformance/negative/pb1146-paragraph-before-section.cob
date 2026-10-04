      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1146. ISO 14.4.1: "If one paragraph is in a section, all paragraphs shall be in sections",
      *> and 14.2.1 Format 1 (with-sections) prints nothing between the procedure division header and the
      *> first section header - so paragraph P0 ahead of section S1 is refused COBOLNET2797 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1146NA.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "A".
       S1 SECTION.
       P1.
           DISPLAY "B".
           STOP RUN.
