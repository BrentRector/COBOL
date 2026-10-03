      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1: IS <= carries no [NOT] (rendered page PDF p217), so
      *> `NOT <=` is no alternative of the format - the sibling of pb1034-not-gteq on the other OR-EQUAL symbol.
      *> The standard spells that comparison IS > (or IS GREATER THAN). Written without the optional IS, `A NOT`
      *> ends the condition at the bare operand A and the NOT starts no statement, so this arm is refused by the
      *> IF statement's own diagnostic (COBOLNET2072); with IS (pb1034-not-gteq) it is a parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF A NOT <= B
               DISPLAY "GT"
           ELSE
               DISPLAY "LE"
           END-IF
           STOP RUN.
