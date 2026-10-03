      *> reject-at: 2002 2014 2023
      *> kb/Work PB1391. ISO 1989:2023 8.8.4.12.3 SR1 - "Relation-condition-1 shall not be a boolean relation
      *> condition." 8.8.4.2.1 - "A relation condition involving operands of class boolean is a boolean relation
      *> condition". `B1 = B2` compares two boolean items, so it is a boolean relation and `OR <> B3` may not
      *> abbreviate after it. The comparison arm that binds it seeded the abbreviation carry like any relation, so
      *> the program compiled and printed T, expanded as B1 <> B3 - a silent wrong reading of source the standard
      *> does not define. The prohibition is now a property of the relation's operand class, decided where the carry
      *> is seeded (COBOLNET2723).
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
      *>   cite.py --check 8.8.4.2.1 "A relation condition involving operands of class boolean is a boolean
      *>     relation condition" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1391NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"0".
       01 B2 PIC 1 VALUE B"1".
       01 B3 PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           IF B1 = B2 OR <> B3
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           STOP RUN.
