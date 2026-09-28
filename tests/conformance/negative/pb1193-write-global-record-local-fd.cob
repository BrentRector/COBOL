      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1193 - a contained program WRITEs a record that is a
      *> global name through its OWN GLOBAL clause (13.18.27.3 SR1 b)
      *> while its file description entry has no GLOBAL clause.
      *> RULE (14.9.51.3 SR21): "If record-name-1 is defined in a
      *> containing program and is referenced in a contained program,
      *> the file description entry for the file-name associated with
      *> record-name-1 shall contain a GLOBAL clause."
      *> cite.py --check 14.9.51.3 "If record-name-1 is defined in a
      *>   containing program and is referenced in a contained program,
      *>   the file description entry for the file-name associated with
      *>   record-name-1 shall contain a GLOBAL clause." -> OK 21)
      *> Before the fix the WRITE was refused only because the record
      *> was "not a logical record of any file description entry" - a
      *> false reason. COBOLNET1757 now names SR21.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1193N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1193N1.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5) GLOBAL.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           CALL "PB1193N1C"
           CLOSE F1
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1193N1C.
       PROCEDURE DIVISION.
           MOVE "INNER" TO R1
           WRITE R1
           EXIT PROGRAM.
       END PROGRAM PB1193N1C.
       END PROGRAM PB1193N1.
