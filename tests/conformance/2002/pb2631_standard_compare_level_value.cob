      *> ISO 15.85.3 r6: "Argument-4, if specified, shall be a positive nonzero integer." 15.85.4 r1: "If
      *> argument-4 is unspecified, the highest level defined in the ordering table is used for the comparison."
      *> 15.85.4 r2: a level "not defined in the ordering table" sets EC-ORDER-NOT-SUPPORTED. 15.3 item 14: an
      *> argument that violates a rule of the function definition sets EC-ARGUMENT-FUNCTION.
      *>
      *> kb/Work PB2631: the run-time function used level 0 as its "argument-4 omitted" sentinel, so a data item
      *> holding 0 silently compared at the highest level, and a data item holding a negative integer raised
      *> EC-ORDER-NOT-SUPPORTED (r2's condition for a level the table does not define) instead of the argument
      *> rule's EC-ARGUMENT-FUNCTION. Each line below names the condition the declarative saw:
      *>   level item = 0, or -1    -> EC-ARGUMENT-FUNCTION (r6 is violated by the VALUE)
      *>   level item = 5           -> EC-ORDER-NOT-SUPPORTED (a positive level the table does not define)
      *>   level item = 4, or omitted -> "<": 'a-b' and 'ab' differ at level 4 only (the shifted variable weight)
      *>   level item = 3           -> "=": the same pair is equal through level 3
      *> A violated statement is abandoned by RESUME AT NEXT STATEMENT, so R keeps its previous value.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       >>TURN EC-ORDER-NOT-SUPPORTED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2631SCL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LV PIC S9 VALUE 0.
       01 R PIC X VALUE "?".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION
                                         EC-ORDER-NOT-SUPPORTED.
       H-P.
           IF FUNCTION EXCEPTION-STATUS = "EC-ARGUMENT-FUNCTION"
               DISPLAY "  CAUGHT=EC-ARGUMENT-FUNCTION"
           ELSE
               IF FUNCTION EXCEPTION-STATUS = "EC-ORDER-NOT-SUPPORTED"
                   DISPLAY "  CAUGHT=EC-ORDER-NOT-SUPPORTED"
               ELSE
                   DISPLAY "  CAUGHT=OTHER"
               END-IF
           END-IF.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO LV.
           MOVE "?" TO R.
           MOVE FUNCTION STANDARD-COMPARE("a-b" "ab" LV) TO R.
           DISPLAY "1-LEVEL-0=[" R "]".
           MOVE -1 TO LV.
           MOVE FUNCTION STANDARD-COMPARE("a-b" "ab" LV) TO R.
           DISPLAY "2-LEVEL-MINUS-1=[" R "]".
           MOVE 5 TO LV.
           MOVE FUNCTION STANDARD-COMPARE("a-b" "ab" LV) TO R.
           DISPLAY "3-LEVEL-5=[" R "]".
           MOVE 4 TO LV.
           MOVE FUNCTION STANDARD-COMPARE("a-b" "ab" LV) TO R.
           DISPLAY "4-LEVEL-4=[" R "]".
           MOVE 3 TO LV.
           MOVE FUNCTION STANDARD-COMPARE("a-b" "ab" LV) TO R.
           DISPLAY "5-LEVEL-3=[" R "]".
           MOVE FUNCTION STANDARD-COMPARE("a-b" "ab") TO R.
           DISPLAY "6-OMITTED=[" R "]".
           STOP RUN.
