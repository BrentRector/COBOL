      *> kb/Work PB1432 - a step that evaluates an OPERAND (a function activation, or the
      *> temporary a function-bearing subscript is stored in) belongs to the statement it was
      *> written in, wherever the short-circuit places it.
      *> ISO 1989:2023 8.8.4.13 GR1: "The constituent connected conditions within a hierarchical
      *> level are evaluated in order from left" and evaluation of the level "terminates as soon
      *> as a truth value for it is determined". 14.9.33.4 GR2 a) 1.: for a condition raised in a
      *> statement "the applicable statement is the one in which the exception condition was
      *> raised", and NOTE 1: "If an exception condition was raised during the evaluation of
      *> 'a', transfer would be after the END-IF".
      *> Before the fix every IF below whose second operand held a function-bearing subscript
      *> was rejected by the backend under EC-ALL checking (C# CS0159: the subscript temporary's
      *> size-error landing was a goto inside the short-circuit lambda), and the first-operand
      *> IF resumed back INTO itself (subscript 0 -> fatal EC-BOUND-SUBSCRIPT).
      *> Expected, derived:
      *>  OR-T        WS-A = 0 is true, so EL(TRC("C", 1)) is never evaluated: no ACT-C
      *>  ACT-D OR2-T WS-A = 1 is false, so the second operand is evaluated: TRC runs, EL(2) = 1
      *>  DECL-ZD AFTER-A  the second operand's ONE / Z raises EC-SIZE-ZERO-DIVIDE; RESUME AT
      *>              NEXT STATEMENT continues after the END-IF (neither A-T nor A-F)
      *>  DECL-ZD AFTER-B  the same with the subscript in the first (hoisted) operand
      *>  C-T AFTER-C the true first operand short-circuits the divide: no declarative
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. TRC-P1432.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T PIC X.
       01 L-N PIC 9.
       01 L-R PIC 9.
       PROCEDURE DIVISION USING L-T L-N RETURNING L-R.
       F-P.
           DISPLAY "ACT-" L-T.
           MOVE L-N TO L-R.
           GOBACK.
       END FUNCTION TRC-P1432.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1432OER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION TRC-P1432.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 0.
       01 Z PIC 9 VALUE 0.
       01 ONE PIC 9 VALUE 1.
       01 TB.
          05 EL PIC 9 OCCURS 3 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       D-SZ-P.
           IF FUNCTION EXCEPTION-STATUS = "EC-SIZE-ZERO-DIVIDE"
               DISPLAY "DECL-ZD"
           ELSE
               DISPLAY "DECL-OTHER"
           END-IF.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           IF WS-A = 0 OR EL(FUNCTION TRC-P1432("C", 1)) = 1
               DISPLAY "OR-T" ELSE DISPLAY "OR-F" END-IF.
           IF WS-A = 1 OR EL(FUNCTION TRC-P1432("D", 2)) = 1
               DISPLAY "OR2-T" ELSE DISPLAY "OR2-F" END-IF.
           IF WS-A = 1 OR EL(FUNCTION INTEGER(ONE / Z)) = 1
               DISPLAY "A-T" ELSE DISPLAY "A-F" END-IF.
           DISPLAY "AFTER-A".
           IF EL(FUNCTION INTEGER(ONE / Z)) = 1
               DISPLAY "B-T" ELSE DISPLAY "B-F" END-IF.
           DISPLAY "AFTER-B".
           IF WS-A = 0 OR EL(FUNCTION INTEGER(ONE / Z)) = 1
               DISPLAY "C-T" ELSE DISPLAY "C-F" END-IF.
           DISPLAY "AFTER-C".
           STOP RUN.
       END PROGRAM PB1432OER.

