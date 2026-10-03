      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1464. The other half of the parenthesis rule: ISO 1989:2023 8.8.4.2.1 says parentheses do not
      *> change a SIMPLE CONDITION, and they make nothing else one. `(WS-X)` over an alphanumeric item is the
      *> same non-condition WS-X is - not a relation, boolean, class, condition-name, switch-status, sign or
      *> omitted-argument condition - so it is refused as COBOLNET2318, naming the item the parentheses enclose
      *> (not an anonymous "arithmetic expression").
      *>   cite.py --check 8.8.4.2.1 "The simple conditions are the relation, boolean, class, condition-name,
      *>     switch-status, sign, and omitted-argument conditions" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1464NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           IF (WS-X)
               DISPLAY "Y"
           ELSE
               DISPLAY "N"
           END-IF
           STOP RUN.
