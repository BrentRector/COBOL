*>--- conformance-case
*> id: ec_bound_subscript
*> rules: [GR-8.4.2.3.4-2, GR-14.6.13.1.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.2.3.4 GR2: "The value of a subscript shall be a positive integer. … If the value of the
      *> subscript is not a positive integer or is less than one or is greater than the highest permissible
      *> occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist." Table 13: Fatal, so
      *> the declarative runs and RESUME AT NEXT STATEMENT (§14.9.33) keeps the run unit alive past it.
      *>
      *> T has 3 occurrences and the subscript is 5. With checking OFF the reference reads a scratch slot and
      *> continues — a conforming implementor choice, since §13.18.38.4 GR7 leaves out-of-bounds occurrence
      *> content undefined; what checking ON adds is the NAMED condition and the chance to handle it. Before
      *> this fix the out-of-range read was silent at every checking state.
      >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECBSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE T (IDX) TO R.
           DISPLAY "AFTER".
           STOP RUN.
