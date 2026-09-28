      *> kb/Work PB1268 — EC-BOUND-SUBSCRIPT ON A DYNAMIC-CAPACITY TABLE'S ELEMENT REFERENCES.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 8.5.1.9.2 "When a data item in a dynamic-capacity table is referenced as a sending
      *>     operand, the result of the operation is the same as for a fixed-capacity table whose number of
      *>     occurrences is the current capacity of the table" -> OK §8.5.1.9.2
      *>   cite.py --check 8.4.2.3.4 "If the value of the subscript is not a positive integer or is less than one or
      *>     is greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition
      *>     is set to exist" -> OK §8.4.2.3.4 2)
      *> A RECEIVING subscript past the current capacity GROWS the table (§8.5.1.9.3) and is no condition; a
      *> receiving subscript below one is not a growth case, so the fixed-table rule applies to it.
      *> Dynamic-capacity tables are a COBOL-2023 introduction, so the construct has no earlier-edition copy.
      *>
      *> WHY EACH LEG CAN FAIL (the pre-PB1268 build raised for the fixed table FR only):
      *>   1  DISPLAY FR(I), I = 5, a FIXED table of 2       -> RAISED EC-BOUND-SUBSCRIPT (the control)
      *>   2  DISPLAY R(I), current capacity 2 (sending)     -> RAISED EC-BOUND-SUBSCRIPT
      *>   3  MOVE "Q" TO R(Z), Z = 0 (receiving, below one) -> RAISED EC-BOUND-SUBSCRIPT
      *>   4  MOVE "W" TO R(3) (receiving, past capacity)    -> no condition; the table grows to 3: [W], RCAP 3
      *>   5  CNT = 03.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CDYNSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC S9(3) VALUE 5.
       01 Z PIC S9(3) VALUE 0.
       01 T.
          05 R PIC X OCCURS DYNAMIC CAPACITY IN RCAP FROM 2 TO 9.
       01 F.
          05 FR PIC X OCCURS 2.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D1P.
           ADD 1 TO CNT
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "1 [" FR(I) "]"
           DISPLAY "2 [" R(I) "]"
           MOVE "Q" TO R(Z)
           MOVE "W" TO R(3)
           DISPLAY "4 [" R(3) "] " RCAP
           DISPLAY "5 CNT=" CNT
           STOP RUN.
