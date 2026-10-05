      *> kb/Work PB1191 - ISO 14.9.51.4 GR21 / GR22 and 14.9.35.4 GR17
      *>   are keyed on RECORD-NAME-1's category, not on the file.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If record-name-1
      *>   is specified implicitly or explicitly as national, a space is
      *>   defined to be the national space character"   -> OK
      *>   14.9.51.4 21)
      *>   python scripts/spec/cite.py --check 14.9.35.4 "then a
      *>   sufficient number of the space character is appended" -> OK
      *>   14.9.35.4 17) c)
      *>   python scripts/spec/cite.py --check 14.9.40.4 "elementary data
      *>   item of usage national and of category numeric" -> OK
      *>   14.9.40.4 7) a) (the standard's own test of a national record)
      *> MW has two record descriptions, a national one (MWN) and an
      *>   alphanumeric one (MWX). Every record description of an FD
      *>   redefines the same area (13.18.33.4 GR3), but a WRITE or a
      *>   REWRITE names ONE of them, and its space, its strip and its
      *>   encoding on the line are that record's own.
      *> Derivation (each line is read back through MR, a varying FD
      *>   whose DEPENDING item receives the number of characters in the
      *>   line, 14.9.30.4):
      *>   1  WRITE MWX holding 'AB' + six spaces: record-name-1 is
      *>      alphanumeric, so the trailing alphanumeric spaces are not
      *>      transferred (GR21): the line is 'AB', LEN 02.
      *>   2  WRITE MWN holding N'XY' + two national spaces: record-name-1
      *>      is national, so the trailing national spaces are not
      *>      transferred: the line is 'XY', LEN 02.
      *>   3  WRITE UWN, PIC 9(4) USAGE NATIONAL holding 42: an elementary
      *>      item of usage national and category numeric is a national
      *>      record, so its four national digits are the line '0042'.
      *>   4  The line 'ABCD' (written through RV, a plain four-byte
      *>      record) is replaced through RWX, PIC X(2), holding 'WX':
      *>      its two bytes are FEWER than the four being replaced, so
      *>      GR17 c) appends the alphanumeric space to four bytes, the
      *>      REWRITE succeeds and the line is 'WX' + two spaces, LEN
      *>      04. (RW also has a national record description, RWN,
      *>      whose area is wider than the line; the append is RWX's,
      *>      an alphanumeric space, never a national pair.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1191C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MW ASSIGN TO "pb1191cm.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT UW ASSIGN TO "pb1191cu.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RW ASSIGN TO "pb1191cr.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RV ASSIGN TO "pb1191cr.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT MR ASSIGN TO "pb1191cm.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT UR ASSIGN TO "pb1191cu.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RR ASSIGN TO "pb1191cr.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD MW.
       01 MWN PIC N(4).
       01 MWX PIC X(8).
       FD UW.
       01 UWN PIC 9(4) USAGE NATIONAL.
       FD RW.
       01 RWN PIC N(4).
       01 RWX PIC X(2).
       FD RV.
       01 RVX PIC X(4).
       FD MR RECORD IS VARYING IN SIZE FROM 1 TO 20 DEPENDING ON RL.
       01 MRR PIC X(20).
       FD UR RECORD IS VARYING IN SIZE FROM 1 TO 20 DEPENDING ON RL.
       01 URR PIC X(20).
       FD RR RECORD IS VARYING IN SIZE FROM 1 TO 20 DEPENDING ON RL.
       01 RRR PIC X(20).
       WORKING-STORAGE SECTION.
       01 RL PIC 99.
       PROCEDURE DIVISION.
           OPEN OUTPUT MW
           MOVE "AB" TO MWX
           WRITE MWX
           MOVE N"XY" TO MWN
           WRITE MWN
           CLOSE MW
           OPEN INPUT MR
           READ MR
           DISPLAY "1 [" MRR(1:RL) "] LEN=" RL
           READ MR
           DISPLAY "2 [" MRR(1:RL) "] LEN=" RL
           CLOSE MR
           OPEN OUTPUT UW
           MOVE 42 TO UWN
           WRITE UWN
           CLOSE UW
           OPEN INPUT UR
           READ UR
           DISPLAY "3 [" URR(1:RL) "] LEN=" RL
           CLOSE UR
           OPEN OUTPUT RV
           MOVE "ABCD" TO RVX
           WRITE RVX
           CLOSE RV
           OPEN I-O RW
           READ RW
           MOVE "WX" TO RWX
           REWRITE RWX
           CLOSE RW
           OPEN INPUT RR
           READ RR
           DISPLAY "4 [" RRR(1:RL) "] LEN=" RL
           CLOSE RR
           STOP RUN.
