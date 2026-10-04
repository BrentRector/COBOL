      *> reject-at: 2002 2014 2023
      *> kb/Work PB1250 -- ISO 13.7.3 SR5 (cite.py --check 13.7.3 "A formal parameter of a function
      *> shall not be used as a receiving operand" -> OK 13.7.3 5)). ADD 1 TO P-X stores into the
      *> function's formal parameter P-X: COBOLNET2747. P-R, the RETURNING item, is not a formal
      *> parameter and COMPUTE P-R is legal (positive twin: 2002/pb1250_function_formal_read_only).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1250N1.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P-X     PIC 9(4).
       01  P-R     PIC 9(5).
       PROCEDURE DIVISION USING P-X RETURNING P-R.
           ADD 1 TO P-X.
           COMPUTE P-R = P-X * 2.
           GOBACK.
       END FUNCTION PB1250N1.
