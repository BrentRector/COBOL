      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB917 - ISO 5.2.6.2 and 5.2.7 (cite.py OK on both).  12.4.5.1 prints [ RESERVE integer-1 [AREA|AREAS] ] once with no ellipsis (only ALTERNATE RECORD KEY
      *> and the collating-sequence clause carry one), so RESERVE written twice in a file control entry is non-conforming.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB917SELRESERVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB917SELRESERVE.dat"
               RESERVE 2 AREAS RESERVE 3 AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
