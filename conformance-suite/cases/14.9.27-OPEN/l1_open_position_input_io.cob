      *>--- conformance-case
      *> id: l1_open_position_input_io
      *> rules: [GR-14.9.27.4-14]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR14 — "When the organization of the file
      *> referenced by file-name-1 is sequential or relative and the
      *> INPUT or I-O phrase is specified in the OPEN statement, the
      *> file position indicator for that file connector is set to 1.
      *> When the organization is indexed, the file position indicator
      *> is set to the characters that have the lowest ordinal position
      *> in the collating sequence associated with the file, and the
      *> prime record key is established as the key of reference."
      *> Three organizations, both phrases the rule names.
      *> SEQUENTIAL / RELATIVE, "set to 1". §14.9.30.4 GR21b: "If the
      *> file position indicator was established by a prior successful
      *> OPEN or START statement, the first existing record that is
      *> selected is made available" — so a position of 1 shows as the
      *> FIRST record. Q1/Q2 walk two records; Q3 re-opens and gets the
      *> FIRST one again (the OPEN reset the indicator, it did not
      *> resume); Q4 does the same through the I-O phrase. R1/R2/R3 are
      *> the relative twin, and §14.9.30.4 GR25 moves the relative
      *> record number of the record made available into the RELATIVE
      *> KEY item, so the indicator's value is printed outright: 0001.
      *> INDEXED, "lowest ordinal position … and the prime record key
      *> is established as the key of reference". The three records are
      *> WRITTEN K005, K003, K007 — out of prime order, which
      *> §14.9.51.4 GR39 permits in dynamic access — so "first written"
      *> and "lowest key" differ. X1 = K003 is the lowest prime key.
      *> The alternate key values are A on K005, C on K003, B on K007,
      *> so the two orderings are different sequences. A START on the
      *> alternate makes it the key of reference (§14.9.41.4 GR16) and
      *> the walk follows alternate order: K007 (B) then K003 (C).
      *> After CLOSE and a fresh OPEN INPUT the walk is K003 then K005
      *> — prime order — which is only true if the OPEN re-established
      *> the PRIME key as the key of reference; under the alternate the
      *> first two would be K005 (A) then K007 (B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FQ ASSIGN TO "l1opn14q.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT FR ASSIGN TO "l1opn14r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT FX ASSIGN TO "l1opn14x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               ALTERNATE RECORD KEY IS X-ALT
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD FQ.
       01 Q-REC PIC X(6).
       FD FR.
       01 R-REC.
          05 R-VAL PIC X(6).
       FD FX.
       01 X-REC.
          05 X-KEY PIC X(4).
          05 X-ALT PIC X(1).
          05 X-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4).
       01 ST-Q PIC XX.
       01 ST-R PIC XX.
       01 ST-X PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ---- sequential organization -----------------------------
           OPEN OUTPUT FQ
           MOVE "SEQ001" TO Q-REC
           WRITE Q-REC
           MOVE "SEQ002" TO Q-REC
           WRITE Q-REC
           MOVE "SEQ003" TO Q-REC
           WRITE Q-REC
           CLOSE FQ
           OPEN INPUT FQ
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q1=" Q-REC
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q2=" Q-REC
           CLOSE FQ
           OPEN INPUT FQ
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q3=" Q-REC
           CLOSE FQ
           OPEN I-O FQ
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q4=" Q-REC
           CLOSE FQ
      *> ---- relative organization -------------------------------
           OPEN OUTPUT FR
           MOVE 1 TO RK
           MOVE "REL001" TO R-VAL
           WRITE R-REC
           MOVE 2 TO RK
           MOVE "REL002" TO R-VAL
           WRITE R-REC
           MOVE 3 TO RK
           MOVE "REL003" TO R-VAL
           WRITE R-REC
           CLOSE FR
           OPEN INPUT FR
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "R1=" RK " " R-VAL
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "R2=" RK " " R-VAL
           CLOSE FR
           OPEN I-O FR
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "R3=" RK " " R-VAL
           CLOSE FR
      *> ---- indexed organization --------------------------------
           OPEN OUTPUT FX
           MOVE "K005" TO X-KEY
           MOVE "A" TO X-ALT
           MOVE "V05" TO X-VAL
           WRITE X-REC
           MOVE "K003" TO X-KEY
           MOVE "C" TO X-ALT
           MOVE "V03" TO X-VAL
           WRITE X-REC
           MOVE "K007" TO X-KEY
           MOVE "B" TO X-ALT
           MOVE "V07" TO X-VAL
           WRITE X-REC
           CLOSE FX
           OPEN INPUT FX
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "X1=" X-KEY
           MOVE "B" TO X-ALT
           START FX KEY IS EQUAL TO X-ALT
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XSTART=" ST-X
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "X2=" X-KEY
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "X3=" X-KEY
           CLOSE FX
           OPEN INPUT FX
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "X4=" X-KEY
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "X5=" X-KEY
           CLOSE FX
           STOP RUN.
