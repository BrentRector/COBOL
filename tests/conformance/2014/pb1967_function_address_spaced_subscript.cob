      *> kb/Work PB1967 (retired) - a SPACED subscript inside the argument of a function-identifier that is
      *> identifier-1 of ADDRESS OF FUNCTION parses, and so does the next statement.
      *> THE RULE. ISO/IEC 1989:2023 8.4.3.12.2 prints ADDRESS OF FUNCTION { function-prototype-name-1 |
      *> identifier-1 }; 8.4.3.1.3 SR1 makes identifier-1 any identifier format, so a function-identifier
      *> whose argument is a subscripted data reference is legal there; 8.3.5 4) makes a parenthesis a
      *> separator, so a space before the subscript's left parenthesis changes nothing.
      *> THE NOTE'S REPRO was a fixed-form line 74 positions long: its closing parenthesis stood at
      *> position 73, past margin R (6.3.1: "Margin R is immediately to the right of the rightmost character
      *> position of the program-text area"; this compiler's margin R follows position 72, DOC-A.1-158), so
      *> it was not program text and the statement was unbalanced. Written inside margin R it compiles.
      *> EXPECTED OUTPUT: FUNCTION UPPER-CASE (WS-NAME (1)) is "PB1967F" (15.97), the name of the function
      *> FPREF addresses (8.4.3.12.4 GR1), so FP = FPREF and the program displays SPACED-OK, then NEXT-OK
      *> from the statement after the SET.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1967F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 7 TO L-RES
           GOBACK.
       END FUNCTION PB1967F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1967M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1967F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PB1967F.
       01 FPREF USAGE FUNCTION-POINTER TO PB1967F.
       01 NAMES.
          05 WS-NAME PIC X(7) OCCURS 2 VALUE "pb1967f".
       PROCEDURE DIVISION.
       MAIN.
           SET FPREF TO ADDRESS OF FUNCTION PB1967F
           SET FP TO ADDRESS OF FUNCTION
               FUNCTION UPPER-CASE (WS-NAME (1)).
           IF FP = FPREF
               DISPLAY "SPACED-OK"
           ELSE
               DISPLAY "SPACED-BAD"
           END-IF
           DISPLAY "NEXT-OK"
           STOP RUN.
       END PROGRAM PB1967M.
