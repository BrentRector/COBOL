      *> PB2643 - the de-edit of a floating-point numeric-edited SENDER (14.9.25.4 GR5) under EC-DATA-INCOMPATIBLE
      *> checking. (1) 13.18.8.4 GR3: an all-spaces sender is "considered to be zero", so a BLANK WHEN ZERO item
      *> holding zero (13.18.8.4 GR1 stored it as all spaces) de-edits to 0 with no exception, to a numeric
      *> receiver and to a floating-point edited receiver. (2) 14.6.13.2 rule 4: content that is
      *> "not a possible result for any editing operation in that data item" raises the exception, and 14.6.8.4 GR1
      *> makes a nonzero value's significand lead with a nonzero digit, so +0.12E+03 is no editing result of
      *> +9.99E+99; nor is a zero with a nonzero exponent, a BLANK WHEN ZERO item holding the zero image instead of
      *> spaces, or all spaces in an item without BLANK WHEN ZERO. The exception is handled by a USE declarative
      *> and RESUME; the receiver is unchanged. The images are planted through REDEFINES.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2643A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FE PIC +9.99E+99 BLANK WHEN ZERO.
       01 FE-X REDEFINES FE PIC X(9).
       01 FX PIC +9.99E+99.
       01 FX-X REDEFINES FX PIC X(9).
       01 FR PIC +9.99E+99.
       01 N PIC S9(5)V99 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO FE
           DISPLAY "blanked [" FE "]"
           MOVE 7 TO N
           MOVE FE TO N
           DISPLAY "blank BWZ to numeric N=" N
           MOVE 5 TO FR
           MOVE FE TO FR
           DISPLAY "blank BWZ to float-edited FR=" FR
           MOVE 120 TO FE
           MOVE FE TO N
           DISPLAY "normal 120 N=" N " from [" FE "]"
           MOVE 7 TO N
           MOVE "+0.12E+03" TO FX-X
           MOVE FX TO N
           DISPLAY "non-normalized N=" N " (unchanged)"
           MOVE "+0.00E+05" TO FX-X
           MOVE FX TO N
           DISPLAY "zero with exponent N=" N " (unchanged)"
           MOVE "+0.00E+00" TO FE-X
           MOVE FE TO N
           DISPLAY "BWZ item holding zero image N=" N " (unchanged)"
           MOVE SPACES TO FX-X
           MOVE FX TO N
           DISPLAY "spaces without BWZ N=" N " (unchanged)"
           MOVE "+0.00E+00" TO FX-X
           MOVE FX TO N
           DISPLAY "zero image N=" N
           STOP RUN.
