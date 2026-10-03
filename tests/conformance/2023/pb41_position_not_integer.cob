      *> ISO 8.4.2.3.4 GR1b / 8.4.3.3.4 rule 5)c - THE INTEGRALITY RULE, which is
      *> the reason the two positions carry an exception condition rather than a
      *> truncation (fix-queue PB41 / PB17 D18). GR1b: "If the evaluation of
      *> arithmetic-expression-1 does not result in an integer, the
      *> EC-BOUND-SUBSCRIPT exception condition is set to exist." Rule 5)c says the
      *> same for a leftmost-position or length and names EC-BOUND-REF-MOD - ONE
      *> rule shape, TWO condition names, which is why the position read is
      *> position-aware and not one helper that always says SUBSCRIPT.
      *>
      *> Under CHECKING ON the USE AFTER EXCEPTION CONDITION declarative selects
      *> (14.9.49), reports via FUNCTION EXCEPTION-STATUS (15.33), and RESUME AT
      *> NEXT STATEMENT (14.9.33) continues past the aborted MOVE - so the receiver
      *> keeps its previous content, which is what the AFTER- lines show.
      *>
      *> THE FUNCTION FORMS ARE THE LOAD-BEARING HALF. D18's execution order had
      *> specified the 15.4 temporary as Scale: 0; a scale-0 temp TRUNCATES on the
      *> way in, so FUNCTION SQRT(2) = 1.414... would have silently indexed
      *> occurrence 1 instead of raising, turning legal source into a wrong answer
      *> by the temp's own description. The function's value is tested on its own
      *> carrier BEFORE the integer position is stored (kb/Work PB1890).
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB41NOTINT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       01 W-A PIC X(9) VALUE "ABCDEFGHI".
       01 W-S PIC 9V9 VALUE 2.5.
       01 W-R PIC 9(2) VALUE 77.
       01 W-U PIC X(2) VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-SUB-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       H-RM SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-RM-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 22 TO W-E (2).
      *> 1 - a non-integer SCALED DATA-NAME subscript (2.5).
           MOVE W-E (W-S) TO W-R.
           DISPLAY "AFTER-SUB-NAME=" W-R.
      *> 2 - a non-integer NUMERIC FUNCTION subscript (SQRT(2) = 1.414...).
           MOVE W-E (FUNCTION SQRT(2)) TO W-R.
           DISPLAY "AFTER-SUB-FUNC=" W-R.
      *> 3 - a non-integer scaled ref-mod leftmost-position.
           MOVE W-A (W-S:2) TO W-U.
           DISPLAY "AFTER-RM-NAME=[" W-U "]".
      *> 4 - a non-integer FUNCTION ref-mod leftmost-position.
           MOVE W-A (FUNCTION SQRT(2):2) TO W-U.
           DISPLAY "AFTER-RM-FUNC=[" W-U "]".
           STOP RUN.
