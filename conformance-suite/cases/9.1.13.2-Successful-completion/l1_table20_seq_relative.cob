      *>--- conformance-case
      *> id: l1_table20_seq_relative
      *> rules: [GR-9.1.13.2-1, GR-9.1.13.7-7, GR-9.1.13.7-8,
      *>    GR-9.1.13.7-9, GR-14.9.27.4-8, GR-14.9.35.4-1,
      *>    GR-14.9.35.4-3, GR-14.9.41.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR8 — "In Table 20, Permissible I-O statements
      *> by access mode and open mode, 'X' at an intersection indicates
      *> that the specified statement, used in the access mode given
      *> for that row, may be used with the open mode given at the top
      *> of the column."
      *> This program walks Table 20 for the SEQUENTIAL and RELATIVE
      *> organizations; l1_table20_indexed.cob walks it for INDEXED.
      *> Every X cell is exercised and shall report success; every
      *> BLANK cell is exercised and shall report the status
      *> §9.1.13.7 names for it:
      *>   item 7 → '47'  READ or START, connector not open input/I-O
      *>   item 8 → '48'  WRITE: a) access sequential and not open
      *>                  extend/output; b) access random or dynamic
      *>                  and not open I-O/output
      *>   item 9 → '49'  REWRITE or DELETE RECORD, not open I-O
      *> The Extend column for the Random and Dynamic rows is blank but
      *> UNREACHABLE from legal source — §14.9.27.3 SR2 confines EXTEND
      *> to sequential access — so it is pinned at the runtime instead,
      *> by unit:Table20WriteOpenModeTests (kb/Work PB325).
      *> Table 20's rows are keyed on the ACCESS MODE, not on the
      *> organization, so the Sequential row's START cells govern a
      *> sequential-ORGANIZATION file too — the FIRST/LAST form
      *> §14.9.41.3 SR2 requires there. They are walked on QS below
      *> (kb/Work PB352; until it landed the statement never reached a
      *> bound node on this organization and the cells were unwalkable).
      *> The Random row's START is blank in every column: §14.9.41.3
      *> SR1 rejects it at compile time (see
      *> conformance:cases/14.9.27-OPEN/pb325-start-random-access).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TB20A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QS ASSIGN TO "l1tb20a-q.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT RS ASSIGN TO "l1tb20a-rs.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RKS
               FILE STATUS IS ST-S.
           SELECT RR ASSIGN TO "l1tb20a-rr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKR
               FILE STATUS IS ST-R.
           SELECT RV ASSIGN TO "l1tb20a-rd.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RKD
               FILE STATUS IS ST-D.
       DATA DIVISION.
       FILE SECTION.
       FD QS.
       01 Q-REC PIC X(6).
       FD RS.
       01 S-REC PIC X(6).
       FD RR.
       01 R-REC PIC X(6).
       FD RV.
       01 D-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 RKS  PIC 9(4).
       01 RKR  PIC 9(4).
       01 RKD  PIC 9(4).
       01 ST-Q PIC XX.
       01 ST-S PIC XX.
       01 ST-R PIC XX.
       01 ST-D PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ================= SEQUENTIAL organization (access sequential)
           OPEN OUTPUT QS
           DISPLAY "QS-O-OPEN=" ST-Q
           MOVE "Q00001" TO Q-REC
           WRITE Q-REC
           DISPLAY "QS-O-W=" ST-Q
           MOVE "Q00002" TO Q-REC
           WRITE Q-REC
           DISPLAY "QS-O-W2=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-O-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-O-RW=" ST-Q
           START QS FIRST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-O-ST=" ST-Q
           CLOSE QS
           OPEN INPUT QS
           DISPLAY "QS-I-OPEN=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-I-R=" ST-Q
           WRITE Q-REC
           DISPLAY "QS-I-W=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-I-RW=" ST-Q
           START QS FIRST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-I-ST=" ST-Q
           CLOSE QS
           OPEN I-O QS
           DISPLAY "QS-IO-OPEN=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-IO-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-IO-RW=" ST-Q
           WRITE Q-REC
           DISPLAY "QS-IO-W=" ST-Q
           START QS LAST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-IO-ST=" ST-Q
           CLOSE QS
           OPEN EXTEND QS
           DISPLAY "QS-E-OPEN=" ST-Q
           MOVE "Q00003" TO Q-REC
           WRITE Q-REC
           DISPLAY "QS-E-W=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-E-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-E-RW=" ST-Q
           START QS LAST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-E-ST=" ST-Q
           CLOSE QS
      *> ================= RELATIVE organization, ACCESS SEQUENTIAL
           OPEN OUTPUT RS
           DISPLAY "RS-O-OPEN=" ST-S
           MOVE "R00001" TO S-REC
           WRITE S-REC
           DISPLAY "RS-O-W=" ST-S
           MOVE "R00002" TO S-REC
           WRITE S-REC
           DISPLAY "RS-O-W2=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-O-R=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-O-ST=" ST-S
           REWRITE S-REC
           DISPLAY "RS-O-RW=" ST-S
           DELETE RS RECORD
           DISPLAY "RS-O-D=" ST-S
           CLOSE RS
           OPEN INPUT RS
           DISPLAY "RS-I-OPEN=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-I-R=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-I-ST=" ST-S
           WRITE S-REC
           DISPLAY "RS-I-W=" ST-S
           REWRITE S-REC
           DISPLAY "RS-I-RW=" ST-S
           DELETE RS RECORD
           DISPLAY "RS-I-D=" ST-S
           CLOSE RS
           OPEN I-O RS
           DISPLAY "RS-IO-OPEN=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-IO-R=" ST-S
           REWRITE S-REC
           DISPLAY "RS-IO-RW=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-IO-R2=" ST-S
           DELETE RS RECORD
           DISPLAY "RS-IO-D=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-IO-ST=" ST-S
           WRITE S-REC
           DISPLAY "RS-IO-W=" ST-S
           CLOSE RS
           OPEN EXTEND RS
           DISPLAY "RS-E-OPEN=" ST-S
           MOVE "R00003" TO S-REC
           WRITE S-REC
           DISPLAY "RS-E-W=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-E-R=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-E-ST=" ST-S
           REWRITE S-REC
           DISPLAY "RS-E-RW=" ST-S
           DELETE RS RECORD
           DISPLAY "RS-E-D=" ST-S
           CLOSE RS
      *> ================= RELATIVE organization, ACCESS RANDOM
           OPEN OUTPUT RR
           DISPLAY "RR-O-OPEN=" ST-R
           MOVE 1 TO RKR
           MOVE "R00001" TO R-REC
           WRITE R-REC
           DISPLAY "RR-O-W=" ST-R
           MOVE 2 TO RKR
           MOVE "R00002" TO R-REC
           WRITE R-REC
           DISPLAY "RR-O-W2=" ST-R
           MOVE 1 TO RKR
           READ RR
           DISPLAY "RR-O-R=" ST-R
           REWRITE R-REC
           DISPLAY "RR-O-RW=" ST-R
           DELETE RR RECORD
           DISPLAY "RR-O-D=" ST-R
           CLOSE RR
           OPEN INPUT RR
           DISPLAY "RR-I-OPEN=" ST-R
           MOVE 1 TO RKR
           READ RR
           DISPLAY "RR-I-R=" ST-R
           WRITE R-REC
           DISPLAY "RR-I-W=" ST-R
           REWRITE R-REC
           DISPLAY "RR-I-RW=" ST-R
           DELETE RR RECORD
           DISPLAY "RR-I-D=" ST-R
           CLOSE RR
           OPEN I-O RR
           DISPLAY "RR-IO-OPEN=" ST-R
           MOVE 1 TO RKR
           READ RR
           DISPLAY "RR-IO-R=" ST-R
           REWRITE R-REC
           DISPLAY "RR-IO-RW=" ST-R
           MOVE 3 TO RKR
           MOVE "R00003" TO R-REC
           WRITE R-REC
           DISPLAY "RR-IO-W=" ST-R
           MOVE 2 TO RKR
           DELETE RR RECORD
           DISPLAY "RR-IO-D=" ST-R
           CLOSE RR
      *> ================= RELATIVE organization, ACCESS DYNAMIC
           OPEN OUTPUT RV
           DISPLAY "RV-O-OPEN=" ST-D
           MOVE 1 TO RKD
           MOVE "R00001" TO D-REC
           WRITE D-REC
           DISPLAY "RV-O-W=" ST-D
           MOVE 2 TO RKD
           MOVE "R00002" TO D-REC
           WRITE D-REC
           DISPLAY "RV-O-W2=" ST-D
           MOVE 1 TO RKD
           READ RV
           DISPLAY "RV-O-R=" ST-D
           START RV KEY IS EQUAL TO RKD
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RV-O-ST=" ST-D
           REWRITE D-REC
           DISPLAY "RV-O-RW=" ST-D
           DELETE RV RECORD
           DISPLAY "RV-O-D=" ST-D
           CLOSE RV
           OPEN INPUT RV
           DISPLAY "RV-I-OPEN=" ST-D
           MOVE 1 TO RKD
           READ RV
           DISPLAY "RV-I-R=" ST-D
           START RV KEY IS EQUAL TO RKD
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RV-I-ST=" ST-D
           WRITE D-REC
           DISPLAY "RV-I-W=" ST-D
           REWRITE D-REC
           DISPLAY "RV-I-RW=" ST-D
           DELETE RV RECORD
           DISPLAY "RV-I-D=" ST-D
           CLOSE RV
           OPEN I-O RV
           DISPLAY "RV-IO-OPEN=" ST-D
           MOVE 1 TO RKD
           READ RV
           DISPLAY "RV-IO-R=" ST-D
           REWRITE D-REC
           DISPLAY "RV-IO-RW=" ST-D
           MOVE 3 TO RKD
           MOVE "R00003" TO D-REC
           WRITE D-REC
           DISPLAY "RV-IO-W=" ST-D
           MOVE 1 TO RKD
           START RV KEY IS EQUAL TO RKD
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RV-IO-ST=" ST-D
           MOVE 2 TO RKD
           DELETE RV RECORD
           DISPLAY "RV-IO-D=" ST-D
           CLOSE RV
           STOP RUN.
