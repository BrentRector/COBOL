       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB322DSH.
      *> kb/Work PB322 determination A (Annex A.1 items 77 and 131).
      *> ISO 9.1.15: "If no specification is made in either location, the
      *> implementor defines the sharing mode in which the file is
      *> opened"; 14.9.27.4 GR23 sends an OPEN with no SHARING phrase and
      *> no SHARING clause to the same implementor choice. WiseOwl COBOL
      *> establishes one of 9.1.15's own three modes from the OPEN's mode
      *> (docs/CONFORMANCE.md DOC-A.1-77 and DOC-A.1-131): OPEN INPUT is
      *> SHARING WITH READ ONLY and OPEN OUTPUT, I-O and EXTEND are
      *> SHARING WITH NO OTHER. Table 19 (14.9.27.4) then arbitrates
      *> every other connector against that mode, so each status below
      *> is the printed cell, not a choice made here:
      *>   READ ONLY input request vs existing READ ONLY input: normal.
      *>   NO OTHER (I-O/EXTEND/OUTPUT) request vs ANY existing: 61.
      *>   READ ONLY input request vs a NO OTHER holder: 61 (9.1.15 1:
      *>   exclusive access).
      *> A refused OPEN leaves the connector closed (14.9.27.4 GR25), so
      *> the program does not CLOSE it.
      *> Three clause-less SELECTs share one file (the DOC-A.1-131
      *> example); F-L carries only a LOCK MODE clause, which 9.1.15
      *> does not treat as a sharing specification, so it gets the same
      *> default.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb322dsh.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb322dsh.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb322dsh.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
           SELECT F-C ASSIGN TO "pb322dsh.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS C-ST.
           SELECT F-L ASSIGN TO "pb322dsh.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS L-ST.
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
       FD F-L.
       01 L-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 A-ST    PIC XX.
       01 B-ST    PIC XX.
       01 C-ST    PIC XX.
       01 L-ST    PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-SEED.
           DISPLAY "SEED-OUT=" SEED-ST.
           MOVE "AAAAA" TO SEED-REC.
           WRITE SEED-REC.
           CLOSE F-SEED.
      *> (1) Readers share: two clause-less OPEN INPUTs are both READ
      *> ONLY, and an updater or writer is refused beside them.
           OPEN INPUT F-A.
           OPEN INPUT F-B.
           DISPLAY "A-IN=" A-ST " B-IN=" B-ST.
           OPEN I-O F-C.
           DISPLAY "C-IO=" C-ST.
           OPEN EXTEND F-C.
           DISPLAY "C-EXT=" C-ST.
           OPEN OUTPUT F-C.
           DISPLAY "C-OUT=" C-ST.
           CLOSE F-B.
           CLOSE F-A.
      *> (2) A clause-less updater has the file to itself: it is open
      *> I-O, so any other OPEN, in any mode, is refused.
           OPEN I-O F-A.
           DISPLAY "A-IO=" A-ST.
           OPEN INPUT F-B.
           DISPLAY "B-IN2=" B-ST.
           OPEN I-O F-C.
           DISPLAY "C-IO2=" C-ST.
           CLOSE F-A.
      *> (3) The LOCK MODE clause alone does not specify sharing: F-L
      *> opens READ ONLY for INPUT beside a clause-less reader, and NO
      *> OTHER (refused beside one) for I-O.
           OPEN INPUT F-A.
           OPEN INPUT F-L.
           DISPLAY "L-IN=" L-ST.
           CLOSE F-L.
           OPEN I-O F-L.
           DISPLAY "L-IO=" L-ST.
           CLOSE F-A.
      *> (4) After the readers close the file is free again: the mode
      *> lasts for the duration of the OPEN (9.1.15), not longer.
           OPEN I-O F-L.
           DISPLAY "L-IO2=" L-ST.
           CLOSE F-L.
           OPEN INPUT F-B.
           DISPLAY "B-IN3=" B-ST.
           CLOSE F-B.
      *> (5) OPEN OUTPUT with the file free: 00, and the successful
      *> execution creates the file with no records (14.9.27.4 GR18), so
      *> the first READ through another connector is at end ('10').
           OPEN OUTPUT F-C.
           DISPLAY "C-OUT2=" C-ST.
           CLOSE F-C.
           OPEN INPUT F-A.
           READ F-A AT END DISPLAY "EMPTY=" A-ST END-READ.
           CLOSE F-A.
           STOP RUN.
