      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB994 - MERGE GIVING AN INDEXED FILE WITH A DESCENDING FIRST KEY IS REFUSED BY SR10.
      *>   cite.py --check 14.9.24.3 "If file-name-4 references an indexed file, the first specification of data-name-1
      *>     shall be associated with an ASCENDING phrase and the data item referenced by that data-name-1 shall occupy
      *>     the same byte positions in its record as the data item associated with the prime record key for that file."
      *>     -> OK §14.9.24.3 10)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB994C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB994C.tmp".
           SELECT F1 ASSIGN TO "PB994C1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK.
           SELECT F2 ASSIGN TO "PB994C2.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK2.
           SELECT R1 ASSIGN TO "PB994C3.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC XX.
          05 SV PIC XX.
       FD F1.
       01 FR1.
          05 IK PIC XX.
          05 IV PIC XX.
       FD F2.
       01 FR2.
          05 IK2 PIC XX.
          05 IV2 PIC XX.
       FD R1.
       01 RR1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 99.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           MERGE SW DESCENDING KEY SK USING F2 R1 GIVING F1
           STOP RUN.
