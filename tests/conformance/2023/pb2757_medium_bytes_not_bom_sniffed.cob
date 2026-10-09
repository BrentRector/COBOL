      *> kb/Work PB2757 -- a sequential file's medium is read as BYTES,
      *> never sniffed for a Unicode byte-order mark.
      *>   python scripts/spec/cite.py --check 14.9.30.4 "is made
      *>   available in the record area"   -> OK  §14.9.30.4 13) c)
      *>   python scripts/spec/cite.py --check 9.1.7.2 "In record
      *>   sequential files the length of each record is determined by
      *>   any information the implementor may add to the record on the
      *>   physical storage medium"         -> OK  §9.1.7.2
      *> (1) A fixed record sequential file adds no information to the
      *> medium (docs/CONFORMANCE.md DOC-A.1-146 (a)), so its record is
      *> the next 4 bytes. The first record's first field is PIC S9(4)
      *> COMP = -2, whose bytes are X'FFFE' -- the UTF-16LE byte-order
      *> mark -- and X'FEFF' (UTF-16BE) and X'EFBBBF' (UTF-8) open the
      *> next two files. Each record READs back exactly as written, and
      *> a REWRITE replaces exactly the record read.
      *> (2) A LINE SEQUENTIAL file is UTF-8 text whose opening UTF-8
      *> byte-order mark is accepted and is not record data
      *> (DOC-A.1-115 (b)): the line EF BB BF 41 C3 A9 42 reads as the
      *> three characters "A", e-acute, "B" with status '00', and a
      *> REWRITE replaces those 4 bytes in place, leaving the mark.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2757BOM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RS ASSIGN TO "pb2757rs.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
           SELECT LB ASSIGN TO "pb2757ls.txt"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
           SELECT LS ASSIGN TO "pb2757ls.txt"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD RS.
       01 RS-REC.
          05 RS-N PIC S9(4) COMP.
          05 RS-T PIC XX.
       01 RS-RAW PIC X(4).
       FD LB.
       01 LB-REC PIC X(10).
       FD LS.
       01 LS-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 ST      PIC XX.
       01 WS-N    PIC -9(4).
       01 WS-LEAD PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> (1a) the PIC S9(4) COMP -2 record (X'FFFE' first)
           OPEN OUTPUT RS
           MOVE -2 TO RS-N
           MOVE "AB" TO RS-T
           WRITE RS-REC
           MOVE "CDEF" TO RS-RAW
           WRITE RS-RAW
           CLOSE RS
           OPEN I-O RS
           READ RS
           MOVE RS-N TO WS-N
           DISPLAY "RS1 " ST " " WS-N " " RS-T
           MOVE "XY" TO RS-T
           REWRITE RS-REC
           DISPLAY "RW1 " ST
           READ RS
           DISPLAY "RS2 " ST " " RS-RAW
           READ RS
           DISPLAY "RS3 " ST
           CLOSE RS
           OPEN INPUT RS
           READ RS
           MOVE RS-N TO WS-N
           DISPLAY "RR1 " ST " " WS-N " " RS-T
           READ RS
           DISPLAY "RR2 " ST " " RS-RAW
           CLOSE RS
      *> (1b) X'FEFF' (PIC S9(4) COMP = -257) and X'EFBBBF' opening a
      *> fixed record file. No literal X'FF' is compared: a medium
      *> byte X'FF' reads back as HIGH-VALUE (DOC-A.1-31).
           OPEN OUTPUT RS
           MOVE -257 TO RS-N
           MOVE "AB" TO RS-T
           WRITE RS-REC
           MOVE "CDEF" TO RS-RAW
           WRITE RS-RAW
           CLOSE RS
           OPEN INPUT RS
           READ RS
           MOVE RS-N TO WS-N
           DISPLAY "FE1 " ST " " WS-N " " RS-T
           READ RS
           DISPLAY "FE2 " ST " " RS-RAW
           CLOSE RS
           MOVE X'EFBBBF41' TO WS-LEAD
           OPEN OUTPUT RS
           MOVE WS-LEAD TO RS-RAW
           WRITE RS-RAW
           MOVE "CDEF" TO RS-RAW
           WRITE RS-RAW
           CLOSE RS
           OPEN INPUT RS
           READ RS
           IF RS-RAW = WS-LEAD
               DISPLAY "EF1 " ST " MARK IS RECORD DATA"
           ELSE
               DISPLAY "EF1 " ST " MARK LOST"
           END-IF
           READ RS
           DISPLAY "EF2 " ST " " RS-RAW
           CLOSE RS
      *> (2) a UTF-8-BOM line sequential file with a non-ASCII record
           OPEN OUTPUT LB
           MOVE X'EFBBBF41C3A9420A430A' TO LB-REC
           WRITE LB-REC
           CLOSE LB
           OPEN INPUT LS
           READ LS
           IF LS-REC = "AéB"
               DISPLAY "LS1 " ST " A-EACUTE-B"
           ELSE
               DISPLAY "LS1 " ST " WRONG"
           END-IF
           READ LS
           DISPLAY "LS2 " ST " " LS-REC
           CLOSE LS
           OPEN I-O LS
           READ LS
           MOVE "XéY" TO LS-REC
           REWRITE LS-REC
           DISPLAY "LW1 " ST
           CLOSE LS
           OPEN INPUT LB
           READ LB
           IF LB-REC = X'EFBBBF58C3A9590A430A'
               DISPLAY "LB1 " ST " BOM KEPT, LINE REPLACED IN PLACE"
           ELSE
               DISPLAY "LB1 " ST " BYTES WRONG"
           END-IF
           CLOSE LB
           STOP RUN.
