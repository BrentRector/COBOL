      *> PB1120 control - with EC-I-O-WARNING on but NO handler (no declarative, no WHEN) the 02
      *>   is only recorded (EXCEPTION-STATUS) and the NOT INVALID KEY phrase runs as usual: the gate
      *>   asks "was it handled", not "was it raised".
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1120C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1120c.dat"
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
           WRITE R
               INVALID KEY DISPLAY "INV2"
               NOT INVALID KEY DISPLAY "NOTINV2 " FS
           END-WRITE
           DISPLAY "AFTER " FS " " FUNCTION EXCEPTION-STATUS
           CLOSE F
           STOP RUN.
