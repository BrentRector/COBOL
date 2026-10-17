      *>--- conformance-case
      *> id: l1c24_relative_record_numbering
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.13.4 GR1 — relative record numbers start at 1 and
      *> give each record's logical ordinal position in the file
      *> "All records stored in a relative file are uniquely identified
      *> by relative record numbers. The relative record number of a
      *> given record specifies the record's logical ordinal position
      *> in the file. The first logical record has a relative record
      *> number of 1, and subsequent logical records have relative
      *> record numbers of 2, 3, 4, ... ."
      *>   cite.py --check 12.4.5.13.4 "The first logical record has a
      *>   relative record number of 1" -> OK §12.4.5.13.4 1)
      *> Supporting rules:
      *>   cite.py --check 14.9.51.4 "If the open mode of the write
      *>   file connector is output, the first record released after
      *>   the OPEN is 1" -> OK §14.9.51.4 29) a)
      *>   cite.py --check 14.9.30.4 "the execution of a READ statement
      *>   moves the relative record number of the record made
      *>   available to the relative key data item" -> OK §14.9.30.4 25)
      *>   cite.py --check 14.9.30.4 "the record whose relative record
      *>   number equals the file position indicator is made
      *>   available" -> OK §14.9.30.4 29)
      *>   cite.py --check 14.9.30.4 "Comparisons for records in
      *>   relative files relate to the relative key number"
      *>     -> OK §14.9.30.4 21) a)
      *> DERIVATION
      *> A: sequential-access OPEN OUTPUT, RELATIVE KEY preset to 77.
      *>    The first record released is RRN 1 and the next ones are
      *>    2, 3 (GR1; §14.9.51.4 29) a) moves each into the key):
      *>    W1 0001 / W2 0002 / W3 0003 - never 0077, 0078 or 0000.
      *> B: the same physical file through a DYNAMIC connector; key 2
      *>    READ selects the record at ordinal position 2, the second
      *>    one written: R2 BBBB 0002.
      *> C: a RANDOM connector writes RRN 5 ("EEEE") BEFORE RRN 2
      *>    ("BB22"). Read back sequentially, order is by relative
      *>    record number (the ordinal position), not by write order:
      *>    S1 BB22 0002 / S2 EEEE 0005 / S-END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24H.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT HA ASSIGN TO "L1C24H1.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK.
           SELECT HB ASSIGN TO "L1C24H1.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS DK.
           SELECT HC ASSIGN TO "L1C24H2.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS GK.
           SELECT HD ASSIGN TO "L1C24H2.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS SK.
       DATA DIVISION.
       FILE SECTION.
       FD HA.
       01 HA-REC PIC X(4).
       FD HB.
       01 HB-REC PIC X(4).
       FD HC.
       01 HC-REC PIC X(4).
       FD HD.
       01 HD-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 DK PIC 9(4).
       01 GK PIC 9(4).
       01 SK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 77 TO RK.
           OPEN OUTPUT HA.
           MOVE "AAAA" TO HA-REC.
           WRITE HA-REC INVALID KEY DISPLAY "W1 INVALID".
           DISPLAY "W1 " RK.
           MOVE "BBBB" TO HA-REC.
           WRITE HA-REC INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "W2 " RK.
           MOVE "CCCC" TO HA-REC.
           WRITE HA-REC INVALID KEY DISPLAY "W3 INVALID".
           DISPLAY "W3 " RK.
           CLOSE HA.
           OPEN INPUT HB.
           MOVE 2 TO DK.
           READ HB INVALID KEY DISPLAY "R2 INVALID".
           DISPLAY "R2 " HB-REC " " DK.
           CLOSE HB.
           OPEN OUTPUT HC.
           MOVE 5 TO GK.
           MOVE "EEEE" TO HC-REC.
           WRITE HC-REC INVALID KEY DISPLAY "G5 INVALID".
           MOVE 2 TO GK.
           MOVE "BB22" TO HC-REC.
           WRITE HC-REC INVALID KEY DISPLAY "G2 INVALID".
           CLOSE HC.
           MOVE 0 TO SK.
           OPEN INPUT HD.
           READ HD AT END DISPLAY "S1 END".
           DISPLAY "S1 " HD-REC " " SK.
           READ HD AT END DISPLAY "S2 END".
           DISPLAY "S2 " HD-REC " " SK.
           READ HD AT END DISPLAY "S-END".
           CLOSE HD.
           STOP RUN.
