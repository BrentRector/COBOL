      *>--- conformance-case
      *> id: l1c12_access_mode_random_dynamic
      *> rules: [GR-12.4.5.5.3-3, GR-12.4.5.5.3-4, FMT-12.4.5.5.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.5.1 / §12.4.5.5.3 GR3/GR4 — ACCESS MODE
      *>   spellings; random and dynamic access
      *> Format: ACCESS MODE IS { SEQUENTIAL | RANDOM | DYNAMIC },
      *>   only ACCESS and the three modes
      *> underlined, so MODE and IS are optional words.  Written
      *>   below: "ACCESS MODE IS SEQUENTIAL",
      *> "ACCESS RANDOM", "ACCESS MODE IS RANDOM", "ACCESS IS
      *>   DYNAMIC", "ACCESS MODE DYNAMIC".
      *>   cite.py --check 12.4.5.5.1 "ACCESS"  -> OK  §12.4.5.5.1
      *>     (General format)
      *> GR3: "If the access mode is random: a) For a relative file,
      *>   the value of a relative key data item
      *> indicates the record to be accessed. b) For an indexed file,
      *>   the value of a record key data item
      *> indicates the record to be accessed."
      *>   cite.py -> OK  §12.4.5.5.3 3) a)  (General rules)
      *>   cite.py -> OK  §12.4.5.5.3 3) b)  (General rules)
      *> GR4: "If the access mode is dynamic, records in the file may
      *>   be accessed sequentially, randomly,
      *> or both."
      *>   cite.py --check 12.4.5.5.3 "If the access mode is dynamic,
      *>     records in the file may be accessed
      *>     sequentially, randomly, or both."  -> OK  §12.4.5.5.3 4)
      *>       (General rules)
      *> §14.9.51.4 GR29 b) (WRITE, relative, random/dynamic): "prior
      *>   to the execution of the WRITE
      *> statement the value of the relative key data item shall be
      *>   initialized by the runtime element
      *> with the relative record number to be associated with the
      *>   record"  -> cite.py OK §14.9.51.4 29)
      *> §9.1.13.5 3) a): I-O status 23 exists because "an attempt is
      *>   made to randomly access a record
      *> that does not exist in the physical file"  -> cite.py OK
      *>   §9.1.13.5 3) a)
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   SQ  (ACCESS MODE IS SEQUENTIAL): writes Q1,Q2 and reads
      *>     them back in order  => SQ:Q1 / SQ:Q2
      *>   RLR (ACCESS RANDOM, relative): WRITE at RK1 = 5, 2, 9 (GR29
      *>     b).  READ with RK1=9 selects the
      *>       record at RRN 9 (GR3 a)
      *>         => RLR:R9 00
      *>       READ with RK1=3: no such record -> INVALID KEY, '23'
      *>         => RLR:INV 23
      *>   IXR (ACCESS MODE IS RANDOM, indexed): WRITE C1,A3,B2 out of
      *>     key order (random access imposes
      *>       no order).  READ with IXR-KEY="B" selects B2 (GR3 b)
      *>         => IXR:B2 00
      *>       READ with IXR-KEY="Z": no such record -> '23'
      *>         => IXR:INV 23
      *>   IXD (ACCESS IS DYNAMIC, indexed): WRITE C1,A3,B2 all
      *>     succeed (random WRITEs) => IXD:W 00 00 00
      *>       random READ KEY IS IXD-KEY with "B" -> B2, then
      *>         sequential READ NEXT from there -> C1, and
      *>       READ NEXT again -> AT END (GR4, both on one open)
      *>         => IXD:B2 / IXD:C1 /
      *>            IXD:END
      *>   RLD (ACCESS MODE DYNAMIC, relative, the RLR file): random
      *>     READ with RK2=2 -> R2 (GR4); READ
      *>       NEXT continues in ascending RRN -> R5 with RK2 = 05,
      *>         then R9 09 => RLD:R2 02 / RLD:R5 05 /
      *>       RLD:R9 09
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "L1C12K.SQ"
               ACCESS MODE IS SEQUENTIAL.
           SELECT RLR ASSIGN TO "L1C12K.RL"
               ORGANIZATION IS RELATIVE
               ACCESS RANDOM
               RELATIVE KEY IS RK1
               FILE STATUS IS RLR-ST.
           SELECT RLD ASSIGN TO "L1C12K.RL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE DYNAMIC
               RELATIVE KEY IS RK2
               FILE STATUS IS RLD-ST.
           SELECT IXR ASSIGN TO "L1C12K.IXR"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IXR-KEY
               FILE STATUS IS IXR-ST.
           SELECT IXD ASSIGN TO "L1C12K.IXD"
               ORGANIZATION IS INDEXED
               ACCESS IS DYNAMIC
               RECORD KEY IS IXD-KEY
               FILE STATUS IS IXD-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ.
       01  SQ-REC      PIC X(2).
       FD  RLR.
       01  RLR-REC     PIC X(2).
       FD  RLD.
       01  RLD-REC     PIC X(2).
       FD  IXR.
       01  IXR-REC.
           02  IXR-KEY PIC X.
           02  IXR-VAL PIC X.
       FD  IXD.
       01  IXD-REC.
           02  IXD-KEY PIC X.
           02  IXD-VAL PIC X.
       WORKING-STORAGE SECTION.
       01  RLR-ST      PIC XX.
       01  RLD-ST      PIC XX.
       01  IXR-ST      PIC XX.
       01  IXD-ST      PIC XX.
       01  ST-1        PIC XX.
       01  ST-2        PIC XX.
       01  RK1         PIC 99.
       01  RK2         PIC 99.
       01  WS-EOF      PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT SQ.
           MOVE "Q1" TO SQ-REC. WRITE SQ-REC.
           MOVE "Q2" TO SQ-REC. WRITE SQ-REC.
           CLOSE SQ.
           OPEN INPUT SQ.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ SQ
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "SQ:" SQ-REC
               END-READ
           END-PERFORM.
           CLOSE SQ.
      *> GR3 a) — relative, random
           OPEN OUTPUT RLR.
           MOVE 5 TO RK1. MOVE "R5" TO RLR-REC.
           WRITE RLR-REC INVALID KEY DISPLAY "RLR:W?" END-WRITE.
           MOVE 2 TO RK1. MOVE "R2" TO RLR-REC.
           WRITE RLR-REC INVALID KEY DISPLAY "RLR:W?" END-WRITE.
           MOVE 9 TO RK1. MOVE "R9" TO RLR-REC.
           WRITE RLR-REC INVALID KEY DISPLAY "RLR:W?" END-WRITE.
           CLOSE RLR.
           OPEN INPUT RLR.
           MOVE 9 TO RK1.
           READ RLR
               INVALID KEY DISPLAY "RLR:INV " RLR-ST
               NOT INVALID KEY DISPLAY "RLR:" RLR-REC " " RLR-ST
           END-READ.
           MOVE 3 TO RK1.
           READ RLR
               INVALID KEY DISPLAY "RLR:INV " RLR-ST
               NOT INVALID KEY DISPLAY "RLR:" RLR-REC " " RLR-ST
           END-READ.
           CLOSE RLR.
      *> GR3 b) — indexed, random
           OPEN OUTPUT IXR.
           MOVE "C1" TO IXR-REC.
           WRITE IXR-REC INVALID KEY DISPLAY "IXR:W?" END-WRITE.
           MOVE "A3" TO IXR-REC.
           WRITE IXR-REC INVALID KEY DISPLAY "IXR:W?" END-WRITE.
           MOVE "B2" TO IXR-REC.
           WRITE IXR-REC INVALID KEY DISPLAY "IXR:W?" END-WRITE.
           CLOSE IXR.
           OPEN INPUT IXR.
           MOVE "B" TO IXR-KEY.
           READ IXR
               INVALID KEY DISPLAY "IXR:INV " IXR-ST
               NOT INVALID KEY DISPLAY "IXR:" IXR-REC " " IXR-ST
           END-READ.
           MOVE "Z" TO IXR-KEY.
           READ IXR
               INVALID KEY DISPLAY "IXR:INV " IXR-ST
               NOT INVALID KEY DISPLAY "IXR:" IXR-REC " " IXR-ST
           END-READ.
           CLOSE IXR.
      *> GR4 — indexed, dynamic: random and sequential on one open
           OPEN OUTPUT IXD.
           MOVE "C1" TO IXD-REC.
           WRITE IXD-REC INVALID KEY CONTINUE END-WRITE.
           MOVE IXD-ST TO ST-1.
           MOVE "A3" TO IXD-REC.
           WRITE IXD-REC INVALID KEY CONTINUE END-WRITE.
           MOVE IXD-ST TO ST-2.
           MOVE "B2" TO IXD-REC.
           WRITE IXD-REC INVALID KEY CONTINUE END-WRITE.
           DISPLAY "IXD:W " ST-1 " " ST-2 " " IXD-ST.
           CLOSE IXD.
           OPEN INPUT IXD.
           MOVE "B" TO IXD-KEY.
           READ IXD KEY IS IXD-KEY
               INVALID KEY DISPLAY "IXD:INV " IXD-ST
               NOT INVALID KEY DISPLAY "IXD:" IXD-REC
           END-READ.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IXD NEXT RECORD
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "IXD:END"
                   NOT AT END DISPLAY "IXD:" IXD-REC
               END-READ
           END-PERFORM.
           CLOSE IXD.
      *> GR4 — relative, dynamic, on the file RLR built
           OPEN INPUT RLD.
           MOVE 2 TO RK2.
           READ RLD
               INVALID KEY DISPLAY "RLD:INV " RLD-ST
               NOT INVALID KEY DISPLAY "RLD:" RLD-REC " " RK2
           END-READ.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ RLD NEXT RECORD
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "RLD:" RLD-REC " " RK2
               END-READ
           END-PERFORM.
           CLOSE RLD.
           STOP RUN.
