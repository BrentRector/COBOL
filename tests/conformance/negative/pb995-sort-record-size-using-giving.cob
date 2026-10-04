      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB995 - THE RECORD SIZES OF A SORT-MERGE FILE AND THE FILES OF ITS USING AND GIVING PHRASES.
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
      *> Each statement below breaks one arm and is refused (COBOLNET1757), the legal twin of each in
      *> 85/pb995_sort_merge_record_size_ranges:
      *>   1  SD fixed 5, USING a fixed 8-byte file                    SR5, fixed arm (the USING record is larger)
      *>   2  SD VARYING 3 TO 5, USING a fixed 2-byte file            SR5, variable arm, smaller than the smallest
      *>   3  SD VARYING 3 TO 5, USING a fixed 9-byte file            SR5, variable arm, larger than the largest
      *>   4  SD fixed 5, GIVING a variable file of 1 TO 2             SR11, variable arm, the SD's record is larger
      *>   5  SD fixed 5, GIVING a fixed 3-byte file                   SR11, fixed arm
      *>   6  SD VARYING 2 TO 6, GIVING a variable file of 3 TO 8      SR11, variable arm, the SD's smallest is smaller
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB995NG1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SDF ASSIGN TO "pb995f.tmp".
           SELECT SDV ASSIGN TO "pb995v.tmp".
           SELECT SDW ASSIGN TO "pb995w.tmp".
           SELECT INBIG ASSIGN TO "pb995big.dat".
           SELECT INSML ASSIGN TO "pb995sml.dat".
           SELECT INHUG ASSIGN TO "pb995hug.dat".
           SELECT OUV12 ASSIGN TO "pb995ov12.dat".
           SELECT OUF3 ASSIGN TO "pb995of3.dat".
           SELECT OUV38 ASSIGN TO "pb995ov38.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SDF.
       01 SF-REC PIC X(5).
       SD SDV
           RECORD IS VARYING IN SIZE FROM 3 TO 5 CHARACTERS.
       01 SV-REC.
          05 SV-K PIC XX.
          05 SV-X PIC X(3).
       SD SDW
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS.
       01 SW-REC.
          05 SW-K PIC XX.
          05 SW-X PIC X(4).
       FD INBIG.
       01 IB-REC PIC X(8).
       FD INSML.
       01 IS-REC PIC X(2).
       FD INHUG.
       01 IH-REC PIC X(9).
       FD OUV12
           RECORD IS VARYING IN SIZE FROM 1 TO 2 CHARACTERS.
       01 OV12-REC PIC X(2).
       FD OUF3.
       01 OF3-REC PIC X(3).
       FD OUV38
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS.
       01 OV38-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           SORT SDF ASCENDING KEY SF-REC USING INBIG GIVING OUF3
           SORT SDV ASCENDING KEY SV-K USING INSML GIVING OUV12
           SORT SDV ASCENDING KEY SV-K USING INHUG GIVING OUV38
           SORT SDF ASCENDING KEY SF-REC USING INSML GIVING OUV12
           SORT SDF ASCENDING KEY SF-REC USING INSML GIVING OUF3
           SORT SDW ASCENDING KEY SW-K USING INSML GIVING OUV38
           STOP RUN.
