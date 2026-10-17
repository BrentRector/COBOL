      *>--- conformance-case
      *> id: pb1494-indicator-not-fixed
      *> rules: [SR-6.3.4-2, GR-6.5-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * ISO/IEC 1989:2023 6.3.3 "The indicator area identifies the type
      * of a source line in accordance with the indicators specified in
      * 6.2.2" - which lists *, / (comment), - (continuation) and space
      * (source). kb/Work PB1494: an 'X' there was silently compiled as
      * source and an 'E' silently dropped, at every edition, because the
      * NIST CCVS column-7 conventions were applied without --nist.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1494-NEG-INDICATOR.
000300 PROCEDURE DIVISION.
000400     DISPLAY "LINE1".
000500X    DISPLAY "X-LINE".
000600     STOP RUN.
