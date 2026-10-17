      *>--- conformance-case
      *> id: pb510_figurative_keyword_spellings
      *> rules: [FMT-13.18.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB510 - ZERO, ZEROS and ZEROES (and SPACE/SPACES, QUOTE/QUOTES ...) are DISTINCT
      *> reserved words (ISO 8.9), interchangeable only where the figurative constant itself is written:
      *> 8.3.3.6.2 format 1 lists "ZERO | ZEROES | ZEROS" as alternatives. Where a general format names
      *> ONE of them as a keyword (5.2.2 "They are required in order to select the functionality
      *> associated with that keyword") only that spelling is admitted - BLANK WHEN ZERO (13.18.8.2) and
      *> the sign condition "IS [NOT] { POSITIVE | NEGATIVE | ZERO }" (8.8.4.7.2). This is the POSITIVE
      *> half: every legal spelling, each at its legal position. The negatives are
      *> negative/pb510-blank-when-zeros and negative/pb510-sign-condition-zeroes.
      *> EXPECTED, from the rules (not measured):
      *>   13.18.8.4 GR1 - a BLANK WHEN ZERO item receiving zero is set to all spaces; WHEN is optional
      *>   (not underlined, 5.2.3), so BLANK ZERO is the same clause: B1 = B2 = 3 spaces. A nonzero value
      *>   edits normally: 7 -> 007.
      *>   8.8.4.7 - N1 (VALUE ZEROES = 0) IS ZERO is true; after COMPUTE N1 = ZEROES + 5 - ZEROS (the
      *>   figurative zero is a numeric literal zero in an arithmetic expression, 8.8.1.1) N1 = 5 and
      *>   IS NOT ZERO is true.
      *>   8.3.3.6.4 GR1 - MOVE ZEROS / ZEROES to PIC X(3) fills '000'; SPACE fills spaces; QUOTES fills '"'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB510P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 9(3) BLANK ZERO.
       01 B2 PIC 9(3) BLANK WHEN ZERO.
       01 N1 PIC S9(3) VALUE ZEROES.
       01 X1 PIC X(3) VALUE SPACES.
       01 X2 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO B1 B2
           DISPLAY "B1=[" B1 "] B2=[" B2 "]"
           MOVE 7 TO B1
           DISPLAY "B1=[" B1 "]"
           IF N1 IS ZERO
               DISPLAY "N1 IS ZERO"
           END-IF
           IF N1 = ZEROS AND N1 = ZEROES AND N1 = ZERO
               DISPLAY "N1 = ZEROS, ZEROES, ZERO"
           END-IF
           COMPUTE N1 = ZEROES + 5 - ZEROS
           IF N1 IS NOT ZERO
               DISPLAY "N1 IS NOT ZERO"
           END-IF
           MOVE ZEROS TO X1
           MOVE ZEROES TO X2
           DISPLAY "X1=[" X1 "] X2=[" X2 "]"
           MOVE SPACE TO X1
           MOVE QUOTES TO X2
           DISPLAY "X1=[" X1 "] X2=[" X2 "]"
           STOP RUN.
