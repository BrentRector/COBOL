      *> ISO §14.9.35.2 REWRITE general format — every optional phrase of
      *> the record-name-1 form, alone and combined, in the printed order
      *> Format: REWRITE {record-name-1 | FILE file-name-1} [RECORD]
      *>   [FROM {identifier-1 | literal-1}] [retry-phrase]
      *>   [WITH LOCK | WITH NO LOCK]
      *>   [ |INVALID KEY imperative-1| |NOT INVALID KEY imperative-2| ]
      *>   [END-REWRITE]
      *>   cite.py --check 14.9.35.2 "retry-phrase is described in
      *>     14.7.9" -> OK §14.9.35.2 (General format)
      *> RECORD and WITH are not underlined (optional words); LOCK / NO
      *>   LOCK are one-of-two; the INVALID KEY bracket carries CHOICE
      *>   INDICATORS (§5.2.6.4): either phrase alone, both in EITHER
      *>   order, or neither. RETRY (§14.7.9.2): "arithmetic-expression-1
      *>   TIMES | FOR arithmetic-expression-2 SECONDS | FOREVER", FOR
      *>   optional. The FILE file-name-1 arm is declined (Annex A.4.13)
      *>   and witnessed by conformance:negative/a413-rewrite-file-
      *>   declined; this program covers the record-name-1 arm.
      *> LOCK MODE IS MANUAL so the LOCK phrases are legal (§14.9.35.3
      *>   SR4 bars them only under automatic locking, OK 4)).
      *> Semantics each leg pins (INDEXED, ACCESS DYNAMIC, keys K1..K5):
      *>   FROM: "equivalent to ... MOVE identifier-1 TO record-name-1
      *>     ... MOVE literal-1 TO record-name-1 ... b) The same REWRITE
      *>     statement without the FROM phrase" (OK §14.9.35.4 7)).
      *>   Random/dynamic: the record replaced is the one whose prime key
      *>     is in the area; none -> '23' (OK §14.9.35.4 23)).
      *>   A RETRY phrase acts only after "a file sharing conflict
      *>     condition or a record operation conflict condition" (OK
      *>     §14.7.9.3 4)); this single connector meets none, so each
      *>     RETRY leg succeeds on its first attempt.
      *>   Branch: INVALID KEY runs on the invalid key condition (OK
      *>     §9.1.14 2)); NOT INVALID KEY on success (OK §9.1.14 2),
      *>     second list). W-BR = "-" means neither ran.
      *>   '00' = successful completion (§9.1.13.2 1)).
      *> Legs:
      *>   R1 RECORD FROM literal RETRY 3 TIMES WITH NO LOCK, NOT INVALID
      *>      KEY written BEFORE INVALID KEY, END-REWRITE  -> 00 N
      *>   R2 FROM identifier RETRY FOR 2 SECONDS WITH LOCK, INVALID
      *>      KEY then NOT INVALID KEY                     -> 00 N
      *>   R3 RETRY FOREVER NO LOCK (no WITH), both key phrases, key K9
      *>      absent                                        -> 23 I
      *>   R4 LOCK alone (no WITH), no key phrase, no END  -> 00 -
      *>   R5 bare REWRITE record-name                      -> 00 -
      *>   R6 RECORD, NOT INVALID KEY alone                 -> 00 N
      *>   R7 INVALID KEY alone, key K8 absent              -> 23 I
      *> Read-back in key order: K1..K5 as rewritten; no K8/K9 record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8RF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1m8rf.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-KEY
               FILE STATUS IS F-ST
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC.
          05 F-KEY PIC XX.
          05 F-DAT PIC X(8).
       WORKING-STORAGE SECTION.
       01 F-ST   PIC XX.
       01 W-BR   PIC X.
       01 W-SRC  PIC X(10) VALUE "K2IDENT-02".
       01 W-EOF  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "K1ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K2ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K3ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K4ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K5ORIGINAL" TO F-REC. WRITE F-REC.
           CLOSE F.
           OPEN I-O F.
           MOVE "-" TO W-BR.
           REWRITE F-REC RECORD FROM "K1LIT-0001"
               RETRY 3 TIMES
               WITH NO LOCK
               NOT INVALID KEY MOVE "N" TO W-BR
               INVALID KEY MOVE "I" TO W-BR
           END-REWRITE.
           DISPLAY "R1 " F-ST " " W-BR.
           MOVE "-" TO W-BR.
           REWRITE F-REC FROM W-SRC
               RETRY FOR 2 SECONDS
               WITH LOCK
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "R2 " F-ST " " W-BR.
           MOVE "-" TO W-BR.
           MOVE "K9NOSUCH-9" TO F-REC.
           REWRITE F-REC RETRY FOREVER NO LOCK
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "R3 " F-ST " " W-BR.
           MOVE "-" TO W-BR.
           MOVE "K3LOCK-003" TO F-REC.
           REWRITE F-REC LOCK.
           DISPLAY "R4 " F-ST " " W-BR.
           MOVE "K4BARE-004" TO F-REC.
           REWRITE F-REC.
           DISPLAY "R5 " F-ST " " W-BR.
           MOVE "K5NOTIK-05" TO F-REC.
           REWRITE F-REC RECORD
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "R6 " F-ST " " W-BR.
           MOVE "-" TO W-BR.
           MOVE "K8NOSUCH-8" TO F-REC.
           REWRITE F-REC
               INVALID KEY MOVE "I" TO W-BR
           END-REWRITE.
           DISPLAY "R7 " F-ST " " W-BR.
           CLOSE F.
           OPEN INPUT F.
           PERFORM UNTIL W-EOF = 1
               READ F NEXT RECORD
                   AT END MOVE 1 TO W-EOF
                   NOT AT END DISPLAY "B=" F-REC
               END-READ
           END-PERFORM.
           CLOSE F.
           DISPLAY "END".
           STOP RUN.
