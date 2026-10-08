      *> kb/Work PB2506. ISO 12.3.7.4 GR14 a): under DECIMAL-POINT IS
      *> COMMA "the character written in numeric literals to represent
      *> the decimal separator shall be the comma"; 8.3.3.3.2 2): "If a
      *> sign is used, it shall appear as the leftmost character of the
      *> literal"; 8.3.5 2): a comma is a separator only when
      *> "immediately followed by a space". So -1,5 +1,5 and -,5 are
      *> each ONE signed literal and ONE argument. Before the fix the
      *> sign and the integer part were lexed as one signed integer and
      *> the ,5 became a second argument: MIN(-1,5 2) was MIN(-1, 0,5,
      *> 2) = -1, MAX(+1,5) was MAX(+1, 0,5) = 1, ORD-MAX(-1,5 -2,5)
      *> was ORD-MAX(-1, 0,5, -2, 0,5) = 2. The results are MOVEd to an
      *> edited item so the comma image is the PICTURE's (PB2507 is the
      *> separate DISPLAY image of a function result).
      *> EXPECTED, by the rule, one line each:
      *>   -1,5 (MIN(-1,5 2))      2,0 (MAX(2 -1,5))
      *>    1,5 (MAX(+1,5))       -0,5 (MIN(-,5 1))
      *>   -1,5 (MIN(3, -1,5))       1 (ORD-MAX(-1,5 -2,5))
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2506A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC -9,9.
       01 N PIC 9.
       PROCEDURE DIVISION.
           MOVE FUNCTION MIN(-1,5 2) TO E
           DISPLAY E
           MOVE FUNCTION MAX(2 -1,5) TO E
           DISPLAY E
           MOVE FUNCTION MAX(+1,5) TO E
           DISPLAY E
           MOVE FUNCTION MIN(-,5 1) TO E
           DISPLAY E
           MOVE FUNCTION MIN(3, -1,5) TO E
           DISPLAY E
           MOVE FUNCTION ORD-MAX(-1,5 -2,5) TO N
           DISPLAY N
           STOP RUN.
