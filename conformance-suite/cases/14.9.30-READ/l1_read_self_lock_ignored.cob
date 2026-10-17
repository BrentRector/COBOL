      *>--- conformance-case
      *> id: l1_read_self_lock_ignored
      *> rules: [GR-14.9.30.4-8]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR8 — "If record locking is enabled for the file
      *> connector referenced by file-name-1 and the record identified
      *> for access by the general rules for the READ statement is
      *> locked by THAT file connector, the record lock is ignored and
      *> the READ operation proceeds as if the record were not locked."
      *> §9.1.16 says the same from the other side: "A locked record may
      *> be re-accessed by the same file connector that holds the lock."
      *>
      *> WHY MULTIPLE RECORD LOCKING IS LOAD-BEARING HERE.  Under SINGLE
      *> record locking 12.4.5.9 GR6 makes "execution of any I-O
      *> statement except START release any previously locked record in
      *> that file for that file connector", so a re-read of a record
      *> this connector locked cannot be told apart from a re-read of a
      *> record whose lock had already been dropped — "ignored" and
      *> "released first" are the same observation.  The LOCK ON
      *> MULTIPLE RECORDS phrase (12.4.5.9 GR7) suppresses that release,
      *> so the lock demonstrably still stands across R3.  MULTIPLE
      *> requires a non-sequential organization and access mode
      *> (12.4.5.9.3 SR2), hence relative + ACCESS RANDOM.
      *>
      *> R1  F1's read of record 1 locks it (12.4.5.9 GR4, automatic).
      *> R2  F2's read of record 1 is refused '51' — the lock is REAL.
      *> R3  F1 reads record 2, which under MULTIPLE locking is added to
      *>     F1's held locks; record 1's lock is NOT released.
      *> R4  F1 reads record 1 again.  GR8: the lock is ignored, the
      *>     read succeeds and R001 is made available.  A '51' here
      *>     would mean the self-held lock was treated as foreign.
      *> R5  F2 is STILL refused '51' — proof that R4 ignored the lock
      *>     rather than releasing it, which is the GR6 confound.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD08A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "l1rd08a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT F1 ASSIGN TO "l1rd08a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K1
               FILE STATUS IS ST1
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS.
           SELECT F2 ASSIGN TO "l1rd08a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K2
               FILE STATUS IS ST2
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC PIC X(4).
       FD F1.
       01 R1-REC PIC X(4).
       FD F2.
       01 R2-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0 PIC XX.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 K1  PIC 9(4).
       01 K2  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS.
           MOVE "R001" TO S-REC.
           WRITE S-REC.
           MOVE "R002" TO S-REC.
           WRITE S-REC.
           CLOSE FS.
           OPEN INPUT F1.
           OPEN INPUT F2.
      *> R1 - automatic locking takes record 1 for F1.
           MOVE 1 TO K1.
           READ F1.
           DISPLAY "R1=" ST1 " " R1-REC.
      *> R2 - the lock is real: another connector is refused.
           MOVE 1 TO K2.
           READ F2.
           DISPLAY "R2=" ST2.
      *> R3 - multiple locking: record 2 is added, record 1 is kept.
           MOVE 2 TO K1.
           READ F1.
           DISPLAY "R3=" ST1 " " R1-REC.
      *> R4 - GR8: F1's own lock on record 1 is ignored.
           MOVE 1 TO K1.
           READ F1.
           DISPLAY "R4=" ST1 " " R1-REC.
      *> R5 - the lock was ignored, not released.
           MOVE 1 TO K2.
           READ F2.
           DISPLAY "R5=" ST2.
           CLOSE F1.
           CLOSE F2.
           STOP RUN.
