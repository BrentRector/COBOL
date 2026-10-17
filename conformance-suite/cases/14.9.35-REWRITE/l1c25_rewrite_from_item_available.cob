      *>--- conformance-case
      *> id: l1c25_rewrite_from_item_available
      *> rules: [GR-14.9.35.4-10]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.4 GR10 — after REWRITE ... FROM identifier-1 the
      *> content of identifier-1 (not in the rewrite file's records) is
      *> still available.
      *> "After the execution of the REWRITE statement is complete, the
      *> information in the area referenced by identifier-1 is
      *> available, provided that identifier-1 is not one or part of
      *> one of the record descriptions subordinate to the
      *> file-description, even though the information in the area
      *> referenced by record-name-1 is not available except as
      *> specified for the SAME RECORD AREA clause as indicated in
      *> General rule 6."
      *> cite.py: OK  §14.9.35.4 10)  (General rules)
      *> cite.py: OK  §14.9.35.4 7)  (General rules) - FROM = MOVE
      *>   identifier-1 TO record-name-1, then REWRITE without FROM
      *> cite.py: OK  §14.9.25.4 6)  (General rules) - a) alphanumeric
      *>   receiver: truncation/space filling per 14.6.8
      *> identifier-1 items are in WORKING-STORAGE. Senders are chosen
      *> so the implicit MOVE changes the image (longer, numeric), so an
      *> implementation that wrote the record image back into
      *> identifier-1 would show it.
      *> Derivation (each line):
      *>  F1 W-LONG X(12) "YYYYYYYYYYYZ" into the X(10) record 1 -> 00;
      *>     W-LONG still "YYYYYYYYYYYZ" (GR10); the record holds the
      *>     first 10 characters "YYYYYYYYYY" (MOVE truncation).
      *>  F2 W-NUM 9(4) 1234 into record 2 -> 00; W-NUM still 1234;
      *>     record "1234" + 6 spaces.
      *>  F3 W-LONG into record 9 (absent) -> 23; "complete" covers an
      *>     unsuccessful execution too: W-LONG still "YYYYYYYYYYYZ".
      *>  Read-back: [YYYYYYYYYY] [1234      ].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C25F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R-FILE ASSIGN TO "L1C25F.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS R-KEY
               FILE STATUS IS R-ST.
       DATA DIVISION.
       FILE SECTION.
       FD R-FILE.
       01 R-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 R-ST   PIC XX.
       01 R-KEY  PIC 9(4).
       01 W-LONG PIC X(12) VALUE "YYYYYYYYYYYZ".
       01 W-NUM  PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT R-FILE.
           MOVE 1 TO R-KEY. MOVE "AAAAAAAAAA" TO R-REC. WRITE R-REC.
           MOVE 2 TO R-KEY. MOVE "BBBBBBBBBB" TO R-REC. WRITE R-REC.
           CLOSE R-FILE.
           OPEN I-O R-FILE.
           MOVE 1 TO R-KEY.
           REWRITE R-REC FROM W-LONG.
           DISPLAY "F1 ST=" R-ST " W=" W-LONG.
           MOVE 2 TO R-KEY.
           REWRITE R-REC FROM W-NUM.
           DISPLAY "F2 ST=" R-ST " W=" W-NUM.
           MOVE 9 TO R-KEY.
           REWRITE R-REC FROM W-LONG
               INVALID KEY CONTINUE
           END-REWRITE.
           DISPLAY "F3 ST=" R-ST " W=" W-LONG.
           MOVE 1 TO R-KEY. READ R-FILE.
           DISPLAY "[" R-REC "]".
           MOVE 2 TO R-KEY. READ R-FILE.
           DISPLAY "[" R-REC "]".
           CLOSE R-FILE.
           STOP RUN.
