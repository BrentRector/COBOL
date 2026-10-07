      *> kb/Work PB2117 (A6's instrument) -- indexed file I/O.
      *> Loads N records into an ORGANIZATION INDEXED file in scrambled
      *> key order (key = MOD(I * 7919, N) + 1, a permutation of 1..N
      *> as 7919 is prime to N), reads every key back at random in a
      *> second permutation (multiplier 104729), then walks the file in
      *> key order with START and READ NEXT, checking the order. The
      *> RECORD KEY is alphanumeric (ISO 12.4.5.12.3 SR2): the eight
      *> zero-padded digits of KEY-NUM, so key order is numeric order.
      *> The same source runs under GnuCOBOL for the external comparison
      *> (scripts/arch/perf_baseline.py), so it is plain COBOL 85.
      *> WITNESS (computed, not observed), N = 2000: RAND-SUM and
      *> SEQ-SUM are each N(N+1)/2 = 2001000, SEQ-COUNT = N,
      *> ERR-COUNT = 0 (no INVALID KEY, no key out of order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDXHOT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO "idxhot.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IR-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD IDX-FILE.
       01 IDX-REC.
          05 IR-KEY    PIC X(8).
          05 IR-AMT    PIC 9(8).
          05 IR-NAME   PIC X(64).
       WORKING-STORAGE SECTION.
       01 LOOP-COUNT   PIC 9(9) COMP VALUE 2000.
       01 I            PIC 9(9) COMP VALUE 0.
       01 PROD         PIC 9(18) COMP VALUE 0.
       01 QUOT         PIC 9(18) COMP VALUE 0.
       01 REM          PIC 9(9) COMP VALUE 0.
       01 KEY-NUM      PIC 9(8) VALUE 0.
       01 PREV-KEY     PIC X(8) VALUE LOW-VALUES.
       01 FS           PIC XX.
       01 EOF-FLAG     PIC X VALUE "N".
       01 RAND-SUM     PIC 9(18) COMP VALUE 0.
       01 SEQ-SUM      PIC 9(18) COMP VALUE 0.
       01 SEQ-COUNT    PIC 9(9) COMP VALUE 0.
       01 ERR-COUNT    PIC 9(9) COMP VALUE 0.
       01 OUT-RAND     PIC 9(18).
       01 OUT-SEQ      PIC 9(18).
       01 OUT-COUNT    PIC 9(9).
       01 OUT-ERR      PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE
           PERFORM VARYING I FROM 0 BY 1 UNTIL I NOT < LOOP-COUNT
               MULTIPLY I BY 7919 GIVING PROD
               DIVIDE PROD BY LOOP-COUNT GIVING QUOT REMAINDER REM
               ADD 1 TO REM GIVING KEY-NUM
               MOVE KEY-NUM TO IR-KEY
               MOVE KEY-NUM TO IR-AMT
               MOVE "INDEXED RECORD" TO IR-NAME
               WRITE IDX-REC
                   INVALID KEY
                       ADD 1 TO ERR-COUNT
               END-WRITE
           END-PERFORM
           CLOSE IDX-FILE
           OPEN INPUT IDX-FILE
           PERFORM VARYING I FROM 0 BY 1 UNTIL I NOT < LOOP-COUNT
               MULTIPLY I BY 104729 GIVING PROD
               DIVIDE PROD BY LOOP-COUNT GIVING QUOT REMAINDER REM
               ADD 1 TO REM GIVING KEY-NUM
               MOVE KEY-NUM TO IR-KEY
               READ IDX-FILE
                   INVALID KEY
                       ADD 1 TO ERR-COUNT
                   NOT INVALID KEY
                       ADD IR-AMT TO RAND-SUM
               END-READ
           END-PERFORM
           MOVE LOW-VALUES TO IR-KEY
           START IDX-FILE KEY IS GREATER THAN IR-KEY
               INVALID KEY
                   ADD 1 TO ERR-COUNT
           END-START
           PERFORM UNTIL EOF-FLAG = "Y"
               READ IDX-FILE NEXT RECORD
                   AT END
                       MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       ADD 1 TO SEQ-COUNT
                       ADD IR-AMT TO SEQ-SUM
                       IF IR-KEY NOT > PREV-KEY
                           ADD 1 TO ERR-COUNT
                       END-IF
                       MOVE IR-KEY TO PREV-KEY
               END-READ
           END-PERFORM
           CLOSE IDX-FILE
           MOVE RAND-SUM TO OUT-RAND
           MOVE SEQ-SUM TO OUT-SEQ
           MOVE SEQ-COUNT TO OUT-COUNT
           MOVE ERR-COUNT TO OUT-ERR
           DISPLAY "IDXHOT " OUT-RAND " " OUT-SEQ " " OUT-COUNT " "
               OUT-ERR
           STOP RUN.
