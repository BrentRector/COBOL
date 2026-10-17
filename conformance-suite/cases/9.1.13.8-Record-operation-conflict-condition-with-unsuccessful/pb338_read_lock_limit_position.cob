      *>--- conformance-case
      *> id: pb338_read_lock_limit_position
      *> rules: [GR-9.1.13.8-4, GR-12.4.5.9.4-7, GR-14.9.30.4-18,
      *>    DOC-A.1-155]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.9.4 GR7 — "The implementor shall specify the
      *> maximum number of record locks that may be held by a file
      *> connector; that maximum shall be at least 15. ... Any I-O
      *> statement that attempts to obtain a record lock that would
      *> exceed either limit is unsuccessful and receives an I-O status
      *> that indicates that condition."  COBOL.NET's per-connector
      *> maximum IS 15 (PhysicalFileTable.ConnectorLockMax, the
      *> standard's floor); §9.1.13.8 item 4 prices the denial '54'.
      *>
      *> THE SUBJECT.  Three legs.  Each opens ONE connector under
      *> LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS — the only
      *> mode that can reach the ceiling, because §12.4.5.9.4 GR6 makes
      *> single record locking release the prior lock on every I-O
      *> statement, so a single-locking connector never holds more than
      *> one.  Legs R and I are the FORMAT-1 sequential walk on the two
      *> keyed organizations; leg X is the FORMAT-2 random read.
      *> ⛔ THERE IS NO SEQUENTIAL-ORGANIZATION LEG, AND ADDING ONE
      *> WOULD BE NON-CONFORMING SOURCE: §12.4.5.9.3 SR2 — "The MULTIPLE
      *> phrase shall not be specified for a file described with
      *> sequential organization or sequential access mode" — so that
      *> organization can never hold a second lock and the ceiling is
      *> unreachable through it.  That is also why R, I and X all
      *> declare ACCESS MODE IS DYNAMIC.
      *>
      *> READ 15 is the last grant.  READ 16 requests a SIXTEENTH lock
      *> and is refused '54'.
      *>
      *> THE POINT OF THE TEST is the NEXT read.  The '54' READ is
      *> unsuccessful, and it is NOT the record operation conflict
      *> condition: §14.9.30.4 GR9 defines that condition as "the record
      *> identified for access is locked by ANOTHER file connector",
      *> where '53'/'54' are this connector's own lock COUNT.  So
      *> §14.9.30.4 GR10 a)'s "The file position indicator is unchanged"
      *> does not reach it and GR18 applies in full: "Unless otherwise
      *> specified, at the completion of any unsuccessful execution of a
      *> READ statement, the content of the associated record area is
      *> undefined, the key of reference is undefined for indexed files,
      *> and the file position indicator is set to indicate that no
      *> valid record position has been established."  §9.1.13.7 item 6
      *> then reads that state back: "I-O status = 46.  A sequential
      *> READ statement is attempted referencing a file connector open
      *> in the input or I-O mode and no valid next record has been
      *> established because: ... b) The preceding READ statement
      *> referencing that file connector was unsuccessful."
      *> So the read after the denial is '46' — NOT '00' on the record
      *> AFTER the one denied, which is what a denial applied AFTER the
      *> physical retrieval leaves behind: a successfully advanced
      *> position wearing a failure status, silently skipping the record
      *> it failed on (kb/Work PB338).  The UNLOCK between them
      *> (§14.9.47.4 GR1) frees all fifteen locks and is shown to prove
      *> the '46' is the POSITION and not a surviving lock ceiling.
      *>
      *> EXPECTED, derived before it was observed:
      *>   R-16=54 R-UNL=00 R-NXT=46
      *>   I-16=54 I-UNL=00 I-NXT=46
      *>   X-16=54 X-UNL=00 X-NXT=46
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB338LIM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEED-R ASSIGN TO "pb338lim-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT RA ASSIGN TO "pb338lim-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS KRA
               FILE STATUS IS STRA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT SEED-I ASSIGN TO "pb338lim-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS I-KEY
               FILE STATUS IS ST0.
           SELECT IA ASSIGN TO "pb338lim-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IA-KEY
               FILE STATUS IS STIA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT SEED-X ASSIGN TO "pb338lim-x.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT XA ASSIGN TO "pb338lim-x.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS KXA
               FILE STATUS IS STXA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD SEED-R.
       01 RS-REC PIC X(4).
       FD RA.
       01 RA-REC PIC X(4).
       FD SEED-I.
       01 IS-REC.
          05 I-KEY  PIC X(4).
       FD IA.
       01 IA-REC.
          05 IA-KEY  PIC X(4).
       FD SEED-X.
       01 XS-REC PIC X(4).
       FD XA.
       01 XA-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0  PIC XX.
       01 STRA PIC XX.
       01 STIA PIC XX.
       01 STXA PIC XX.
       01 STU  PIC XX.
       01 KRA  PIC 9(4).
       01 KXA  PIC 9(4).
       01 N    PIC 9(4).
       01 SEQ  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SEED-RELATIVE.
           PERFORM LEG-RELATIVE.
           PERFORM SEED-INDEXED.
           PERFORM LEG-INDEXED.
           PERFORM SEED-RANDOM.
           PERFORM LEG-RANDOM.
           STOP RUN.

       SEED-RELATIVE.
           OPEN OUTPUT SEED-R.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 20
               MOVE N TO SEQ
               MOVE SEQ TO RS-REC
               WRITE RS-REC
           END-PERFORM.
           CLOSE SEED-R.

       LEG-RELATIVE.
           OPEN I-O RA.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 16
               READ RA NEXT RECORD WITH LOCK
           END-PERFORM.
           DISPLAY "R-16=" STRA.
           UNLOCK RA RECORD.
           MOVE STRA TO STU.
           DISPLAY "R-UNL=" STU.
           READ RA NEXT RECORD.
           DISPLAY "R-NXT=" STRA.
           CLOSE RA.

       SEED-INDEXED.
           OPEN OUTPUT SEED-I.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 20
               MOVE N TO SEQ
               MOVE SEQ TO I-KEY
               WRITE IS-REC
           END-PERFORM.
           CLOSE SEED-I.

       LEG-INDEXED.
           OPEN I-O IA.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 16
               READ IA NEXT RECORD WITH LOCK
           END-PERFORM.
           DISPLAY "I-16=" STIA.
           UNLOCK IA RECORD.
           MOVE STIA TO STU.
           DISPLAY "I-UNL=" STU.
           READ IA NEXT RECORD.
           DISPLAY "I-NXT=" STIA.
           CLOSE IA.

       SEED-RANDOM.
           OPEN OUTPUT SEED-X.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 20
               MOVE N TO SEQ
               MOVE SEQ TO XS-REC
               WRITE XS-REC
           END-PERFORM.
           CLOSE SEED-X.

       LEG-RANDOM.
           OPEN I-O XA.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 16
               MOVE N TO KXA
               READ XA RECORD WITH LOCK
           END-PERFORM.
           DISPLAY "X-16=" STXA.
           UNLOCK XA RECORD.
           MOVE STXA TO STU.
           DISPLAY "X-UNL=" STU.
           READ XA NEXT RECORD.
           DISPLAY "X-NXT=" STXA.
           CLOSE XA.
