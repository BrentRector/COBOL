      *> reject-at: 2002 2014 2023
      *> kb/Work PB1418 - ISO 14.8.2.3.3 rule 2 a): "If the formal parameter
      *> is numeric, the conformance rules are the same as for a COMPUTE
      *> statement with the argument as the sending operand and the
      *> corresponding formal parameter as the receiving operand", and
      *> 8.8.1.1 admits "a numeric literal, the figurative constant ZERO"
      *> as COMPUTE operands - never a nonnumeric literal. The literal
      *> "ABCD" crosses BY CONTENT (8.4.3.2.4 GR5 b) into the PIC 9(4)
      *> formal L-X, and 8.4.3.2.3 SR13 imports 14.8.2 -> COBOLNET2470.
      *> (Before the fix it compiled and the function saw 0000.)
      *> cite.py --check 14.8.2.3.3 "If the formal parameter is numeric,
      *>   the conformance rules are the same as for a COMPUTE statement"
      *>   -> OK  14.8.2.3.3 2) a)
      *> cite.py --check 8.8.1.1 "a numeric literal, the figurative
      *>   constant ZERO" -> OK  8.8.1.1
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1418LF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           MOVE L-X TO L-R
           GOBACK.
       END FUNCTION N1418LF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1418LM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1418LF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WR PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE WR = FUNCTION N1418LF("ABCD")
           DISPLAY "N=" WR
           STOP RUN.
       END PROGRAM N1418LM.
