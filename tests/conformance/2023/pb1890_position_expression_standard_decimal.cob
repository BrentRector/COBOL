      *> kb/Work PB1890, the STANDARD-ARITHMETIC arm. ISO 5.5 3) b): under
      *> standard-decimal arithmetic an integer operand "shall be equal to a
      *> standard intermediate data item ... whose decimal fixed-point
      *> representation contains only zeros to the right of the decimal point".
      *> 8.4.2.3.4 1) b) sets EC-BOUND-SUBSCRIPT when the evaluation of the
      *> subscript's expression "does not result in an integer".
      *> The subscript IX + 0.0000000001 is 2.0000000001 in the standard-decimal
      *> intermediate (an SDIDI): not an integer, so the condition exists. Before
      *> kb/Work PB1890 the value was truncated into a 9-fraction-digit temporary
      *> first, whatever the arithmetic mode, and selected occurrence 2 silently.
      *> IX / 1 and 6 / 3 are integers in the SDIDI and select normally.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1890SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T PIC 9 OCCURS 3.
       01 IX PIC 9 VALUE 2.
       01 R PIC 9(2) VALUE 77.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-SUB-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "123" TO TB.
           MOVE T (IX + 0.0000000001) TO R.
           DISPLAY "SD-SUB=" R.
           MOVE T (IX / 1) TO R.
           DISPLAY "SD-SUB-DIV=" R.
           MOVE T (6 / 3 + 1) TO R.
           DISPLAY "SD-SUB-LIT=" R.
           STOP RUN.
