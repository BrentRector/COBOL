      *>--- conformance-case
      *> id: l1c01_alt_key_duplicates
      *> rules: [GR-12.4.5.6.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.6.4 r4 — alternate key without/with DUPLICATES
      *> THE RULE: "If the DUPLICATES phrase is not specified, the value
      *> of the associated alternate record key shall not be equal to
      *> the value of the same alternate record key in another record in
      *> the physical file."
      *>   cite.py --check 12.4.5.6.4 "If the DUPLICATES phrase is not
      *>     specified, the value of the associated alternate record key
      *>     shall not be equal to the value of the same alternate
      *>     record key in another record in the physical file"
      *>     -> OK §12.4.5.6.4 4)
      *> ITS OBSERVABLE CONSEQUENCES:
      *>   cite.py --check 14.9.51.4 "When an alternate record key of
      *>     the record to be written does not allow duplicates and the
      *>     value of that alternate record key is equal to the value of
      *>     the corresponding alternate record key of a record in the
      *>     file, the I-O status associated with the write file
      *>     connector is set to '22'" -> OK §14.9.51.4 42)
      *>   cite.py --check 14.9.35.4 "When an alternate record key of
      *>     the record to be replaced does not allow duplicates and the
      *>     value of that alternate record key is equal to the value of
      *>     the corresponding alternate record key of a record in that
      *>     physical file, the I-O status associated with the rewrite
      *>     file connector is set to '22'" -> OK §14.9.35.4 25)
      *>   cite.py --check 9.1.13.2 "For a REWRITE or WRITE statement,
      *>     the record just written created a duplicate key value for
      *>     at least one alternate record key for which duplicates are
      *>     allowed" -> OK §9.1.13.2 2)   (I-O status '02')
      *> AK1 is unique (no DUPLICATES); AK2 is WITH DUPLICATES.
      *> DERIVATION of every .out line:
      *>   W1 0001/AAAA/XX  nothing to equal              -> W1 00
      *>   W2 0002/AAAA/YY  AK1 = W1's AK1, no DUPLICATES
      *>                                         -> W2 IK, W2 22
      *>   W3 0003/BBBB/XX  AK2 = W1's AK2, DUPLICATES    -> W3 02
      *>   W4 0004/AAAB/ZZ  AK1 differs from AAAA         -> W4 00
      *>   RW 0004 with AK1 := BBBB, = W3's AK1  -> RW IK, RW 22
      *>   read by prime key: 0001, 0003, 0004 (W2 was never stored,
      *>   and the failed REWRITE left 0004's AK1 as AAAB)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1C01C.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS AK1
               ALTERNATE RECORD KEY IS AK2 WITH DUPLICATES
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 PK  PIC X(4).
          05 AK1 PIC X(4).
          05 AK2 PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "0001AAAAXX" TO REC
           WRITE REC INVALID KEY DISPLAY "W1 IK" END-WRITE
           DISPLAY "W1 " FS
           MOVE "0002AAAAYY" TO REC
           WRITE REC INVALID KEY DISPLAY "W2 IK" END-WRITE
           DISPLAY "W2 " FS
           MOVE "0003BBBBXX" TO REC
           WRITE REC INVALID KEY DISPLAY "W3 IK" END-WRITE
           DISPLAY "W3 " FS
           MOVE "0004AAABZZ" TO REC
           WRITE REC INVALID KEY DISPLAY "W4 IK" END-WRITE
           DISPLAY "W4 " FS
           CLOSE F
           OPEN I-O F
           MOVE "0004" TO PK
           READ F INVALID KEY DISPLAY "R4 IK" END-READ
           MOVE "BBBB" TO AK1
           REWRITE REC INVALID KEY DISPLAY "RW IK" END-REWRITE
           DISPLAY "RW " FS
           MOVE LOW-VALUES TO PK
           START F KEY IS NOT LESS THAN PK
               INVALID KEY DISPLAY "ST IK"
           END-START
           PERFORM UNTIL EOF = "Y"
               READ F NEXT RECORD
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "REC " REC
               END-READ
           END-PERFORM
           CLOSE F
           STOP RUN.
