      *> ISO §9.1.13.3 1) — I-O status 0x is Not provided (Annex A.1
      *> item 106): every successful statement, at the sites where an
      *> implementor-defined 0x could be reported, answers '00'.
      *>
      *> THE RULE. §9.1.13.3 1): "I-O status = 0x. An implementor-
      *>   defined condition exists. The value of x is specified by the
      *>   implementor ... This condition shall not duplicate any
      *>   condition specified by another I-O status value."
      *>   OK  §9.1.13.3 1)  (Implementor-defined successful completion)
      *> Annex A.1 106): "I-O status 0x (value of x). This item is
      *>   optional."   OK  §A.1 106)
      *>
      *> THE DOCUMENTED CHOICE, docs/CONFORMANCE.md DOC-A.1-106: "Not
      *>   provided. No input-output statement ever sets an I-O status
      *>   of '0' followed by a letter".
      *>
      *> DERIVATION. With no 0x condition defined, a successful
      *>   statement for which none of the standard's other successful
      *>   conditions holds can only report §9.1.13.2 1): "I-O status =
      *>   00. The input-output statement is successfully executed and
      *>   no further information is available concerning the input-
      *>   output operation."  OK  §9.1.13.2 1)
      *>   Every statement below is chosen so that NO other §9.1.13.2
      *>   value applies: 02 needs a duplicate alternate key (no file
      *>   here has one); 04 a record length outside the fixed
      *>   attributes (each file is read through its own description);
      *>   05 an OPTIONAL file that is absent (none is OPTIONAL);
      *>   06/09 a line sequential file (none); 07 NO REWIND / REEL /
      *>   UNIT / FOR REMOVAL (none written). They are the operations an
      *>   implementor would plausibly annotate with a 0x: an OPEN
      *>   OUTPUT that REPLACES an existing file (SQ-OPEN-OUT-2), an
      *>   OPEN EXTEND, a READ under OPEN I-O, a REWRITE, a START, a
      *>   DELETE, an UNLOCK, and every CLOSE, over all three
      *>   organizations. Each therefore answers 00, and the lines read
      *>   '<site>=00' throughout.
      *> UNLOCK. §9.1.13.1 names UNLOCK among the eight statements
      *>   that set the I-O status. §14.9.47.4 1): "The presence or
      *>   absence of any record locks does not affect the success of
      *>   the execution of the UNLOCK statement"  OK  §14.9.47.4 1);
      *>   §14.9.47.4 3): it "causes the value of the I-O status ... to
      *>   be updated"  OK  §14.9.47.4 3). An UNLOCK with no lock held
      *>   is the successful-with-extra-information case 0x exists
      *>   for; no §9.1.13.2 value 02-09 names UNLOCK, so it answers
      *>   00. The status is pre-set to "ZZ" so the line proves the
      *>   update happened: a stale 00 cannot fake it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DNS106.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "l1dns106q.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSQ.
           SELECT RL ASSIGN TO "l1dns106r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FSR.
           SELECT IX ASSIGN TO "l1dns106i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-K
               FILE STATUS IS FSI.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SQ-R PIC X(4).
       FD RL.
       01 RL-R PIC X(4).
       FD IX.
       01 IX-R.
          05 IX-K PIC X(2).
          05 IX-D PIC X(2).
       WORKING-STORAGE SECTION.
       01 FSQ PIC XX.
       01 FSR PIC XX.
       01 FSI PIC XX.
       01 RK  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> SEQUENTIAL
           OPEN OUTPUT SQ
           DISPLAY "SQ-OPEN-OUT-1=" FSQ
           MOVE "AAAA" TO SQ-R
           WRITE SQ-R
           DISPLAY "SQ-WRITE-1=" FSQ
           CLOSE SQ
           DISPLAY "SQ-CLOSE-1=" FSQ
           OPEN OUTPUT SQ
           DISPLAY "SQ-OPEN-OUT-2=" FSQ
           MOVE "BBBB" TO SQ-R
           WRITE SQ-R
           DISPLAY "SQ-WRITE-2=" FSQ
           CLOSE SQ
           DISPLAY "SQ-CLOSE-2=" FSQ
           OPEN EXTEND SQ
           DISPLAY "SQ-OPEN-EXTEND=" FSQ
           MOVE "CCCC" TO SQ-R
           WRITE SQ-R
           DISPLAY "SQ-WRITE-3=" FSQ
           CLOSE SQ
           DISPLAY "SQ-CLOSE-3=" FSQ
           OPEN I-O SQ
           DISPLAY "SQ-OPEN-IO=" FSQ
           MOVE "ZZ" TO FSQ
           UNLOCK SQ
           DISPLAY "SQ-UNLOCK=" FSQ
           READ SQ
           DISPLAY "SQ-READ-IO=" FSQ
           MOVE "DDDD" TO SQ-R
           REWRITE SQ-R
           DISPLAY "SQ-REWRITE=" FSQ
           CLOSE SQ
           DISPLAY "SQ-CLOSE-4=" FSQ
           OPEN INPUT SQ
           DISPLAY "SQ-OPEN-IN=" FSQ
           READ SQ
           DISPLAY "SQ-READ-1=" FSQ
           READ SQ
           DISPLAY "SQ-READ-2=" FSQ
           CLOSE SQ
           DISPLAY "SQ-CLOSE-5=" FSQ
      *> RELATIVE
           OPEN OUTPUT RL
           DISPLAY "RL-OPEN-OUT=" FSR
           MOVE 1 TO RK
           MOVE "R1R1" TO RL-R
           WRITE RL-R
           DISPLAY "RL-WRITE-1=" FSR
           MOVE 2 TO RK
           MOVE "R2R2" TO RL-R
           WRITE RL-R
           DISPLAY "RL-WRITE-2=" FSR
           CLOSE RL
           DISPLAY "RL-CLOSE-1=" FSR
           OPEN I-O RL
           DISPLAY "RL-OPEN-IO=" FSR
           MOVE "ZZ" TO FSR
           UNLOCK RL RECORDS
           DISPLAY "RL-UNLOCK=" FSR
           MOVE 1 TO RK
           READ RL
           DISPLAY "RL-READ-RANDOM=" FSR
           MOVE "R1X1" TO RL-R
           REWRITE RL-R
           DISPLAY "RL-REWRITE=" FSR
           MOVE 2 TO RK
           START RL KEY IS EQUAL TO RK
           DISPLAY "RL-START=" FSR
           READ RL NEXT
           DISPLAY "RL-READ-NEXT=" FSR
           DELETE RL
           DISPLAY "RL-DELETE=" FSR
           CLOSE RL
           DISPLAY "RL-CLOSE-2=" FSR
      *> INDEXED
           OPEN OUTPUT IX
           DISPLAY "IX-OPEN-OUT=" FSI
           MOVE "K1" TO IX-K
           MOVE "d1" TO IX-D
           WRITE IX-R
           DISPLAY "IX-WRITE-1=" FSI
           MOVE "K2" TO IX-K
           MOVE "d2" TO IX-D
           WRITE IX-R
           DISPLAY "IX-WRITE-2=" FSI
           CLOSE IX
           DISPLAY "IX-CLOSE-1=" FSI
           OPEN I-O IX
           DISPLAY "IX-OPEN-IO=" FSI
           MOVE "ZZ" TO FSI
           UNLOCK IX
           DISPLAY "IX-UNLOCK=" FSI
           MOVE "K1" TO IX-K
           READ IX
           DISPLAY "IX-READ-RANDOM=" FSI
           MOVE "x1" TO IX-D
           REWRITE IX-R
           DISPLAY "IX-REWRITE=" FSI
           MOVE "K2" TO IX-K
           START IX KEY IS EQUAL TO IX-K
           DISPLAY "IX-START=" FSI
           READ IX NEXT
           DISPLAY "IX-READ-NEXT=" FSI
           DELETE IX
           DISPLAY "IX-DELETE=" FSI
           CLOSE IX
           DISPLAY "IX-CLOSE-2=" FSI
           STOP RUN.
