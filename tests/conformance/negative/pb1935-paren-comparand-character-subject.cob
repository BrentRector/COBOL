      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1935 -- A = B OR (C), with A and B alphanumeric and C
      *> PIC 9, read the parenthesized (C) as the bare item C and
      *> compiled clean. (C) is an arithmetic expression (ISO 8.8.1.1:
      *> "an arithmetic expression enclosed in parentheses"; 8.8.1.2 1):
      *> "the result is treated as a single operand"), and 8.8.4.2.5
      *> admits against a character operand only "an integer literal or
      *> an integer numeric data item of usage display or national"
      *> (cite.py: OK for each). The same reading made A = (C) and
      *> A = B OR (3) compile clean; the build already refused
      *> A = (C + 0). COBOLNET2532 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1935A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X VALUE "3".
       01 B PIC X VALUE "1".
       01 C PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A = B OR (C)
               DISPLAY "T"
           END-IF.
           STOP RUN.
