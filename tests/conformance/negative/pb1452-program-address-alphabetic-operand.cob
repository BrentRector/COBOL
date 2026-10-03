      *> reject-at: 2002 2014 2023
      *> kb/Work PB1452 — the negative twin of 2002/pb1452_program_address_identifier_operands. §8.4.3.13.3 SR1
      *> "Identifier-1 shall be of category alphanumeric or national": a PIC A item is category ALPHABETIC (§8.5.2.1
      *> Table 2 lists alphabetic as its own category), outside SR1's list, though the storage model folds it into
      *> alphanumeric. The raw category test accepted it and located the program named by its content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1452N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 AB PIC A(8) VALUE "PBNEGSUB".
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM AB
           STOP RUN.
       END PROGRAM PB1452N1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBNEGSUB.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PBNEGSUB.
