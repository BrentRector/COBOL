      *>--- conformance-case
      *> id: l1_delete_invalid_key_transfer
      *> rules: [GR-9.1.14-2, GR-9.1.14-3, GR-9.1.14-L2.1, GR-9.1.14-L2.2, GR-14.9.10.4-11]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.10.4 GR11 - transfer of control after a DELETE RECORD
      *> depends on the presence or absence of the optional INVALID KEY
      *> and NOT INVALID KEY phrases "as specified in 9.1.14, Invalid
      *> key condition". All four 9.1.14 arms are exercised on a
      *> relative file in ACCESS RANDOM (14.9.10.3 SR2 forbids the
      *> phrases in sequential access mode, so random is the only legal
      *> carrier), with a USE AFTER STANDARD ERROR declarative standing
      *> in for "any applicable exception processing statements"
      *> (9.1.12 item 2); its DISPLAY is the observable for whether
      *> exception processing ran.
      *>  A  invalid key condition + INVALID KEY written (9.1.14 item
      *>     2): imp-1 runs and the applicable exception processing
      *>     statements are NOT executed - "INV=23", no "D=" line.
      *>  B  no invalid key condition, successful completion (9.1.14
      *>     not-exists item 2): control goes to the NOT INVALID KEY
      *>     imperative - "NOTINV=00"; INVALID KEY is ignored.
      *>  C  invalid key condition with NO INVALID KEY phrase (9.1.14
      *>     item 3): exception processing IS executed - "D=23" - and
      *>     the NOT INVALID KEY imperative is not taken, because
      *>     not-exists item 2 runs it only on SUCCESSFUL completion.
      *>  D  an unsuccessful completion that is NOT an invalid key
      *>     condition ('49' - 9.1.13.7 item 9; the open mode is not
      *>     I-O, 14.9.10.4 GR1) with INVALID KEY written (9.1.14
      *>     not-exists item 1): the phrase does NOT cover it, so imp-1
      *>     must not run and exception processing must - "D=49"
      *>     appears and "INV-D" does not. This is the arm a blanket
      *>     phrase-suppression gets wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DEL04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1del04.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-K
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-K  PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-PARA.
           DISPLAY "D=" WS-ST.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN OUTPUT F
           MOVE 1 TO WS-K
           MOVE "ONE" TO F-REC
           WRITE F-REC
           MOVE 2 TO WS-K
           MOVE "TWO" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN I-O F
      *> A - key 9 is in no record: the invalid key condition, with the
      *> INVALID KEY phrase written.
           MOVE 9 TO WS-K
           DELETE F RECORD
               INVALID KEY DISPLAY "INV=" WS-ST
               NOT INVALID KEY DISPLAY "NOTINV-A"
           END-DELETE
      *> B - key 1 exists: successful, so NOT INVALID KEY is taken.
           MOVE 1 TO WS-K
           DELETE F RECORD
               INVALID KEY DISPLAY "INV-B"
               NOT INVALID KEY DISPLAY "NOTINV=" WS-ST
           END-DELETE
      *> C - the invalid key condition again, with NO INVALID KEY.
           MOVE 9 TO WS-K
           DELETE F RECORD
               NOT INVALID KEY DISPLAY "NOTINV-C"
           END-DELETE
           DISPLAY "C=" WS-ST
      *> D - '49': unsuccessful but NOT an invalid key condition, with
      *> the INVALID KEY phrase written.
           CLOSE F
           OPEN INPUT F
           MOVE 2 TO WS-K
           DELETE F RECORD
               INVALID KEY DISPLAY "INV-D"
           END-DELETE
           DISPLAY "E=" WS-ST
           CLOSE F
           STOP RUN.
