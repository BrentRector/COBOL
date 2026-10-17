      *>--- conformance-case
      *> id: l1c38_write_key_any_order
      *> rules: [GR-14.9.51.4-37, GR-14.9.51.4-39]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.4 37) 39) — key set before WRITE; any order
      *> RULE 37): "The data item specified as the prime record key
      *>   shall be set by the runtime element to the desired value
      *>   prior to the execution of the WRITE statement."
      *>   cite.py --check 14.9.51.4 "The data item specified as the
      *>     prime record key shall be set by the runtime element to
      *>     the desired value prior to the execution of the WRITE
      *>     statement" -> OK §14.9.51.4 37)
      *> RULE 39): "If the access mode of the write file connector is
      *>   random or dynamic, WRITE statements may release records to
      *>   the operating environment through that connector in any
      *>   order."
      *>   cite.py --check 14.9.51.4 "If the access mode of the write
      *>     file connector is random or dynamic, WRITE statements may
      *>     release records to the operating environment through that
      *>     connector in any order" -> OK §14.9.51.4 39)
      *> SIBLING (the sequential arm this golden contrasts with):
      *>   cite.py --check 14.9.51.4 "If the record is not in the
      *>     sequence above, the execution of the WRITE statement is
      *>     unsuccessful, the invalid key condition exists, and the
      *>     I-O status for the write file connector is set to '21'"
      *>     -> OK §14.9.51.4 38)
      *> FROM phrase (the key is set by FROM's MOVE, before release):
      *>   cite.py --check 14.9.51.4 "The result of the execution of a
      *>     WRITE statement specifying record-name-1 and the FROM
      *>     phrase is equivalent to the execution of the following
      *>     statements in the order specified" -> OK §14.9.51.4 5)
      *> Random READ of an absent key:
      *>   cite.py --check 9.1.13.5 "an attempt is made to randomly
      *>     access a record that does not exist in the physical file"
      *>     -> OK §9.1.13.5 3) a)   (I-O status 23)
      *> NOTE: cite.py ignores operator symbols and can mislabel list
      *>   items (PB1554); every clause number above was re-read in
      *>   specs/ISO_COBOL.md.
      *> DERIVATION of every .out line:
      *>   FILE-R (RANDOM), keys written K30,K10,K20 - descending then
      *>   ascending; rule 39) permits any order -> R1 00, R2 00, R3 00
      *>   (a rule-38 sequence check would give R2 21).
      *>   K40/R4 -> R4 00.  Then only RK := K05, RDATA := R5 -> R5 00:
      *>   by rule 37) the key is the value in RK at the WRITE (K05).
      *>   Record area := K99XX, WRITE ... FROM WS-SRC (K15R6): rule 5)
      *>   makes it MOVE WS-SRC TO R-REC then WRITE, so the prime key
      *>   at release is K15, not K99 -> R6 00.
      *>   Random reads K05 K10 K15 K20 K30 K40 return R5 R2 R6 R3 R1
      *>   R4 with 00; K99 was never written -> K99 IK 23 (9.1.13.5 3a).
      *>   FILE-D (DYNAMIC), keys K50,K20,K40 -> D1 00, D2 00, D3 00;
      *>   random reads K20 K40 K50 -> D2 D3 D1, 00 each.
      *>   FILE-S (SEQUENTIAL, the rule-38 sibling), K30 then K10:
      *>   S1 00, then K10 < K30 -> S2 IK, S2 21.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C38A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-R ASSIGN TO "L1C38A1.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS RK
               FILE STATUS IS FSR.
           SELECT FILE-D ASSIGN TO "L1C38A2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS DK
               FILE STATUS IS FSD.
           SELECT FILE-S ASSIGN TO "L1C38A3.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS SK
               FILE STATUS IS FSS.
       DATA DIVISION.
       FILE SECTION.
       FD FILE-R.
       01 R-REC.
          05 RK PIC X(3).
          05 RDATA PIC X(2).
       FD FILE-D.
       01 D-REC.
          05 DK PIC X(3).
          05 DDATA PIC X(2).
       FD FILE-S.
       01 S-REC.
          05 SK PIC X(3).
          05 SDATA PIC X(2).
       WORKING-STORAGE SECTION.
       01 FSR    PIC XX.
       01 FSD    PIC XX.
       01 FSS    PIC XX.
       01 WS-SRC PIC X(5) VALUE "K15R6".
       01 WS-KEY PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FILE-R
           MOVE "K30R1" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R1 IK" END-WRITE
           DISPLAY "R1 " FSR
           MOVE "K10R2" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R2 IK" END-WRITE
           DISPLAY "R2 " FSR
           MOVE "K20R3" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R3 IK" END-WRITE
           DISPLAY "R3 " FSR
           MOVE "K40" TO RK
           MOVE "R4" TO RDATA
           WRITE R-REC INVALID KEY DISPLAY "R4 IK" END-WRITE
           DISPLAY "R4 " FSR
           MOVE "K05" TO RK
           MOVE "R5" TO RDATA
           WRITE R-REC INVALID KEY DISPLAY "R5 IK" END-WRITE
           DISPLAY "R5 " FSR
           MOVE "K99XX" TO R-REC
           WRITE R-REC FROM WS-SRC
               INVALID KEY DISPLAY "R6 IK"
           END-WRITE
           DISPLAY "R6 " FSR
           CLOSE FILE-R
           OPEN INPUT FILE-R
           MOVE "K05" TO WS-KEY
           PERFORM READ-R
           MOVE "K10" TO WS-KEY
           PERFORM READ-R
           MOVE "K15" TO WS-KEY
           PERFORM READ-R
           MOVE "K20" TO WS-KEY
           PERFORM READ-R
           MOVE "K30" TO WS-KEY
           PERFORM READ-R
           MOVE "K40" TO WS-KEY
           PERFORM READ-R
           MOVE "K99" TO WS-KEY
           PERFORM READ-R
           CLOSE FILE-R
           OPEN OUTPUT FILE-D
           MOVE "K50D1" TO D-REC
           WRITE D-REC INVALID KEY DISPLAY "D1 IK" END-WRITE
           DISPLAY "D1 " FSD
           MOVE "K20D2" TO D-REC
           WRITE D-REC INVALID KEY DISPLAY "D2 IK" END-WRITE
           DISPLAY "D2 " FSD
           MOVE "K40D3" TO D-REC
           WRITE D-REC INVALID KEY DISPLAY "D3 IK" END-WRITE
           DISPLAY "D3 " FSD
           CLOSE FILE-D
           OPEN INPUT FILE-D
           MOVE "K20" TO WS-KEY
           PERFORM READ-D
           MOVE "K40" TO WS-KEY
           PERFORM READ-D
           MOVE "K50" TO WS-KEY
           PERFORM READ-D
           CLOSE FILE-D
           OPEN OUTPUT FILE-S
           MOVE "K30S1" TO S-REC
           WRITE S-REC INVALID KEY DISPLAY "S1 IK" END-WRITE
           DISPLAY "S1 " FSS
           MOVE "K10S2" TO S-REC
           WRITE S-REC INVALID KEY DISPLAY "S2 IK" END-WRITE
           DISPLAY "S2 " FSS
           CLOSE FILE-S
           STOP RUN.
       READ-R.
           MOVE WS-KEY TO RK
           READ FILE-R
               INVALID KEY DISPLAY WS-KEY " IK " FSR
               NOT INVALID KEY DISPLAY WS-KEY " " RDATA " " FSR
           END-READ.
       READ-D.
           MOVE WS-KEY TO DK
           READ FILE-D
               INVALID KEY DISPLAY WS-KEY " IK " FSD
               NOT INVALID KEY DISPLAY WS-KEY " " DDATA " " FSD
           END-READ.
