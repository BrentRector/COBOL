      *> PB1120 - ISO 14.6.13.1.4 3): the second WRITE completes with status 02 (duplicate alternate
      *>   key), EC-I-O-WARNING is on, the USE declarative runs, and "the imperative-statement in
      *>   that phrase is not executed" - so NOT INVALID KEY runs for the first WRITE (00) only.
      *>   cite.py: OK 14.6.13.1.4 3)
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1120A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1120a.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K
               ALTERNATE RECORD KEY IS AK WITH DUPLICATES
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
           05 K  PIC X(4).
           05 AK PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       H-P.
           DISPLAY "DECL " FS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F
           MOVE "0001DUPE" TO R
           WRITE R
               INVALID KEY DISPLAY "INV1"
               NOT INVALID KEY DISPLAY "NOTINV1 " FS
           END-WRITE
           MOVE "0002DUPE" TO R
           WRITE R
               INVALID KEY DISPLAY "INV2"
               NOT INVALID KEY DISPLAY "NOTINV2 " FS
           END-WRITE
           DISPLAY "AFTER " FS
           CLOSE F
           STOP RUN.
