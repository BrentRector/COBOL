      *> kb/Work PB2117 (A6's instrument) -- sequential file I/O.
      *> Writes N 80-byte records to an ORGANIZATION SEQUENTIAL file,
      *> closes it, and reads every record back.
      *> The same source runs under GnuCOBOL for the external comparison
      *> (scripts/arch/perf_baseline.py), so it is plain COBOL 85.
      *> WITNESS (computed, not observed), N = 500000: READ-COUNT = N,
      *> SUM-KEY = N(N+1)/2 = 125000250000, ERR-COUNT = 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQHOT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "seqhot.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC.
          05 SR-KEY    PIC 9(8).
          05 SR-AMT    PIC 9(7)V99.
          05 SR-NAME   PIC X(63).
       WORKING-STORAGE SECTION.
       01 LOOP-COUNT   PIC 9(9) COMP VALUE 500000.
       01 I            PIC 9(9) COMP VALUE 0.
       01 FS           PIC XX.
       01 EOF-FLAG     PIC X VALUE "N".
       01 READ-COUNT   PIC 9(18) COMP VALUE 0.
       01 SUM-KEY      PIC 9(18) COMP VALUE 0.
       01 ERR-COUNT    PIC 9(9) COMP VALUE 0.
       01 OUT-COUNT    PIC 9(9).
       01 OUT-SUM      PIC 9(18).
       01 OUT-ERR      PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LOOP-COUNT
               MOVE I TO SR-KEY
               MOVE I TO SR-AMT
               MOVE "SEQUENTIAL RECORD" TO SR-NAME
               WRITE SEQ-REC
               IF FS NOT = "00"
                   ADD 1 TO ERR-COUNT
               END-IF
           END-PERFORM
           CLOSE SEQ-FILE
           OPEN INPUT SEQ-FILE
           PERFORM UNTIL EOF-FLAG = "Y"
               READ SEQ-FILE
                   AT END
                       MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       ADD 1 TO READ-COUNT
                       ADD SR-KEY TO SUM-KEY
               END-READ
           END-PERFORM
           CLOSE SEQ-FILE
           MOVE READ-COUNT TO OUT-COUNT
           MOVE SUM-KEY TO OUT-SUM
           MOVE ERR-COUNT TO OUT-ERR
           DISPLAY "SEQHOT " OUT-COUNT " " OUT-SUM " " OUT-ERR
           STOP RUN.
