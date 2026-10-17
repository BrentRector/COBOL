*>--- conformance-case
*> id: pb338_random_read_conflict_position
*> rules: [GR-14.9.30.4-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.30.4 GR10 a) and d) on the FORMAT-2 (random) READ —
      *> "If the record operation conflict condition exists as a result
      *> of the READ statement: a) The file position indicator is
      *> unchanged. ... d) The key of reference for indexed files is
      *> unchanged."  GR10 is an ALL FORMATS rule; the two legs below
      *> are the only two organizations Format 2 admits.
      *>
      *> LEG I — THE KEY OF REFERENCE (GR10 d).  One indexed file, prime
      *> key P1/P2/P3 carrying AAAAA/BBBBB/CCCCC and an ALTERNATE key
      *> whose order is the exact REVERSE (Z3/Z2/Z1), so the two walks
      *> diverge and the answer names which one ran.
      *>   I-ST : START B KEY IS NOT LESS THAN the alternate key makes
      *>          the ALTERNATE the key of reference (§14.9.41.4 GR16).
      *>   I-N1 : the first READ NEXT then delivers the LOWEST alternate
      *>          key, Z1 — the record whose prime key is P3, CCCCC
      *>          (§14.9.30.4 GR21 indexed rule d) 1.).
      *>   I-A  : the other connector locks the record whose PRIME key
      *>          is P3 with a random READ ... WITH LOCK (§12.4.5.9.4
      *>          GR5 — manual locking obtains a lock only with the
      *>          explicit phrase).
      *>   I-R  : B now executes a FORMAT-2 random READ on its PRIME
      *>          key P3.  §14.9.30.4 GR30/GR31 would establish the
      *>          prime key as B's key of reference "for this retrieval"
      *>          — but the record is locked by another file connector,
      *>          so GR9 + §9.1.13.8 item 1 make it '51' and 14.9.30.4 GR10 d)
      *>          leaves the key of reference UNCHANGED: still the
      *>          alternate.  GR10 a) likewise leaves the file position
      *>          indicator at Z1.
      *>   I-N2 : THE DISCRIMINATOR.  A READ NEXT under ACCESS DYNAMIC
      *>          continues in the key of reference (GR30/GR31 —
      *>          "this key of reference is also used for retrievals by
      *>          any subsequent executions of sequential format READ
      *>          statements"), so it delivers the next record in
      *>          ALTERNATE order after Z1: Z2, whose data is BBBBB.
      *>          Had the refused READ established the prime key of
      *>          reference and moved the indicator to P3, this READ
      *>          would answer '10' — P3 is the LAST record in prime
      *>          order (kb/Work PB338).
      *>
      *> LEG R — THE FILE POSITION INDICATOR (GR10 a).  One relative
      *> file, five records.  §14.9.30.4 GR29: "For a relative file,
      *> execution of a READ statement sets the file position indicator
      *> to the value contained in the data item referenced by the
      *> RELATIVE KEY clause for the file".
      *>   R-N1 : a sequential READ NEXT leaves the indicator at RRN 1.
      *>   R-A  : the other connector locks RRN 4.
      *>   R-R  : B's FORMAT-2 random READ of RRN 4 is refused '51', and
      *>          GR10 a) forbids GR29's assignment — the indicator is
      *>          still 1.
      *>   R-N2 : so the next sequential READ delivers RRN 2 (GR21
      *>          relative rule c), "greater than the file position
      *>          indicator"), NOT RRN 5.
      *>
      *> EXPECTED, derived before it was observed:
      *>   I-ST=00 I-N1=00 CCCCC I-A=00 I-R=51 I-N2=00 BBBBB
      *>   R-N1=00 RK=0001 R-A=00 R-R=51 R-N2=00 RK=0002
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB338RND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEED-I ASSIGN TO "pb338rnd-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS I-KEY
               ALTERNATE RECORD KEY IS I-ALT
               FILE STATUS IS ST0.
           SELECT IA ASSIGN TO "pb338rnd-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IA-KEY
               ALTERNATE RECORD KEY IS IA-ALT
               FILE STATUS IS STIA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT IB ASSIGN TO "pb338rnd-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IB-KEY
               ALTERNATE RECORD KEY IS IB-ALT
               FILE STATUS IS STIB
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SEED-R ASSIGN TO "pb338rnd-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT RA ASSIGN TO "pb338rnd-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KRA
               FILE STATUS IS STRA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT RB ASSIGN TO "pb338rnd-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS KRB
               FILE STATUS IS STRB
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD SEED-I.
       01 IS-REC.
          05 I-KEY  PIC X(2).
          05 I-ALT  PIC X(2).
          05 I-DATA PIC X(5).
       FD IA.
       01 IA-REC.
          05 IA-KEY  PIC X(2).
          05 IA-ALT  PIC X(2).
          05 IA-DATA PIC X(5).
       FD IB.
       01 IB-REC.
          05 IB-KEY  PIC X(2).
          05 IB-ALT  PIC X(2).
          05 IB-DATA PIC X(5).
       FD SEED-R.
       01 RS-REC PIC X(5).
       FD RA.
       01 RA-REC PIC X(5).
       FD RB.
       01 RB-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST0  PIC XX.
       01 STIA PIC XX.
       01 STIB PIC XX.
       01 STRA PIC XX.
       01 STRB PIC XX.
       01 KRA  PIC 9(4).
       01 KRB  PIC 9(4).
       01 N    PIC 9(4).
       01 SEQ  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SEED-INDEXED.
           PERFORM LEG-INDEXED.
           PERFORM SEED-RELATIVE.
           PERFORM LEG-RELATIVE.
           STOP RUN.

       SEED-INDEXED.
           OPEN OUTPUT SEED-I.
           MOVE "P1" TO I-KEY.
           MOVE "Z3" TO I-ALT.
           MOVE "AAAAA" TO I-DATA.
           WRITE IS-REC.
           MOVE "P2" TO I-KEY.
           MOVE "Z2" TO I-ALT.
           MOVE "BBBBB" TO I-DATA.
           WRITE IS-REC.
           MOVE "P3" TO I-KEY.
           MOVE "Z1" TO I-ALT.
           MOVE "CCCCC" TO I-DATA.
           WRITE IS-REC.
           CLOSE SEED-I.

       LEG-INDEXED.
           OPEN I-O IA.
           OPEN I-O IB.
           MOVE "Z1" TO IB-ALT.
           START IB KEY IS NOT LESS THAN IB-ALT.
           DISPLAY "I-ST=" STIB.
           READ IB NEXT RECORD.
           DISPLAY "I-N1=" STIB " " IB-DATA.
           MOVE "P3" TO IA-KEY.
           READ IA WITH LOCK.
           DISPLAY "I-A =" STIA.
           MOVE "P3" TO IB-KEY.
           READ IB KEY IS IB-KEY.
           DISPLAY "I-R =" STIB.
           READ IB NEXT RECORD.
           DISPLAY "I-N2=" STIB " " IB-DATA.
           CLOSE IA.
           CLOSE IB.

       SEED-RELATIVE.
           OPEN OUTPUT SEED-R.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 5
               MOVE N TO SEQ
               MOVE SEQ TO RS-REC
               WRITE RS-REC
           END-PERFORM.
           CLOSE SEED-R.

       LEG-RELATIVE.
           OPEN I-O RA.
           OPEN I-O RB.
           READ RB NEXT RECORD.
           DISPLAY "R-N1=" STRB " RK=" KRB.
           MOVE 4 TO KRA.
           READ RA WITH LOCK.
           DISPLAY "R-A =" STRA.
           MOVE 4 TO KRB.
           READ RB.
           DISPLAY "R-R =" STRB.
           READ RB NEXT RECORD.
           DISPLAY "R-N2=" STRB " RK=" KRB.
           CLOSE RA.
           CLOSE RB.
