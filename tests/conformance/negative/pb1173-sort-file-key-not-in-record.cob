      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1173 — A FILE SORT KEY MUST BE DESCRIBED IN A RECORD OF THE SORT FILE.
      *>   cite.py --check 14.9.40.3 "The data items identified by key data-names shall be described in records
      *>     associated with file-name-1." -> OK §14.9.40.3 6) a)
      *> WK is a WORKING-STORAGE item, not part of any record of SW, so it cannot be a key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173NR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173nr.tmp".
           SELECT IN1 ASSIGN TO "pb1173nri.dat".
           SELECT OU1 ASSIGN TO "pb1173nro.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
       FD IN1.
       01 IR PIC X(5).
       FD OU1.
       01 OR1 PIC X(5).
       WORKING-STORAGE SECTION.
       01 WK PIC X.
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY WK USING IN1 GIVING OU1
           STOP RUN.
