      *> kb/Work PB1193 - a GLOBAL clause on a file-section level-1
      *> record under an FD WITHOUT GLOBAL makes the RECORD-NAME a
      *> global name while the FILE stays the container's alone.
      *> RULE (13.18.27.3 SR1 b): "The GLOBAL clause may be specified
      *> only in the following entries: ... A data description entry
      *> whose level-number is 1 that is specified in the file,
      *> working-storage, local-storage, or linkage section."
      *> RULE (13.18.27.4 GR1): a data-name "described using a GLOBAL
      *> clause is a global name"; GR2: "A statement in a program
      *> contained directly or indirectly within a program that
      *> describes a global name may reference that name without
      *> describing it again."
      *> The contained PB1193C reads and moves into R1 (legal); the
      *> container alone WRITEs it (14.9.51.3 SR21 forbids the WRITE in
      *> PB1193C - negative pb1193-write-global-record-local-fd). The
      *> GLOBAL FD F2's record R2 is written by the contained program,
      *> which SR21 admits.
      *> cite.py --check 13.18.27.3 "A data description entry whose
      *>   level-number is 1 that is specified in the file, working-
      *>   storage, local-storage, or linkage section." -> OK 1) b)
      *> cite.py --check 14.9.51.3 "If record-name-1 is defined in a
      *>   containing program and is referenced in a contained program"
      *>   -> OK 21)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1193M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1193F1.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "PB1193F2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5) GLOBAL.
       FD F2 GLOBAL.
       01 R2 PIC X(5).
       PROCEDURE DIVISION.
           OPEN OUTPUT F1 F2
           MOVE "OUTER" TO R1
           CALL "PB1193C"
           WRITE R1
           CLOSE F1 F2
           OPEN INPUT F1 F2
           READ F1 AT END DISPLAY "F1 EOF".
           DISPLAY "F1 " R1
           READ F2 AT END DISPLAY "F2 EOF".
           DISPLAY "F2 " R2
           CLOSE F1 F2
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1193C.
       PROCEDURE DIVISION.
           DISPLAY "C " R1
           MOVE "INNER" TO R1
           MOVE "WROTE" TO R2
           WRITE R2
           EXIT PROGRAM.
       END PROGRAM PB1193C.
       END PROGRAM PB1193M.
