*>--- conformance-case
*> id: pb357_start_length_not_integer
*> rules: [GR-9.1.13.5-3, GR-14.9.41.4-13, GR-14.9.41.4-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! START WITH LENGTH - GR14 IS ASKED OF THE EXPRESSION ITSELF (kb/Work PB357).
      *> ISO 14.9.41.4 GR14: "If arithmetic-expression-1 does not evaluate to a positive nonzero
      *> integer that is less than or equal to the length of the associated key, the I-O status
      *> value in the file connector referenced by file-name-1 is set to '23', the invalid key
      *> condition exists, and the execution of the START statement is unsuccessful."
      *> 14.9.41.3 has no rule confining arithmetic-expression-1 to an integer, so every
      *> statement below is LEGAL source; the compiler used to truncate the count to scale 0 and
      *> narrow it to 32 bits at emit time, so 2.5 became 2 and 4294967297 became 1, and all of
      *> L1-L4 positioned with '00'.
      *> DERIVATION - the file holds AB01 and AB02; the prime key is 4 characters long.
      *>  . L1 2.5, L2 3 / 2 (= 1.5), L3 an item holding 2.5: not integers -> '23', INVALID KEY.
      *>  . L4 an item holding 4294967297: an integer greater than the key's 4 -> '23'.
      *>  . L5 an item holding 2.0 (PIC 9V9): an integer, 1 <= 2 <= 4 -> GR17 b) takes the
      *>    temporary area "AB"; "AB01" = "AB" over 2 characters, the first record -> '00',
      *>    and READ NEXT delivers AB01 (9.1.13.2 rule 1: '00' on success).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB357SLN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb357sln.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 WL  PIC 9V9 VALUE 2.5.
       01 W2  PIC 9V9 VALUE 2.0.
       01 BIG PIC 9(12) VALUE 4294967297.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AB01" TO IX-KEY WRITE IX-REC
           MOVE "AB02" TO IX-KEY WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
           MOVE "AB01" TO IX-KEY
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH 2.5
               INVALID KEY DISPLAY "L1=INVALID " ST1
               NOT INVALID KEY DISPLAY "L1=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH 3 / 2
               INVALID KEY DISPLAY "L2=INVALID " ST1
               NOT INVALID KEY DISPLAY "L2=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH WL
               INVALID KEY DISPLAY "L3=INVALID " ST1
               NOT INVALID KEY DISPLAY "L3=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH BIG
               INVALID KEY DISPLAY "L4=INVALID " ST1
               NOT INVALID KEY DISPLAY "L4=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH W2
               INVALID KEY DISPLAY "L5=INVALID " ST1
               NOT INVALID KEY DISPLAY "L5=OK " ST1
           END-START
           READ IXF NEXT RECORD
               AT END DISPLAY "L5=EOF"
           END-READ
           DISPLAY "L5=" IX-KEY "|" ST1
           CLOSE IXF
           STOP RUN.
