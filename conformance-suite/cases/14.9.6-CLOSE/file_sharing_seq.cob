*>--- conformance-case
*> id: file_sharing_seq
*> rules: [GR-14.9.6.4-9, GR-14.9.30.4-12, GR-14.9.35.4-5, GR-14.9.47.4-1, FMT-14.9.47.2, DOC-A.1-109]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FSSEQP10FL.
      *> P10 Step 8: record locking on the SEQUENTIAL organization (ISO
      *> 9.1.16 applies to every organization; the READ/REWRITE/WRITE lock
      *> rules 14.9.30 GR7-GR12 / 14.9.35 GR11-GR12 / 14.9.51 GR10-GR11 are
      *> ALL-FORMATS rules). A sequential record's lock identity is its
      *> ordinal position. Exercised: WITH LOCK read -> the other
      *> connector's READ is 51 (FPI unchanged, GR10a); RETRY n TIMES;
      *> IGNORING LOCK; the 43 REWRITE precondition after a conflicted
      *> READ; UNLOCK; the GR11a single-lock auto-release; REWRITE WITH
      *> LOCK (GR12c) blocking another connector's REWRITE (51 + RETRY);
      *> WRITE ... WITH LOCK on an EXTEND connector locking the appended
      *> ordinal (GR11); RETRY FOREVER deadlock-bailing to 52.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "shareseq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "shareseq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "shareseq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-C ASSIGN TO "shareseq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS C-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-D ASSIGN TO "shareseq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS D-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-C.
       01 C-REC PIC X(5).
       FD F-D.
       01 D-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 A-ST    PIC XX.
       01 B-ST    PIC XX.
       01 C-ST    PIC XX.
       01 D-ST    PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Seed three records through an ordinary (non-sharing) connector.
           OPEN OUTPUT F-SEED.
           MOVE "AAAAA" TO SEED-REC. WRITE SEED-REC.
           MOVE "BBBBB" TO SEED-REC. WRITE SEED-REC.
           MOVE "CCCCC" TO SEED-REC. WRITE SEED-REC.
           CLOSE F-SEED.
      *> Two sharing I-O connectors (ALL OTHER + ALL OTHER: no conflict).
           OPEN I-O F-A. DISPLAY "OPEN-A=" A-ST.
           OPEN I-O F-B. DISPLAY "OPEN-B=" B-ST.
      *> A reads record 1 and locks it (MANUAL + WITH LOCK, GR11d).
           READ F-A WITH LOCK. DISPLAY "READA1=" A-ST " " A-REC.
      *> B's READ of record 1 is blocked by A's lock -> 51 (GR9);
      *> the pre-read check leaves B's position unchanged (GR10a).
           READ F-B. DISPLAY "READB1=" B-ST.
      *> B retries; A cannot release mid single-thread -> still 51.
           READ F-B RETRY 2 TIMES. DISPLAY "RETRYB=" B-ST.
      *> B ignores the lock and reads record 1 (GR12).
           READ F-B IGNORING LOCK. DISPLAY "IGNB=" B-ST " " B-REC.
      *> B reads record 2 WITH LOCK.
           READ F-B WITH LOCK. DISPLAY "READB2=" B-ST " " B-REC.
      *> A's next READ targets record 2, locked by B -> 51.
           READ F-A. DISPLAY "READA2=" A-ST.
      *> A's REWRITE after that UNSUCCESSFUL READ is 43 (14.9.35 GR5).
           REWRITE A-REC FROM "XXXXX". DISPLAY "REW43=" A-ST.
      *> B releases its lock; A's READ of record 2 now succeeds, and the
      *> single-lock discipline auto-releases A's lock on record 1 (GR11a).
           UNLOCK F-B RECORD. DISPLAY "UNLB=" B-ST.
           READ F-A. DISPLAY "READA3=" A-ST " " A-REC.
      *> A rewrites record 2 WITH LOCK (GR12c: the lock is set at
      *> completion), so B's REWRITE of its last-read record (2) is 51.
           REWRITE A-REC FROM "MMMMM" WITH LOCK. DISPLAY "REWA=" A-ST.
           REWRITE B-REC FROM "ZZZZZ". DISPLAY "REWB51=" B-ST.
           REWRITE B-REC FROM "ZZZZZ" RETRY 2 TIMES.
           DISPLAY "REWBRT=" B-ST.
      *> A unlocks; B's re-executed REWRITE is '43' (9.1.13.7 3) - the
      *> intervening UNSUCCESSFUL REWRITE ('51') was the last executed
      *> I-O statement, not a successful READ, and the setter chokepoint
      *> drops the gate on EVERY outcome (kb/Work PB140; a program must
      *> re-READ or use RETRY to land it). Record 2 keeps A's MMMMM.
           UNLOCK F-A. DISPLAY "UNLA=" A-ST.
           REWRITE B-REC FROM "ZZZZZ". DISPLAY "REWB=" B-ST.
           CLOSE F-A.
           CLOSE F-B.
      *> A fresh reader proves the on-disk content.
           OPEN INPUT F-D. DISPLAY "OPEN-D=" D-ST.
           READ F-D. DISPLAY "D1=" D-REC.
           READ F-D. DISPLAY "D2=" D-REC.
           READ F-D. DISPLAY "D3=" D-REC.
      *> An EXTEND connector appends record 4 WITH LOCK (GR11: the lock is
      *> set on the record written; the appended ordinal continues the
      *> file's numbering). D's READ of ordinal 4 is 51 BEFORE any
      *> physical read; RETRY FOREVER deadlock-bails to 52 (14.7.9).
           OPEN EXTEND F-C. DISPLAY "OPEN-C=" C-ST.
           WRITE C-REC FROM "DDDDD" WITH LOCK. DISPLAY "WRC=" C-ST.
           READ F-D. DISPLAY "READD4=" D-ST.
           READ F-D RETRY FOREVER. DISPLAY "READD5=" D-ST.
      *> CLOSE releases C's locks (9.1.16); D then reads the new record.
           CLOSE F-C.
           READ F-D. DISPLAY "D4=" D-ST " " D-REC.
           CLOSE F-D.
           STOP RUN.
