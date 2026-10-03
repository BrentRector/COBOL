      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB994 - SORT GIVING AN INDEXED FILE WHOSE FIRST KEY IS NOT THE PRIME KEY'S BYTES IS REFUSED BY SR9.
      *>   cite.py --check 14.9.40.3 "If file-name-3 references an indexed file, the first specification of data-name-1
      *>     shall be associated with an ASCENDING phrase and the data item referenced by that data-name-1 shall begin
      *>     at the same byte location within its record and occupy the same number of bytes as the prime record key
      *>     for that file." -> OK §14.9.40.3 9)
      *> The first key SV lies at bytes 3-4 of the SD record; the prime record key IK of F1 is bytes 1-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB994B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB994B.tmp".
           SELECT F1 ASSIGN TO "PB994B1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK.
           SELECT F2 ASSIGN TO "PB994B2.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK2.
           SELECT R1 ASSIGN TO "PB994B3.dat"
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
           SORT SW ASCENDING KEY SV USING R1 GIVING F1
           STOP RUN.
