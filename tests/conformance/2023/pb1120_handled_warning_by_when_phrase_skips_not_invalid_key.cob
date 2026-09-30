      *> PB1120 - ISO 14.6.13.1.4 2): an exception-checking PERFORM whose WHEN EC-I-O-WARNING
      *>   handles the 02 raised by the second WRITE: the WHEN runs and the NOT INVALID KEY phrase
      *>   "is also not executed"; the PERFORM body continues (IN-BODY).
      *>   cite.py: OK 14.6.13.1.4 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1120B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1120b.dat"
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
       MAIN-P.
           OPEN OUTPUT F
           MOVE "0001DUPE" TO R
           WRITE R
               INVALID KEY DISPLAY "INV1"
               NOT INVALID KEY DISPLAY "NOTINV1 " FS
           END-WRITE
           MOVE "0002DUPE" TO R
           PERFORM
               WRITE R
                   INVALID KEY DISPLAY "INV2"
                   NOT INVALID KEY DISPLAY "NOTINV2 " FS
               END-WRITE
               DISPLAY "IN-BODY"
           WHEN EC-I-O-WARNING
               DISPLAY "WHEN " FS
           END-PERFORM
           DISPLAY "AFTER " FS
           CLOSE F
           STOP RUN.
