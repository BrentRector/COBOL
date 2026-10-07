      *> kb/Work PB434 - ISO/IEC 1989:2023 14.9.28.3 SR8: "The UNTIL EXIT phrase shall not be specified in a
      *> PERFORM statement with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE
      *> or TEST AFTER phrase" (negative pb434-until-exit-under-varying, COBOLNET2954). This golden pins what
      *> the rule does NOT reach, each case running its 14.9.28.4 GR11 loop ("condition-1 never evaluates as
      *> true") to its EXIT PERFORM:
      *>   - UNTIL EXIT under a TIMES PERFORM: neither VARYING nor TEST;
      *>   - UNTIL EXIT under an until-phrase that WRITES no TEST phrase: SR1's assumed TEST BEFORE is not
      *>     "specified" (SR1 governs the until-phrase UNTIL EXIT itself is, so it cannot count);
      *>   - UNTIL EXIT in a paragraph that follows a VARYING PERFORM's range in the text but is not in it
      *>     (14.9.28.4 GR4: the range is SUB-A alone), performed by a TIMES PERFORM.
      *> EXPECTED OUTPUT, derived line by line:
      *>   TIMES 0002      the outer loop runs 2 times; each inner UNTIL EXIT loop adds 1 once, then exits
      *>   UNTIL 0032      M goes 1, 2, 3 before M > 2 holds: 3 passes, each adding 10 (2 + 30)
      *>   VARYING 0003 0232   SUB-A runs for K = 1, 2, 3 (C = 3); SUB-B runs 2 times adding 100 (32 + 200)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB434UX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC 9(4) VALUE 0.
       01 K PIC 9(4) VALUE 0.
       01 M PIC 9(4) VALUE 0.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM 2 TIMES
               PERFORM UNTIL EXIT
                   ADD 1 TO N
                   EXIT PERFORM
               END-PERFORM
           END-PERFORM
           DISPLAY "TIMES " N
           PERFORM UNTIL M > 2
               ADD 1 TO M
               PERFORM UNTIL EXIT
                   ADD 10 TO N
                   EXIT PERFORM
               END-PERFORM
           END-PERFORM
           DISPLAY "UNTIL " N
           PERFORM SUB-A VARYING K FROM 1 BY 1 UNTIL K > 3
           PERFORM SUB-B 2 TIMES
           DISPLAY "VARYING " C " " N
           STOP RUN.
       SUB-A.
           ADD 1 TO C.
       SUB-B.
           PERFORM UNTIL EXIT
               ADD 100 TO N
               EXIT PERFORM
           END-PERFORM.
