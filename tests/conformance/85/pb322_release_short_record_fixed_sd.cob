      *> kb/Work PB322 F - THE SIZE OF THE RECORDS OF A SORT-MERGE FILE WITH NO RECORD CLAUSE, AND WHAT A RELEASE OF A
      *> SHORTER RECORD DESCRIPTION SENDS.
      *>   cite.py --check 13.18.43.4 "If the RECORD clause is not specified, an implicit format 1 or format 2 RECORD
      *>     clause is assumed to be specified." -> OK §13.18.43.4 5)   (the implementor chooses; WiseOwl COBOL implies
      *>     format 1 unless a record description is variable-length: docs/CONFORMANCE.md DOC-A.1-147)
      *>   cite.py --check 13.18.43.4 "If format 1 is implied, integer-1 shall be the record size of the largest record
      *>     description entry in this file description entry." -> OK §13.18.43.4 5) a)
      *>   cite.py --check 14.9.32.4 "If the number of bytes to be released to the sort operation is greater than the
      *>     number of bytes in record-name-1, the content of the bytes that extend beyond the end of record-name-1 are
      *>     undefined." -> OK §14.9.32.4 6)    (Annex A.2 item 45)
      *> The SD has two record descriptions, A (4 bytes) and B (10 bytes), and no RECORD clause. The implied Format 1 gives
      *> EVERY record of the sort file the largest description's size, 10 bytes, so RELEASE A releases 10 bytes: record
      *> A's four, and six bytes GR6 leaves undefined. WiseOwl COBOL's determination is the one WRITE and REWRITE make
      *> (D-WRT1, docs/CONFORMANCE.md §3): position n past the end of record-name-1 is position n of the record area
      *> the level-1 entries implicitly redefine (§13.18.33.4 GR3), so the bytes are what B holds. (Before the change the
      *> sort file held a 4-byte and a 10-byte record, and a GIVING file received `AAAA` followed by SPACES.)
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  an INPUT PROCEDURE releases "AAAAZZZZZZ" through A (B is set first, so the area holds it), "DDDDDDDDDD"
      *>      through B and "BBBBYYYYYY" through A; GIVING OU (10-byte records). Ascending on the first four bytes:
      *>      AAAAZZZZZZ, BBBBYYYYYY, DDDDDDDDDD. A release at record A's own size would put `AAAA      ` first.
      *>   2  the same three records, an OUTPUT PROCEDURE RETURNing each and displaying the 10-byte area B: the same three
      *>      lines (RETURN makes the record available in the whole area, §13.18.33.4 GR3).
      *>   3  RELEASE A FROM F4 moves F4 ("CCCC") into A first (§14.9.32.4 GR4 a) and then releases, so with B holding
      *>      "QQQQQQQQQQ" before the MOVE the record is CCCCQQQQQQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB322F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb322f.tmp".
           SELECT OU ASSIGN TO "pb322fo.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 A.
          05 AK PIC X(4).
       01 B PIC X(10).
       FD OU.
       01 OU-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 F4 PIC X(4) VALUE "CCCC".
       01 OK-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY AK INPUT PROCEDURE IS FEED GIVING OU
           OPEN INPUT OU
           PERFORM 3 TIMES
               READ OU
               DISPLAY "1 [" OU-REC "]"
           END-PERFORM
           CLOSE OU
           SORT SW ASCENDING KEY AK INPUT PROCEDURE IS FEED
                OUTPUT PROCEDURE IS DRAIN3
           SORT SW ASCENDING KEY AK INPUT PROCEDURE IS FEED-FROM
                OUTPUT PROCEDURE IS DRAIN1
           STOP RUN.
       FEED SECTION.
       FEED-1.
           MOVE "AAAAZZZZZZ" TO B
           RELEASE A
           MOVE "DDDDDDDDDD" TO B
           RELEASE B
           MOVE "BBBBYYYYYY" TO B
           RELEASE A.
       FEED-FROM SECTION.
       FEED-FROM-1.
           MOVE "QQQQQQQQQQ" TO B
           RELEASE A FROM F4.
       DRAIN3 SECTION.
       DRAIN3-1.
           PERFORM 3 TIMES
               RETURN SW AT END MOVE "Y" TO OK-FLAG END-RETURN
               DISPLAY "2 [" B "]"
           END-PERFORM.
       DRAIN1 SECTION.
       DRAIN1-1.
           RETURN SW AT END MOVE "Y" TO OK-FLAG END-RETURN
           DISPLAY "3 [" B "]".
