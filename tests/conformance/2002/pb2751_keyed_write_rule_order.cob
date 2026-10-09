      *> kb/Work PB2751 - docs/CONFORMANCE.md DOC-A.1-104: when more
      *> than one I-O status applies, the rules are tested in the
      *> order the general rules state them. WRITE: the open mode
      *> (14.9.51.4 GR3, '48') before the record size (GR14, '44'),
      *> and both before the release and invalid key rules (GR29 b
      *> '34', GR33 a '22'). REWRITE: '43' (14.9.35.4 GR5) before
      *> '44' (GR20) before the record it replaces ('23', GR21). And
      *> DOC-A.1-104 (4): a sequential REWRITE or DELETE of a record
      *> another connector deleted since the READ is '23' (14.9.10.4
      *> GR5: it can no longer be accessed), never a re-created
      *> record or a success over nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2751ORDER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "pb2751r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS RS.
           SELECT X ASSIGN TO "pb2751x.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM
               RECORD KEY IS X-KEY
               FILE STATUS IS XS.
           SELECT Y ASSIGN TO "pb2751y.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS Y-KEY
               FILE STATUS IS YS.
           SELECT Q ASSIGN TO "pb2751q.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS QS.
           SELECT Q2 ASSIGN TO "pb2751q.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS QK
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS Q2S.
       DATA DIVISION.
       FILE SECTION.
       FD R RECORD IS VARYING IN SIZE FROM 10 TO 20 CHARACTERS
             DEPENDING ON R-LEN.
       01 R-REC PIC X(20).
       FD X RECORD IS VARYING IN SIZE FROM 10 TO 20 CHARACTERS
             DEPENDING ON X-LEN.
       01 X-REC.
          05 X-KEY PIC X(4).
          05 X-DATA PIC X(16).
       FD Y RECORD IS VARYING IN SIZE FROM 10 TO 20 CHARACTERS
             DEPENDING ON Y-LEN.
       01 Y-REC.
          05 Y-KEY PIC X(4).
          05 Y-DATA PIC X(16).
       FD Q.
       01 Q-REC PIC X(4).
       FD Q2.
       01 Q2-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 QK PIC 9(4).
       01 R-LEN PIC 99.
       01 X-LEN PIC 99.
       01 Y-LEN PIC 99.
       01 RS PIC XX.
       01 XS PIC XX.
       01 YS PIC XX.
       01 QS PIC XX.
       01 Q2S PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT R
           MOVE 3 TO RK MOVE 12 TO R-LEN MOVE ALL "A" TO R-REC
           WRITE R-REC
           DISPLAY "R SEED " RS
           CLOSE R
           OPEN I-O R
           MOVE 9 TO RK MOVE 5 TO R-LEN
           REWRITE R-REC
           DISPLAY "R REWRITE ABSENT SHORT " RS
           MOVE 3 TO RK MOVE 5 TO R-LEN
           WRITE R-REC
           DISPLAY "R WRITE OCCUPIED SHORT " RS
           MOVE 0 TO RK MOVE 5 TO R-LEN
           WRITE R-REC
           DISPLAY "R WRITE KEY-0 SHORT " RS
           MOVE 3 TO RK MOVE 12 TO R-LEN
           READ R
           DISPLAY "R READ 3 " RS " " R-LEN
           CLOSE R
           OPEN OUTPUT X
           CLOSE X
           OPEN INPUT X
           MOVE 5 TO X-LEN MOVE "0001" TO X-KEY
           WRITE X-REC
           DISPLAY "X WRITE INPUT SHORT " XS
           CLOSE X
           OPEN OUTPUT Y
           MOVE 12 TO Y-LEN MOVE "0001AAAAAAAA" TO Y-REC
           WRITE Y-REC
           CLOSE Y
           OPEN I-O Y
           MOVE 5 TO Y-LEN
           REWRITE Y-REC
           DISPLAY "Y REWRITE NO-READ SHORT " YS
           CLOSE Y
           OPEN OUTPUT Q
           MOVE "AAAA" TO Q-REC
           WRITE Q-REC
           MOVE "BBBB" TO Q-REC
           WRITE Q-REC
           CLOSE Q
           OPEN I-O Q
           OPEN I-O Q2
           READ Q
           DISPLAY "Q READ " QS " " Q-REC
           MOVE 1 TO QK
           DELETE Q2 RECORD
           DISPLAY "Q2 DELETE 1 " Q2S
           MOVE "ZZZZ" TO Q-REC
           REWRITE Q-REC
           DISPLAY "Q REWRITE DELETED " QS
           READ Q
           DISPLAY "Q READ " QS " " Q-REC
           MOVE 2 TO QK
           DELETE Q2 RECORD
           DISPLAY "Q2 DELETE 2 " Q2S
           DELETE Q RECORD
           DISPLAY "Q DELETE DELETED " QS
           CLOSE Q Q2
           OPEN INPUT Q
           READ Q
           DISPLAY "Q AFTER " QS
           CLOSE Q
           STOP RUN.
