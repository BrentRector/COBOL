      *>--- conformance-case
      *> id: l1_read_indexed_seq_walk
      *> rules: [GR-14.9.30.4-21]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR21 indexed rule e — the sequential walk when
      *> the previous operation was a successful READ and the key of
      *> reference is NOT a duplicate-permitting alternate (the prime
      *> key here).
      *> e.1 "If NEXT is specified or implied, the record to be made
      *>      available is the first existing record in the physical
      *>      file whose key value is greater than or equal to the key
      *>      value in the file position indicator."
      *> e.2 "If PREVIOUS is specified, the first existing record in
      *>      the physical file whose key value is less than or equal
      *>      to the key value in the file position indicator."
      *> e.3 "If no such record is found, the at end condition exists
      *>      and execution proceeds as indicated in General rule 24.
      *>      Otherwise, the first record in the physical file whose
      *>      key value is greater than the key of reference is made
      *>      available."
      *> Rule g sets the file position indicator to the key of the
      *> record MADE AVAILABLE, so e.1/e.2 read alone would re-deliver
      *> that same record for ever.  e.3's closing sentence is the
      *> corrective: the >= / <= of e.1/e.2 is the EXISTENCE test and
      *> the record made available is the first STRICTLY greater (and,
      *> symmetrically, strictly less).
      *>
      *> Keys 01/02/03.  N1 is rule d.1 (the previous operation is the
      *> OPEN; §14.9.27.4 GR14 sets the file position indicator to "the
      *> characters that have the lowest ordinal position in the
      *> collating sequence associated with the file, and the prime
      *> record key is established as the key of reference", so the
      *> first record made available is 01).
      *> N2/N3 are e.1+e.3 -> 02 then 03; a literal
      *> ">=" walk would print 01 three times.  P1/P2 are e.2+e.3 ->
      *> 02 then 01; a literal "<=" walk would print 03 for ever.
      *> P3 is e.3's first sentence: no record has a key below 01, so
      *> the at end condition exists and the I-O status is '10'
      *> (GR24 a) and the AT END imperative runs (GR24 c).
      *> The key of reference is the PRIME key throughout, so GR27's
      *> '02' cannot arise and every successful status is '00'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RDE01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rde01.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01" TO R-KEY.
           MOVE "AAA" TO R-VAL.
           WRITE R.
           MOVE "02" TO R-KEY.
           MOVE "BBB" TO R-VAL.
           WRITE R.
           MOVE "03" TO R-KEY.
           MOVE "CCC" TO R-VAL.
           WRITE R.
           CLOSE F.
           OPEN INPUT F.
      *> rule d.1 - the previous operation is the OPEN.
           READ F NEXT AT END DISPLAY "N1-ATEND" END-READ.
           DISPLAY "N1=" F-ST " " R-KEY " " R-VAL.
      *> rule e.1 + e.3 - strictly greater than the key of reference.
           READ F NEXT AT END DISPLAY "N2-ATEND" END-READ.
           DISPLAY "N2=" F-ST " " R-KEY " " R-VAL.
           READ F NEXT AT END DISPLAY "N3-ATEND" END-READ.
           DISPLAY "N3=" F-ST " " R-KEY " " R-VAL.
      *> rule e.2 + e.3 - strictly less than the key of reference.
           READ F PREVIOUS AT END DISPLAY "P1-ATEND" END-READ.
           DISPLAY "P1=" F-ST " " R-KEY " " R-VAL.
           READ F PREVIOUS AT END DISPLAY "P2-ATEND" END-READ.
           DISPLAY "P2=" F-ST " " R-KEY " " R-VAL.
      *> rule e.3 first sentence - no such record, the at end condition.
           READ F PREVIOUS AT END DISPLAY "P3-ATEND=" F-ST END-READ.
           CLOSE F.
           STOP RUN.
