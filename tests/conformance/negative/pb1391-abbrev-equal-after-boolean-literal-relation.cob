      *> reject-at: 2002 2014 2023
      *> kb/Work PB1391. ISO 1989:2023 8.8.4.12.3 SR1 with the boolean-LITERAL relation: `B1 = B"0"` involves an
      *> operand of class boolean (8.8.4.2.1), so `OR = B3` may not abbreviate after it, whichever operand carries
      *> the class. The omitted-subject-and-operator form (`OR B3`) is the bare-object arm of the same carry.
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1391NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"0".
       01 B3 PIC 1 VALUE B"1".
       01 N  PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF B1 = B"0" OR = B3
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           IF B1 = B"0" AND N
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           STOP RUN.
