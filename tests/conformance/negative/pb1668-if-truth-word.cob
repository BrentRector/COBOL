      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1668. ISO 1989:2023 8.8.4.2.1 - "The simple conditions are the relation, boolean, class,
      *> condition-name, switch-status, sign, and omitted-argument conditions": no member of that list is a bare
      *> truth word. TRUE and FALSE are written only as an EVALUATE selection subject or selection object
      *> (14.9.13.3 SR7 b). `IF TRUE` is therefore not a conditional expression in any edition. It reached the
      *> internal-error net (COBOLNET2319), which names no rule; it is now COBOLNET2318, which names the word and
      *> the rule it breaks.
      *>   cite.py --check 8.8.4.2.1 "The simple conditions are the relation, boolean, class, condition-name,
      *>     switch-status, sign, and omitted-argument conditions" -> OK
      *>   cite.py --check 14.9.13.3 "Condition-2 or the words TRUE or FALSE appearing as a selection object
      *>     shall correspond to condition-1 or the words TRUE or FALSE in the set of selection subjects." -> OK 7) b)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1668NEGIF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF TRUE
               DISPLAY "Y"
           ELSE
               DISPLAY "N"
           END-IF
           STOP RUN.
