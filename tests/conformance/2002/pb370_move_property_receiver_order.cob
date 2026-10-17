      *> ISO §14.9.49.4 GR11: "The exception condition that is used to
      *> determine which USE statement to select is the first one that
      *> occurs in the order of evaluation of the statement that causes
      *> the exception to exist." For a MOVE the order is §14.9.25.4
      *> GR1's: identifier-1's "reference modifier, subscript, or
      *> function-identifier is evaluated only once, immediately before
      *> data is moved to the first of the receiving operands", and
      *> "Item identification for identifier-2 is performed immediately
      *> before the data is moved to the respective data item". This is
      *> pb370_move_first_condition's question asked of an OBJECT
      *> PROPERTY receiver (§8.4.3.9; kb/Work PB370), whose
      *> identification evaluates identifier-3 - its subscript, a
      *> function in that subscript - and, for a reference-modified
      *> property, invokes its get method for the part the MOVE keeps
      *> (§8.4.3.3.4 GR5). All of it comes after the sender. Every leg
      *> makes both operands raise a fatal condition, so the
      *> declarative that runs names the operand evaluated first.
      *>   K  MOVE X (1:L) TO P OF R (FUNCTION SQRT (NEG)): identifier-3's
      *>      subscript is a function outside its domain
      *>      (EC-ARGUMENT-FUNCTION); the sender's EC-BOUND-REF-MOD is
      *>      first.
      *>   L  MOVE X (1:L) TO P OF R (I): identifier-3's subscript is out
      *>      of range (EC-BOUND-SUBSCRIPT); EC-BOUND-REF-MOD is first.
      *>   M  MOVE P OF R (I) TO X (1:L): the mirror - the sending
      *>      property's EC-BOUND-SUBSCRIPT.
      *>   N  MOVE X (1:L) TO Q OF R (1) (1:2): Q's get method DISPLAYs
      *>      "GET Q"; the sender's EC-BOUND-REF-MOD is selected and the
      *>      statement ends there, so the get method never runs.
      *> Each fatal declarative resumes at the next statement. The
      *> control leg moves the same shapes in range and shows the
      *> values arrive: P of the second object is ABCD, and Q keeps
      *> what its get method supplied past the slice (ABYZ).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370MP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB370PC
           PROPERTY P
           PROPERTY Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 R USAGE OBJECT REFERENCE PB370PC OCCURS 3.
       01 RR TYPE RT.
       01 X  PIC X(8) VALUE "ABCDEFGH".
       01 I  PIC 99 VALUE 9.
       01 L  PIC 99 VALUE 50.
       01 NEG PIC S99 VALUE -4.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-SUB-P.
           DISPLAY "  SELECTED EC-BOUND-SUBSCRIPT"
           RESUME AT NEXT STATEMENT.
       D-REF SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       D-REF-P.
           DISPLAY "  SELECTED EC-BOUND-REF-MOD"
           RESUME AT NEXT STATEMENT.
       D-ARG SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       D-ARG-P.
           DISPLAY "  SELECTED EC-ARGUMENT-FUNCTION"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-BOUND EC-ARGUMENT CHECKING ON
           INVOKE PB370PC "NEW" RETURNING R (1)
           INVOKE PB370PC "NEW" RETURNING R (2)
           INVOKE PB370PC "NEW" RETURNING R (3)
           DISPLAY "K MOVE X (1:L) TO P OF R (FUNCTION SQRT (NEG))"
           MOVE X (1:L) TO P OF R (FUNCTION SQRT (NEG))
           DISPLAY "L MOVE X (1:L) TO P OF R (I)"
           MOVE X (1:L) TO P OF R (I)
           DISPLAY "M MOVE P OF R (I) TO X (1:L)"
           MOVE P OF R (I) TO X (1:L)
           DISPLAY "N MOVE X (1:L) TO Q OF R (1) (1:2)"
           MOVE X (1:L) TO Q OF R (1) (1:2)
           MOVE 4 TO NEG
           MOVE 2 TO L
           MOVE X (1:L) TO Q OF R (1) (1:2)
           MOVE 4 TO L
           MOVE X (1:L) TO P OF R (FUNCTION SQRT (NEG))
           DISPLAY "CONTROL P = " P OF R (2) " Q = " Q OF R (1)
           STOP RUN.
       END PROGRAM PB370MP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB370PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X(4) VALUE "...." PROPERTY.
       01 QV PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC X(4).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           DISPLAY "  GET Q"
           MOVE QV TO LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC X(4).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           MOVE LK-V TO QV.
       END METHOD.
       END OBJECT.
       END CLASS PB370PC.
