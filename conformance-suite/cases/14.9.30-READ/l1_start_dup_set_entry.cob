      *>--- conformance-case
      *> id: l1_start_dup_set_entry
      *> rules: [GR-14.9.30.4-21, GR-14.9.30.4-26]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.41.4 GR17 e) 1 - after a START the file position
      *> indicator holds a KEY VALUE and nothing else:
      *>   "The file position indicator is set to the value of the key
      *>    of reference in the first logical record whose key
      *>    satisfies the comparison"
      *> so it cannot record WHICH record of a duplicate set the search
      *> stopped on.  14.9.30.4 GR21 rule d) therefore compares the key
      *> alone when the previous operation was a START:
      *>   d.1 "If NEXT is specified or implied, the record to be made
      *>        available is the first existing record in the physical
      *>        file whose key of reference value is greater than or
      *>        equal to the key value in the file position indicator"
      *>   d.2 "If PREVIOUS is specified and the previous operation on
      *>        the file was a START statement, the first existing
      *>        record in the physical file whose key of reference
      *>        value is less than or equal to the key value in the
      *>        file position indicator."
      *> and GR26 settles which end of the duplicate set "first" names
      *> in each direction: duplicates are made available "in the same
      *> order, or, in the case of PREVIOUS, in the reverse order, in
      *> which they are released".  So a START into a duplicate set is
      *> entered at the FIRST-released record going forward and at the
      *> LAST-released record going backward, WHICHEVER DIRECTION THE
      *> START ITSELF SEARCHED IN.  Modelling the position as the pair
      *> (key, release ordinal) instead made each READ resume from the
      *> record the START stopped on and lose the rest of the set.
      *>
      *> Released OUT of prime order so release order (03,01,02) and
      *> prime order (01,02,03) disagree inside the PP set:
      *>   03/PP  01/PP  02/PP  04/QQ
      *> LEG A - FORWARD START (>= PP), then walk BACKWARD.  The
      *>   forward search stops on 03, but the FPI holds only "PP", so
      *>   d.2 enters the set at its LAST-released record: 02, 01, 03,
      *>   then at end.  Statuses are GR27 b): '02' while the record
      *>   immediately PRECEDING duplicates - 03 is first in the set,
      *>   so nothing precedes it and it reports '00'.
      *> LEG B - REVERSE START (<= PP), then walk FORWARD.  The reverse
      *>   search stops on 02, and d.1 enters the set at its
      *>   FIRST-released record: 03, 01, 02, then 04/QQ, then at end.
      *>   Statuses are GR27 a): '02' while the FOLLOWING record
      *>   duplicates - 02 is followed by 04/QQ, so it reports '00'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1STD01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1std01.dat"
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
           OPEN OUTPUT F.
           MOVE "03" TO R-KEY.
           MOVE "PP" TO R-ALT.
           WRITE R.
           MOVE "01" TO R-KEY.
           MOVE "PP" TO R-ALT.
           WRITE R.
           MOVE "02" TO R-KEY.
           MOVE "PP" TO R-ALT.
           WRITE R.
           MOVE "04" TO R-KEY.
           MOVE "QQ" TO R-ALT.
           WRITE R.
           CLOSE F.
           OPEN INPUT F.
      *> LEG A - forward START into the duplicate set, walk backward.
           MOVE "PP" TO R-ALT.
           START F KEY IS NOT LESS R-ALT
               INVALID KEY DISPLAY "A-START-INV"
           END-START.
           DISPLAY "A0=" F-ST.
           READ F PREVIOUS AT END DISPLAY "A1-ATEND" END-READ.
           DISPLAY "A1=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "A2-ATEND" END-READ.
           DISPLAY "A2=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "A3-ATEND" END-READ.
           DISPLAY "A3=" F-ST " " R-KEY "/" R-ALT.
           READ F PREVIOUS AT END DISPLAY "A4-ATEND=" F-ST END-READ.
      *> LEG B - reverse START into the duplicate set, walk forward.
           MOVE "PP" TO R-ALT.
           START F KEY IS NOT GREATER R-ALT
               INVALID KEY DISPLAY "B-START-INV"
           END-START.
           DISPLAY "B0=" F-ST.
           READ F NEXT AT END DISPLAY "B1-ATEND" END-READ.
           DISPLAY "B1=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "B2-ATEND" END-READ.
           DISPLAY "B2=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "B3-ATEND" END-READ.
           DISPLAY "B3=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "B4-ATEND" END-READ.
           DISPLAY "B4=" F-ST " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "B5-ATEND=" F-ST END-READ.
           CLOSE F.
           STOP RUN.
