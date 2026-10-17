      *>--- conformance-case
      *> id: pb1360-display-directive-continued-fixed
      *> rules: [SR-7.3.3-1, SR-7.3.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 7.3.12.3 SR1: 'The DISPLAY directive shall begin on a new line
      *> and shall be specified entirely on that line.' The >>DISPLAY
      *> below is continued with a column-7 hyphen; the continuation
      *> line is refused by name (kb/Work PB1360, 7.3.3 SR1).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1360DD.
000300 PROCEDURE DIVISION.
000400 >>DISPLAY "AAAA"
000500-    "BBB"
000600     STOP RUN.
