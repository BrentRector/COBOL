      *>--- conformance-case
      *> id: l1c37_indexed_dup_prime_key
      *> rules: [GR-14.9.51.4-36]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.4 GR36 — a WRITE whose prime record key equals an
      *>   existing record's prime key is refused ('22', INVALID KEY)
      *> Rule: "The value of the prime record key shall not be equal to
      *>   the value of the prime record key of any record existing in
      *>   the file."
      *>   cite.py --check 14.9.51.4 "The value of the prime record key
      *>   shall not be equal to the value of the prime record key of
      *>   any record existing in the file" -> OK  §14.9.51.4 36)
      *>   cite.py --check 14.9.51.4 "When the value of the prime
      *>   record key of the record to be written is equal to the value
      *>   of the prime record key of any record existing in the file,
      *>   the I-O status associated with the write file connector is
      *>   set to '22'" -> OK  §14.9.51.4 42) (item b); cite.py prints
      *>   the enclosing rule only)
      *>   cite.py --check 14.9.51.4 "If the execution of a WRITE
      *>   statement is unsuccessful, the write operation does not take
      *>   place, the content of the record area is unaffected"
      *>   -> OK  §14.9.51.4 15)
      *> DERIVATION. RANDOM access, so GR42 a)'s sequential-order '21'
      *> cannot apply; the only failing rule is GR36 / GR42 b).
      *>   WRITE K01/OLD1 into the empty file   -> "W1 00"
      *>   WRITE K01/NEW1 (equal prime key): unsuccessful, INVALID KEY
      *>   runs, status '22', record area unaffected (GR15)
      *>                                         -> "W2 INVALID 22 NEW1"
      *>   WRITE K02/OTH2 (distinct key)        -> "W3 00"
      *>   READ K01 after reopening: the ORIGINAL record, since the
      *>   write "does not take place" (GR15)    -> "R K01 OLD1"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C37E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "L1C37E.IDX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS PK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IREC.
           05 PK PIC X(3).
           05 ID1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT IXF
           MOVE "K01OLD1" TO IREC
           WRITE IREC INVALID KEY DISPLAY "W1 INVALID " FS
               NOT INVALID KEY DISPLAY "W1 " FS
           END-WRITE
           MOVE "K01NEW1" TO IREC
           WRITE IREC INVALID KEY DISPLAY "W2 INVALID " FS " " ID1
               NOT INVALID KEY DISPLAY "W2 " FS
           END-WRITE
           MOVE "K02OTH2" TO IREC
           WRITE IREC INVALID KEY DISPLAY "W3 INVALID " FS
               NOT INVALID KEY DISPLAY "W3 " FS
           END-WRITE
           CLOSE IXF
           OPEN INPUT IXF
           MOVE "K01" TO PK
           READ IXF INVALID KEY DISPLAY "R INVALID " FS
               NOT INVALID KEY DISPLAY "R " PK " " ID1
           END-READ
           CLOSE IXF
           STOP RUN.
