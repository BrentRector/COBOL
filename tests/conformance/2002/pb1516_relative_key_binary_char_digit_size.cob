      *> kb/Work PB1516 -- the sequential-READ '14' test of a RELATIVE KEY
      *> that has no PICTURE, read in DIGITS.
      *>   python scripts/spec/cite.py --check 9.1.13.4 "the number of
      *>   significant digits in the relative record number is larger
      *>   than the size of the relative key data item described for
      *>   the file"                            -> OK  §9.1.13.4 2)
      *>   python scripts/spec/cite.py --check 14.9.30.4 "number of
      *>   significant digits"                  -> OK  §14.9.30.4 21) d)
      *> DERIVATION. §9.1.13.4 2) and §14.9.30.4 GR21 d) state the test
      *> in DIGITS: I-O status 14 when the number of SIGNIFICANT DIGITS
      *> of the relative record number is larger than the SIZE of the
      *> relative key data item. The standard gives a PICTURE-less
      *> BINARY-CHAR UNSIGNED a RANGE (§13.18.60.4 GR12: 0 <= n < 256),
      *> never a digit size, so its size in digits is implementor-
      *> defined, and WiseOwl COBOL's is the decimal width of the range's
      *> maximum magnitude: 3 (PicInfo.BinaryItem; docs/CONFORMANCE.md
      *> "DETERMINATION -- the size of a PICTURE-less relative key").
      *> Records sit at RRN 255, 256 and 1000 of one relative file,
      *> written through a PIC 9(4) key and read back sequentially
      *> through a BINARY-CHAR UNSIGNED key:
      *>   RRN 255  -> 3 digits, not larger than 3            -> '00'
      *>   RRN 256  -> 3 digits, not larger than 3            -> '00'
      *>              (a capacity reading, 0..255, would say '14' here;
      *>              no clause states it, which is what this leg pins)
      *>   RRN 1000 -> 4 digits, larger than 3                -> '14'
      *> The third READ takes AT END (status class 1, §14.9.30.4 GR24 c).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1516G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW ASSIGN TO "pb1516.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WK
               FILE STATUS IS ST-W.
           SELECT RR ASSIGN TO "pb1516.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
       DATA DIVISION.
       FILE SECTION.
       FD RW.
       01 RW-REC PIC X(4).
       FD RR.
       01 RR-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WK PIC 9(4).
       01 RK USAGE BINARY-CHAR UNSIGNED.
       01 ST-W PIC XX.
       01 ST-R PIC XX.
       01 AT-END PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RW
           MOVE 255 TO WK
           MOVE "R255" TO RW-REC
           WRITE RW-REC
           MOVE 256 TO WK
           MOVE "R256" TO RW-REC
           WRITE RW-REC
           MOVE 1000 TO WK
           MOVE "R1K0" TO RW-REC
           WRITE RW-REC
           CLOSE RW
           OPEN INPUT RR
           READ RR AT END MOVE "Y" TO AT-END END-READ
           DISPLAY "RRN-255 STATUS=" ST-R " REC=" RR-REC
           READ RR AT END MOVE "Y" TO AT-END END-READ
           DISPLAY "RRN-256 STATUS=" ST-R " REC=" RR-REC
           DISPLAY "AT-END-SO-FAR=" AT-END
           READ RR AT END MOVE "Y" TO AT-END END-READ
           DISPLAY "RRN-1000 STATUS=" ST-R
           DISPLAY "AT-END=" AT-END
           CLOSE RR
           STOP RUN.
