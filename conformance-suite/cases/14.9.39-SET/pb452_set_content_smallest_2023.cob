*>--- conformance-case
*> id: pb452_set_content_smallest_2023
*> rules: [GR-14.9.39.4-36]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 Annex D.32, third equivalence - "The statements  SET numeric-item TO NEAREST-TO-ZERO
      *> IN-ARITHMETIC-RANGE  and  MOVE SMALLEST-ALGEBRAIC (numeric-item) TO numeric-item  may be used to set the
      *> minimum nonzero value permitted for any numeric or numeric-edited item." kb/Work PB452.
      *>
      *> This is the 2023 half of the D.32 assertion: SET format 15 is a COBOL-2014 addition and the
      *> SMALLEST-ALGEBRAIC function a COBOL-2023 one, so the two can only be compared at 2023. The other two
      *> equivalences (FARTHEST-FROM-ZERO against HIGHEST-ALGEBRAIC, and with SIGN NEGATIVE against
      *> LOWEST-ALGEBRAIC) are asserted at the introducing edition in 2014/pb452_set_content_numeric.
      *>
      *> Expected values, computed from the descriptions rather than from a run (14.9.39.4 GR36 a / 15.83.4 r2 -
      *> the nonzero value nearest to zero is one unit in the last digit position, 10 to the minus scale):
      *>   S9(4)V99 -> 0.01, printed "+000001" through a LEADING SEPARATE sign (13.18.52.4 GR6 b pins '+'/'-').
      *>   S9(5)    -> 1,    printed "+00001".
      *> Under the default NATIVE arithmetic the intermediate is binary64, whose own nearest-to-zero magnitude is
      *> 4.94E-324; GR36 b takes whichever is FARTHER from zero, so each item's own value stands and the
      *> IN-ARITHMETIC-RANGE phrase changes nothing for a fixed-point receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452SETCONTENTSMALL23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SCALED PIC S9(4)V99 SIGN IS LEADING SEPARATE.
       01 WS-WHOLE  PIC S9(5)    SIGN IS LEADING SEPARATE.
       01 WS-FOLD   PIC S9(4)V99 SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CONTENT OF WS-SCALED WS-WHOLE TO NEAREST-TO-ZERO
                IN-ARITHMETIC-RANGE
           DISPLAY "A:" WS-SCALED
           DISPLAY "B:" WS-WHOLE
           MOVE FUNCTION SMALLEST-ALGEBRAIC(WS-FOLD) TO WS-FOLD
           IF WS-SCALED = WS-FOLD
               DISPLAY "C:SAME"
           ELSE
               DISPLAY "C:DIFF"
           END-IF
           STOP RUN.
