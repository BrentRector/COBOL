      *> kb/Work PB1276 -- a Format 1 RECORD CONTAINS integer-1 SIZES THE FILE, on an
      *> FD and on an SD alike. Until PB1276 integer-1 was stored only for a report
      *> file's line width, and every connector was sized from the largest record
      *> description, so two 10-byte WRITEs under RECORD CONTAINS 20 made ONE 20-byte
      *> record, and a sort file under RECORD CONTAINS 20 cut each record to 10.
      *>
      *> RULES (each run through scripts/spec/cite.py --check) --
      *>   13.18.43.4 GR6  "Integer-1 specifies the number of bytes contained in
      *>                   each record in the file".
      *>   13.18.43.3 SR3  "No record description entry for the file may specify a
      *>                   number of bytes greater than integer-1" -- so a SMALLER
      *>                   description (R1, SRT-REC: 10 bytes) is legal.
      *>   14.9.40.4 GR7   a USING record shorter than the sort file's fixed length
      *>                   is space filled; a record OF that length is released as
      *>                   it is, and GR16 writes it to the GIVING file unchanged.
      *>
      *> DERIVATION --
      *>   W1/W2=00        two successful WRITEs (9.1.13.2 item 1).
      *>   F2 reads R2     F2's implied Format 1 is 20 bytes (13.18.43.4 GR5 a), the
      *>                   same size GR6 gives every record of F1, so F2 reads TWO
      *>                   records, each beginning with the ten characters WRITE R1
      *>                   sent. Positions 11-20 are not shown: no record
      *>                   description of F1 describes them.
      *>   RECORDS=02, and the third READ is at end: FS=10 (9.1.13.4 item 1 a).
      *>   F1 reads R1     the same two records through F1 itself: R1 is their
      *>                   first ten bytes.
      *>   OUT=            SORT SRT (fixed length 20 by GR6) of two 20-byte USING
      *>                   records, ascending on SK: each GIVING record is the whole
      *>                   20-byte USING record (GR7/GR16), AAAA first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1276RC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1276rc1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT F2 ASSIGN TO "pb1276rc1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FIN ASSIGN TO "pb1276rc2.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FOUT ASSIGN TO "pb1276rc3.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SRT ASSIGN TO "pb1276rc4.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD CONTAINS 20 CHARACTERS.
       01  R1 PIC X(10).
       FD  F2.
       01  R2 PIC X(20).
       FD  FIN.
       01  IN-REC PIC X(20).
       FD  FOUT.
       01  OUT-REC PIC X(20).
       SD  SRT RECORD CONTAINS 20 CHARACTERS.
       01  SRT-REC.
           05 SK PIC X(4).
           05 SV PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "ABCDEFGHIJ" TO R1
           WRITE R1
           DISPLAY "W1=" FS
           MOVE "KLMNOPQRST" TO R1
           WRITE R1
           DISPLAY "W2=" FS
           CLOSE F1
           OPEN INPUT F2
           READ F2
           PERFORM UNTIL FS NOT = "00"
               ADD 1 TO N
               DISPLAY "R2=[" R2 (1:10) "]"
               READ F2
           END-PERFORM
           DISPLAY "RECORDS=" N " FS=" FS
           CLOSE F2
           OPEN INPUT F1
           READ F1
           PERFORM UNTIL FS NOT = "00"
               DISPLAY "R1=[" R1 "]"
               READ F1
           END-PERFORM
           CLOSE F1
           OPEN OUTPUT FIN
           MOVE "BBBB222222tail-two  " TO IN-REC
           WRITE IN-REC
           MOVE "AAAA111111tail-one  " TO IN-REC
           WRITE IN-REC
           CLOSE FIN
           SORT SRT ON ASCENDING KEY SK USING FIN GIVING FOUT
           OPEN INPUT FOUT
           READ FOUT
           DISPLAY "OUT=[" OUT-REC "]"
           READ FOUT
           DISPLAY "OUT=[" OUT-REC "]"
           CLOSE FOUT
           STOP RUN.
