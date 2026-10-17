*>--- conformance-case
*> id: pb340_advancing_on_lock_relative
*> rules: [SR-14.9.30.3-9, GR-14.9.30.4-19, GR-14.9.30.4-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB340ADR.
      *> !! ADVANCING ON LOCK IS A FORMAT-1 RULE, NOT A SEQUENTIAL-ORGANIZATION
      *> ONE (kb/Work PB340). ISO 14.9.30.4 GR22: "If the ADVANCING ON LOCK
      *> phrase is specified on the READ statement of a file open for file
      *> sharing and the record to be made available is locked by another file
      *> connector, the result of this READ statement is as if the locked record
      *> were read and then the same READ statement were executed. If the record
      *> to be made available is locked by another file connector, this action is
      *> repeated until either an unlocked record is read or the end of the file
      *> is encountered if NEXT is specified or implied, or the beginning of file
      *> is encountered if PREVIOUS is specified. A record operation conflict
      *> condition does not exist."
      *> The phrase appears in the Format-1 general format only (14.9.30.2) and
      *> 14.9.30.3 SR6 bars it under ACCESS MODE RANDOM, so it is available to
      *> EVERY organization read sequentially - here RELATIVE, ACCESS DYNAMIC.
      *> The skip-scan loop used to live only inside the sequential-organization
      *> arm of the governed read, so every READ below answered '51' - precisely
      *> the status GR22 says cannot arise.
      *> 14.9.30.3 SR9 is exercised by T3: "If neither the NEXT phrase nor the
      *> PREVIOUS phrase is specified and ACCESS MODE DYNAMIC is specified in the
      *> file control entry for file-name-1, the NEXT phrase is implied if any of
      *> the following phrases is specified: ADVANCING, AT END, or NOT AT END."
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS SEED-KEY
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS A-KEY
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS B-KEY
               FILE STATUS IS B-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-C ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS C-KEY
               FILE STATUS IS C-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-D ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS D-KEY
               FILE STATUS IS D-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-E ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS E-KEY
               FILE STATUS IS E-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-F ASSIGN TO "pb340adr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS F-KEY
               FILE STATUS IS F-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-C.
       01 C-REC PIC X(5).
       FD F-D.
       01 D-REC PIC X(5).
       FD F-E.
       01 E-REC PIC X(5).
       FD F-F.
       01 F-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-KEY PIC 9(4).
       01 A-KEY    PIC 9(4).
       01 B-KEY    PIC 9(4).
       01 C-KEY    PIC 9(4).
       01 D-KEY    PIC 9(4).
       01 E-KEY    PIC 9(4).
       01 F-KEY    PIC 9(4).
       01 SEED-ST  PIC XX.
       01 A-ST     PIC XX.
       01 B-ST     PIC XX.
       01 C-ST     PIC XX.
       01 D-ST     PIC XX.
       01 E-ST     PIC XX.
       01 F-ST     PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Seed RRN 1..3 through an ordinary (non-sharing) connector.
           OPEN OUTPUT F-SEED.
           MOVE 1 TO SEED-KEY. MOVE "ALPHA" TO SEED-REC. WRITE SEED-REC.
           MOVE 2 TO SEED-KEY. MOVE "BRAVO" TO SEED-REC. WRITE SEED-REC.
           MOVE 3 TO SEED-KEY. MOVE "CHARL" TO SEED-REC. WRITE SEED-REC.
           CLOSE F-SEED.
           OPEN I-O F-A. OPEN I-O F-B. OPEN I-O F-C.
           OPEN I-O F-D. OPEN I-O F-E. OPEN I-O F-F.
      *> T1 - A and C take RRN 1 and RRN 2 WITH LOCK. 14.9.30.4 GR11 d): under
      *> manual locking the lock is set only when the LOCK phrase is written.
           MOVE 1 TO A-KEY. READ F-A WITH LOCK. DISPLAY "T1A=" A-ST.
           MOVE 2 TO C-KEY. READ F-C WITH LOCK. DISPLAY "T1C=" C-ST.
      *> T2 - THE CONTROL. Without ADVANCING, RRN 1's lock is the GR9 record
      *> operation conflict and GR10 b) puts '51' in the I-O status.
           READ F-D NEXT
               AT END DISPLAY "T2=EOF|" D-ST
               NOT AT END DISPLAY "T2=" D-REC "|" D-ST
           END-READ.
           DISPLAY "T2ST=" D-ST.
      *> T3 - GR22 with the skip REPEATED: RRN 1 and RRN 2 are both locked by
      *> another connector, so both are read-and-discarded and RRN 3 is the
      *> record made available, with a successful status and no conflict.
      *> Neither NEXT nor AT END is written, so SR9's ADVANCING term is what
      *> makes this Format 1 at all.
           READ F-B ADVANCING ON LOCK.
           DISPLAY "T3=" B-REC "|" B-ST.
      *> GR25 - "the execution of a READ statement moves the relative record
      *> number of the record made available to the relative key data item".
           DISPLAY "T3KEY=" B-KEY.
      *> T4 - the PREVIOUS direction. GR21 b): after a successful START the first
      *> existing record selected is made available "regardless of whether NEXT or
      *> PREVIOUS is specified", so the first READ PREVIOUS delivers RRN 3 itself
      *> (unlocked so far); the second walks back over the two locked records and
      *> reaches the BEGINNING of file, which GR22 sends to General rule 24 - the
      *> at end condition, I-O status '10'.
           MOVE 3 TO F-KEY.
           START F-F KEY IS EQUAL TO F-KEY. DISPLAY "T4START=" F-ST.
           READ F-F PREVIOUS ADVANCING ON LOCK
               AT END DISPLAY "T4A=EOF|" F-ST
               NOT AT END DISPLAY "T4A=" F-REC "|" F-ST
           END-READ.
           READ F-F PREVIOUS ADVANCING ON LOCK
               AT END DISPLAY "T4B=EOF|" F-ST
               NOT AT END DISPLAY "T4B=" F-REC "|" F-ST
           END-READ.
      *> T5 - the NEXT tail. D takes RRN 3 WITH LOCK, so every record in the file
      *> is now locked by another connector; E's skip-scan reaches the end of the
      *> file and GR22 hands it to GR24 - status '10' and the AT END imperative.
           MOVE 3 TO D-KEY. READ F-D WITH LOCK. DISPLAY "T5D=" D-ST.
           READ F-E NEXT ADVANCING ON LOCK
               AT END DISPLAY "T5=EOF|" E-ST
               NOT AT END DISPLAY "T5=" E-REC "|" E-ST
           END-READ.
           CLOSE F-A. CLOSE F-B. CLOSE F-C.
           CLOSE F-D. CLOSE F-E. CLOSE F-F.
           STOP RUN.
