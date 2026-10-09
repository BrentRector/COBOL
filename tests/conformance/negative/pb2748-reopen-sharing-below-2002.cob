      *> reject-at: 85
      *> kb/Work PB2748 - the 2002 golden pb2748_reopen_answers_41
      *> re-opens an open connector with an OPEN statement that writes
      *> a SHARING phrase; the phrase is a COBOL-2002 introduction
      *> (ISO 14.9.27), so below 2002 the statement is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2748GATE85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2748g.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F
           OPEN EXTEND SHARING WITH NO OTHER F
           STOP RUN.
