      *> A LINE SEQUENTIAL FILE KEEPS THE VARIABLE-LENGTH RECORD CLAUSE (kb/Work PB1238).
      *> ISO/IEC 1989:2023 §13.4.5.3 SR4 bars only "the BLOCK CONTAINS clause" and "the
      *> RECORD CONTAINS clause" (§13.18.43.2 Formats 1 and 3) from a LINE SEQUENTIAL file;
      *> Format 2, RECORD IS VARYING ... DEPENDING ON, is not a RECORD CONTAINS clause, and
      *> §14.9.51.4 GR22 describes exactly this file: "For a line sequential file with a file
      *> description entry containing a RECORD clause with the DEPENDING phrase". The screen
      *> (COBOLNET2605) must not refuse it. The record written with LN = 3 is "ABC" (GR22:
      *> the record area is filled to the right as depending on data-name-1); read back into
      *> a space-filled area it is ABC, so the display is [ABC].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1238LV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LS ASSIGN TO "pb1238lv.txt"
               ORGANIZATION LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD LS RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
             DEPENDING ON LN.
       01 LS-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01 LN PIC 99.
       PROCEDURE DIVISION.
           OPEN OUTPUT LS
           MOVE "ABC" TO LS-REC
           MOVE 3 TO LN
           WRITE LS-REC
           CLOSE LS
           OPEN INPUT LS
           MOVE SPACES TO LS-REC
           READ LS
           DISPLAY "[" LS-REC(1:3) "]"
           CLOSE LS
           STOP RUN.
