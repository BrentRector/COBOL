      *> reject-at: 2002 2014 2023
      *> kb/Work PB1418 - the CALL arm of the shared 14.8.2 argument rules.
      *> ISO 14.8.2.3.3 rule 2 a): "If the formal parameter is numeric, the
      *> conformance rules are the same as for a COMPUTE statement with the
      *> argument as the sending operand", and 8.8.1.1 admits "a numeric
      *> literal, the figurative constant ZERO" as COMPUTE operands - never
      *> a nonnumeric literal. 14.9.4.3 SR25 imports 14.8.2 into the NESTED
      *> CALL, so BY CONTENT "ABCD" into the PIC 9(4) formal is
      *> COBOLNET1688. (Before the fix the MOVE rule of 2 d) was asked
      *> instead, the call compiled, and the callee saw 0000.)
      *> cite.py --check 14.8.2.3.3 "If the formal parameter is numeric,
      *>   the conformance rules are the same as for a COMPUTE statement"
      *>   -> OK  14.8.2.3.3 2) a)
      *> cite.py --check 14.9.4.3 "The rules for conformance specified in
      *>   14.8.2, Parameters and 14.8.3, Returning items" -> OK 14.9.4.3 25)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1418CM.
       PROCEDURE DIVISION.
           CALL "N1418CS" AS NESTED USING BY CONTENT "ABCD"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1418CS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "X=" L-X
           GOBACK.
       END PROGRAM N1418CS.
       END PROGRAM N1418CM.
