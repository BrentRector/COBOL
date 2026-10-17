      *>--- conformance-case
      *> id: l1c11_select_clause_any_order
      *> rules: [SR-12.4.5.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.2 SR1 — clauses after SELECT in any order
      *> Rule: "The SELECT clause shall be specified first in the file
      *>   control entry. The clauses that follow the SELECT clause may
      *>   appear in any order."
      *>   cite.py --check 12.4.5.2 "The SELECT clause shall be
      *>   specified first in the file control entry. The clauses that
      *>   follow the SELECT clause may appear in any order."
      *>   -> OK  §12.4.5.2 1)  (Syntax rules)
      *> F-IX writes its clauses in the REVERSE of the printed order
      *> (FILE STATUS, RECORD KEY, ACCESS MODE, ORGANIZATION, ASSIGN).
      *> F-SQ puts ASSIGN last after ORGANIZATION and FILE STATUS.
      *> F-RD names the same physical file as F-SQ with the clauses in
      *> printed order, so it reads what F-SQ wrote only if the
      *> trailing ASSIGN clause of F-SQ took effect.
      *> Every clause must take effect wherever it is written:
      *>   IX-OPEN=00   OPEN OUTPUT of the indexed file: success 00.
      *>   IX-W2=00     WRITE key K2: success.
      *>   IX-W1=00     WRITE key K1: success.
      *>   IX-DUP=22    WRITE of K1 again: RECORD KEY is honoured, so a
      *>                duplicate prime key is an invalid key (22), and
      *>                FILE STATUS (written first) receives it.
      *>   IX-R=K1ONE   random READ by key K1 (ACCESS DYNAMIC) finds
      *>                the record written with that key.
      *>   IX-NX=K2TWO  READ NEXT continues in key order (INDEXED).
      *>   IX-MISS=23   random READ of key K9: no record, status 23.
      *>   SQ=00        OPEN OUTPUT / WRITE / CLOSE of F-SQ: 00.
      *>   RD=HELLO     F-RD (same ASSIGN literal) reads F-SQ's record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-IX
               FILE STATUS IS IX-ST
               RECORD KEY IS IX-KEY
               ACCESS MODE IS DYNAMIC
               ORGANIZATION IS INDEXED
               ASSIGN TO "L1C11A.IDX".
           SELECT F-SQ
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SQ-ST
               ASSIGN TO "L1C11A.DAT".
           SELECT F-RD ASSIGN TO "L1C11A.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS RD-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-IX.
       01 IX-REC.
          05 IX-KEY  PIC XX.
          05 IX-DATA PIC X(3).
       FD F-SQ.
       01 SQ-REC PIC X(5).
       FD F-RD.
       01 RD-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 IX-ST PIC XX.
       01 SQ-ST PIC XX.
       01 RD-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-IX.
           DISPLAY "IX-OPEN=" IX-ST.
           MOVE "K2TWO" TO IX-REC.
           WRITE IX-REC.
           DISPLAY "IX-W2=" IX-ST.
           MOVE "K1ONE" TO IX-REC.
           WRITE IX-REC.
           DISPLAY "IX-W1=" IX-ST.
           MOVE "K1XXX" TO IX-REC.
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE.
           DISPLAY "IX-DUP=" IX-ST.
           CLOSE F-IX.
           OPEN INPUT F-IX.
           MOVE "K1" TO IX-KEY.
           READ F-IX KEY IS IX-KEY INVALID KEY CONTINUE END-READ.
           DISPLAY "IX-R=" IX-REC.
           READ F-IX NEXT RECORD AT END CONTINUE END-READ.
           DISPLAY "IX-NX=" IX-REC.
           MOVE "K9" TO IX-KEY.
           READ F-IX KEY IS IX-KEY INVALID KEY CONTINUE END-READ.
           DISPLAY "IX-MISS=" IX-ST.
           CLOSE F-IX.
           OPEN OUTPUT F-SQ.
           MOVE "HELLO" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE F-SQ.
           DISPLAY "SQ=" SQ-ST.
           OPEN INPUT F-RD.
           READ F-RD AT END MOVE "EOF" TO RD-REC END-READ.
           DISPLAY "RD=" RD-REC.
           CLOSE F-RD.
           STOP RUN.
