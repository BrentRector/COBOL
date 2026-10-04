      *> reject-at: 85 2002 2014 2023
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
