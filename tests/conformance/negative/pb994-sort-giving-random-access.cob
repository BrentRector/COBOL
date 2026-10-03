      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB994 - AN INDEXED FILE DESCRIBED WITH ACCESS MODE RANDOM IS REFUSED AS A SORT GIVING FILE.
      *>   cite.py --check 12.4.5.5.2 "The RANDOM clause shall not be specified for file-names specified in the USING or
      *>     GIVING phrase of a SORT or MERGE statement" -> OK §12.4.5.5.2 1)
      *> §14.9.40.3 SR12 speaks of USING only; §12.4.5.5.2 SR1 covers GIVING as well.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB994E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB994E.tmp".
           SELECT F1 ASSIGN TO "PB994E1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK.
           SELECT F2 ASSIGN TO "PB994E2.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM
               RECORD KEY IS IK2.
           SELECT R1 ASSIGN TO "PB994E3.dat"
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
           SORT SW ASCENDING KEY SK USING F1 GIVING F2
           STOP RUN.
