      *> reject-at: 2023
      *> Train 1021 review finding C-1 (kb/Work PB434) - ISO/IEC 1989:2023 14.9.28.4 GR16: "There shall be no
      *> statements that include a transfer of control out of the PERFORM statement within imperative-statement-5".
      *> STOP RUN is one: 14.9.42.4 GR6 "Execution of the run unit terminates and control is transferred to the
      *> operating system", and 14.9.18.4 GR3 makes a main program's GOBACK (refused by
      *> pb434-finally-transfer-out) operate "as if executing a STOP statement". The FINALLY phrase below holds
      *> only a STOP RUN, so the COBOLNET2927 is its own. It used to compile and print IMP1 / FIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB434SR.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               DISPLAY "IMP1"
           WHEN EC-USER-DEMO
               DISPLAY "WHEN"
           FINALLY
               DISPLAY "FIN"
               STOP RUN
           END-PERFORM
           DISPLAY "DONE".
           STOP RUN.
