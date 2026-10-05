      *> ISO/IEC 1989:2023 7.3.11.2 general format (kb/Work PB1367):
      *>   >>DEFINE compilation-variable-name-1 [AS] { { literal | arithmetic-expression | boolean-expression |
      *>   PARAMETER } [OVERRIDE] | OFF }
      *> (cite.py --check 7.3.11.2 "OVERRIDE" -> OK). Every alternative is written here: AS present and omitted; a numeric
      *> literal, an alphanumeric literal, an arithmetic expression (7.3.6: 2 * 3 + 1 = 7) and a boolean expression
      *> (7.3.7: B"1100" B-OR B"0011" = B"1111"); OVERRIDE after a value; OFF with and without AS; PARAMETER with and
      *> without OVERRIDE, whose names have no value in the operating environment so 7.3.11.4 GR4 leaves them undefined.
      *> Each IF below selects the line that names the alternative it checks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1367OK.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>DEFINE A1 AS 1
       >>DEFINE A2 2
       >>DEFINE A3 AS "TXT"
       >>DEFINE A4 AS 2 * 3 + 1
       >>DEFINE A5 AS B"1100" B-OR B"0011"
       >>DEFINE A6 AS 5
       >>DEFINE A6 AS 6 OVERRIDE
       >>DEFINE A7 7 OVERRIDE
       >>DEFINE A8 AS 1
       >>DEFINE A8 AS OFF
       >>DEFINE A9 9
       >>DEFINE A9 OFF
       >>DEFINE PB1367UNSET1 AS PARAMETER
       >>DEFINE PB1367UNSET2 PARAMETER OVERRIDE
       >>IF A1 = 1
           DISPLAY "A1".
       >>END-IF
       >>IF A2 = 2
           DISPLAY "A2".
       >>END-IF
       >>IF A3 = "TXT"
           DISPLAY "A3".
       >>END-IF
       >>IF A4 = 7
           DISPLAY "A4".
       >>END-IF
       >>IF A5 = B"1111"
           DISPLAY "A5".
       >>END-IF
       >>IF A6 = 6
           DISPLAY "A6".
       >>END-IF
       >>IF A7 = 7
           DISPLAY "A7".
       >>END-IF
       >>IF A8 IS NOT DEFINED
           DISPLAY "A8".
       >>END-IF
       >>IF A9 IS NOT DEFINED
           DISPLAY "A9".
       >>END-IF
       >>IF PB1367UNSET1 IS NOT DEFINED
           DISPLAY "P1".
       >>END-IF
       >>IF PB1367UNSET2 IS NOT DEFINED
           DISPLAY "P2".
       >>END-IF
           STOP RUN.
