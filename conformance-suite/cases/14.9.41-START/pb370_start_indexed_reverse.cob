      *>--- conformance-case
      *> id: pb370_start_indexed_reverse
      *> rules: [GR-14.9.41.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR17 (INDEXED FILES). START behaves as if:
      *>   b) the first temporary area's length is the LENGTH value if
      *>      specified, else the length of data-name-1;
      *>   c) for EQUAL, GREATER, NOT LESS or GREATER OR EQUAL "the file
      *>      is searched sequentially", and d) for LESS, NOT GREATER or
      *>      LESS OR EQUAL "the file is searched in reverse order", the
      *>      key of reference of each record "truncated to the same
      *>      length as the first";
      *>   e) the comparison is made "with the second temporary area on
      *>      the left hand side", and then
      *>      1. "The file position indicator is set to the value of the
      *>         key of reference in the first logical record whose key
      *>         satisfies the comparison", or
      *>      2. no record satisfies it: the invalid key condition
      *>         exists.
      *> GR15: with the KEY phrase omitted the START behaves as KEY IS
      *> EQUAL TO the prime record key.
      *> FX holds prime keys AAA100, BBB200 and CCC300. Each START is
      *> followed by READ NEXT, which delivers the record the file
      *> position indicator names, so its key prints outright.
      *>   EQ   no KEY phrase, X-KEY = BBB200: GR15's EQUAL -> BBB200.
      *>   GT   KEY > BBB200: forward -> CCC300.
      *>   LT   KEY < BBB999: REVERSE (d) -> BBB200, the LARGEST key
      *>        below BBB999. A forward search would find AAA100 first,
      *>        so this is the leg that tells d) from c).
      *>   EQ3  KEY = BBB999 WITH LENGTH 3: both areas are truncated to
      *>        3 characters (b, c), BBB = BBB -> BBB200; untruncated,
      *>        no key equals BBB999 and the START would be '23'.
      *>   NG3  KEY NOT GREATER CCA WITH LENGTH 3 (the area holds
      *>        CCAZZZ): reverse over the truncated keys AAA, BBB, CCC;
      *>        CCC > CCA, so the first satisfying key in reverse is
      *>        BBB -> BBB200 (forward would give AAA100).
      *>   LT0  KEY < AAA000: nothing is below, e) 2. -> INVALID KEY,
      *>        '23'.
      *> The WITH LENGTH phrase and the reverse operators are COBOL-2002
      *> introductions, so this golden runs at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370SI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "pb370si.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD FX.
       01 X-REC.
          05 X-KEY PIC X(6).
          05 X-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FX
           MOVE "AAA100" TO X-KEY MOVE "V100" TO X-VAL WRITE X-REC
           MOVE "BBB200" TO X-KEY MOVE "V200" TO X-VAL WRITE X-REC
           MOVE "CCC300" TO X-KEY MOVE "V300" TO X-VAL WRITE X-REC
           CLOSE FX
           OPEN INPUT FX
           MOVE "BBB200" TO X-KEY
           START FX
               INVALID KEY DISPLAY "EQ INVALID"
           END-START
           READ FX NEXT AT END DISPLAY "EQ AT END" END-READ
           DISPLAY "EQ " ST " " X-KEY " " X-VAL
           MOVE "BBB200" TO X-KEY
           START FX KEY > X-KEY
               INVALID KEY DISPLAY "GT INVALID"
           END-START
           READ FX NEXT AT END DISPLAY "GT AT END" END-READ
           DISPLAY "GT " ST " " X-KEY " " X-VAL
           MOVE "BBB999" TO X-KEY
           START FX KEY < X-KEY
               INVALID KEY DISPLAY "LT INVALID"
           END-START
           READ FX NEXT AT END DISPLAY "LT AT END" END-READ
           DISPLAY "LT " ST " " X-KEY " " X-VAL
           MOVE "BBB999" TO X-KEY
           START FX KEY = X-KEY WITH LENGTH 3
               INVALID KEY DISPLAY "EQ3 INVALID"
           END-START
           READ FX NEXT AT END DISPLAY "EQ3 AT END" END-READ
           DISPLAY "EQ3 " ST " " X-KEY " " X-VAL
           MOVE "CCAZZZ" TO X-KEY
           START FX KEY IS NOT GREATER THAN X-KEY WITH LENGTH 3
               INVALID KEY DISPLAY "NG3 INVALID"
           END-START
           READ FX NEXT AT END DISPLAY "NG3 AT END" END-READ
           DISPLAY "NG3 " ST " " X-KEY " " X-VAL
           MOVE "AAA000" TO X-KEY
           START FX KEY < X-KEY
               INVALID KEY DISPLAY "LT0 INVALID"
               NOT INVALID KEY DISPLAY "LT0 FOUND"
           END-START
           DISPLAY "LT0 " ST
           CLOSE FX
           STOP RUN.
