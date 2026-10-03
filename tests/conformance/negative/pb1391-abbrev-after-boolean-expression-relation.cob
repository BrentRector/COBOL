      *> reject-at: 2002 2014 2023
      *> kb/Work PB1391, the boolean-OPERATOR parse path of the same rule. `B1 B-AND B2 = B"0"` is a relation of
      *> boolean expressions (8.8.4.2.2 Format 2), a boolean relation condition, so 8.8.4.12.3 SR1 forbids
      *> abbreviating after it. This arm already refused `OR = B3`, but through the COBOLNET2319 internal-error
      *> net with a wrong reason ("no subject precedes it": the relation IS a relation, and a boolean one); it
      *> now names the rule (COBOLNET2723).
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1391NEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"0".
       01 B3 PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
       MAIN.
           IF B1 B-AND B2 = B"0" OR = B3
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           STOP RUN.
