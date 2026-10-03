      *> kb/Work PB1890 - a subscript or reference-modifier bound that is an
      *> EXPRESSION (or a floating-point item) is tested for integrality on the
      *> value the intermediate HOLDS, never on a copy stored at a fixed width.
      *> ISO 8.4.2.3.4 1) b): "If the evaluation of arithmetic-expression-1 does
      *> not result in an integer, the EC-BOUND-SUBSCRIPT exception condition is
      *> set to exist." 8.4.3.3.4 5) names EC-BOUND-REF-MOD for a
      *> leftmost-position or length that "results in a non-integer value".
      *> 5.5 3) a) leaves "when the operand represents an integer" to the
      *> implementor in native arithmetic; DOC-A.1-124 defines it as the exact
      *> value having no nonzero digit right of the point.
      *> MEASURED BEFORE THE FIX: the value was stored into the 15.4 temporary
      *> (21 integer + 9 fraction digits, TRUNCATION) and tested afterwards, so a
      *> fraction past the 9th digit vanished: T(F) with F COMP-2 2.0000000001,
      *> T(IX + 0.0000000001) and W(1 + 0.0000000001:2) all selected a position
      *> with no condition. Now each raises, the declarative reports it, and
      *> RESUME AT NEXT STATEMENT leaves the receiver unchanged ("77" / "??").
      *> The integral controls (2.0 as COMP-2, IX + 0, 1 + 2 / 2) still select.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1890EXACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F COMP-2 VALUE 2.0000000001.
       01 F2 COMP-2 VALUE 2.0.
       01 TB.
          05 T PIC 9 OCCURS 3.
       01 IX PIC 9 VALUE 2.
       01 W PIC X(5) VALUE "ABCDE".
       01 R PIC 9(2) VALUE 77.
       01 U PIC X(2) VALUE "??".
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
           MOVE "123" TO TB.
      *> 1 - a floating-point item whose fraction lies past 9 digits.
           MOVE T (F) TO R.
           DISPLAY "FLOAT-SUB=" R.
      *> 2 - an expression whose fraction lies past 9 digits.
           MOVE T (IX + 0.0000000001) TO R.
           DISPLAY "EXPR-SUB=" R.
      *> 3 - the same expression as a ref-mod leftmost-position.
           MOVE W (1 + 0.0000000001:2) TO U.
           DISPLAY "EXPR-RM=[" U "]".
      *> 4 - integral controls on the same routes.
           MOVE T (F2) TO R.
           DISPLAY "FLOAT-SUB-INT=" R.
           MOVE T (IX + 0) TO R.
           DISPLAY "EXPR-SUB-INT=" R.
           MOVE W (1 + 2 / 2:2) TO U.
           DISPLAY "EXPR-RM-INT=[" U "]".
           STOP RUN.
