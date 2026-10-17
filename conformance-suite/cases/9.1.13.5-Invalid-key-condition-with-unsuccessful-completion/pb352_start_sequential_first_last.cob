      *>--- conformance-case
      *> id: pb352_start_sequential_first_last
      *> rules: [GR-9.1.13.5-3, SR-14.9.41.3-1, SR-14.9.41.3-2,
      *>    GR-14.9.41.4-1, GR-14.9.41.4-2, GR-14.9.41.4-4,
      *>    GR-14.9.41.4-5, GR-14.9.41.4-6, GR-14.9.41.4-7,
      *>    GR-14.9.41.4-20, GR-14.9.41.4-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 §14.9.41 START on SEQUENTIAL organization — the
      *> arm the standard REQUIRES and this compiler refused until
      *> kb/Work PB352. §14.9.41.3 SR2: "If the organization of the file
      *> referenced by file-name-1 is sequential, either the FIRST or
      *> the LAST phrase shall be specified" — so START is not merely
      *> permitted here, FIRST/LAST is the ONLY form it can take.
      *> The values below are read off the SEQUENTIAL FILES general
      *> rules, not off a run:
      *>   GR20  FIRST — "the file position indicator is set to 1 if
      *>         records exist in the physical file. If no records
      *>         exist in the file, or the physical file does not
      *>         support the ability to position at the first record,
      *>         the I-O status value … is set to '23', the invalid key
      *>         condition exists, and the execution of the START
      *>         statement is unsuccessful."
      *>   GR21  LAST — the twin, at "the record number of the last
      *>         existing logical record in the physical file".
      *>   GR2   "The execution of the START statement does not alter
      *>         either the content of the record area …" — AREA= below
      *>         prints the value MOVEd in before the START, unchanged.
      *>   GR1   the open mode shall be input or I-O; Table 20's blank
      *>         Output/Extend cells are §9.1.13.7 item 7's '47', and
      *>         neither INVALID nor NOT INVALID runs for a '4x'
      *>         (§9.1.14 keys the two branches on the invalid key
      *>         condition and on successful completion).
      *>   GR5   an OPTIONAL input file that is not present raises the
      *>         invalid key condition — §9.1.13.5 item 3 b)'s '23'.
      *>   GR7   after an unsuccessful START "the file position
      *>         indicator is set to indicate that no valid record
      *>         position has been established", which §9.1.13.7 item
      *>         6 a) reads back as '46' on the next sequential READ.
      *> The record the next READ delivers is §14.9.30.4 GR21's own
      *> sequential arm b): "If the file position indicator was
      *> established by a prior successful OPEN or START statement, the
      *> first existing record that is selected is made available" —
      *> INCLUSIVE positioning, so START FIRST + READ yields record 1
      *> and START LAST + READ yields the last record.
      *> §9.1.7.2's RECORD-sequential type is walked here. The
      *> LINE-sequential type is the same two rules over a different
      *> framing, and is walked by
      *> 2023/pb352_start_sequential_first_last_line: ORGANIZATION IS
      *> LINE SEQUENTIAL is a COBOL-2023 introduction (kb/Work PB688)
      *> while THIS program exists to pin START FIRST/LAST at the
      *> OLDEST edition that has it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P352SQFL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "p352sqfl.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT EMF ASSIGN TO "p352sqfe.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-E.
           SELECT OPTIONAL OPF ASSIGN TO "p352sqfo.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-O.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(5).
       FD EMF.
       01 EM-REC PIC X(4).
       FD OPF.
       01 OP-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-Q PIC XX.
       01 ST-E PIC XX.
       01 ST-O PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ---- a record-sequential file with three records -------------
           OPEN OUTPUT SQF
           MOVE "AAA-1" TO SQ-REC
           WRITE SQ-REC
           MOVE "BBB-2" TO SQ-REC
           WRITE SQ-REC
           MOVE "CCC-3" TO SQ-REC
           WRITE SQ-REC
      *> Table 20's blank START x Output cell (GR1) -> '47', and no
      *> INVALID / NOT INVALID branch runs.
           START SQF FIRST
               INVALID KEY DISPLAY "W-INV"
               NOT INVALID KEY DISPLAY "W-OK"
           END-START
           DISPLAY "WOUT=" ST-Q
           CLOSE SQF
           OPEN INPUT SQF
           DISPLAY "OPEN=" ST-Q
           READ SQF AT END CONTINUE END-READ
           DISPLAY "R1=" SQ-REC
           MOVE "XXXXX" TO SQ-REC
           START SQF FIRST
               INVALID KEY DISPLAY "F-INV"
               NOT INVALID KEY DISPLAY "F-OK"
           END-START
           DISPLAY "SF=" ST-Q
           DISPLAY "AREA=" SQ-REC
           READ SQF AT END CONTINUE END-READ
           DISPLAY "R2=" SQ-REC
           START SQF LAST
               INVALID KEY DISPLAY "L-INV"
               NOT INVALID KEY DISPLAY "L-OK"
           END-START
           DISPLAY "SL=" ST-Q
           READ SQF AT END CONTINUE END-READ
           DISPLAY "R3=" SQ-REC
           READ SQF AT END CONTINUE END-READ
           DISPLAY "R4=" ST-Q
           CLOSE SQF
      *> ---- a file with no records at all (GR20/GR21 second arm) ----
           OPEN OUTPUT EMF
           CLOSE EMF
           OPEN INPUT EMF
           DISPLAY "EOPEN=" ST-E
           START EMF FIRST
               INVALID KEY DISPLAY "EF-INV"
               NOT INVALID KEY DISPLAY "EF-OK"
           END-START
           DISPLAY "EF=" ST-E
      *> GR7 -> §9.1.13.7 item 6 a): the next sequential READ is '46'.
           READ EMF AT END CONTINUE END-READ
           DISPLAY "ER=" ST-E
           START EMF LAST
               INVALID KEY DISPLAY "EL-INV"
               NOT INVALID KEY DISPLAY "EL-OK"
           END-START
           DISPLAY "EL=" ST-E
           CLOSE EMF
      *> ---- an OPTIONAL file that is not present (GR5) --------------
           OPEN INPUT OPF
           DISPLAY "OOPEN=" ST-O
           START OPF FIRST
               INVALID KEY DISPLAY "OF-INV"
               NOT INVALID KEY DISPLAY "OF-OK"
           END-START
           DISPLAY "OF=" ST-O
           CLOSE OPF
           STOP RUN.
