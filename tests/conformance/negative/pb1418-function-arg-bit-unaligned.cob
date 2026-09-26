      *> reject-at: 2002 2014 2023
      *> kb/Work PB1418 - ISO 8.4.3.2.3 SR14: "If function-prototype-name-1
      *> or function-pointer-name-1 is specified and the formal parameter
      *> corresponding to argument-1 is specified with the BY REFERENCE
      *> phrase ... and argument-1 is a bit data item, argument-1 shall be
      *> described such that it is aligned on a byte boundary". B2 follows
      *> the one-bit B1 in a GROUP-USAGE BIT group, so it starts at bit 1
      *> of its record -> COBOLNET2471. The formal has B2's own description
      *> (PIC 1(3) USAGE BIT), so 14.8.2.3.2 rule 2 is met and the
      *> alignment rule is the only violation.
      *> cite.py --check 8.4.3.2.3 "argument-1 shall be described such
      *>   that it is aligned on a byte boundary" -> OK  8.4.3.2.3 14)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1418BF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 1(3) USAGE BIT.
       01 L-R PIC X(3).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           MOVE L-X TO L-R
           GOBACK.
       END FUNCTION N1418BF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1418BM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1418BF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GROUP-USAGE BIT.
          05 B1 PIC 1 VALUE B"0".
          05 B2 PIC 1(3) VALUE B"101".
       01 WR PIC X(3).
       PROCEDURE DIVISION.
           MOVE FUNCTION N1418BF(B2) TO WR
           DISPLAY "R=" WR
           STOP RUN.
       END PROGRAM N1418BM.
