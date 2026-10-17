      *>--- conformance-case
      *> id: l1c17_separator_spaces
      *> rules: [SR-8.3.5-1, SR-8.3.5-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.5 1) and 9) — space separators; literal delimiters
      *> 1) "The COBOL character space is a separator. Anywhere a space
      *>  is used as a separator or as part of a separator, more than
      *>  one space may be used. All spaces immediately following the
      *>  separators comma, semicolon, or period are considered part of
      *>  that separator and are not considered to be the separator
      *>  space."
      *> 9) "The separator space may optionally immediately follow any
      *>  separator except the opening delimiter of a literal. A space
      *>  following the opening delimiter of a literal is part of the
      *>  literal and not a separator."
      *> cite.py --check 8.3.5 "All spaces immediately following the
      *>   separators comma, semicolon, or period are considered part
      *>   of that separator and are not considered to be the
      *>   separator space." -> OK §8.3.5 1)
      *> cite.py --check 8.3.5 "A space following the opening
      *>   delimiter of a literal is part of the literal and not a
      *>   separator." -> OK §8.3.5 9)
      *>
      *> DERIVED OUTPUT (the program is legal only if rules 1) and 9)
      *> hold; each line's value follows from the statements):
      *>   N=001 002 003  rule 1): runs of spaces as separators, and a
      *>                  comma / semicolon followed by SEVERAL spaces
      *>                  (all part of that one separator) in the MOVE
      *>                  receiving lists.
      *>   D=004          rule 1): a separator of many spaces between
      *>                  every word of MOVE ... TO.
      *>   P1 / P2        rule 1): a period followed by several spaces
      *>                  ends a sentence and the next begins.
      *>   [ B  ]         rule 9): " B" is a 2-character literal whose
      *>                  first character is the space after the opening
      *>                  quote; moved to PIC X(4) it is " B" + 2 pad
      *>                  spaces.
      *>   [  Z]          rule 9): the literal "  Z" keeps both spaces.
      *>   T=BB           rule 9): a space after "(" (a separator) is a
      *>                  permitted separator space in a subscript.
      *>   RM=B           rule 9): the same inside a reference modifier
      *>                  " B  "(2:1) = "B".
      *>   C=010          rule 9): spaces after "(" in an arithmetic
      *>                  expression; ( 2 + 3 ) * 2 = 10.
      *>   RP=OK          rule 9): a space after the OPENING pseudo-
      *>                  text delimiter is a separator, so == XXQ ==
      *>                  matches the text-word XXQ; it becomes DISPLAY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A PIC 999.
       01 N-B PIC 999.
       01 N-C PIC 999.
       01 N-D PIC 999.
       01 N-E PIC 999.
       01 W-A PIC X(4).
       01 W-TV PIC X(6) VALUE "AABBCC".
       01 W-T REDEFINES W-TV.
          05 W-E PIC XX OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE   1   TO   N-A,    N-B;    N-C.
           ADD 1 TO N-B,   N-C;   N-C.
           DISPLAY "N=" N-A " " N-B " " N-C.
           MOVE     4     TO     N-D.
           DISPLAY    "D="    N-D.
           DISPLAY "P1".    DISPLAY "P2".
           MOVE " B" TO W-A.
           DISPLAY "[" W-A "]".
           DISPLAY "[" "  Z" "]".
           DISPLAY "T=" W-E( 2 ).
           DISPLAY "RM=" W-A( 2 : 1 ).
           COMPUTE N-E = ( 2 + 3 ) * 2.
           DISPLAY "C=" N-E.
           REPLACE == XXQ == BY == DISPLAY ==.
           XXQ "RP=OK".
           REPLACE OFF.
           STOP RUN.
