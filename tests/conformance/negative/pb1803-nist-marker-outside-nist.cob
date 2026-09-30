      *> reject-at: 85 2002 2014 2023
      * ISO/IEC 1989:2023 6.3.3 "The indicator area identifies the type
      * of a source line in accordance with the indicators specified in
      * 6.2.2". The CCVS archive marker below has 'R' in position 7.
      * kb/Work PB1803: it was blanked on EVERY compilation, so the
      * line was discarded silently instead of diagnosed; the strip is
      * the --nist dialect's (the line-entry stage, PhysicalLines).
*HEADER,COBOL,PB1803
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1803-NEG-MARKER.
000300 PROCEDURE DIVISION.
000400     DISPLAY "M".
000500     STOP RUN.
