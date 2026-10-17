*>--- conformance-case
*> id: pb340_advancing_on_lock_indexed
*> rules: [SR-14.9.30.3-9, GR-14.9.30.4-19, GR-14.9.30.4-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB340ADX.
      *> !! THE INDEXED TWIN OF pb340_advancing_on_lock_relative (kb/Work PB340).
      *> ISO 14.9.30.4 GR22 is a FORMAT-1 rule - ADVANCING ON LOCK appears only
      *> in the Format-1 general format (14.9.30.2) and 14.9.30.3 SR6 bars it
      *> under ACCESS MODE RANDOM - so it reaches every organization that can be
      *> read sequentially. The skip-scan lived only inside the sequential-
      *> ORGANIZATION arm of the governed read, so an indexed READ NEXT
      *> ADVANCING ON LOCK answered '51', the one status GR22 rules out: "A
      *> record operation conflict condition does not exist."
      *> Only rule d) of GR21's indexed selection is relied on here - "If the
      *> previous operation on the file was a successful OPEN or START statement"
      *> - so each PREVIOUS read below is re-anchored by its own START.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS SEED-KEY
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS A-KEY
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS B-KEY
               FILE STATUS IS B-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-C ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS C-KEY
               FILE STATUS IS C-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-D ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS D-KEY
               FILE STATUS IS D-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-E ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS E-KEY
               FILE STATUS IS E-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-F ASSIGN TO "pb340adx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-KEY
               FILE STATUS IS F-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC.
          05 SEED-KEY  PIC X(4).
          05 SEED-DATA PIC X(5).
       FD F-A.
       01 A-REC.
          05 A-KEY  PIC X(4).
          05 A-DATA PIC X(5).
       FD F-B.
       01 B-REC.
          05 B-KEY  PIC X(4).
          05 B-DATA PIC X(5).
       FD F-C.
       01 C-REC.
          05 C-KEY  PIC X(4).
          05 C-DATA PIC X(5).
       FD F-D.
       01 D-REC.
          05 D-KEY  PIC X(4).
          05 D-DATA PIC X(5).
       FD F-E.
       01 E-REC.
          05 E-KEY  PIC X(4).
          05 E-DATA PIC X(5).
       FD F-F.
       01 F-REC.
          05 F-KEY  PIC X(4).
          05 F-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST  PIC XX.
       01 A-ST     PIC XX.
       01 B-ST     PIC XX.
       01 C-ST     PIC XX.
       01 D-ST     PIC XX.
       01 E-ST     PIC XX.
       01 F-ST     PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Seed K001..K003 through an ordinary (non-sharing) connector.
           OPEN OUTPUT F-SEED.
           MOVE "K001" TO SEED-KEY. MOVE "ALPHA" TO SEED-DATA.
           WRITE SEED-REC.
           MOVE "K002" TO SEED-KEY. MOVE "BRAVO" TO SEED-DATA.
           WRITE SEED-REC.
           MOVE "K003" TO SEED-KEY. MOVE "CHARL" TO SEED-DATA.
           WRITE SEED-REC.
           CLOSE F-SEED.
           OPEN I-O F-A. OPEN I-O F-B. OPEN I-O F-C.
           OPEN I-O F-D. OPEN I-O F-E. OPEN I-O F-F.
      *> T1 - A and C take K001 and K002 WITH LOCK (GR11 d): under manual
      *> locking the lock is set only when the LOCK phrase is written).
           MOVE "K001" TO A-KEY. READ F-A WITH LOCK. DISPLAY "T1A=" A-ST.
           MOVE "K002" TO C-KEY. READ F-C WITH LOCK. DISPLAY "T1C=" C-ST.
      *> T2 - THE CONTROL. Without ADVANCING, K001's lock is the GR9 record
      *> operation conflict and GR10 b) puts '51' in the I-O status.
           READ F-D NEXT
               AT END DISPLAY "T2=EOF|" D-ST
               NOT AT END DISPLAY "T2=" D-DATA "|" D-ST
           END-READ.
           DISPLAY "T2ST=" D-ST.
      *> T3 - GR22 with the skip REPEATED: K001 and K002 are both locked by
      *> another connector, so both are read-and-discarded and K003 is the
      *> record made available. Neither NEXT nor AT END is written, so SR9's
      *> ADVANCING term is what makes this Format 1 at all.
           READ F-B ADVANCING ON LOCK.
           DISPLAY "T3=" B-KEY "/" B-DATA "|" B-ST.
      *> T4A - PREVIOUS after a START (GR21 d) 2): the first existing record
      *> whose key of reference value is less than or equal to K003 is K003
      *> itself, and nothing holds its lock yet.
           MOVE "K003" TO F-KEY.
           START F-F KEY IS EQUAL TO F-KEY. DISPLAY "T4START=" F-ST.
           READ F-F PREVIOUS ADVANCING ON LOCK
               AT END DISPLAY "T4A=EOF|" F-ST
               NOT AT END DISPLAY "T4A=" F-DATA "|" F-ST
           END-READ.
      *> T4B - the BEGINNING-OF-FILE tail of GR22. Re-anchored by a START to
      *> K001, the PREVIOUS read selects K001, finds it locked, discards it and
      *> repeats - and "the beginning of file is encountered", which GR22 sends
      *> to General rule 24: I-O status '10' and the AT END imperative.
           MOVE "K001" TO F-KEY.
           START F-F KEY IS EQUAL TO F-KEY. DISPLAY "T4BSTART=" F-ST.
           READ F-F PREVIOUS ADVANCING ON LOCK
               AT END DISPLAY "T4B=EOF|" F-ST
               NOT AT END DISPLAY "T4B=" F-DATA "|" F-ST
           END-READ.
      *> T5 - the END-OF-FILE tail. D takes K003 WITH LOCK, so every record is
      *> now locked by another connector and E's skip-scan runs off the end.
           MOVE "K003" TO D-KEY. READ F-D WITH LOCK. DISPLAY "T5D=" D-ST.
           READ F-E NEXT ADVANCING ON LOCK
               AT END DISPLAY "T5=EOF|" E-ST
               NOT AT END DISPLAY "T5=" E-DATA "|" E-ST
           END-READ.
           CLOSE F-A. CLOSE F-B. CLOSE F-C.
           CLOSE F-D. CLOSE F-E. CLOSE F-F.
           STOP RUN.
