      *> ISO §14.9.49.4 GR11: "The exception condition that is used to
      *> determine which USE statement to select is the first one that
      *> occurs in the order of evaluation of the statement that causes
      *> the exception to exist." For a MOVE the order is §14.9.25.4
      *> GR1's: identifier-1's "reference modifier, subscript, or
      *> function-identifier is evaluated only once, immediately before
      *> data is moved to the first of the receiving operands", and
      *> "Item identification for identifier-2 is performed immediately
      *> before the data is moved to the respective data item" - the
      *> sender is evaluated FIRST, as §14.6.4's left-to-right order
      *> also has it (kb/Work PB370). Every leg below makes BOTH
      *> operands raise a fatal condition, and the two conditions
      *> differ, so the declarative that runs names the operand that was
      *> evaluated first. Each leg is written in both orders.
      *>   A  MOVE X (1:L) TO T (I): the sender's reference modifier
      *>      (EC-BOUND-REF-MOD) before the receiver's subscript.
      *>   B  MOVE T (I) TO X (1:L): the sender's subscript
      *>      (EC-BOUND-SUBSCRIPT) before the receiver's slice.
      *>   C  MOVE N (I) TO O (1): the sender's subscript before the
      *>      receiver's occurs-depending reference, which §13.18.38.4
      *>      GR7 checks (EC-BOUND-ODO: D lies outside 1 TO 3).
      *>   D  MOVE O (1) TO N (I): the mirror - EC-BOUND-ODO.
      *>   E  MOVE X (1:L) TO T (I) (1:2): a receiving slice of a
      *>      subscripted element - the sender's EC-BOUND-REF-MOD.
      *>   F  MOVE X (1:L) TO T (FUNCTION SQRT (NEG)): the receiver's
      *>      subscript holds a function whose argument is outside its
      *>      domain (EC-ARGUMENT-FUNCTION); the sender's
      *>      EC-BOUND-REF-MOD is first.
      *>   G  MOVE FUNCTION SQRT (NEG) TO N (I): the mirror - the
      *>      sending function-identifier's EC-ARGUMENT-FUNCTION.
      *>   I  MOVE X (1:L) TO BX: BX is BASED with its address NULL, so
      *>      identifying it raises EC-DATA-PTR-NULL (§13.18.5.4 GR3);
      *>      the sender's EC-BOUND-REF-MOD is first.
      *>   J  MOVE BX TO X (1:L): the mirror - EC-DATA-PTR-NULL.
      *>   H  EVALUATE V WHEN "Z" THRU "A" ... WHEN T (I): the first
      *>      condition is NONFATAL - §14.7.8 2) sets EC-RANGE-INVALID
      *>      for a range whose start is greater than its end, and
      *>      "upon completion of any exception processing, execution
      *>      proceeds as if the range of values were empty" - so its
      *>      declarative runs first (§14.6.13.1.4 3)), and the second
      *>      WHEN's EC-BOUND-SUBSCRIPT runs the other one after it.
      *> Each fatal declarative resumes at the next statement, so a
      *> leg's second fatal condition is never reached. The control leg moves the
      *> same operand shapes in range and shows the values arrive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370MF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 NT.
          05 N PIC 9(4) OCCURS 3 TIMES.
       01 D  PIC 99 VALUE 9.
       01 OT.
          05 O PIC 9(4) OCCURS 1 TO 3 TIMES DEPENDING ON D.
       01 X  PIC X(8) VALUE "ABCDEFGH".
       01 I  PIC 99 VALUE 9.
       01 L  PIC 99 VALUE 50.
       01 NEG PIC S99 VALUE -4.
       01 V  PIC X VALUE "M".
       01 BX PIC X(4) BASED.
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
       D-ODO SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-ODO.
       D-ODO-P.
           DISPLAY "  SELECTED EC-BOUND-ODO"
           RESUME AT NEXT STATEMENT.
       D-ARG SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       D-ARG-P.
           DISPLAY "  SELECTED EC-ARGUMENT-FUNCTION"
           RESUME AT NEXT STATEMENT.
       D-PTR SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-PTR-NULL.
       D-PTR-P.
           DISPLAY "  SELECTED EC-DATA-PTR-NULL"
           RESUME AT NEXT STATEMENT.
       D-RNG SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-INVALID.
       D-RNG-P.
           DISPLAY "  SELECTED EC-RANGE-INVALID".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-BOUND EC-ARGUMENT EC-RANGE EC-DATA CHECKING ON
           DISPLAY "A MOVE X (1:L) TO T (I)"
           MOVE X (1:L) TO T (I)
           DISPLAY "B MOVE T (I) TO X (1:L)"
           MOVE T (I) TO X (1:L)
           DISPLAY "C MOVE N (I) TO O (1)"
           MOVE N (I) TO O (1)
           DISPLAY "D MOVE O (1) TO N (I)"
           MOVE O (1) TO N (I)
           DISPLAY "E MOVE X (1:L) TO T (I) (1:2)"
           MOVE X (1:L) TO T (I) (1:2)
           DISPLAY "F MOVE X (1:L) TO T (FUNCTION SQRT (NEG))"
           MOVE X (1:L) TO T (FUNCTION SQRT (NEG))
           DISPLAY "G MOVE FUNCTION SQRT (NEG) TO N (I)"
           MOVE FUNCTION SQRT (NEG) TO N (I)
           DISPLAY "I MOVE X (1:L) TO BX"
           MOVE X (1:L) TO BX
           DISPLAY "J MOVE BX TO X (1:L)"
           MOVE BX TO X (1:L)
           DISPLAY "H EVALUATE V WHEN Z THRU A WHEN T (I)"
           EVALUATE V
               WHEN "Z" THRU "A" DISPLAY "  WHEN-1"
               WHEN T (I) DISPLAY "  WHEN-2"
               WHEN OTHER DISPLAY "  WHEN-OTHER"
           END-EVALUATE
           MOVE 2 TO I
           MOVE 4 TO L
           MOVE 3 TO D
           MOVE 9 TO NEG
           MOVE X (1:L) TO T (I)
           MOVE FUNCTION SQRT (NEG) TO N (I)
           MOVE N (I) TO O (1)
           DISPLAY "CONTROL T (2) = " T (2) " N (2) = " N (2)
               " O (1) = " O (1)
           STOP RUN.
