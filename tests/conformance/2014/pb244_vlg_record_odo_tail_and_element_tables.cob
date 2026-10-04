      *> kb/Work PB244 shape (b) - a VARIABLE-LENGTH RECORD that combines a
      *> dynamic-length item with the OCCURS DEPENDING table that ends it, and
      *> one whose dynamic-length item sits inside a fixed OCCURS table, round
      *> trips through a sequential file.
      *>
      *> 8.5.1.11.2: "a variable-length data item behaves in all respects as
      *> though it were in fact contiguous with its neighbors whenever a
      *> procedural operation is applied to a group containing it" - so a WRITE
      *> sends the contiguous image (14.9.11.4 GR7's documented format, the same
      *> one DISPLAY shows, docs/CONFORMANCE.md DOC-A.1-57) and a READ into the
      *> same description gives every member back the content it was written with
      *> (determination D-FRA). 13.18.38.3 SR22 makes the occurs-depending table
      *> the record's trailing storage, so the record's own length says how many
      *> occurrences it holds, and 13.18.38.4 GR8 uses only the part the DEPENDING
      *> item names when the record is SENT.
      *>
      *> EXPECTED VALUES, DERIVED (each READ displays the record image at its
      *> current extent, then the DEPENDING item the READ restored):
      *>   Q1 QK=2 QA="abc" QT(1..2)="xy"  -> [2abcxy] 2
      *>   Q2 QK=3 QA="de"  QT(1..3)="pqr" -> [3depqr] 3
      *>   Q3 QK=1 QA=""    QT(1)="m"      -> [1m] 1
      *>   E1 EH "H" + (EDD "ab" EFX "1") + (EDD "c" EFX "2")  -> [Hab1c2]
      *>   E2 EH "K" + (EDD ""   EFX "7") + (EDD "xyz" EFX "8") -> [K7xyz8]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244REC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb244rec.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT EF ASSIGN TO "pb244rec.el"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QR.
          05 QK PIC 9.
          05 QA PIC X DYNAMIC LENGTH LIMIT 5.
          05 QT PIC X OCCURS 1 TO 4 DEPENDING ON QK.
       FD EF.
       01 ER.
          05 EH PIC X.
          05 ETE OCCURS 2.
             10 EDD PIC X DYNAMIC LENGTH LIMIT 5.
             10 EFX PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT QF
           MOVE 2 TO QK
           MOVE "abc" TO QA
           MOVE "x" TO QT(1)
           MOVE "y" TO QT(2)
           WRITE QR
           MOVE 3 TO QK
           MOVE "de" TO QA
           MOVE "p" TO QT(1)
           MOVE "q" TO QT(2)
           MOVE "r" TO QT(3)
           WRITE QR
           MOVE 1 TO QK
           MOVE "" TO QA
           MOVE "m" TO QT(1)
           WRITE QR
           CLOSE QF
           OPEN INPUT QF
           PERFORM 3 TIMES
               READ QF
               DISPLAY "Q=[" QR "] " QK
           END-PERFORM
           CLOSE QF
           OPEN OUTPUT EF
           MOVE "H" TO EH
           MOVE "ab" TO EDD(1)
           MOVE "1" TO EFX(1)
           MOVE "c" TO EDD(2)
           MOVE "2" TO EFX(2)
           WRITE ER
           MOVE "K" TO EH
           MOVE "" TO EDD(1)
           MOVE "7" TO EFX(1)
           MOVE "xyz" TO EDD(2)
           MOVE "8" TO EFX(2)
           WRITE ER
           CLOSE EF
           OPEN INPUT EF
           PERFORM 2 TIMES
               READ EF
               DISPLAY "E=[" ER "]"
           END-PERFORM
           CLOSE EF
           STOP RUN.
