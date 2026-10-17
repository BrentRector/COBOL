      *>--- conformance-case
      *> id: pb338_read_conflict_position
      *> rules: [GR-14.9.30.4-10, GR-14.9.30.4-13]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR10 a) — "If the record operation conflict
      *> condition exists as a result of the READ statement: a) The file
      *> position indicator is unchanged."  GR13 a) says it from the
      *> other side: "The I-O status associated with file-name-1 is
      *> updated and, if the record operation conflict condition did not
      *> occur, the file position indicator is set."
      *>
      *> THE SUBJECT.  Three legs, one per ORGANIZATION — record
      *> sequential, relative and indexed — differing in NOTHING but the
      *> ORGANIZATION clause.  Each seeds three records through a plain
      *> connector, opens two SHARING WITH ALL OTHER / LOCK MODE MANUAL
      *> connectors A and B on it, has A lock the FIRST record with
      *> READ ... WITH LOCK (§12.4.5.9.4 GR5 — a manual lock is obtained
      *> only with the explicit phrase), then walks B.
      *>
      *> B1.  §14.9.30.4 GR9 — "If record locking is enabled for the
      *> file connector referenced by file-name-1 and the record
      *> identified for access is locked by another file connector ...
      *> If the RETRY phrase is not specified ... the record operation
      *> conflict condition exists."  §9.1.13.8 item 1 prices it '51'.
      *> The record identified for access is the FIRST existing record:
      *> B's file position indicator was established by its own OPEN, so
      *> §14.9.30.4 GR21 selects "the first existing record that is
      *> selected ... regardless of whether NEXT or PREVIOUS is
      *> specified" (relative rule b, sequential rule b) / indexed rule
      *> d) 1.).  GR10 c) makes the record area undefined, so B1 shows
      *> the STATUS only.
      *>
      *> B2 — THE POINT OF THE TEST.  After the lock is released
      *> (UNLOCK, §14.9.47.4 GR1), B's very next sequential READ shall
      *> deliver THAT SAME FIRST RECORD, because GR10 a) left B's file
      *> position indicator exactly where its OPEN put it.  '46' is NOT
      *> the answer: §9.1.13.7 item 6 requires BOTH that "a sequential
      *> READ statement is attempted referencing a file connector open
      *> in the input or I-O mode AND NO VALID NEXT RECORD HAS BEEN
      *> ESTABLISHED" and one of its two causes, and GR10 a) is exactly
      *> §14.9.30.4 GR18's "Unless otherwise specified" — GR18's blanket
      *> invalidation of the position on an unsuccessful READ is
      *> displaced here, so a valid next record IS still established.
      *> (Read the other way, GR10 a) and GR10 d) would have no
      *> observable consequence anywhere, and a '51' on a
      *> sequential-ORGANIZATION file — which has no START to reposition
      *> with — would be unrecoverable.)
      *>
      *> B3 is the control: the walk continues normally to the SECOND
      *> record, proving B2 was a re-delivery of the first and not a
      *> stalled indicator.
      *>
      *> LEG P - THE OTHER DIRECTION.  The record identified for access
      *> is direction-sensitive: §14.9.30.4 GR21 relative rule c)
      *> selects the record "greater than the file position indicator if
      *> NEXT is specified or implied or is less than the file position
      *> indicator if PREVIOUS is specified", so GR10 a) has to hold for
      *> a READ PREVIOUS as well.  The relative pair is reopened (which
      *> resets both indicators, §14.9.27.4 GR14); B walks forward to
      *> the third record, A then locks the SECOND with two
      *> READ ... WITH LOCK - single record locking releases the first
      *> (§12.4.5.9.4 GR6) so A holds exactly that one - and B's
      *> READ PREVIOUS is refused '51'.  After the UNLOCK the SAME
      *> statement delivers the SECOND record, because B's indicator is
      *> still on the third.  Were the refused read to have moved it,
      *> the second PREVIOUS would deliver the FIRST record instead.
      *>
      *> EXPECTED, derived before it was observed:
      *>   ?-A =00, ?-B1=51, ?-B2=00 AAAAA, ?-B3=00 BBBBB   (? = S/R/I)
      *>   P-B3=00 CCCCC, P-A2=00, P-P1=51, P-P2=00 BBBBB
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB338POS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEED-S ASSIGN TO "pb338pos-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT SA ASSIGN TO "pb338pos-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STSA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SB ASSIGN TO "pb338pos-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STSB
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SEED-R ASSIGN TO "pb338pos-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT RA ASSIGN TO "pb338pos-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS KRA
               FILE STATUS IS STRA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT RB ASSIGN TO "pb338pos-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS KRB
               FILE STATUS IS STRB
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SEED-I ASSIGN TO "pb338pos-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS I-KEY
               FILE STATUS IS ST0.
           SELECT IA ASSIGN TO "pb338pos-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IA-KEY
               FILE STATUS IS STIA
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT IB ASSIGN TO "pb338pos-i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IB-KEY
               FILE STATUS IS STIB
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD SEED-S.
       01 SS-REC PIC X(5).
       FD SA.
       01 SA-REC PIC X(5).
       FD SB.
       01 SB-REC PIC X(5).
       FD SEED-R.
       01 RS-REC PIC X(5).
       FD RA.
       01 RA-REC PIC X(5).
       FD RB.
       01 RB-REC PIC X(5).
       FD SEED-I.
       01 IS-REC.
          05 I-KEY  PIC X(2).
          05 I-DATA PIC X(5).
       FD IA.
       01 IA-REC.
          05 IA-KEY  PIC X(2).
          05 IA-DATA PIC X(5).
       FD IB.
       01 IB-REC.
          05 IB-KEY  PIC X(2).
          05 IB-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST0  PIC XX.
       01 STSA PIC XX.
       01 STSB PIC XX.
       01 STRA PIC XX.
       01 STRB PIC XX.
       01 STIA PIC XX.
       01 STIB PIC XX.
       01 KRA  PIC 9(4).
       01 KRB  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SEED-SEQUENTIAL.
           PERFORM LEG-SEQUENTIAL.
           PERFORM SEED-RELATIVE.
           PERFORM LEG-RELATIVE.
           PERFORM SEED-INDEXED.
           PERFORM LEG-INDEXED.
           PERFORM LEG-PREVIOUS.
           STOP RUN.

       SEED-SEQUENTIAL.
           OPEN OUTPUT SEED-S.
           MOVE "AAAAA" TO SS-REC.
           WRITE SS-REC.
           MOVE "BBBBB" TO SS-REC.
           WRITE SS-REC.
           MOVE "CCCCC" TO SS-REC.
           WRITE SS-REC.
           CLOSE SEED-S.

       LEG-SEQUENTIAL.
           OPEN I-O SA.
           OPEN I-O SB.
           READ SA WITH LOCK.
           DISPLAY "S-A =" STSA.
           READ SB.
           DISPLAY "S-B1=" STSB.
           UNLOCK SA RECORD.
           READ SB.
           DISPLAY "S-B2=" STSB " " SB-REC.
           READ SB.
           DISPLAY "S-B3=" STSB " " SB-REC.
           CLOSE SA.
           CLOSE SB.

       SEED-RELATIVE.
           OPEN OUTPUT SEED-R.
           MOVE "AAAAA" TO RS-REC.
           WRITE RS-REC.
           MOVE "BBBBB" TO RS-REC.
           WRITE RS-REC.
           MOVE "CCCCC" TO RS-REC.
           WRITE RS-REC.
           CLOSE SEED-R.

       LEG-RELATIVE.
           OPEN I-O RA.
           OPEN I-O RB.
           READ RA WITH LOCK.
           DISPLAY "R-A =" STRA.
           READ RB.
           DISPLAY "R-B1=" STRB.
           UNLOCK RA RECORD.
           READ RB.
           DISPLAY "R-B2=" STRB " " RB-REC.
           READ RB.
           DISPLAY "R-B3=" STRB " " RB-REC.
           CLOSE RA.
           CLOSE RB.

       SEED-INDEXED.
           OPEN OUTPUT SEED-I.
           MOVE "K1" TO I-KEY.
           MOVE "AAAAA" TO I-DATA.
           WRITE IS-REC.
           MOVE "K2" TO I-KEY.
           MOVE "BBBBB" TO I-DATA.
           WRITE IS-REC.
           MOVE "K3" TO I-KEY.
           MOVE "CCCCC" TO I-DATA.
           WRITE IS-REC.
           CLOSE SEED-I.

       LEG-INDEXED.
           OPEN I-O IA.
           OPEN I-O IB.
           READ IA WITH LOCK.
           DISPLAY "I-A =" STIA.
           READ IB.
           DISPLAY "I-B1=" STIB.
           UNLOCK IA RECORD.
           READ IB.
           DISPLAY "I-B2=" STIB " " IB-DATA.
           READ IB.
           DISPLAY "I-B3=" STIB " " IB-DATA.
           CLOSE IA.
           CLOSE IB.

       LEG-PREVIOUS.
           OPEN I-O RA.
           OPEN I-O RB.
           READ RB.
           READ RB.
           READ RB.
           DISPLAY "P-B3=" STRB " " RB-REC.
           READ RA WITH LOCK.
           READ RA WITH LOCK.
           DISPLAY "P-A2=" STRA.
           READ RB PREVIOUS RECORD.
           DISPLAY "P-P1=" STRB.
           UNLOCK RA RECORD.
           READ RB PREVIOUS RECORD.
           DISPLAY "P-P2=" STRB " " RB-REC.
           CLOSE RA.
           CLOSE RB.
