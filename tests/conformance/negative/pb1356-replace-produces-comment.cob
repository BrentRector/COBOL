*> reject-at: 2002 2014 2023
*> ISO §7.2.4.4 GR9 "The text produced as a result of processing a
*> REPLACE statement shall not contain a COPY statement, a REPLACE
*> statement, a SOURCE FORMAT directive, a comment, or a blank line"
*> (cite.py --check: OK §7.2.4.4 9)). LEADING ==Q== BY ==*== turns
*> the text-word Q>1 into *>1, a comment that would swallow the rest
*> of the line: COBOLNET2574 (kb/Work PB1356). Pinned from 2002, the
*> introducing edition of the partial-word phrase is kb/Work PB1670.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1356RC.
       PROCEDURE DIVISION.
           REPLACE LEADING ==Q== BY ==*==.
           DISPLAY "A" Q>1 "B".
           STOP RUN.
