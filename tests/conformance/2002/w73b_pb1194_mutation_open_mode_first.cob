      *> kb/Work PB1194 - a REWRITE or DELETE RECORD through a file
      *> connector that is not open in the I-O mode answers '49' even
      *> when ANOTHER connector holds a lock on the record it names.
      *> ISO 14.9.35.4 GR3: "If the open mode is some other value or
      *> the file is not open, the I-O status in the rewrite file
      *> connector is set to '49'"; 14.9.10.4 GR1 and 9.1.13.7 9) give
      *> DELETE RECORD the same '49'. A connector that is not open has
      *> no sharing mode in effect (9.1.15: the sharing mode applies
      *> "throughout the duration of this OPEN"), so GR11's record
      *> operation conflict cannot arise for it. For a connector open
      *> INPUT both GR3's '49' and GR11's '51' apply; 9.1.13.1 lets the
      *> implementor choose, and DOC-A.1-104 (2) tests the statement's
      *> rules in their own order, so GR3's '49' is placed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BPB1194.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "w73b_pb1194.idx" ORGANIZATION INDEXED
               ACCESS MODE RANDOM RECORD KEY IS KA
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS STA.
           SELECT FB ASSIGN TO "w73b_pb1194.idx" ORGANIZATION INDEXED
               ACCESS MODE RANDOM RECORD KEY IS KB
               SHARING WITH ALL OTHER LOCK MODE IS AUTOMATIC
               FILE STATUS IS STB.
           SELECT RA ASSIGN TO "w73b_pb1194.rel" ORGANIZATION RELATIVE
               ACCESS MODE RANDOM RELATIVE KEY IS RKA
               SHARING WITH ALL OTHER LOCK MODE IS MANUAL
               FILE STATUS IS STRA.
           SELECT RB ASSIGN TO "w73b_pb1194.rel" ORGANIZATION RELATIVE
               ACCESS MODE RANDOM RELATIVE KEY IS RKB
               SHARING WITH ALL OTHER LOCK MODE IS AUTOMATIC
               FILE STATUS IS STRB.
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 RECA.
          05 KA PIC X(4).
          05 DA PIC X(6).
       FD FB.
       01 RECB.
          05 KB PIC X(4).
          05 DB PIC X(6).
       FD RA.
       01 RRECA PIC X(10).
       FD RB.
       01 RRECB PIC X(10).
       WORKING-STORAGE SECTION.
       01 STA  PIC XX.
       01 STB  PIC XX.
       01 STRA PIC XX.
       01 STRB PIC XX.
       01 RKA  PIC 9(4).
       01 RKB  PIC 9(4).
       PROCEDURE DIVISION.
           OPEN OUTPUT FB.
           MOVE "K001" TO KB. MOVE "ORIG" TO DB.
           WRITE RECB.
           CLOSE FB.
      *> FB reads K001 under AUTOMATIC locking, so K001 is locked by FB.
           OPEN I-O FB.
           MOVE "K001" TO KB.
           READ FB.
           DISPLAY "IDX-READ-LOCKS=" STB.
      *> FA is not open: GR3's '49', not GR11's '51'.
           MOVE "K001" TO KA. MOVE "NEWA" TO DA.
           REWRITE RECA.
           DISPLAY "IDX-REWRITE-NOT-OPEN=" STA.
           DELETE FA.
           DISPLAY "IDX-DELETE-NOT-OPEN=" STA.
      *> FA is open INPUT: '49' and '51' both apply; '49' is placed.
           OPEN INPUT FA.
           DISPLAY "IDX-OPEN-INPUT=" STA.
           MOVE "K001" TO KA.
           REWRITE RECA.
           DISPLAY "IDX-REWRITE-INPUT=" STA.
           DELETE FA.
           DISPLAY "IDX-DELETE-INPUT=" STA.
           CLOSE FA.
      *> Once FA is open I-O, the lock FB holds is FA's conflict: '51'.
           OPEN I-O FA.
           MOVE "K001" TO KA. MOVE "NEWA" TO DA.
           REWRITE RECA.
           DISPLAY "IDX-REWRITE-I-O=" STA.
           CLOSE FA.
           CLOSE FB.
      *> The relative organization, the same rule.
           OPEN OUTPUT RB.
           MOVE 1 TO RKB. MOVE "REL-ORIG" TO RRECB.
           WRITE RRECB.
           CLOSE RB.
           OPEN I-O RB.
           MOVE 1 TO RKB.
           READ RB.
           DISPLAY "REL-READ-LOCKS=" STRB.
           MOVE 1 TO RKA. MOVE "REL-NEWA" TO RRECA.
           REWRITE RRECA.
           DISPLAY "REL-REWRITE-NOT-OPEN=" STRA.
           DELETE RA.
           DISPLAY "REL-DELETE-NOT-OPEN=" STRA.
           CLOSE RB.
           STOP RUN.
