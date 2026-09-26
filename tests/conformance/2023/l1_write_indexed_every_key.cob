      *> ISO §14.9.51.4 34) — a record WRITTEN to an indexed file is then accessible by EVERY record key: prime and each alternate
      *> THE RULE: "Successful execution of a WRITE statement causes the
      *>   content of the record area to be released. The operating
      *>   environment utilizes the contents of the record keys in such a
      *>   way that subsequent access of the record may be made based
      *>   upon any of these specified record keys."
      *>   cite.py --check 14.9.51.4 "Successful execution of a WRITE
      *>     statement causes the content of the record area to be
      *>     released" -> OK §14.9.51.4 34)
      *>   cite.py --check 14.9.51.4 "subsequent access of the record may
      *>     be made based upon any of these specified record keys"
      *>     -> OK §14.9.51.4 34)
      *> The golden writes three records OUT of every key's order, then
      *> reaches each written record through the prime key and through
      *> each of the two alternate keys, sequentially and randomly.
      *> Three orders differ on purpose: write order K03,K01,K02; prime
      *> order K01,K02,K03; AK1 order A1(K02),B1(K03),C1(K01).
      *> SUPPORTING RULES:
      *>   cite.py --check 14.9.51.4 "If the access mode of the write file
      *>     connector is random or dynamic, WRITE statements may release
      *>     records to the operating environment through that connector
      *>     in any order" -> OK §14.9.51.4 39)
      *>   cite.py --check 14.9.27.4 "the prime record key is established
      *>     as the key of reference" -> OK §14.9.27.4 14) (OPEN INPUT)
      *>   cite.py --check 14.9.30.4 "If NEXT is specified or implied, the
      *>     record to be made available is the first existing record in
      *>     the physical file whose key of reference value is greater
      *>     than or equal to the key value in the file position
      *>     indicator" -> OK §14.9.30.4 21) (d) 1.)
      *>   cite.py --check 14.9.30.4 "the first record in the physical
      *>     file whose key value is greater than the key of reference is
      *>     made available" -> OK §14.9.30.4 21) (e) 3.)
      *>   cite.py --check 14.9.30.4 "if the KEY phrase is specified,
      *>     data-name-1 or record-key-name-1 is established as the key of
      *>     reference for this retrieval. If the dynamic access mode is
      *>     specified, this key of reference is also used for retrievals
      *>     by any subsequent executions of sequential format READ
      *>     statements" -> OK §14.9.30.4 30)
      *>   cite.py --check 14.9.30.4 "This value is compared with the
      *>     value contained in the corresponding data item of the stored
      *>     records in the file until the first record having an equal
      *>     value is found" -> OK §14.9.30.4 32)
      *>   cite.py --check 14.9.30.4 "The I-O status for the file
      *>     connector referenced by file-name-1 is set to '02' if the
      *>     execution of the READ statement is successful, an indexed
      *>     file is being sequentially accessed, the key of reference is
      *>     an alternate record key" -> OK §14.9.30.4 27) (needs a
      *>     DUPLICATE alternate value; neither key here has one, so
      *>     every status is '00')
      *>   cite.py --check 9.1.13.2 "I-O status = 00. The input-output
      *>     statement is successfully executed and no further
      *>     information is available concerning the input-output
      *>     operation" -> OK §9.1.13.2 1)
      *> DERIVATION of every .out line:
      *>   W1..W3 00   distinct prime keys, distinct AK1 and AK2 values,
      *>               any order allowed (GR39) -> each WRITE succeeds
      *>   S x3        OPEN INPUT makes the PRIME key the key of reference;
      *>               READ NEXT walks prime order:
      *>               K01C1XXAAAA, K02A1YYBBBB, K03B1ZZCCCC, each 00
      *>   PK          READ KEY IS PK = "K02" -> K02A1YYBBBB 00
      *>   A2          READ KEY IS AK2 = "ZZ" -> K03B1ZZCCCC 00
      *>   A1          READ KEY IS AK1 = "A1" -> K02A1YYBBBB 00, and AK1
      *>               stays the key of reference (GR30, DYNAMIC)
      *>   N x2        READ NEXT walks AK1 order from A1: B1 -> K03B1ZZCCCC,
      *>               C1 -> K01C1XXAAAA, each 00
      *> The record area is set to SPACES before each random READ, so
      *> every non-key byte shown came from the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1W34IX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1W34IX.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS AK1
               ALTERNATE RECORD KEY IS AK2
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 PK  PIC X(3).
          05 AK1 PIC X(2).
          05 AK2 PIC X(2).
          05 PAY PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "K03B1ZZCCCC" TO REC
           WRITE REC INVALID KEY DISPLAY "W1 IK" END-WRITE
           DISPLAY "W1 " FS
           MOVE "K01C1XXAAAA" TO REC
           WRITE REC INVALID KEY DISPLAY "W2 IK" END-WRITE
           DISPLAY "W2 " FS
           MOVE "K02A1YYBBBB" TO REC
           WRITE REC INVALID KEY DISPLAY "W3 IK" END-WRITE
           DISPLAY "W3 " FS
           CLOSE F
           OPEN INPUT F
           PERFORM 3 TIMES
               READ F NEXT RECORD
                   AT END DISPLAY "S EOF"
                   NOT AT END DISPLAY "S " REC " " FS
               END-READ
           END-PERFORM
           MOVE SPACES TO REC
           MOVE "K02" TO PK
           READ F KEY IS PK
               INVALID KEY DISPLAY "PK IK " FS
               NOT INVALID KEY DISPLAY "PK " REC " " FS
           END-READ
           MOVE SPACES TO REC
           MOVE "ZZ" TO AK2
           READ F KEY IS AK2
               INVALID KEY DISPLAY "A2 IK " FS
               NOT INVALID KEY DISPLAY "A2 " REC " " FS
           END-READ
           MOVE SPACES TO REC
           MOVE "A1" TO AK1
           READ F KEY IS AK1
               INVALID KEY DISPLAY "A1 IK " FS
               NOT INVALID KEY DISPLAY "A1 " REC " " FS
           END-READ
           PERFORM 2 TIMES
               READ F NEXT RECORD
                   AT END DISPLAY "N EOF"
                   NOT AT END DISPLAY "N " REC " " FS
               END-READ
           END-PERFORM
           CLOSE F
           STOP RUN.
