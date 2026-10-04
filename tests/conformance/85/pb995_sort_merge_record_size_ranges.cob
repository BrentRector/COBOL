      *> kb/Work PB995 - THE RECORD SIZES OF A SORT-MERGE FILE AND OF THE FILES OF ITS USING AND GIVING PHRASES.
      *>   cite.py --check 14.9.40.3 "If the USING phrase is specified and the file description entry for file-name-1
      *>     describes variable-length records, the file description entry for file-name-2 shall describe neither
      *>     records smaller than the smallest record nor larger than the largest record described for file-name-1.
      *>     If the file description entry for file-name-1 describes fixed-length records, the file description entry
      *>     for file-name-2 shall not describe a record that is larger than the record described for file-name-1."
      *>     -> OK §14.9.40.3 5)
      *>   cite.py --check 14.9.40.3 "If the GIVING phrase is specified and the file description entry for
      *>     file-name-3 describes variable-length records, the file description entry for file-name-1 shall describe
      *>     neither records smaller than the smallest record nor larger than the largest record described for
      *>     file-name-3. If the file description entry for file-name-3 describes fixed-length records, the file
      *>     description entry for file-name-1 shall not describe a record that is larger than the record described
      *>     for file-name-3." -> OK §14.9.40.3 11)
      *>   cite.py --check 14.9.24.3 "If the file description entry for file-name-1 describes variable-length records,
      *>     the file description entry for file-name-2 or file-name-3 shall describe neither records smaller than the
      *>     smallest record nor larger than the largest record described for file-name-1." -> OK §14.9.24.3 3)
      *>   cite.py --check 14.9.24.3 "If the GIVING phrase is specified and the file description entry for file-name-4
      *>     describes variable-length records, the file description entry for file-name-1 shall describe neither
      *>     records smaller than the smallest record nor larger than the largest record described for file-name-4."
      *>     -> OK §14.9.24.3 12)
      *> Every statement below sits on the LEGAL side of each rule, at the edge where the rule stops: a fixed-length file
      *> may be bounded by a LARGER or an EQUAL record (the rule only forbids larger), and a variable-length file's range
      *> may equal or contain the other's. The refusals are negative/pb995-sort-merge-record-size-*.
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  SORT USING a fixed 4-byte file, SD fixed 6, GIVING a fixed 8-byte file. SR5: the 4-byte USING record is
      *>      not larger than the SD's 6. SR11: the SD's 6-byte record is not larger than the GIVING file's 8. Sorted
      *>      ascending the keys read AAAA, BBBB, CCCC, DDDD (each GIVING record is its key and four spaces).
      *>   2  SORT with a variable SD of 2 TO 6 bytes, USING a variable file of 3 TO 5 (inside 2..6: SR5), GIVING a
      *>      variable file of 1 TO 8 (2..6 inside 1..8: SR11). The USING file holds "EEE", "BBBBB", "CCC", and the
      *>      SD key is the first two bytes, so the output reads BBBBB (5 bytes), CCC (3), EEE (3). The first three
      *>      bytes of each record and its length are displayed (a record is only its own length long).
      *>   3  MERGE USING two fixed 5-byte files, SD fixed 5, GIVING a variable file of 4 TO 6 (the range holds the SD's
      *>      record). USING files of EQUAL size to the SD are legal (MERGE SR3 forbids only a LARGER record), and the
      *>      GIVING range 4..6 contains 5 (SR12). The merged order is AA111, BB222, CC333, DD444.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB995OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW1 ASSIGN TO "pb995a.tmp".
           SELECT IN1 ASSIGN TO "pb995i1.dat" ORGANIZATION SEQUENTIAL.
           SELECT OU1 ASSIGN TO "pb995o1.dat" ORGANIZATION SEQUENTIAL.
           SELECT SW2 ASSIGN TO "pb995b.tmp".
           SELECT IN2 ASSIGN TO "pb995i2.dat" ORGANIZATION SEQUENTIAL.
           SELECT OU2 ASSIGN TO "pb995o2.dat" ORGANIZATION SEQUENTIAL.
           SELECT SW3 ASSIGN TO "pb995c.tmp".
           SELECT MA ASSIGN TO "pb995m1.dat" ORGANIZATION SEQUENTIAL.
           SELECT MB ASSIGN TO "pb995m2.dat" ORGANIZATION SEQUENTIAL.
           SELECT MO ASSIGN TO "pb995mo.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD SW1.
       01 S1R.
          05 S1K PIC X(4).
          05 S1F PIC X(2).
       FD IN1.
       01 I1R PIC X(4).
       FD OU1.
       01 O1R PIC X(8).
       SD SW2
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS.
       01 S2R.
          05 S2K PIC XX.
          05 S2F PIC X(4).
       FD IN2
           RECORD IS VARYING IN SIZE FROM 3 TO 5 CHARACTERS
           DEPENDING ON L2I.
       01 I2R PIC X(5).
       FD OU2
           RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS
           DEPENDING ON L2O.
       01 O2R.
          05 O2A PIC X(3).
          05 O2B PIC X(5).
       SD SW3.
       01 S3R.
          05 S3K PIC XX.
          05 S3F PIC X(3).
       FD MA.
       01 MAR PIC X(5).
       FD MB.
       01 MBR PIC X(5).
       FD MO
           RECORD IS VARYING IN SIZE FROM 4 TO 6 CHARACTERS
           DEPENDING ON L3.
       01 MOR PIC X(5).
       WORKING-STORAGE SECTION.
       01 L2I PIC 99.
       01 L2O PIC 99.
       01 L3 PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IN1
           MOVE "DDDD" TO I1R WRITE I1R
           MOVE "AAAA" TO I1R WRITE I1R
           MOVE "CCCC" TO I1R WRITE I1R
           MOVE "BBBB" TO I1R WRITE I1R
           CLOSE IN1
           SORT SW1 ASCENDING KEY S1K USING IN1 GIVING OU1
           OPEN INPUT OU1
           PERFORM 4 TIMES
               READ OU1
               DISPLAY "1 [" O1R "]"
           END-PERFORM
           CLOSE OU1

           OPEN OUTPUT IN2
           MOVE 3 TO L2I MOVE "EEE" TO I2R WRITE I2R
           MOVE 5 TO L2I MOVE "BBBBB" TO I2R WRITE I2R
           MOVE 3 TO L2I MOVE "CCC" TO I2R WRITE I2R
           CLOSE IN2
           SORT SW2 ASCENDING KEY S2K USING IN2 GIVING OU2
           OPEN INPUT OU2
           PERFORM 3 TIMES
               READ OU2
               DISPLAY "2 [" O2A "] " L2O
           END-PERFORM
           CLOSE OU2

           OPEN OUTPUT MA
           MOVE "AA111" TO MAR WRITE MAR
           MOVE "CC333" TO MAR WRITE MAR
           CLOSE MA
           OPEN OUTPUT MB
           MOVE "BB222" TO MBR WRITE MBR
           MOVE "DD444" TO MBR WRITE MBR
           CLOSE MB
           MERGE SW3 ASCENDING KEY S3K USING MA MB GIVING MO
           OPEN INPUT MO
           PERFORM 4 TIMES
               READ MO
               DISPLAY "3 [" MOR "] " L3
           END-PERFORM
           CLOSE MO
           STOP RUN.
