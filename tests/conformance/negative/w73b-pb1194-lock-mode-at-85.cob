      *> reject-at: 85
      *> kb/Work PB1194 - the edition floor of the golden
      *> 2002/w73b_pb1194_mutation_open_mode_first: record locking (the
      *> LOCK MODE clause, ISO 12.4.5.9) and the 5x record operation
      *> conflict statuses it produces arrive in COBOL 2002, so a
      *> COBOL-85 program cannot write the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BNEG1194.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FB ASSIGN TO "w73b_neg1194.idx" ORGANIZATION INDEXED
               ACCESS MODE RANDOM RECORD KEY IS KB
               LOCK MODE IS AUTOMATIC
               FILE STATUS IS STB.
       DATA DIVISION.
       FILE SECTION.
       FD FB.
       01 RECB.
          05 KB PIC X(4).
          05 DB PIC X(6).
       WORKING-STORAGE SECTION.
       01 STB PIC XX.
       PROCEDURE DIVISION.
           OPEN I-O FB.
           CLOSE FB.
           STOP RUN.
