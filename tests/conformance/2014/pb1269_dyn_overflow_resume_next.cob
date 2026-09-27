      >>TURN EC-BOUND-OVERFLOW CHECKING ON
      *> kb/Work PB1269 - EC-BOUND-OVERFLOW on the implicit growth of a dynamic-capacity table, handled by a
      *> declarative that executes RESUME AT NEXT STATEMENT: ISO 8.5.1.9.6 1) - "If checking for the
      *> EC-BOUND-OVERFLOW exception condition ... results in the execution of a declarative procedure ...
      *> that executes a RESUME statement with the NEXT STATEMENT phrase, the operation shall be allowed to
      *> continue, thus exceeding the receiving table's specified expected capacity." So MOVE 22 TO WS-E (6)
      *> takes the FROM 2 TO 4 table to capacity 6 with E6 = 022, and the second growth (to 7) raises
      *> nothing because the expected capacity was already exceeded before it ("If the change in capacity
      *> was implicit and the expected capacity had already been exceeded before the operation, no exception
      *> shall exist") - one declarative call in all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1269-DYN-OVERFLOW-RESUME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP
                FROM 2 TO 4.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-OVERFLOW.
       D1-P.
           ADD 1 TO N.
           DISPLAY "DECL=[" FUNCTION EXCEPTION-STATUS "]".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE 22 TO WS-E (6).
           DISPLAY "CAP=" WS-CAP.
           DISPLAY "E6=" WS-E (6).
           MOVE 33 TO WS-E (7).
           DISPLAY "CAP=" WS-CAP " N=" N " E7=" WS-E (7).
           STOP RUN.
