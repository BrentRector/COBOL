      *> ISO 1989:2023 8.4.3.13.3 SR1: the program-address-identifier's
      *> "Identifier-1 shall be of category alphanumeric or national." An
      *> alphanumeric-edited item is neither (8.5.2.1 Table 2; the negative
      *> golden pb850-set-program-address-alphanumeric-edited), but a
      *> REFERENCE-MODIFIED view of it is: 8.4.3.3.4 GR6 a) "the category
      *> alphanumeric-edited is considered class and category alphanumeric".
      *> This golden pins that the tightened category screen (kb/Work PB850)
      *> still admits the view, and that the view names the program: AE holds
      *> "PB850SUB X" through its insertion B, AE(1:8) is "PB850SUB", and
      *> 8.4.3.13.4 GR2 takes the outermost program of that name, so the CALL
      *> through the pointer reaches PB850SUB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB850RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 AE PIC X(8)BX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "PB850SUBX" TO AE
           DISPLAY "[" AE "]"
           SET PP TO ADDRESS OF PROGRAM AE(1:8)
           CALL PP
           DISPLAY "BACK"
           STOP RUN.
       END PROGRAM PB850RM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB850SUB.
       PROCEDURE DIVISION.
       MAIN-SUB.
           DISPLAY "IN PB850SUB"
           GOBACK.
       END PROGRAM PB850SUB.
