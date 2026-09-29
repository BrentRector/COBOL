      *> EVERY CLAUSE A FILE DESCRIPTION FORMAT PRINTS IS ADMITTED IN IT (kb/Work PB1238).
      *> ISO/IEC 1989:2023 §13.4.5.3 SR5: "Format 1 is the file description entry for a
      *> sequential file"; SR7: "Format 2 is the file description entry for a relative file
      *> or an indexed file". The rendered §13.4.5.2 Format 1 carries IS GLOBAL, BLOCK
      *> CONTAINS, the RECORD clause, the LINAGE clause and CODE-SET; Format 2 carries IS
      *> GLOBAL, BLOCK CONTAINS and the RECORD clause. The per-format admissibility screen
      *> (COBOLNET2604) must refuse NONE of them — this program fails to compile if the
      *> screen's table drops a clause from the format that prints it. LABEL RECORDS is the
      *> COBOL-85 clause that stood in every '85 FD format. The relative leg then proves the
      *> Format 2 clauses bound: record 3 written at RELATIVE KEY 3 reads back at key 3
      *> (§14.9.30.4), so the display is REL-REC-03.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1238FA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb1238sq.txt".
           SELECT RL ASSIGN TO "pb1238rl.dat" ORGANIZATION RELATIVE
               ACCESS MODE RANDOM RELATIVE KEY RK.
       DATA DIVISION.
       FILE SECTION.
       FD SQ IS GLOBAL LABEL RECORDS STANDARD BLOCK CONTAINS 2 RECORDS
             RECORD CONTAINS 10 CHARACTERS LINAGE IS 20 LINES
             CODE-SET IS AL1.
       01 SQ-REC PIC X(10).
       FD RL IS GLOBAL LABEL RECORDS STANDARD BLOCK CONTAINS 2 RECORDS
             RECORD CONTAINS 10 CHARACTERS.
       01 RL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
           OPEN OUTPUT RL
           MOVE 3 TO RK
           MOVE "REL-REC-03" TO RL-REC
           WRITE RL-REC
           CLOSE RL
           OPEN INPUT RL
           MOVE SPACES TO RL-REC
           MOVE 3 TO RK
           READ RL
           DISPLAY RL-REC
           CLOSE RL
           STOP RUN.
