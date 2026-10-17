*>--- conformance-case
*> id: file_sharing_mutate
*> rules: [GR-9.1.13.8-1, GR-14.9.10.4-4, GR-14.9.10.4-6,
*>    GR-14.9.10.4-10, GR-14.9.47.4-1, DOC-A.1-109]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FSMUTP10FL.
      *> P10 Step 8: the record-operation-conflict checks on the MUTATING
      *> verbs (ISO 9.1.16): a record locked by another file connector
      *> shall not be rewritten (14.9.35 GR11 -> 51) or deleted (14.9.10
      *> GR6 -> 51); the RETRY phrase re-attempts on REWRITE and DELETE
      *> (14.7.9 - n TIMES exhausts to 51, FOREVER deadlock-bails to 52
      *> in one run unit); WRITE ... WITH LOCK locks the record written
      *> (14.9.51 GR11), blocking another connector's REWRITE of it.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-S ASSIGN TO "sharemut.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS S-KEY
               FILE STATUS IS S-ST.
           SELECT F-P ASSIGN TO "sharemut.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS P-KEY
               FILE STATUS IS P-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-Q ASSIGN TO "sharemut.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS Q-KEY
               FILE STATUS IS Q-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-S.
       01 S-REC PIC X(5).
       FD F-P.
       01 P-REC PIC X(5).
       FD F-Q.
       01 Q-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 S-KEY PIC 9(4).
       01 P-KEY PIC 9(4).
       01 Q-KEY PIC 9(4).
       01 S-ST  PIC XX.
       01 P-ST  PIC XX.
       01 Q-ST  PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Seed two records through an ordinary (non-sharing) connector.
           OPEN OUTPUT F-S.
           MOVE 1 TO S-KEY. MOVE "ALPHA" TO S-REC. WRITE S-REC.
           MOVE 2 TO S-KEY. MOVE "BRAVO" TO S-REC. WRITE S-REC.
           CLOSE F-S.
           OPEN I-O F-P. DISPLAY "OPENP=" P-ST.
           OPEN I-O F-Q. DISPLAY "OPENQ=" Q-ST.
      *> P locks record 1 (MANUAL + WITH LOCK).
           MOVE 1 TO P-KEY. READ F-P WITH LOCK.
           DISPLAY "READP1=" P-ST " " P-REC.
      *> Q may not REWRITE the record P holds (14.9.35 GR11 -> 51);
      *> RETRY n TIMES exhausts to 51; RETRY FOREVER deadlock-bails 52.
           MOVE 1 TO Q-KEY. MOVE "QQQQ1" TO Q-REC.
           REWRITE Q-REC. DISPLAY "REWQ51=" Q-ST.
           REWRITE Q-REC RETRY 2 TIMES. DISPLAY "REWQRT=" Q-ST.
           REWRITE Q-REC RETRY FOREVER. DISPLAY "REWQFV=" Q-ST.
      *> Q may not DELETE it either (14.9.10 GR6 -> 51; RETRY likewise).
           DELETE F-Q RECORD. DISPLAY "DELQ51=" Q-ST.
           DELETE F-Q RECORD RETRY 1 TIMES. DISPLAY "DELQRT=" Q-ST.
      *> P releases; Q's DELETE now removes record 1.
           UNLOCK F-P. DISPLAY "UNLP=" P-ST.
           DELETE F-Q RECORD. DISPLAY "DELQ=" Q-ST.
           MOVE 1 TO Q-KEY. READ F-Q. DISPLAY "READQ23=" Q-ST.
      *> Q writes record 3 WITH LOCK (14.9.51 GR11); P's REWRITE of the
      *> record identified by that key is blocked -> 51 (14.9.35 GR11).
           MOVE 3 TO Q-KEY. MOVE "CHRLY" TO Q-REC.
           WRITE Q-REC WITH LOCK. DISPLAY "WRQ=" Q-ST.
           MOVE 3 TO P-KEY. MOVE "PPPPP" TO P-REC.
           REWRITE P-REC. DISPLAY "REWP51=" P-ST.
           CLOSE F-P.
           CLOSE F-Q.
           STOP RUN.
