      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB994 - AN INDEXED FILE DESCRIBED WITH ACCESS MODE RANDOM IS REFUSED AS A MERGE USING FILE.
      *>   cite.py --check 14.9.24.3 "If file-name-2 or file-name-3 references a relative or an indexed file, its access
      *>     mode shall be sequential or dynamic." -> OK §14.9.24.3 13)
      *>   cite.py --check 12.4.5.5.2 "The RANDOM clause shall not be specified for file-names specified in the USING or
      *>     GIVING phrase of a SORT or MERGE statement" -> OK §12.4.5.5.2 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB994F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB994F.tmp".
           SELECT F1 ASSIGN TO "PB994F1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK.
           SELECT F2 ASSIGN TO "PB994F2.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM
               RECORD KEY IS IK2.
           SELECT R1 ASSIGN TO "PB994F3.dat"
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
           MERGE SW ASCENDING KEY SK USING F1 F2 GIVING R1
           STOP RUN.
