      *>--- conformance-case
      *> id: pb352_start_sequential_first_last_line
      *> rules: [SR-14.9.41.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 §14.9.41 START FIRST/LAST on the LINE SEQUENTIAL
      *> type of the sequential organization — the half of
      *> 2002/pb352_start_sequential_first_last (kb/Work PB352) that
      *> cannot live below 2023, because ORGANIZATION IS LINE
      *> SEQUENTIAL is a COBOL-2023 introduction (§12.4.5.10.3 GR2; the
      *> Foreword's list of the main changes over ISO/IEC 1989:2014 —
      *> kb/Work PB688). The 2002 program keeps the record-sequential
      *> legs, so START FIRST/LAST is still pinned at the OLDEST
      *> edition that has it; this one pins that GR20/GR21 are stated
      *> over "records" and NOT over a framing.
      *> The values below are read off the general rules, not off a run:
      *>   §12.4.5.10.3 GR2 "The LINE SEQUENTIAL phrase specifies that
      *>         the file organization is line sequential", and
      *>         §9.1.7.2 makes line sequential one of the two types of
      *>         SEQUENTIAL file — so §14.9.41.3 SR2 ("If the
      *>         organization of the file referenced by file-name-1 is
      *>         sequential, either the FIRST or the LAST phrase shall
      *>         be specified") governs this file exactly as it governs
      *>         a record-sequential one.
      *>   GR21  LAST — the file position indicator is set to "the
      *>         record number of the last existing logical record in
      *>         the physical file"; three records exist, so the START
      *>         succeeds ('00', §9.1.13.2 item 1) and NOT INVALID KEY
      *>         runs (§9.1.14).
      *>   §14.9.30.4 GR21 b) — "If the file position indicator was
      *>         established by a prior successful OPEN or START
      *>         statement, the first existing record that is selected
      *>         is made available": INCLUSIVE positioning, so the READ
      *>         after START LAST delivers the LAST record, L-3.
      *>   GR20  FIRST — the twin at record 1, so the READ after START
      *>         FIRST delivers L-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P352SQFLN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LSF ASSIGN TO "p352sqln.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS ST-L.
       DATA DIVISION.
       FILE SECTION.
       FD LSF.
       01 LS-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 ST-L PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT LSF
           MOVE "L-1" TO LS-REC
           WRITE LS-REC
           MOVE "L-2" TO LS-REC
           WRITE LS-REC
           MOVE "L-3" TO LS-REC
           WRITE LS-REC
           CLOSE LSF
           OPEN INPUT LSF
           START LSF LAST
               INVALID KEY DISPLAY "LL-INV"
               NOT INVALID KEY DISPLAY "LL-OK"
           END-START
           DISPLAY "LSL=" ST-L
           READ LSF AT END CONTINUE END-READ
           DISPLAY "LR1=" LS-REC
           START LSF FIRST
               INVALID KEY DISPLAY "LF-INV"
               NOT INVALID KEY DISPLAY "LF-OK"
           END-START
           DISPLAY "LSF=" ST-L
           READ LSF AT END CONTINUE END-READ
           DISPLAY "LR2=" LS-REC
           CLOSE LSF
           STOP RUN.
