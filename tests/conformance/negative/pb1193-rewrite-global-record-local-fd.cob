      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1193 - the REWRITE twin of pb1193-write-global-record-
      *> local-fd: a contained program REWRITEs a record that is a global
      *> name through its OWN GLOBAL clause (13.18.27.3 SR1 b) while its
      *> file description entry has no GLOBAL clause.
      *> RULE (14.9.35.3 SR3): "If record-name-1 is defined in a
      *> containing program and is referenced in a contained program,
      *> the file description entry for the file associated with
      *> record-name-1 shall contain a GLOBAL clause."
      *> cite.py --check 14.9.35.3 "If record-name-1 is defined in a
      *>   containing program and is referenced in a contained program,
      *>   the file description entry for the file associated with
      *>   record-name-1 shall contain a GLOBAL clause." -> OK 3)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1193N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1193N2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5) GLOBAL.
       PROCEDURE DIVISION.
           OPEN I-O F1
           CALL "PB1193N2C"
           CLOSE F1
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1193N2C.
       PROCEDURE DIVISION.
           MOVE "INNER" TO R1
           REWRITE R1
           EXIT PROGRAM.
       END PROGRAM PB1193N2C.
       END PROGRAM PB1193N2.
