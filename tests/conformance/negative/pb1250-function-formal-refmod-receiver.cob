      *> reject-at: 2002 2014 2023
      *> kb/Work PB1250 -- ISO 13.7.3 SR5 (cite.py --check 13.7.3 "A formal parameter of a function
      *> shall not be used as a receiving operand" -> OK 13.7.3 5)). MOVE 3 TO P-X(1:1) stores
      *> into a reference-modified part of the formal parameter P-X, which is the parameter's own
      *> storage: COBOLNET2747.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1250N2.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P-X     PIC 9(4).
       01  P-R     PIC 9(5).
       PROCEDURE DIVISION USING P-X RETURNING P-R.
           MOVE 3 TO P-X (1:1).
           MOVE P-X TO P-R.
           GOBACK.
       END FUNCTION PB1250N2.
