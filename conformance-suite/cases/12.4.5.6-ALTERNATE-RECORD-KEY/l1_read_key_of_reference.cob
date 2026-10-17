      *>--- conformance-case
      *> id: l1_read_key_of_reference
      *> rules: [GR-12.4.5.6.4-1, GR-14.9.30.4-30, GR-14.9.30.4-31]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR30 and GR31 — which key becomes the key of
      *> reference on a Format-2 (random) READ of an indexed file, and
      *> how long it lasts.
      *> GR30 "if the KEY phrase is specified, data-name-1 or
      *>   record-key-name-1 is established as the key of reference for
      *>   this retrieval.  If the dynamic access mode is specified,
      *>   this key of reference is also used for retrievals by any
      *>   subsequent executions of sequential format READ statements
      *>   ... until a different key of reference is established."
      *> GR31 is the same sentence for an ABSENT KEY phrase, with "the
      *>   prime record key" established instead.
      *> The key of reference is not directly displayable, so it is
      *> measured by the record the FOLLOWING sequential READ selects
      *> (§14.9.30 GR21 rule e reads it through the file position
      *> indicator).  The file is built so the two orders are exact
      *> opposites — prime 01 02 03 04 against alternate AA(04) BB(03)
      *> CC(02) DD(01) — and each leg's second line is the answer only
      *> one of them can give.
      *>
      *> LEG A (GR30, KEY IS written).  A1 fetches 02 by its ALTERNATE
      *> key "CC".  A2 is the sequential read that follows: in
      *> alternate-key order the record after CC is DD, i.e. 01.  A
      *> prime key of reference would have given 03.  A3 confirms DD is
      *> the last record under that key of reference (at end '10').
      *> LEG B (GR31, no KEY phrase).  B1 re-establishes the ALTERNATE
      *> as the key of reference, so B2's bare random READ is the one
      *> under test: GR31 makes the PRIME key the key of reference for
      *> that retrieval and for the reads after it.  B3 must therefore
      *> walk in PRIME order and give 02.  Had B2 left the alternate
      *> standing, B3 would be the at end '10' (nothing follows DD).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD30A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rd30a.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT
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
           OPEN OUTPUT F.
           MOVE "01" TO R-KEY.
           MOVE "DD" TO R-ALT.
           WRITE R.
           MOVE "02" TO R-KEY.
           MOVE "CC" TO R-ALT.
           WRITE R.
           MOVE "03" TO R-KEY.
           MOVE "BB" TO R-ALT.
           WRITE R.
           MOVE "04" TO R-KEY.
           MOVE "AA" TO R-ALT.
           WRITE R.
           CLOSE F.
      *> LEG A - GR30: the KEY phrase names the alternate key.
           OPEN INPUT F.
           MOVE "CC" TO R-ALT.
           READ F KEY IS R-ALT
               INVALID KEY DISPLAY "A1-INV"
           END-READ.
           DISPLAY "A1=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A2-ATEND" END-READ.
           DISPLAY "A2=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A3-ATEND=" F-ST END-READ.
           CLOSE F.
      *> LEG B - GR31: no KEY phrase, so the prime record key.
           OPEN INPUT F.
           MOVE "CC" TO R-ALT.
           READ F KEY IS R-ALT
               INVALID KEY DISPLAY "B1-INV"
           END-READ.
           DISPLAY "B1=" F-ST " " R-KEY "/" R-ALT.
           MOVE "01" TO R-KEY.
           READ F INVALID KEY DISPLAY "B2-INV" END-READ.
           DISPLAY "B2=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "B3-ATEND" END-READ.
           DISPLAY "B3=" F-ST " " R-KEY "/" R-ALT.
           CLOSE F.
           STOP RUN.
