      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB995 - MERGE'S TWIN OF THE SORT RECORD-SIZE RULES (the USING half had no owner before this note).
      *>   cite.py --check 14.9.24.3 "If the file description entry for file-name-1 describes variable-length records,
      *>     the file description entry for file-name-2 or file-name-3 shall describe neither records smaller than the
      *>     smallest record nor larger than the largest record described for file-name-1. If the file description entry
      *>     for file-name-1 describes fixed-length records, the file description entry for file-name-2 or file-name-3
      *>     shall not describe a record that is larger than the record described for file-name-1." -> OK §14.9.24.3 3)
      *>   cite.py --check 14.9.24.3 "If the GIVING phrase is specified and the file description entry for file-name-4
      *>     describes variable-length records, the file description entry for file-name-1 shall describe neither
      *>     records smaller than the smallest record nor larger than the largest record described for file-name-4. If
      *>     the file description entry for file-name-4 describes fixed-length records, the file description entry for
      *>     file-name-1 shall not describe a record that is larger than the record described for file-name-4."
      *>     -> OK §14.9.24.3 12)
      *> Each statement below breaks one arm and is refused (COBOLNET1757); the legal twin is in
      *> 85/pb995_sort_merge_record_size_ranges:
      *>   1  SD fixed 5, one USING file of 5 and the other of 9          MERGE SR3, "file-name-2 or file-name-3": the
      *>      SECOND USING file is checked as well as the first
      *>   2  SD VARYING 3 TO 5, USING files of 5 and 2 (2 < 3)           SR3, variable arm, smaller than the smallest
      *>   3  SD fixed 5, GIVING a fixed 3-byte file                      SR12, fixed arm
      *>   4  SD VARYING 3 TO 5, GIVING a variable file of 4 TO 8        SR12, variable arm, the SD's smallest is smaller
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB995NG2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SDF ASSIGN TO "pb995mf.tmp".
           SELECT SDV ASSIGN TO "pb995mv.tmp".
           SELECT IN5 ASSIGN TO "pb995m5.dat".
           SELECT IN5B ASSIGN TO "pb995m5b.dat".
           SELECT IN9 ASSIGN TO "pb995m9.dat".
           SELECT IN2 ASSIGN TO "pb995m2.dat".
           SELECT OUF3 ASSIGN TO "pb995mof3.dat".
           SELECT OUV48 ASSIGN TO "pb995mov48.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SDF.
       01 SF-REC.
          05 SF-K PIC XX.
          05 SF-X PIC XXX.
       SD SDV
           RECORD IS VARYING IN SIZE FROM 3 TO 5 CHARACTERS.
       01 SV-REC.
          05 SV-K PIC XX.
          05 SV-X PIC XXX.
       FD IN5.
       01 I5-REC PIC X(5).
       FD IN5B.
       01 I5B-REC PIC X(5).
       FD IN9.
       01 I9-REC PIC X(9).
       FD IN2.
       01 I2-REC PIC X(2).
       FD OUF3.
       01 OF3-REC PIC X(3).
       FD OUV48
           RECORD IS VARYING IN SIZE FROM 4 TO 8 CHARACTERS.
       01 OV48-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SDF ASCENDING KEY SF-K USING IN5 IN9 GIVING OUF3
           MERGE SDV ASCENDING KEY SV-K USING IN5 IN2 GIVING OUV48
           MERGE SDF ASCENDING KEY SF-K USING IN5 IN5B GIVING OUF3
           MERGE SDV ASCENDING KEY SV-K USING IN5 IN5B GIVING OUV48
           STOP RUN.
