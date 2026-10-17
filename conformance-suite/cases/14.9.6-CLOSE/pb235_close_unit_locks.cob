*>--- conformance-case
*> id: pb235_close_unit_locks
*> rules: [GR-14.9.6.4-9, DOC-A.1-24]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB235CUL.
      *> kb/Work PB235 - ISO 14.9.6.4 GR9 against Table 14 symbol e. GR9
      *> carries NO "without the UNIT phrase" qualifier -
      *>   "Except when the file is specified in an APPLY COMMIT clause,
      *>    the file lock and any record locks associated with the file
      *>    connector referenced by file-name-1 are released by the
      *>    execution of the CLOSE statement."
      *> - so the question this golden settles is whether CLOSE ... UNIT
      *> releases them. DERIVED ANSWER: NO.
      *>
      *> 1. GR3's Table 14 gives CLOSE UNIT on a Non-unit physical file
      *>    the single symbol e, NOT symbol c ("Close file"). Symbol e on
      *>    non-unit media reads: "Execution of this statement is
      *>    considered successful. The file remains in the open mode, the
      *>    file position indicator is unchanged, the I-O status
      *>    indicator for the file connector is set to '07', and no other
      *>    action takes place." A lock release IS another action.
      *> 2. The same holds on the unit media GR2 (b)/(c) describe, where
      *>    symbol e performs a UNIT SWAP and the connector keeps reading
      *>    - a connector that released its own file lock mid-file would
      *>    arbitrate against nothing.
      *> 3. 9.1.16 states the record-lock half as "all record locks
      *>    established for a file are released by the execution of an
      *>    explicit or implicit CLOSE statement FOR THE FILE", and
      *>    Table 14's own vocabulary separates symbol c "Close file"
      *>    from symbol e "Close unit".
      *> So GR9's release rides symbol c, i.e. every CLOSE format except
      *> the two UNIT rows. The plain CLOSE below must release both locks
      *> and the CLOSE ... UNIT must release neither.
      *>
      *> GR9's APPLY COMMIT carve-out cannot be exercised: the commit and
      *> rollback module (Annex A.4.3) is not claimed - docs/CONFORMANCE.md
      *> 5 - so no program can name an APPLY COMMIT clause.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb235cul.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb235cul.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "pb235cul.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-X ASSIGN TO "pb235cux.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS X-ST
               SHARING WITH NO OTHER.
           SELECT F-Y ASSIGN TO "pb235cux.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS Y-ST
               SHARING WITH NO OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-X.
       01 X-REC PIC X(5).
       FD F-Y.
       01 Y-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 A-ST    PIC XX.
       01 B-ST    PIC XX.
       01 X-ST    PIC XX.
       01 Y-ST    PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SEED-BOTH
           PERFORM RECORD-LOCK-HALF
           PERFORM FILE-LOCK-HALF
           STOP RUN.

       SEED-BOTH.
           OPEN OUTPUT F-SEED
           MOVE "RRRRR" TO SEED-REC
           WRITE SEED-REC
           CLOSE F-SEED
           OPEN OUTPUT F-X
           MOVE "XXXXX" TO X-REC
           WRITE X-REC
           CLOSE F-X.

      *> The RECORD-lock half. A holds a manual record lock; 9.1.16 says
      *> "While locked by a given file connector, a record is not
      *> accessible to another file connector", so B's READ of the same
      *> record answers '51' (9.1.13.8 item 1) while it is held.
       RECORD-LOCK-HALF.
           OPEN INPUT F-A
           OPEN INPUT F-B
           READ F-A WITH LOCK AT END CONTINUE END-READ
           DISPLAY "A-LOCKS=" A-ST
           READ F-B AT END CONTINUE END-READ
           DISPLAY "B-BLOCKED=" B-ST
      *> Symbol e, "no other action takes place": the record lock stays.
           CLOSE F-A UNIT
           DISPLAY "A-UNIT=" A-ST
           READ F-B AT END CONTINUE END-READ
           DISPLAY "B-STILL-BLOCKED=" B-ST
      *> Symbol c: GR9 / 9.1.16 release it now.
           CLOSE F-A
           DISPLAY "A-CLOSE=" A-ST
           READ F-B AT END CONTINUE END-READ
           DISPLAY "B-FREED=" B-ST " " B-REC
           CLOSE F-B.

      *> The FILE-lock half, over a SHARING WITH NO OTHER pair: while X
      *> holds the file, Y's OPEN is refused '61' (9.1.13.9 item 1).
       FILE-LOCK-HALF.
           OPEN INPUT F-X
           DISPLAY "X-OPEN=" X-ST
           OPEN INPUT F-Y
           DISPLAY "Y-BLOCKED=" Y-ST
           CLOSE F-X UNIT
           DISPLAY "X-UNIT=" X-ST
           OPEN INPUT F-Y
           DISPLAY "Y-STILL-BLOCKED=" Y-ST
           CLOSE F-X
           DISPLAY "X-CLOSE=" X-ST
           OPEN INPUT F-Y
           DISPLAY "Y-FREED=" Y-ST
      *> 14.9.6.4 GR1: the file connector is no longer open, so this
      *> CLOSE is unsuccessful ('42') and performs none of the closing
      *> actions - including GR9's release, which is why Y keeps the
      *> file it just took.
           CLOSE F-X
           DISPLAY "X-AGAIN=" X-ST
           CLOSE F-Y
           DISPLAY "Y-CLOSE=" Y-ST.
