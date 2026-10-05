*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.2.1 (cite.py --check 7.2.1 "syntactically correct in the initial source text and library text" -> OK):
*> compiler directives are among the elements that shall be syntactically correct in the initial source text and library text,
*> and the false path of an IF directive is part of that text - so a TURN directive that matches no format there is as
*> malformed as in a compiled branch (COBOLNET0718). It compiled clean: the stage that parses a TURN operand never saw the
*> omitted line. kb/Work PB2003.
       >>IF 1 = 2
       >>TURN GARBAGE ON
       >>END-IF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2003N01.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
