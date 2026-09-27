      *> kb/Work PB1543 - a character that only LOOKS like a space is
      *> an ordinary text-word character in COPY REPLACING / REPLACE:
      *> only the COBOL character space (and the tab and line end the
      *> lexer also treats as white space) separates text-words.
      *> RULES (each run through scripts/spec/cite.py --check):
      *>   8.3.5 1) "The COBOL character space is a separator" -> OK 1)
      *>   7.2.2.5 3) "any other character or sequence of contiguous
      *>     characters from the compile-time coded character set,
      *>     bounded by separators"                       -> OK 3)
      *>   DOC-A.1-23: no character is prohibited in a text-word.
      *> The REPLACE operand below is P, U+00A0 NO-BREAK SPACE, Q: ONE
      *> text-word.
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [12]  the two text-words P Q are not the one text-word
      *>         P<U+00A0>Q, so they are not replaced (splitting on
      *>         Unicode White_Space printed [OK]).
      *>   [OK]  P<U+00A0>Q is replaced by "OK".
       REPLACE ==P Q== BY =="OK"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1543NBSP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X VALUE "1".
       01 Q PIC X VALUE "2".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" P Q "]"
           DISPLAY "[" P Q "]"
           STOP RUN.
