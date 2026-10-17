      *>--- conformance-case
      *> id: l1_table20_indexed
      *> rules: [GR-14.9.27.4-8, GR-14.9.35.4-3, GR-14.9.41.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR8 — "In Table 20, Permissible I-O statements
      *> by access mode and open mode, 'X' at an intersection indicates
      *> that the specified statement, used in the access mode given
      *> for that row, may be used with the open mode given at the top
      *> of the column."
      *> This program walks Table 20 for the INDEXED organization;
      *> l1_table20_seq_relative.cob walks it for SEQUENTIAL and
      *> RELATIVE. Every X cell is exercised and shall report success;
      *> every BLANK cell is exercised and shall report the status
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
      *> The Random row's START cells are blank in every column:
      *> §14.9.41.3 SR1 rejects that at compile time (see
      *> conformance:negative/pb325-start-random-access).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TB20B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XS ASSIGN TO "l1tb20b-xs.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST-S.
           SELECT XR ASSIGN TO "l1tb20b-xr.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XR-KEY
               FILE STATUS IS ST-R.
           SELECT XV ASSIGN TO "l1tb20b-xv.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XV-KEY
               FILE STATUS IS ST-V.
       DATA DIVISION.
       FILE SECTION.
       FD XS.
       01 XS-REC.
          05 XS-KEY PIC X(4).
          05 XS-VAL PIC X(3).
       FD XR.
       01 XR-REC.
          05 XR-KEY PIC X(4).
          05 XR-VAL PIC X(3).
       FD XV.
       01 XV-REC.
          05 XV-KEY PIC X(4).
          05 XV-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-R PIC XX.
       01 ST-V PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ================= INDEXED organization, ACCESS SEQUENTIAL
           OPEN OUTPUT XS
           DISPLAY "XS-O-OPEN=" ST-S
           MOVE "K001" TO XS-KEY
           MOVE "V01" TO XS-VAL
           WRITE XS-REC
           DISPLAY "XS-O-W=" ST-S
           MOVE "K002" TO XS-KEY
           MOVE "V02" TO XS-VAL
           WRITE XS-REC
           DISPLAY "XS-O-W2=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-O-R=" ST-S
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-O-ST=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-O-RW=" ST-S
           DELETE XS RECORD
           DISPLAY "XS-O-D=" ST-S
           CLOSE XS
           OPEN INPUT XS
           DISPLAY "XS-I-OPEN=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-I-R=" ST-S
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-I-ST=" ST-S
           WRITE XS-REC
           DISPLAY "XS-I-W=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-I-RW=" ST-S
           DELETE XS RECORD
           DISPLAY "XS-I-D=" ST-S
           CLOSE XS
           OPEN I-O XS
           DISPLAY "XS-IO-OPEN=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-IO-R=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-IO-RW=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-IO-R2=" ST-S
           DELETE XS RECORD
           DISPLAY "XS-IO-D=" ST-S
           MOVE "K001" TO XS-KEY
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-IO-ST=" ST-S
           WRITE XS-REC
           DISPLAY "XS-IO-W=" ST-S
           CLOSE XS
           OPEN EXTEND XS
           DISPLAY "XS-E-OPEN=" ST-S
           MOVE "K003" TO XS-KEY
           MOVE "V03" TO XS-VAL
           WRITE XS-REC
           DISPLAY "XS-E-W=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-E-R=" ST-S
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-E-ST=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-E-RW=" ST-S
           DELETE XS RECORD
           DISPLAY "XS-E-D=" ST-S
           CLOSE XS
      *> ================= INDEXED organization, ACCESS RANDOM
           OPEN OUTPUT XR
           DISPLAY "XR-O-OPEN=" ST-R
           MOVE "K001" TO XR-KEY
           MOVE "V01" TO XR-VAL
           WRITE XR-REC
           DISPLAY "XR-O-W=" ST-R
           MOVE "K002" TO XR-KEY
           MOVE "V02" TO XR-VAL
           WRITE XR-REC
           DISPLAY "XR-O-W2=" ST-R
           READ XR
           DISPLAY "XR-O-R=" ST-R
           REWRITE XR-REC
           DISPLAY "XR-O-RW=" ST-R
           DELETE XR RECORD
           DISPLAY "XR-O-D=" ST-R
           CLOSE XR
           OPEN INPUT XR
           DISPLAY "XR-I-OPEN=" ST-R
           MOVE "K001" TO XR-KEY
           READ XR
           DISPLAY "XR-I-R=" ST-R
           WRITE XR-REC
           DISPLAY "XR-I-W=" ST-R
           REWRITE XR-REC
           DISPLAY "XR-I-RW=" ST-R
           DELETE XR RECORD
           DISPLAY "XR-I-D=" ST-R
           CLOSE XR
           OPEN I-O XR
           DISPLAY "XR-IO-OPEN=" ST-R
           MOVE "K001" TO XR-KEY
           READ XR
           DISPLAY "XR-IO-R=" ST-R
           REWRITE XR-REC
           DISPLAY "XR-IO-RW=" ST-R
           MOVE "K003" TO XR-KEY
           MOVE "V03" TO XR-VAL
           WRITE XR-REC
           DISPLAY "XR-IO-W=" ST-R
           MOVE "K002" TO XR-KEY
           DELETE XR RECORD
           DISPLAY "XR-IO-D=" ST-R
           CLOSE XR
      *> ================= INDEXED organization, ACCESS DYNAMIC
           OPEN OUTPUT XV
           DISPLAY "XV-O-OPEN=" ST-V
           MOVE "K001" TO XV-KEY
           MOVE "V01" TO XV-VAL
           WRITE XV-REC
           DISPLAY "XV-O-W=" ST-V
           MOVE "K002" TO XV-KEY
           MOVE "V02" TO XV-VAL
           WRITE XV-REC
           DISPLAY "XV-O-W2=" ST-V
           READ XV
           DISPLAY "XV-O-R=" ST-V
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XV-O-ST=" ST-V
           REWRITE XV-REC
           DISPLAY "XV-O-RW=" ST-V
           DELETE XV RECORD
           DISPLAY "XV-O-D=" ST-V
           CLOSE XV
           OPEN INPUT XV
           DISPLAY "XV-I-OPEN=" ST-V
           MOVE "K001" TO XV-KEY
           READ XV
           DISPLAY "XV-I-R=" ST-V
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XV-I-ST=" ST-V
           WRITE XV-REC
           DISPLAY "XV-I-W=" ST-V
           REWRITE XV-REC
           DISPLAY "XV-I-RW=" ST-V
           DELETE XV RECORD
           DISPLAY "XV-I-D=" ST-V
           CLOSE XV
           OPEN I-O XV
           DISPLAY "XV-IO-OPEN=" ST-V
           MOVE "K001" TO XV-KEY
           READ XV
           DISPLAY "XV-IO-R=" ST-V
           REWRITE XV-REC
           DISPLAY "XV-IO-RW=" ST-V
           MOVE "K003" TO XV-KEY
           MOVE "V03" TO XV-VAL
           WRITE XV-REC
           DISPLAY "XV-IO-W=" ST-V
           MOVE "K001" TO XV-KEY
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XV-IO-ST=" ST-V
           MOVE "K002" TO XV-KEY
           DELETE XV RECORD
           DISPLAY "XV-IO-D=" ST-V
           CLOSE XV
           STOP RUN.
