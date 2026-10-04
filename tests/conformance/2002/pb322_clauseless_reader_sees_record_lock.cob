       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB322CLR.
      *> kb/Work PB322 determination A with PB669. A connector with NO
      *> SHARING clause, NO LOCK MODE clause and no phrase is the
      *> implementor-defined case of 9.1.15 / 14.9.27.4 GR23 and of
      *> 12.4.5.9.4 GR1 b) 2. This compiler's defaults for it
      *> (docs/CONFORMANCE.md DOC-A.1-131 and DOC-A.1-153) are: OPEN
      *> INPUT establishes SHARING WITH READ ONLY, and the connector
      *> SETS no record lock. Table 19 (14.9.27.4) admits a READ ONLY
      *> input request beside a connector that declared SHARING WITH ALL
      *> OTHER in the input mode, and 9.1.16 then applies to the pair:
      *> "While locked by a given file connector, a record is not
      *> accessible to another file connector in the same or a different
      *> run unit, except by the execution of a READ statement with the
      *> IGNORING LOCK phrase." The clause-less reader therefore SEES the
      *> lock F-A set, though it set none itself.
      *> EXPECTED VALUES, COMPUTED FROM THE RULES:
      *>   OPENA, OPENB '00' - Table 19, request READ ONLY/input vs
      *>          existing ALL OTHER/input: Normal open (and F-A's own
      *>          ALL OTHER/input request vs nothing).
      *>   READA  '00' ALPHA - READ WITH LOCK under MANUAL locking sets
      *>          the lock (12.4.5.9.4 GR5; ALL OTHER keeps record locks
      *>          in effect, 9.1.15 3).
      *>   READB  '51' - 14.9.30.4 GR9 with no RETRY phrase is the record
      *>          operation conflict condition (9.1.13.8 item 1).
      *>   IGNB   '00' ALPHA - 14.9.30.4 GR12.
      *>   READB2 '00' BRAVO - GR9 names the record IDENTIFIED for
      *>          access; record 2 is not locked.
      *>   AFTER  '00' ALPHA - 14.9.47 GR1: UNLOCK releases F-A's locks.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "pb322clr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS A-KEY
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "pb322clr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS B-KEY
               FILE STATUS IS B-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 A-KEY PIC 9(4).
       01 B-KEY PIC 9(4).
       01 A-ST  PIC XX.
       01 B-ST  PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-B.
           MOVE 1 TO B-KEY. MOVE "ALPHA" TO B-REC. WRITE B-REC.
           MOVE 2 TO B-KEY. MOVE "BRAVO" TO B-REC. WRITE B-REC.
           CLOSE F-B.
           OPEN INPUT F-A. DISPLAY "OPENA=" A-ST.
           OPEN INPUT F-B. DISPLAY "OPENB=" B-ST.
           MOVE 1 TO A-KEY. READ F-A WITH LOCK.
           DISPLAY "READA=" A-ST " " A-REC.
           MOVE 1 TO B-KEY. READ F-B.
           DISPLAY "READB=" B-ST.
           MOVE 1 TO B-KEY. READ F-B IGNORING LOCK.
           DISPLAY "IGNB=" B-ST " " B-REC.
           MOVE 2 TO B-KEY. READ F-B.
           DISPLAY "READB2=" B-ST " " B-REC.
           UNLOCK F-A.
           MOVE 1 TO B-KEY. READ F-B.
           DISPLAY "AFTER=" B-ST " " B-REC.
           CLOSE F-A. CLOSE F-B.
           STOP RUN.
