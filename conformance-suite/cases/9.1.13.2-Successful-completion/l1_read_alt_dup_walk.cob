      *>--- conformance-case
      *> id: l1_read_alt_dup_walk
      *> rules: [GR-9.1.13.2-2, GR-9.1.13.4-1, GR-14.9.30.4-21, GR-14.9.30.4-26, GR-14.9.30.4-27]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR21 indexed rule f + GR27 — the sequential walk
      *> when the previous operation was a successful READ and the key
      *> of reference IS an alternate key that allows duplicates.
      *> f.1 "If NEXT is specified or implied, and there exists in the
      *>      physical file a record whose key value is equal to the
      *>      key of reference and whose logical position within the
      *>      set of duplicates is after the record that was made
      *>      available by that prior READ statement, the record within
      *>      the set of duplicates that is immediately after the
      *>      record that was made available by that prior READ
      *>      statement.  Otherwise, the first record in the physical
      *>      file whose key value is greater than the key of reference
      *>      value."
      *> f.2 is the mirror for PREVIOUS: the duplicate immediately
      *>      before, otherwise the LAST record of the greatest smaller
      *>      key.  (f.2's printed text is garbled — "is made
      *>      available." lands mid-sentence — but it mirrors f.1.)
      *> "Logical position within the set of duplicates" is fixed by
      *> GR26: duplicates are made available "in the same order, or, in
      *> the case of PREVIOUS, in the reverse order, in which they are
      *> released by execution of WRITE statements" — RELEASE order,
      *> NOT prime-key order.  The records below are therefore released
      *> OUT of prime order (03, 01, 04, 02) so the two orders differ:
      *> release order among the XX duplicates is 03, 01, 02.
      *> GR27 sets the I-O status to '02' on a successful sequential
      *> read whose key of reference is an alternate record key when
      *> a) the FOLLOWING record duplicates that key (NEXT), or
      *> b) the record immediately PRECEDING it does (PREVIOUS).
      *>
      *> Alternate-key sequence (alt value, release order):
      *>     XX/03  XX/01  XX/02  YY/04
      *> LEG A (NEXT):     03, 01, 02, 04 then at end — a prime-order
      *>   walk would give 01, 02, 03, 04 and a "next greater prime"
      *>   walk would leave 02 behind 04.  Statuses 02,02,00,00 are
      *>   GR27 a): only the last XX has no duplicate after it.
      *> LEG B (PREVIOUS): 04, 02, 01, 03 then at end — the exact
      *>   reverse (GR26).  B2 is f.2's "otherwise" arm: the LAST
      *>   record of the XX duplicate set, not its first.  Statuses
      *>   00,02,02,00 are GR27 b).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RDF01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rdf01.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT WITH DUPLICATES
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-ALT PIC X(2).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Released out of prime order so GR26 release order (03,01,02)
      *> and prime order (01,02,03) disagree on the XX duplicate set.
           OPEN OUTPUT F.
           MOVE "03" TO R-KEY.
           MOVE "XX" TO R-ALT.
           WRITE R.
           MOVE "01" TO R-KEY.
           MOVE "XX" TO R-ALT.
           WRITE R.
           MOVE "04" TO R-KEY.
           MOVE "YY" TO R-ALT.
           WRITE R.
           MOVE "02" TO R-KEY.
           MOVE "XX" TO R-ALT.
           WRITE R.
           CLOSE F.
           OPEN INPUT F.
      *> LEG A - forward over the alternate key of reference.
           MOVE "AA" TO R-ALT.
           START F KEY IS >= R-ALT
               INVALID KEY DISPLAY "A-START-INV"
           END-START.
           DISPLAY "A0=" F-ST.
           READ F NEXT AT END DISPLAY "A1-ATEND" END-READ.
           DISPLAY "A1=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A2-ATEND" END-READ.
           DISPLAY "A2=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A3-ATEND" END-READ.
           DISPLAY "A3=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A4-ATEND" END-READ.
           DISPLAY "A4=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A5-ATEND=" F-ST END-READ.
      *> LEG B - reverse over the same key of reference.  The START
      *> re-establishes a valid file position indicator after the at
      *> end above (§14.9.41 GR16/GR17).
           MOVE "ZZ" TO R-ALT.
           START F KEY IS <= R-ALT
               INVALID KEY DISPLAY "B-START-INV"
           END-START.
           DISPLAY "B0=" F-ST.
           READ F PREVIOUS AT END DISPLAY "B1-ATEND" END-READ.
           DISPLAY "B1=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "B2-ATEND" END-READ.
           DISPLAY "B2=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "B3-ATEND" END-READ.
           DISPLAY "B3=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "B4-ATEND" END-READ.
           DISPLAY "B4=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "B5-ATEND=" F-ST END-READ.
           CLOSE F.
           STOP RUN.
