      * kb/Work PB2734 - a grouping parenthesis around a CONDITION is
      * not an arithmetic context, so a ZERO beside it keeps its
      * figurative identity.  The token pass used to rewrite a ZERO
      * next to EITHER paren into the numeric literal 0, so
      * IF (WS-A = ZERO) over PIC X(3) VALUE "000" answered NOT EQUAL
      * where IF WS-A = ZERO answers EQUAL, and IF (WS-N IS ZERO)
      * failed to parse.
      *
      * 8.3.3.6.4 4): "The zero format represents the numeric value
      *   '0', ... or one or more of the character '0' in the
      *   computer's runtime coded character set, depending on
      *   context."  (cite.py: OK 8.3.3.6.4 4))
      * 8.3.3.6.4 2) NOTE 1: a figurative constant is associated with
      *   a data item when it is "compared with it".  (cite.py: OK)
      *   Against WS-A (alphanumeric, 3 characters) ZERO is "000";
      *   against WS-B (VALUE "0  ") it is "000" too, so NOT EQUAL.
      * 8.8.4.9: the truth value of a complex condition is the same
      *   "whether parenthesized or not".  (cite.py: OK 8.8.4.9)
      * 8.8.1.1: (ZERO) alone in parentheses IS an arithmetic
      *   expression, the numeric value 0 - legs A9/A10 keep that.
      *
      *   A1  IF WS-A = ZERO              EQUAL (the unparenthesized
      *                                   reference answer)
      *   A2  IF (WS-A = ZERO)            EQUAL
      *   A3  IF (ZERO = WS-A)            EQUAL
      *   A4  IF NOT (WS-A NOT = ZEROS)   EQUAL
      *   A5  IF (WS-A = "1" OR ZEROES)   EQUAL (abbreviated object)
      *   A6  IF (WS-B = ZERO)            NOT EQUAL ("0  " is not
      *                                   "000"; the literal 0 would
      *                                   have padded to "0  ")
      *   A7  IF (WS-N IS ZERO)           ZERO (the sign condition)
      *   A8  PERFORM UNTIL (WS-A = ZEROS) OR CNT > 2  - no iteration
      *   A9  COMPUTE WS-N = (ZERO) + 5   5
      *   A10 IF (WS-N = (ZERO) + 5)      EQUAL
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2734ZP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(3) VALUE "000".
       01 WS-B PIC X(3) VALUE "0  ".
       01 WS-N PIC 9(3) VALUE 0.
       01 CNT  PIC 9    VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-A = ZERO DISPLAY "A1 EQUAL"
           ELSE DISPLAY "A1 NOT EQUAL" END-IF.
           IF (WS-A = ZERO) DISPLAY "A2 EQUAL"
           ELSE DISPLAY "A2 NOT EQUAL" END-IF.
           IF (ZERO = WS-A) DISPLAY "A3 EQUAL"
           ELSE DISPLAY "A3 NOT EQUAL" END-IF.
           IF NOT (WS-A NOT = ZEROS) DISPLAY "A4 EQUAL"
           ELSE DISPLAY "A4 NOT EQUAL" END-IF.
           IF (WS-A = "1" OR ZEROES) DISPLAY "A5 EQUAL"
           ELSE DISPLAY "A5 NOT EQUAL" END-IF.
           IF (WS-B = ZERO) DISPLAY "A6 EQUAL"
           ELSE DISPLAY "A6 NOT EQUAL" END-IF.
           IF (WS-N IS ZERO) DISPLAY "A7 ZERO"
           ELSE DISPLAY "A7 NOT ZERO" END-IF.
           PERFORM UNTIL (WS-A = ZEROS) OR CNT > 2
               ADD 1 TO CNT
           END-PERFORM.
           DISPLAY "A8 " CNT.
           COMPUTE WS-N = (ZERO) + 5.
           DISPLAY "A9 " WS-N.
           IF (WS-N = (ZERO) + 5) DISPLAY "A10 EQUAL"
           ELSE DISPLAY "A10 NOT EQUAL" END-IF.
           STOP RUN.
