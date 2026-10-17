*>--- conformance-case
*> id: pb140_close_lock
*> rules: [GR-14.9.6.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140CL.
      *> kb/Work PB140 - ISO 14.9.6.4 GR1: a CLOSE of a not-open file is
      *> UNSUCCESSFUL ('42'), and an unsuccessful CLOSE WITH LOCK performs
      *> none of its closing actions - the later OPEN succeeds ('00',
      *> never the poisoned '38'). A SUCCESSFUL CLOSE WITH LOCK does lock:
      *> the reopen is '38'. (CLOSE ... WITH LOCK is legal through 2014.)
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb140cl.dat"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           CLOSE S WITH LOCK
           DISPLAY "CWL42=" WS-ST
           OPEN OUTPUT S
           DISPLAY "OPEN1=" WS-ST
           MOVE "DATA0001" TO S-REC
           WRITE S-REC
           CLOSE S WITH LOCK
           DISPLAY "CWL00=" WS-ST
           OPEN INPUT S
           DISPLAY "OPEN38=" WS-ST
           STOP RUN.
