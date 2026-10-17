      *>--- conformance-case
      *> id: pb338_read_releases_prior_lock
      *> rules: [GR-12.4.5.9.4-6, GR-14.9.30.4-11]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR11 a) — "If single record locking is specified
      *> for the file connector associated with file-name-1, any prior
      *> record lock associated with that file connector is released BY
      *> THE EXECUTION OF THE READ STATEMENT."  §12.4.5.9.4 GR6 states
      *> the same rule for every verb: "Execution of any I-O statement
      *> except START releases any previously locked record in that file
      *> for that file connector."
      *>
      *> THE RULE IS TIED TO EXECUTION, NOT TO SUCCESS, and the standard
      *> qualifies DELIBERATELY in the very same general rule where it
      *> means to: GR11 b) releases "at the completion of the SUCCESSFUL
      *> execution of the READ statement", GR11 c) and d) speak of "a
      *> SUCCESSFULLY ACCESSED record".  GR11 a) carries no such
      *> qualifier.  An at end READ is an executed I-O statement, so it
      *> releases (kb/Work PB338).
      *>
      *> THE SUBJECT.  Three legs, one per ORGANIZATION — record
      *> sequential, relative and indexed — differing in NOTHING but the
      *> ORGANIZATION clause.  Three records; two connectors A and C on
      *> one physical file, both SHARING WITH ALL OTHER and LOCK MODE IS
      *> MANUAL, which §12.4.5.9.4 GR6 makes SINGLE record locking (the
      *> LOCK ON phrase omitted).
      *>
      *> A1..A3 are three READ ... WITH LOCK.  Under single record
      *> locking each releases the one before it, so after A3 the
      *> connector holds exactly ONE lock: the THIRD record's.
      *>
      *> C1..C3 walk the same file through the other connector.  C3 is
      *> the CONTROL: the third record is locked by another file
      *> connector, so §14.9.30.4 GR9 + §9.1.13.8 item 1 make it '51'
      *> and GR10 a) leaves C's file position indicator unchanged.
      *>
      *> A4 is the SUBJECT.  It is a plain READ that finds no next
      *> record: §14.9.30.4 GR24 sets '10' and "When the at end
      *> condition exists, execution of the READ statement is
      *> unsuccessful."  GR11 a) fires anyway, so A's lock on the third
      *> record is gone.
      *>
      *> C4 proves both halves at once: it is the SAME statement as C3
      *> and it now answers '00' with the third record — the lock was
      *> released by A's FAILING read (GR11 a), and C could still reach
      *> that record because its own '51' had not moved its position
      *> (GR10 a).
      *>
      *> EXPECTED, derived before it was observed:
      *>   ?-A3=00 CCCCC, ?-C3=51, ?-A4=10, ?-C4=00 CCCCC  (?=S/R/I)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB338REL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEED-S ASSIGN TO "pb338rel-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT SA ASSIGN TO "pb338rel-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STSA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SC ASSIGN TO "pb338rel-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STSC
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SEED-R ASSIGN TO "pb338rel-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT RA ASSIGN TO "pb338rel-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS KRA
               FILE STATUS IS STRA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT RC ASSIGN TO "pb338rel-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS KRC
               FILE STATUS IS STRC
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SEED-I ASSIGN TO "pb338rel-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS I-KEY
               FILE STATUS IS ST0.
           SELECT IA ASSIGN TO "pb338rel-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IA-KEY
               FILE STATUS IS STIA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT IC ASSIGN TO "pb338rel-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IC-KEY
               FILE STATUS IS STIC
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD SEED-S.
       01 SS-REC PIC X(5).
       FD SA.
       01 SA-REC PIC X(5).
       FD SC.
       01 SC-REC PIC X(5).
       FD SEED-R.
       01 RS-REC PIC X(5).
       FD RA.
       01 RA-REC PIC X(5).
       FD RC.
       01 RC-REC PIC X(5).
       FD SEED-I.
       01 IS-REC.
          05 I-KEY  PIC X(2).
          05 I-DATA PIC X(5).
       FD IA.
       01 IA-REC.
          05 IA-KEY  PIC X(2).
          05 IA-DATA PIC X(5).
       FD IC.
       01 IC-REC.
          05 IC-KEY  PIC X(2).
          05 IC-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST0  PIC XX.
       01 STSA PIC XX.
       01 STSC PIC XX.
       01 STRA PIC XX.
       01 STRC PIC XX.
       01 STIA PIC XX.
       01 STIC PIC XX.
       01 KRA  PIC 9(4).
       01 KRC  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SEED-SEQUENTIAL.
           PERFORM LEG-SEQUENTIAL.
           PERFORM SEED-RELATIVE.
           PERFORM LEG-RELATIVE.
           PERFORM SEED-INDEXED.
           PERFORM LEG-INDEXED.
           STOP RUN.

       SEED-SEQUENTIAL.
           OPEN OUTPUT SEED-S.
           MOVE "AAAAA" TO SS-REC.
           WRITE SS-REC.
           MOVE "BBBBB" TO SS-REC.
           WRITE SS-REC.
           MOVE "CCCCC" TO SS-REC.
           WRITE SS-REC.
           CLOSE SEED-S.

       LEG-SEQUENTIAL.
           OPEN I-O SA.
           OPEN I-O SC.
           READ SA WITH LOCK.
           READ SA WITH LOCK.
           READ SA WITH LOCK.
           DISPLAY "S-A3=" STSA " " SA-REC.
           READ SC.
           READ SC.
           READ SC.
           DISPLAY "S-C3=" STSC.
           READ SA.
           DISPLAY "S-A4=" STSA.
           READ SC.
           DISPLAY "S-C4=" STSC " " SC-REC.
           CLOSE SA.
           CLOSE SC.

       SEED-RELATIVE.
           OPEN OUTPUT SEED-R.
           MOVE "AAAAA" TO RS-REC.
           WRITE RS-REC.
           MOVE "BBBBB" TO RS-REC.
           WRITE RS-REC.
           MOVE "CCCCC" TO RS-REC.
           WRITE RS-REC.
           CLOSE SEED-R.

       LEG-RELATIVE.
           OPEN I-O RA.
           OPEN I-O RC.
           READ RA WITH LOCK.
           READ RA WITH LOCK.
           READ RA WITH LOCK.
           DISPLAY "R-A3=" STRA " " RA-REC.
           READ RC.
           READ RC.
           READ RC.
           DISPLAY "R-C3=" STRC.
           READ RA.
           DISPLAY "R-A4=" STRA.
           READ RC.
           DISPLAY "R-C4=" STRC " " RC-REC.
           CLOSE RA.
           CLOSE RC.

       SEED-INDEXED.
           OPEN OUTPUT SEED-I.
           MOVE "K1" TO I-KEY.
           MOVE "AAAAA" TO I-DATA.
           WRITE IS-REC.
           MOVE "K2" TO I-KEY.
           MOVE "BBBBB" TO I-DATA.
           WRITE IS-REC.
           MOVE "K3" TO I-KEY.
           MOVE "CCCCC" TO I-DATA.
           WRITE IS-REC.
           CLOSE SEED-I.

       LEG-INDEXED.
           OPEN I-O IA.
           OPEN I-O IC.
           READ IA WITH LOCK.
           READ IA WITH LOCK.
           READ IA WITH LOCK.
           DISPLAY "I-A3=" STIA " " IA-DATA.
           READ IC.
           READ IC.
           READ IC.
           DISPLAY "I-C3=" STIC.
           READ IA.
           DISPLAY "I-A4=" STIA.
           READ IC.
           DISPLAY "I-C4=" STIC " " IC-DATA.
           CLOSE IA.
           CLOSE IC.
