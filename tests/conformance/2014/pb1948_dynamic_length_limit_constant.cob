      *> kb/Work PB1948 - DYNAMIC LENGTH ... LIMIT IS integer-1 (ISO 13.18.19.2) names an integer-n, so an
      *>   integer constant-name stands there (13.10.3 SR2; 5.5 1); 13.10.4 GR1: "as if literal-1 ... were
      *>   written where constant-name-1 is written").
      *>   cite.py: OK  13.10.3 2)  (Syntax rules)
      *>   cite.py: OK  13.18.19.4 2)  (General rules)
      *> A receiving MOVE truncates on the RIGHT to the LIMIT with no padding (13.18.19.4 GR2 / 8.5.1.10.4),
      *> exactly as the written literal 5 does in the sibling conformance/2014/dynamic_length_limit: the
      *> constant KL = 5 gives L[ABCDE] and a length of 05. The LIMIT is read at FULL width (no host limit
      *> applies to it), so KBIG = 99999999999 (more than a host int) is a LIMIT too: the item is bounded
      *> by the implementor maximum instead (8.5.1.10.1, warning COBOLNET2027), and the whole value is kept.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1948DL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KL    CONSTANT AS 5.
       01 KBIG  CONSTANT AS 99999999999.
       01 WS-L  PIC X DYNAMIC LENGTH LIMIT IS KL.
       01 WS-B  PIC X DYNAMIC LENGTH LIMIT IS KBIG.
       01 WS-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDEFGHIJ" TO WS-L.
           DISPLAY "L[" WS-L "]".
           MOVE FUNCTION LENGTH(WS-L) TO WS-N.
           DISPLAY "LLEN=" WS-N.
           MOVE "ABCDEFGHIJ" TO WS-B.
           DISPLAY "B[" WS-B "]".
           MOVE FUNCTION LENGTH(WS-B) TO WS-N.
           DISPLAY "BLEN=" WS-N.
           STOP RUN.
