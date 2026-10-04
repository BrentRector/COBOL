      *> kb/Work PB1416 - ADDRESS OF FUNCTION identifier-1 with a SUBSCRIPTED or REFERENCE-MODIFIED identifier-1.
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.3.12.2 prints the function-address-identifier as
      *> `ADDRESS OF FUNCTION { function-prototype-name-1 | identifier-1 }` (ADDRESS and FUNCTION underlined, OF not),
      *> and 8.4.3.1.3 SR1 makes identifier-1 "any of the formats for an identifier" - so WS-NAME (2) and
      *> WS-LONG (3:7) are legal. The lexer used to type the '(' after `FUNCTION <word>` as the intrinsic
      *> ARGUMENT-list paren (8.4.3.2.3 SR6), which no subscript can take, and the subscripted form died COBOL0001.
      *> 8.4.3.12.4 GR1 a) takes the function from "the content of the data item referenced by identifier-1".
      *>
      *> EXPECTED OUTPUT, line by line (PB1416F is the function; FPREF is its address through the prototype arm,
      *> 8.4.3.12.4 GR1 b), and every identifier-1 below holds the text "PB1416F"):
      *>   SUBSCRIPT-OK   WS-NAME (2) = "PB1416F"
      *>   NO-OF-OK       ADDRESS FUNCTION WS-NAME (N), OF omitted, N = 2
      *>   REFMOD-OK      WS-LONG (3:7) = "PB1416F" (WS-LONG is "XXPB1416FXX")
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1416F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 7 TO L-RES
           GOBACK.
       END FUNCTION PB1416F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1416M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1416F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PB1416F.
       01 FPREF USAGE FUNCTION-POINTER TO PB1416F.
       01 N PIC 9 VALUE 2.
       01 WS-NAMES.
          05 WS-NAME PIC X(7) OCCURS 2.
       01 WS-LONG PIC X(11) VALUE "XXPB1416FXX".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "PB1416F" TO WS-NAME (2)
           SET FPREF TO ADDRESS OF FUNCTION PB1416F
           SET FP TO ADDRESS OF FUNCTION WS-NAME(2)
           IF FP = FPREF
               DISPLAY "SUBSCRIPT-OK"
           ELSE
               DISPLAY "SUBSCRIPT-BAD"
           END-IF
           SET FP TO NULL
           SET FP TO ADDRESS FUNCTION WS-NAME (N)
           IF FP = FPREF
               DISPLAY "NO-OF-OK"
           ELSE
               DISPLAY "NO-OF-BAD"
           END-IF
           SET FP TO NULL
           SET FP TO ADDRESS OF FUNCTION WS-LONG (3:7)
           IF FP = FPREF
               DISPLAY "REFMOD-OK"
           ELSE
               DISPLAY "REFMOD-BAD"
           END-IF
           STOP RUN.
       END PROGRAM PB1416M.
