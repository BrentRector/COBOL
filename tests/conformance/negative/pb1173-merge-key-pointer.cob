      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A MERGE KEY OF CLASS POINTER IS REFUSED BY §14.9.24.3 SR4 c).
      *>   cite.py --check 14.9.24.3 "Key data items shall not be of the class boolean, message-tag, object, or
      *>     pointer." -> OK §14.9.24.3 4) c)
      *> The MERGE twin of negative/pb1173-sort-file-key-pointer: SPR is a level-1 USAGE POINTER record of the SD, and
      *> the key rule is asked before the record's image is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173MP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173mp.tmp".
           SELECT MI1 ASSIGN TO "pb1173mp1.dat".
           SELECT MI2 ASSIGN TO "pb1173mp2.dat".
           SELECT MO1 ASSIGN TO "pb1173mpo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SPR USAGE POINTER.
       FD MI1.
       01 M1R PIC X(5).
       FD MI2.
       01 M2R PIC X(5).
       FD MO1.
       01 MOR PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY SPR USING MI1 MI2 GIVING MO1
           STOP RUN.
