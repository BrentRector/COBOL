      *>--- conformance-case
      *> id: l1c26_rewrite_indexed_prime_key
      *> rules: [GR-14.9.35.4-22, GR-14.9.35.4-23]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.4 GR22 GR23 — indexed REWRITE by prime key
      *> GR22: "If the access mode of the REWRITE file connector is
      *>   sequential, the record to be replaced is specified by the
      *>   value of the prime record key. When the REWRITE statement is
      *>   executed the value of the prime record key of the record to
      *>   be replaced shall be equal to the value of the prime record
      *>   key of the last record read using this file connector. If it
      *>   is not, the execution of the REWRITE statement is
      *>   unsuccessful and the I-O status in the rewrite file
      *>   connector is set to the invalid key condition, '21'."
      *> GR23: "If the access mode of the rewrite file connector is
      *>   random or dynamic, the record to be replaced is specified by
      *>   the prime record key. If there is no existing record in the
      *>   physical file with that prime record key, the execution of
      *>   the REWRITE statement is unsuccessful and the I-O status in
      *>   the rewrite file connector is set to the invalid key
      *>   condition, '23'."
      *> cite.py --check:
      *>   OK  §14.9.35.4 22)  (General rules)
      *>   OK  §14.9.35.4 23)  (General rules)
      *>   OK  §14.9.35.4 14)  (General rules)  unsuccessful: "no
      *>       logical record updating takes place, the content of the
      *>       record area is unaffected"
      *>   OK  §14.9.35.4 13)  "The file position indicator in the
      *>       rewrite file connector is not affected"
      *>   OK  §14.9.35.4 5)  sequential access: the previous I-O
      *>       statement shall be a successful READ (else '43') -- both
      *>       sequential REWRITEs below directly follow a successful
      *>       READ, so '21' is the only possible failure
      *>   OK  §14.9.35.3 2)  INVALID KEY is forbidden only for
      *>       sequential organization and relative sequential access,
      *>       so it is legal on this indexed sequential file
      *>   OK  §9.1.14 2)  INVALID KEY phrase -> its imperative runs
      *>   OK  §9.1.14 2)  (second list) success -> NOT INVALID KEY
      *> Setup: XS (ACCESS SEQUENTIAL) holds K001A01V01, K002A02V02;
      *>        XD (ACCESS DYNAMIC)    holds K001B01V01, K002B02V02.
      *> Derivation of every expected line:
      *>  S1 READ XS -> K001; key changed to K002: GR22 -> '21', INVALID
      *>     arm; GR14 -> record area still K002NEWVAL:
      *>     "S1 ST=21 REC=K002NEWVAL ARM=INVALID"
      *>  S2 READ XS: GR13 -> position unchanged, next is K002, and GR14
      *>     -> K002 was not overwritten by S1:
      *>     "S2 ST=00 REC=K002A02V02"
      *>  S3 key equals last read (K002): GR22 satisfied -> '00', NOT
      *>     INVALID arm:
      *>     "S3 ST=00 ARM=NOT-INVALID"
      *>  S4/S5 reopen INPUT, read both: K001 intact, K002 replaced:
      *>     "S4 ST=00 REC=K001A01V01"
      *>     "S5 ST=00 REC=K002UPD002"
      *>  D1 XD, prime key K009 absent: GR23 -> '23', INVALID arm, GR14
      *>     record area unaffected:
      *>     "D1 ST=23 REC=K009NEWK09 ARM=INVALID"
      *>  D2 READ K001, then REWRITE with prime key K002: GR23 -> the
      *>     record replaced is the one with THAT prime key (K002), not
      *>     the last one read; '00', NOT INVALID arm:
      *>     "D2 ST=00 ARM=NOT-INVALID"
      *>  D3 READ K002 -> replaced; D4 READ K001 -> untouched:
      *>     "D3 ST=00 REC=K002NEWK02"
      *>     "D4 ST=00 REC=K001B01V01"
      *>  D5 reopen I-O, REWRITE K001 with no preceding READ: GR23
      *>     needs only an existing prime key (GR5 is sequential-only):
      *>     "D5 ST=00 ARM=NOT-INVALID"
      *>  D6 READ K001 -> "D6 ST=00 REC=K001NEWK01"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C26B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XS ASSIGN TO "L1C26BS.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST.
           SELECT XD ASSIGN TO "L1C26BD.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XD-KEY
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  XS.
       01  XS-REC.
           05  XS-KEY              PIC X(4).
           05  XS-DATA             PIC X(6).
       FD  XD.
       01  XD-REC.
           05  XD-KEY              PIC X(4).
           05  XD-DATA             PIC X(6).
       WORKING-STORAGE SECTION.
       01  ST                      PIC XX.
       01  ARM                     PIC X(11).
       PROCEDURE DIVISION.
       M-1.
           OPEN OUTPUT XS
           MOVE "K001A01V01" TO XS-REC
           WRITE XS-REC
           MOVE "K002A02V02" TO XS-REC
           WRITE XS-REC
           CLOSE XS
           OPEN I-O XS
           READ XS
      *> S1: prime key differs from the last record read
           MOVE "NONE" TO ARM
           MOVE "K002" TO XS-KEY
           MOVE "NEWVAL" TO XS-DATA
           REWRITE XS-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "S1 ST=" ST " REC=" XS-REC " ARM=" ARM
           READ XS
           DISPLAY "S2 ST=" ST " REC=" XS-REC
      *> S3: prime key equals the last record read
           MOVE "NONE" TO ARM
           MOVE "UPD002" TO XS-DATA
           REWRITE XS-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "S3 ST=" ST " ARM=" ARM
           CLOSE XS
           OPEN INPUT XS
           READ XS
           DISPLAY "S4 ST=" ST " REC=" XS-REC
           READ XS
           DISPLAY "S5 ST=" ST " REC=" XS-REC
           CLOSE XS
           OPEN OUTPUT XD
           MOVE "K001B01V01" TO XD-REC
           WRITE XD-REC
           MOVE "K002B02V02" TO XD-REC
           WRITE XD-REC
           CLOSE XD
           OPEN I-O XD
      *> D1: no record with this prime key
           MOVE "NONE" TO ARM
           MOVE "K009NEWK09" TO XD-REC
           REWRITE XD-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "D1 ST=" ST " REC=" XD-REC " ARM=" ARM
      *> D2: replace by prime key, not by the last record read
           MOVE "K001" TO XD-KEY
           READ XD
           MOVE "NONE" TO ARM
           MOVE "K002NEWK02" TO XD-REC
           REWRITE XD-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "D2 ST=" ST " ARM=" ARM
           MOVE "K002" TO XD-KEY
           READ XD
           DISPLAY "D3 ST=" ST " REC=" XD-REC
           MOVE "K001" TO XD-KEY
           READ XD
           DISPLAY "D4 ST=" ST " REC=" XD-REC
           CLOSE XD
      *> D5: no preceding READ is required outside sequential access
           OPEN I-O XD
           MOVE "NONE" TO ARM
           MOVE "K001NEWK01" TO XD-REC
           REWRITE XD-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "D5 ST=" ST " ARM=" ARM
           MOVE "K001" TO XD-KEY
           READ XD
           DISPLAY "D6 ST=" ST " REC=" XD-REC
           CLOSE XD
           STOP RUN.
