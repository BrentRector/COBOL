*>--- conformance-case
*> id: pb2729-float-significand-point-argument
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2729 - a floating-point literal is "formed from two fixed-point numeric literals separated by
      *> the letter 'E'" (8.3.3.3.3 rule 1), and a fixed-point literal's decimal point "may appear anywhere within
      *> the literal except as the rightmost character" (8.3.3.3.2 rule 3), so the signed `-1.E2` in a
      *> keyword-omitted argument list is no literal: the lexer reads `-1` then a period, and a period with no
      *> space after it is no separator (8.3.5 rule 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2729ARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(5).
       PROCEDURE DIVISION.
           COMPUTE X = FUNCTION MAX(-1.E2 2)
           DISPLAY X
           STOP RUN.
