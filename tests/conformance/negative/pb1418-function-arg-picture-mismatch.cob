      *> reject-at: 2002 2014 2023
      *> kb/Work PB1418 - ISO 8.4.3.2.3 SR13: "If function-prototype-name-1
      *> or function-pointer-name-1 is specified, the rules for conformance
      *> specified in 14.8.2, Parameters and 14.8.3, Returning items,
      *> apply." WX is an identifier permitted as a receiving operand, so
      *> 8.4.3.2.4 GR5 a) passes it BY REFERENCE, and 14.8.2.3.2 rule 2
      *> (whose list names "a function") requires the formal and the
      *> argument to "have the same ALIGN, BLANK WHEN ZERO, DYNAMIC
      *> LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses". PIC X(4)
      *> against PIC 9(4) differs -> COBOLNET2470. (Before the fix the
      *> function aliased WX's characters and printed X=0013.)
      *> cite.py --check 8.4.3.2.3 "the rules for conformance specified
      *>   in 14.8.2, Parameters and 14.8.3, Returning items, apply"
      *>   -> OK  8.4.3.2.3 13)  (Syntax rules)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1418PF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           MOVE L-X TO L-R
           GOBACK.
       END FUNCTION N1418PF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1418PM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1418PF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC X(4) VALUE "0013".
       01 WR PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE WR = FUNCTION N1418PF(WX)
           DISPLAY "X=" WR
           STOP RUN.
       END PROGRAM N1418PM.
