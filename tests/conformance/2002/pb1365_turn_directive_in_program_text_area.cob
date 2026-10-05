      *> ISO/IEC 1989:2023 7.3.3 SR3 (fixed-form): "a compiler directive shall be written in the program-text area and may be
      *> followed only by space characters and an optional inline comment" (cite.py --check 7.3.3 "When the reference format is
      *> fixed-form, a compiler directive shall be written in the program-text area" -> OK 3)). 6.3.1: "The sequence number
      *> occupies six character positions (1-6)" and "The program-text area begins in character position 8" (cite.py --check
      *> 6.3.1 "The program-text area begins in character position 8" -> OK). 6.3.2: the sequence number area "may consist of
      *> any character in the computer's coded character set" (cite.py --check 6.3.2 "may consist of any character in the
      *> computer's coded character set"), so a sequence number on the directive's line is not part of the directive.
      *> kb/Work PB1365: the TURN below carries a sequence number with a letter in it and an inline comment after its CHECKING
      *> phrase, and the CHECKING phrase has its ON implied (7.3.25.2 printed diagram, PDF page 115: ON is not underlined).
      *> 7.3.25.4 GR6: "If the ON phrase is specified or implied" checking is enabled, so the subscript 5 of 3 sets
      *> EC-BOUND-SUBSCRIPT (8.4.2.3.4 GR2: "If the value of the subscript is not a positive integer or is less than one or is
      *> greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist" ->
      *> cite.py --check OK) and the declarative runs: the handler line, then AFTER. Fixed form.
000010 >>TURN EC-BOUND-SUBSCRIPT CHECKING *> sequence number 000010
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1365PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE T (IDX) TO R.
           DISPLAY "AFTER".
           STOP RUN.
