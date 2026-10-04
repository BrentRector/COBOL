      *> reject-at: 2014 2023
      *> kb/Work PB1407. ISO 8.4.3.11.3 SR3: "Identifier-1 shall not reference a data item that is described with the
      *> CONSTANT RECORD clause, or any data item subordinate to such a data item." A pointer to a constant record would
      *> let a BASED view write the constant, so the record (CR) and its subordinate (CR-A) are each refused COBOLNET2785,
      *> in the SET sender and the relation operand. An ordinary record (OR1) is addressed in the same statement shapes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR CONSTANT RECORD.
          05 CR-A PIC X(4) VALUE "ABCD".
       01 OR1.
          05 OR-A PIC X(4) VALUE "WXYZ".
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF CR
           SET P TO ADDRESS OF CR-A
           IF P = ADDRESS OF CR-A
               DISPLAY "EQ"
           END-IF
           SET P TO ADDRESS OF OR1
           SET P TO ADDRESS OF OR-A
           STOP RUN.
