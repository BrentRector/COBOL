      *>--- conformance-case
      *> id: l1c38_write_alt_key_dup_order
      *> rules: [GR-14.9.51.4-40]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.4 40) — alternate key duplicates: order, '22'
      *> RULE 40): "When the ALTERNATE RECORD KEY clause is specified
      *>   ..., the value of the alternate record key may be non unique
      *>   only if the DUPLICATES phrase is specified for that data
      *>   item. In this case the operating environment provides
      *>   storage of records such that when records are accessed
      *>   sequentially, the order of retrieval of those records is the
      *>   order in which the operating environment actually writes the
      *>   record into the physical file. If the DUPLICATES phrase is
      *>   not specified and the alternate key value is non unique, the
      *>   execution of the WRITE statement is unsuccessful, the invalid
      *>   key condition exists, and the I-O status of the write file
      *>   connector is set to '22'."
      *>   cite.py --check 14.9.51.4 "the order of retrieval of those
      *>     records is the order in which the operating environment
      *>     actually writes the record into the physical file"
      *>     -> OK §14.9.51.4 40)
      *>   cite.py --check 14.9.51.4 "If the DUPLICATES phrase is not
      *>     specified and the alternate key value is non unique, the
      *>     execution of the WRITE statement is unsuccessful, the
      *>     invalid key condition exists" -> OK §14.9.51.4 40)
      *> SUPPORTING STATUS RULES:
      *>   cite.py --check 9.1.13.2 "the record just written created a
      *>     duplicate key value for at least one alternate record key
      *>     for which duplicates are allowed" -> OK §9.1.13.2 2)
      *>     (cite.py labels the item "2)"; the text is item 2) c),
      *>     status '02' - a PB1554-class list-item mislabel)
      *>   cite.py --check 14.9.30.4 "the NEXT phrase is specified or
      *>     implied and the alternate record key in the record that
      *>     follows the record that was successfully read duplicates
      *>     the same key in the record that was successfully read"
      *>     -> OK §14.9.30.4 27)   (READ status '02', item a))
      *>   cite.py --check 9.1.13.5 "an attempt is made to randomly
      *>     access a record that does not exist in the physical file"
      *>     -> OK §9.1.13.5 3) a)  (status '23')
      *> NOTE: cite.py ignores operator symbols (PB1554); every clause
      *>   number above was re-read in specs/ISO_COBOL.md.
      *> AU has no DUPLICATES; AD is WITH DUPLICATES. ACCESS DYNAMIC,
      *> so writes are in non-ascending prime order (rule 39)).
      *> DERIVATION of every .out line:
      *>   W1 P50/U1/AA  nothing to equal                  -> W1 00
      *>   W2 P20/U2/AA  AD dup, DUPLICATES  (9.1.13.2 2c) -> W2 02
      *>   W3 P40/U1/BB  AU = W1's AU, no DUPLICATES: rule 40) ->
      *>                 INVALID KEY branch, W3 IK, then W3 22
      *>   W4 P10/U4/AA  AD dup                            -> W4 02
      *>   W5 P30/U5/BB  W3 was unsuccessful, so no stored BB -> W5 00
      *>   START AD = "AA", READ NEXT to end: rule 40) = write order
      *>   within AA (P50, P20, P10; prime order would be P10 P20 P50),
      *>   then BB (P30).  READ status (14.9.30.4 27a): next record
      *>   has the same AD -> 02, otherwise 00:
      *>     N P50 02 / N P20 02 / N P10 00 / N P30 00 / EOF
      *>   READ KEY PK = P40: never stored -> P40 IK 23
      *>   READ KEY AU = U1: the surviving U1 record -> U1 P50 AA 00
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C38B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1C38B.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS AU
               ALTERNATE RECORD KEY IS AD WITH DUPLICATES
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 PK PIC X(3).
          05 AU PIC X(2).
          05 AD PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "P50U1AA" TO REC
           WRITE REC INVALID KEY DISPLAY "W1 IK" END-WRITE
           DISPLAY "W1 " FS
           MOVE "P20U2AA" TO REC
           WRITE REC INVALID KEY DISPLAY "W2 IK" END-WRITE
           DISPLAY "W2 " FS
           MOVE "P40U1BB" TO REC
           WRITE REC INVALID KEY DISPLAY "W3 IK" END-WRITE
           DISPLAY "W3 " FS
           MOVE "P10U4AA" TO REC
           WRITE REC INVALID KEY DISPLAY "W4 IK" END-WRITE
           DISPLAY "W4 " FS
           MOVE "P30U5BB" TO REC
           WRITE REC INVALID KEY DISPLAY "W5 IK" END-WRITE
           DISPLAY "W5 " FS
           CLOSE F
           OPEN INPUT F
           MOVE "AA" TO AD
           START F KEY IS EQUAL TO AD
               INVALID KEY DISPLAY "ST IK " FS
           END-START
           PERFORM UNTIL EOF = "Y"
               READ F NEXT RECORD
                   AT END MOVE "Y" TO EOF
                          DISPLAY "EOF"
                   NOT AT END DISPLAY "N " PK " " FS
               END-READ
           END-PERFORM
           MOVE "P40" TO PK
           READ F KEY IS PK
               INVALID KEY DISPLAY "P40 IK " FS
               NOT INVALID KEY DISPLAY "P40 " REC " " FS
           END-READ
           MOVE "U1" TO AU
           READ F KEY IS AU
               INVALID KEY DISPLAY "U1 IK " FS
               NOT INVALID KEY DISPLAY "U1 " PK " " AD " " FS
           END-READ
           CLOSE F
           STOP RUN.
