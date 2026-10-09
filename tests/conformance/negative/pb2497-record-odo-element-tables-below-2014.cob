      *> reject-at: 85 2002
      *> kb/Work PB2497 - a file record whose OCCURS DEPENDING table has
      *> variable-length elements is written and read back (determination
      *> D-FRA (viii)), but the DYNAMIC LENGTH clause that makes the
      *> elements variable-length is a COBOL-2014 addition (ISO 8.5.1.10),
      *> so below 2014 the record is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2497NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb2497n.seq" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QREC.
          05 QK PIC 9.
          05 QE OCCURS 1 TO 3 DEPENDING ON QK.
             10 QD PIC X DYNAMIC LENGTH LIMIT 5.
             10 QX PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT QF
           MOVE 1 TO QK
           MOVE "ab" TO QD(1)
           WRITE QREC
           CLOSE QF
           OPEN INPUT QF
           READ QF
           CLOSE QF
           STOP RUN.
