      *> kb/Work PB1034, the compile-time lane. ISO 1989:2023 7.3.8.2 SR1 a): a constant-conditional relation "shall
      *> be formed according to the rules in 8.8.4.2" - its operators are 8.8.4.2.2 Format 1's (rendered page PDF
      *> p217), each with its NOT part of the printed alternative, and the directive's own prefix (`IS NOT` before the
      *> operator) is not a second way to write it. Each >>IF below selects its DISPLAY only when the relation of the
      *> literals is true; the expected lines are derived from the integers alone:
      *>   5 > 3 true C1 | 5 NOT > 3 false | 5 NOT = 3 true C3 | 5 IS NOT EQUAL TO 5 false | 5 <> 3 true C5
      *>   5 >= 5 true C6 | 5 <= 4 false | 3 NOT < 5 false | 5 NOT < 3 true C8
      *>   cite.py --check 7.3.8.2 "The condition shall be formed according to the rules in 8.8.4.2, Simple
      *>     relation conditions." -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034CCEP.
       PROCEDURE DIVISION.
       MAIN.
       >>IF 5 > 3
           DISPLAY "C1".
       >>END-IF
       >>IF 5 NOT > 3
           DISPLAY "C2".
       >>END-IF
       >>IF 5 NOT = 3
           DISPLAY "C3".
       >>END-IF
       >>IF 5 IS NOT EQUAL TO 5
           DISPLAY "C4".
       >>END-IF
       >>IF 5 <> 3
           DISPLAY "C5".
       >>END-IF
       >>IF 5 >= 5
           DISPLAY "C6".
       >>END-IF
       >>IF 5 <= 4
           DISPLAY "C7".
       >>END-IF
       >>IF 3 NOT < 5
           DISPLAY "C7B".
       >>END-IF
       >>IF 5 NOT < 3
           DISPLAY "C8".
       >>END-IF
           STOP RUN.
