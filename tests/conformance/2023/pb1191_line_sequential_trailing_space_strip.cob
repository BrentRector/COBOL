      *> PB1191 - ISO 14.9.51.4 GR21 / GR22 for a LINE SEQUENTIAL file.
      *> cite.py --check 14.9.51.4 "any spaces to the right of the
      *>   rightmost non-space character are not transferred" -> OK
      *>   14.9.51.4 21)
      *> GR21: the trailing SPACES are not transferred - for a file
      *>   description "not containing a RECORD clause with the DEPENDING
      *>   phrase"; GR22: with the phrase, the record is transferred at
      *>   data-name-1's length, spaces included. A space is the
      *>   alphanumeric SPACE character - not every Unicode white space.
      *> Derivation (each line is read back through a varying-length FD,
      *>   whose DEPENDING item receives the number of characters in the
      *>   line):
      *>   A  DEPENDING FD, record 'AB' with VL = 6: the line is 'AB' and
      *>      four spaces: LEN 06.
      *>   B  fixed FD, record 'AB' + U+00A0 + spaces: U+00A0 is not the
      *>      alphanumeric space, so only the spaces after it are dropped:
      *>      the line is 'AB' U+00A0: LEN 03.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1191.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AW ASSIGN TO "pb1191a.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT AR ASSIGN TO "pb1191a.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT BW ASSIGN TO "pb1191b.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT BR ASSIGN TO "pb1191b.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD AW RECORD IS VARYING IN SIZE FROM 1 TO 10 DEPENDING ON VL.
       01 AWR PIC X(10).
       FD AR RECORD IS VARYING IN SIZE FROM 1 TO 10 DEPENDING ON RL.
       01 ARR PIC X(10).
       FD BW.
       01 BWR PIC X(8).
       FD BR RECORD IS VARYING IN SIZE FROM 1 TO 10 DEPENDING ON RL2.
       01 BRR PIC X(10).
       WORKING-STORAGE SECTION.
       01 VL PIC 99.
       01 RL PIC 99.
       01 RL2 PIC 99.
       PROCEDURE DIVISION.
           OPEN OUTPUT AW
           MOVE "AB" TO AWR
           MOVE 6 TO VL
           WRITE AWR
           CLOSE AW
           OPEN INPUT AR
           READ AR
           DISPLAY "A LEN=" RL
           CLOSE AR
           OPEN OUTPUT BW
           MOVE "AB" TO BWR
           MOVE FUNCTION CHAR(161) TO BWR(3:1)
           WRITE BWR
           CLOSE BW
           OPEN INPUT BR
           READ BR
           DISPLAY "B LEN=" RL2
           CLOSE BR
           STOP RUN.
