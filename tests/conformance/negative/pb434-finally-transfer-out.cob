      *> reject-at: 2023
      *> kb/Work PB434 - ISO/IEC 1989:2023 14.9.28.4 GR16: "There shall be no statements that include a transfer
      *> of control out of the PERFORM statement within imperative-statement-5". The FINALLY phrase below holds
      *> a GO TO, which used to compile and run IMP1 / FIN / OUTSIDE - "DONE" never printed, the PERFORM was
      *> abandoned from its own end phrase. Each transfer-out statement draws COBOLNET2927: GO TO, EXIT
      *> PARAGRAPH, EXIT SECTION, GOBACK and NEXT SENTENCE. (Format 3 is a 2023 introduction, so below 2023 the
      *> statement is the edition's COBOLNET0900 instead - the positive 2023/pb434_finally_exit_perform_admitted
      *> pins the EXIT PERFORM the rule admits.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB434NG.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           PERFORM
               DISPLAY "IMP1"
           WHEN EC-USER-DEMO
               DISPLAY "WHEN"
           FINALLY
               DISPLAY "FIN"
               GO TO OUTSIDE-PARA
               EXIT PARAGRAPH
               EXIT SECTION
               GOBACK
               IF 1 = 1 NEXT SENTENCE END-IF
           END-PERFORM
           DISPLAY "DONE".
           STOP RUN.
       OUTSIDE-PARA.
           DISPLAY "OUTSIDE".
           STOP RUN.
