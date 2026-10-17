      *>--- conformance-case
      *> id: pb807-display-directive-concatenation
      *> rules: [SR-7.3.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 7.3.3 SR10: a literal in a compiler directive shall not be
      *> specified as a concatenation expression; the DISPLAY directive
      *> reads its literals by the same rule as DEFINE (kb/Work PB807).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807C.
000300 PROCEDURE DIVISION.
000400 >>DISPLAY "A" & "B"
000500     STOP RUN.
