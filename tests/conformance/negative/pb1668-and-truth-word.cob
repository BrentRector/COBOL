      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1668, the combined-condition twin of pb1668-if-truth-word. ISO 1989:2023 8.8.4.9: a complex
      *> condition is built only from simple conditions, and 8.8.4.2.1 lists them - TRUE is not one. `A = 1 AND
      *> TRUE` has a relation condition on its left and nothing the standard calls a condition on its right.
      *>   cite.py --check 8.8.4.2.1 "The simple conditions are the relation, boolean, class, condition-name,
      *>     switch-status, sign, and omitted-argument conditions" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1668NEGAND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF A = 1 AND TRUE
               DISPLAY "Y"
           ELSE
               DISPLAY "N"
           END-IF
           STOP RUN.
