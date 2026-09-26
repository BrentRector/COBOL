      *> ISO §14.9.35.2 REWRITE general format — the phrases a COBOL-85
      *> program can write: FROM identifier-1, INVALID KEY, NOT INVALID
      *> KEY, END-REWRITE (retry-phrase, the LOCK phrases, FROM literal-1
      *> and the FILE arm are later additions; they are pinned at 2023 by
      *> conformance:2023/l1m8_rewrite_format_phrases)
      *> Format (2023 text): REWRITE {record-name-1 | FILE file-name-1}
      *>   [RECORD] [FROM {identifier-1 | literal-1}] [retry-phrase]
      *>   [WITH LOCK | WITH NO LOCK]
      *>   [ |INVALID KEY imperative-1| |NOT INVALID KEY imperative-2| ]
      *>   [END-REWRITE]
      *>   cite.py --check 14.9.35.2 "retry-phrase is described in
      *>     14.7.9" -> OK §14.9.35.2 (General format)
      *> The 1985 text is not in the repository, so this program writes
      *> the key phrases only in INVALID-then-NOT order (legal under the
      *> 2023 choice indicators, §5.2.6.4, and in the classic order).
      *> Semantics (INDEXED, ACCESS DYNAMIC, keys K1..K4):
      *>   FROM identifier-1 = "MOVE identifier-1 TO record-name-1"
      *>     then the REWRITE without FROM (OK §14.9.35.4 7)).
      *>   No record with the prime key -> '23' (OK §14.9.35.4 23)).
      *>   INVALID KEY / NOT INVALID KEY branches (OK §9.1.14 2)).
      *>   '00' = successful completion (OK §9.1.13.2 1)).
      *> Legs:
      *>   E1 FROM identifier, INVALID KEY + NOT INVALID KEY,
      *>      END-REWRITE, K1 present                     -> 00 N
      *>   E2 bare REWRITE record-name, K2 present         -> 00 -
      *>   E3 INVALID KEY alone, K9 absent                 -> 23 I
      *>   E4 NOT INVALID KEY alone, K3 present            -> 00 N
      *> Read-back in key order: K1..K3 rewritten, K4 untouched, no K9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8R85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1m8r85.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-KEY
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC.
          05 F-KEY PIC XX.
          05 F-DAT PIC X(8).
       WORKING-STORAGE SECTION.
       01 F-ST   PIC XX.
       01 W-BR   PIC X.
       01 W-SRC  PIC X(10) VALUE "K1IDENT-01".
       01 W-EOF  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "K1ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K2ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K3ORIGINAL" TO F-REC. WRITE F-REC.
           MOVE "K4ORIGINAL" TO F-REC. WRITE F-REC.
           CLOSE F.
           OPEN I-O F.
           MOVE "-" TO W-BR.
           REWRITE F-REC FROM W-SRC
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "E1 " F-ST " " W-BR.
           MOVE "-" TO W-BR.
           MOVE "K2BARE-002" TO F-REC.
           REWRITE F-REC.
           DISPLAY "E2 " F-ST " " W-BR.
           MOVE "K9NOSUCH-9" TO F-REC.
           REWRITE F-REC
               INVALID KEY MOVE "I" TO W-BR
           END-REWRITE.
           DISPLAY "E3 " F-ST " " W-BR.
           MOVE "-" TO W-BR.
           MOVE "K3NOTIK-03" TO F-REC.
           REWRITE F-REC
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "E4 " F-ST " " W-BR.
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
