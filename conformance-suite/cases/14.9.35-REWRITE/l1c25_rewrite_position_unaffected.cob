      *>--- conformance-case
      *> id: l1c25_rewrite_position_unaffected
      *> rules: [GR-14.9.35.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.4 GR13 — REWRITE leaves the file position
      *> indicator where the preceding READ set it (sequential file;
      *> relative file in sequential and dynamic access).
      *> "The file position indicator in the rewrite file connector is
      *> not affected by the execution of a REWRITE statement."
      *> cite.py: OK  §14.9.35.4 13)  (General rules)
      *> cite.py: OK  §14.9.35.4 5)  (General rules) - sequential
      *>   access: the REWRITE replaces the record accessed by the READ
      *> cite.py: OK  §14.9.30.4 21)  (General rules) - c) "If the file
      *>   position indicator was established by a prior successful
      *>   READ statement, the first existing record in the physical
      *>   file whose relative key number is greater than the file
      *>   position indicator if NEXT is specified or implied" (the
      *>   relative-file and sequential-file lists state it alike);
      *>   f) the READ sets the file position indicator to the
      *>   (relative) record number of the record made available.
      *> So a following READ NEXT delivers the record after the one the
      *> last READ delivered, however the REWRITE was targeted; a
      *> REWRITE that moved the indicator to its own target would skip
      *> or repeat records.
      *> Four records AAAAA..DDDDD in each file.
      *> Derivation (each line):
      *>  SEQ: READ -> AAAAA, READ -> BBBBB (FPI=2); REWRITE 2 -> 00;
      *>       READ -> record 3: "SEQ ST=00 NEXT=CCCCC".
      *>  RSQ: relative, sequential access, same steps:
      *>       "RSQ ST=00 NEXT=CCCCC".
      *>  RDY: relative, dynamic access: READ key 1 (FPI=1); REWRITE
      *>       key 3 by RELATIVE KEY (random form) -> 00; READ NEXT ->
      *>       first record with number > 1 = record 2:
      *>       "RDY ST=00 NEXT=BBBBB". Record 3 now holds "33333".
      *>  Read-back: "SEQ2=22222", "RSQ2=22222", "RDY3=33333".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C25C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-FILE ASSIGN TO "L1C25C1.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS S-ST.
           SELECT Q-FILE ASSIGN TO "L1C25C2.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS Q-ST.
           SELECT D-FILE ASSIGN TO "L1C25C3.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS D-KEY
               FILE STATUS IS D-ST.
       DATA DIVISION.
       FILE SECTION.
       FD S-FILE.
       01 S-REC PIC X(5).
       FD Q-FILE.
       01 Q-REC PIC X(5).
       FD D-FILE.
       01 D-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 S-ST  PIC XX.
       01 Q-ST  PIC XX.
       01 D-ST  PIC XX.
       01 D-KEY PIC 9(4).
       01 W-ST  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT S-FILE Q-FILE D-FILE.
           MOVE "AAAAA" TO S-REC Q-REC D-REC.
           MOVE 1 TO D-KEY.
           WRITE S-REC. WRITE Q-REC. WRITE D-REC.
           MOVE "BBBBB" TO S-REC Q-REC D-REC.
           MOVE 2 TO D-KEY.
           WRITE S-REC. WRITE Q-REC. WRITE D-REC.
           MOVE "CCCCC" TO S-REC Q-REC D-REC.
           MOVE 3 TO D-KEY.
           WRITE S-REC. WRITE Q-REC. WRITE D-REC.
           MOVE "DDDDD" TO S-REC Q-REC D-REC.
           MOVE 4 TO D-KEY.
           WRITE S-REC. WRITE Q-REC. WRITE D-REC.
           CLOSE S-FILE Q-FILE D-FILE.
      *> Sequential organization.
           OPEN I-O S-FILE.
           READ S-FILE. READ S-FILE.
           MOVE "22222" TO S-REC.
           REWRITE S-REC.
           MOVE S-ST TO W-ST.
           READ S-FILE.
           DISPLAY "SEQ ST=" W-ST " NEXT=" S-REC.
           CLOSE S-FILE.
      *> Relative organization, sequential access.
           OPEN I-O Q-FILE.
           READ Q-FILE. READ Q-FILE.
           MOVE "22222" TO Q-REC.
           REWRITE Q-REC.
           MOVE Q-ST TO W-ST.
           READ Q-FILE.
           DISPLAY "RSQ ST=" W-ST " NEXT=" Q-REC.
           CLOSE Q-FILE.
      *> Relative organization, dynamic access.
           OPEN I-O D-FILE.
           MOVE 1 TO D-KEY.
           READ D-FILE.
           MOVE 3 TO D-KEY.
           MOVE "33333" TO D-REC.
           REWRITE D-REC.
           MOVE D-ST TO W-ST.
           READ D-FILE NEXT RECORD.
           DISPLAY "RDY ST=" W-ST " NEXT=" D-REC.
           CLOSE D-FILE.
      *> Read-back.
           OPEN INPUT S-FILE Q-FILE D-FILE.
           READ S-FILE. READ S-FILE.
           DISPLAY "SEQ2=" S-REC.
           READ Q-FILE. READ Q-FILE.
           DISPLAY "RSQ2=" Q-REC.
           MOVE 3 TO D-KEY.
           READ D-FILE.
           DISPLAY "RDY3=" D-REC.
           CLOSE S-FILE Q-FILE D-FILE.
           STOP RUN.
