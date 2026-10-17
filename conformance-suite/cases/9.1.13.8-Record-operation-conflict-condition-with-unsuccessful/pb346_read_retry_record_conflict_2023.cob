*>--- conformance-case
*> id: pb346_read_retry_record_conflict_2023
*> rules: [GR-9.1.13.8-2, GR-14.9.30.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB346RTY.
      *> kb/Work PB346 — §14.9.30.4 GR9 bound to §14.7.9, on the RECORD
      *> OPERATION conflict class, through BOTH READ formats.
      *>
      *> GR9: "If the RETRY phrase is specified, additional attempts may be
      *> made to read the record as specified in the rules in 14.7.9, RETRY
      *> phrase. If the RETRY phrase is not specified or the record is not
      *> successfully accessed as specified by the RETRY phrase, the record
      *> operation conflict condition exists. The I-O status is set in
      *> accordance with the rules for the RETRY phrase."
      *>
      *> Every holder here is a file connector of the EXECUTING run unit, and
      *> a connector cannot release a lock while another statement of the same
      *> run unit is executing, so every retry form exhausts. What differs is
      *> WHERE each form lands, and the four answers are four different rules:
      *>
      *>   no phrase        14.7.9.3 GR4 a) -> 9.1.13.8 item 1 = '51'
      *>   0 / -3 TIMES     GR4 a) ("arithmetic-expression-1 ... negative or
      *>                    zero")                                     = '51'
      *>   2 TIMES          GR4 b) -> GR1, count exhausts -> the clause's
      *>                    closing paragraph -> 9.1.13              = '51'
      *>   0 / -5 SECONDS   GR4 a) ("arithmetic-expression-2 ... negative or
      *>                    zero")                                     = '51'
      *>   30 SECONDS       GR4 b) -> GR2. The implementor "shall specify the
      *>                    maximum meaningful value of arithmetic-
      *>                    expression-2"; COBOL.NET defines it as ZERO
      *>                    (Annex A.1 item 166, docs/CONFORMANCE.md §7), so
      *>                    the timeout temporary holds a zero-length period,
      *>                    no attempt is made in it, and the closing
      *>                    paragraph lands the same             = '51'
      *>   FOREVER          GR4 b) -> GR3, "until the input-output operation
      *>                    has been completed" — which here never happens, so
      *>                    it is the deadlock 9.1.13.8 item 2 leaves the
      *>                    implementor to detect (A.1 item 109)      = '52'
      *>
      *> The 0-SECONDS / 30-SECONDS PAIR is the point: they agree, and they
      *> agree with 0 TIMES. 2023/pb142_retry_conflict_class_ec pins the same
      *> pair on the FILE SHARING class ('62'); nothing pinned it on the
      *> RECORD class, and nothing passed a RETRY phrase to a READ at all.
      *> The SECONDS amounts are DATA ITEMS, not literals, so GR4 a)'s "result
      *> of the evaluation" is a genuine runtime evaluation.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> Format 2 (ACCESS MODE RANDOM) — the keyed post-read governance path.
           SELECT RA ASSIGN TO "pb346rty-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RA-RRN
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS RA-ST.
           SELECT RB ASSIGN TO "pb346rty-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RB-RRN
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS RB-ST.
      *> Format 1 (ACCESS MODE SEQUENTIAL) — the pre-read conflict leg.
           SELECT SA ASSIGN TO "pb346rty-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS SA-ST.
           SELECT SB ASSIGN TO "pb346rty-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS SB-ST.
       DATA DIVISION.
       FILE SECTION.
       FD RA.
       01 RA-REC PIC X(5).
       FD RB.
       01 RB-REC PIC X(5).
       FD SA.
       01 SA-REC PIC X(5).
       FD SB.
       01 SB-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 RA-RRN   PIC 9(4).
       01 RB-RRN   PIC 9(4).
       01 RA-ST    PIC XX.
       01 RB-ST    PIC XX.
       01 SA-ST    PIC XX.
       01 SB-ST    PIC XX.
       01 ZERO-N   PIC S9(4) VALUE 0.
       01 NEG-N    PIC S9(4) VALUE -3.
       01 POS-N    PIC S9(4) VALUE 2.
       01 ZERO-SEC PIC S9(4) VALUE 0.
       01 NEG-SEC  PIC S9(4) VALUE -5.
       01 POS-SEC  PIC S9(4) VALUE 30.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SEED-FILES
      *> ── Format 2: RA locks RRN 1, RB reads it under each retry form. ──
           OPEN I-O RA
           OPEN I-O RB
           MOVE 1 TO RA-RRN
           READ RA WITH LOCK
               INVALID KEY DISPLAY "R2-SEED-INVALID"
           END-READ
           DISPLAY "R2LOCK=" RA-ST
           MOVE 1 TO RB-RRN
           READ RB INVALID KEY CONTINUE END-READ
           DISPLAY "R2NONE=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY ZERO-N TIMES INVALID KEY CONTINUE END-READ
           DISPLAY "R2T0=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY NEG-N TIMES INVALID KEY CONTINUE END-READ
           DISPLAY "R2TNEG=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY POS-N TIMES INVALID KEY CONTINUE END-READ
           DISPLAY "R2T2=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY FOR ZERO-SEC SECONDS
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "R2S0=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY FOR NEG-SEC SECONDS
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "R2SNEG=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY FOR POS-SEC SECONDS
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "R2S30=" RB-ST
           MOVE 1 TO RB-RRN
           READ RB RETRY FOREVER INVALID KEY CONTINUE END-READ
           DISPLAY "R2FVR=" RB-ST
      *> §14.9.47 GR1 — once RA releases, the same READ succeeds: the
      *> conflict was the lock, not a wedged connector.
           UNLOCK RA RECORDS
           MOVE 1 TO RB-RRN
           READ RB INVALID KEY CONTINUE END-READ
           DISPLAY "R2FREE=" RB-ST " " RB-REC
           CLOSE RA
           CLOSE RB
      *> ── Format 1: SA locks ordinal 1, SB reads it under each form. ──
           OPEN INPUT SA
           OPEN INPUT SB
           READ SA NEXT RECORD WITH LOCK
               AT END DISPLAY "S1-SEED-EOF"
           END-READ
           DISPLAY "S1LOCK=" SA-ST
           READ SB NEXT RECORD AT END CONTINUE END-READ
           DISPLAY "S1NONE=" SB-ST
           READ SB NEXT RECORD RETRY ZERO-N TIMES
               AT END CONTINUE
           END-READ
           DISPLAY "S1T0=" SB-ST
           READ SB NEXT RECORD RETRY POS-N TIMES
               AT END CONTINUE
           END-READ
           DISPLAY "S1T2=" SB-ST
           READ SB NEXT RECORD RETRY FOR ZERO-SEC SECONDS
               AT END CONTINUE
           END-READ
           DISPLAY "S1S0=" SB-ST
           READ SB NEXT RECORD RETRY FOR NEG-SEC SECONDS
               AT END CONTINUE
           END-READ
           DISPLAY "S1SNEG=" SB-ST
           READ SB NEXT RECORD RETRY FOR POS-SEC SECONDS
               AT END CONTINUE
           END-READ
           DISPLAY "S1S30=" SB-ST
           READ SB NEXT RECORD RETRY FOREVER
               AT END CONTINUE
           END-READ
           DISPLAY "S1FVR=" SB-ST
      *> §14.9.30.4 GR10 a) — every conflict above left the file position
      *> indicator unchanged, so once the lock is gone SB still reads
      *> ordinal 1, not ordinal 2.
           UNLOCK SA RECORDS
           READ SB NEXT RECORD AT END CONTINUE END-READ
           DISPLAY "S1FREE=" SB-ST " " SB-REC
           CLOSE SA
           CLOSE SB
           STOP RUN.
       SEED-FILES.
           OPEN OUTPUT RA
           MOVE 1 TO RA-RRN
           MOVE "RREC1" TO RA-REC
           WRITE RA-REC
           MOVE 2 TO RA-RRN
           MOVE "RREC2" TO RA-REC
           WRITE RA-REC
           CLOSE RA
           OPEN OUTPUT SA
           MOVE "SREC1" TO SA-REC
           WRITE SA-REC
           MOVE "SREC2" TO SA-REC
           WRITE SA-REC
           CLOSE SA.
