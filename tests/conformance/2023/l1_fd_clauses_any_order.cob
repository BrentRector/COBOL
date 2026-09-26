      *> ISO §13.4.5.3 SR2 — the clauses that follow file-name-1 may appear in any order (reverse of the printed order)
      *> RULE (§13.4.5.3 2), cite.py OK): "The clauses that follow
      *> file-name-1 may appear in any order." Both FDs below write their
      *> clauses in the REVERSE of the §13.4.5.2 Format 1 order (EXTERNAL,
      *> GLOBAL, BLOCK, record, LINAGE, CODE-SET). The program must compile
      *> strict and each clause must still take effect:
      *>  L1FDO-P  LINAGE / RECORD / GLOBAL (reversed).
      *>    GLOBAL is witnessed by the contained program L1FDORN writing
      *>    PF-REC. LINAGE is witnessed by LINAGE-COUNTER: §13.18.34.4 7) d)
      *>    "set to one at the time an OPEN statement with the OUTPUT phrase
      *>    is executed" and 7) c) 3. "When the ADVANCING phrase of the
      *>    WRITE statement is not specified, the LINAGE-COUNTER is
      *>    incremented by the value one" (both cite.py OK). OPEN -> 1, two
      *>    WRITEs -> 3 -> "LC=0003" (moved to PIC 9(4): the counter's own
      *>    size is the implementor's).
      *>  SF  CODE-SET / RECORD / BLOCK / GLOBAL / EXTERNAL (reversed).
      *>    Two 4-character records written then read back in order:
      *>    R1=ABCD, R2=WXYZ, then AT END -> EOF. CODE-SET STANDARD-1 is a
      *>    round trip, so the content is unchanged.
      *> SR2 sits under "ALL FORMATS", so Formats 2 and 3 are witnessed
      *> too:
      *>  QF  (Format 2, relative) RECORD / BLOCK / GLOBAL, the reverse of
      *>    the printed GLOBAL, BLOCK, record order. One record QRST is
      *>    written and read back -> Q1=QRST.
      *>  L1FDO-R  (Format 3, report) REPORT / GLOBAL, the reverse of the
      *>    printed GLOBAL ... REPORT order (REPORT is printed last). No
      *>    record description entry (§13.4.5.3 8)). OPEN OUTPUT, INITIATE,
      *>    GENERATE, TERMINATE, CLOSE, then RPT-DONE. The report file's
      *>    content is not compared (§13.4.5.4 4)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET L1FDO-STD IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT L1FDO-P ASSIGN TO "L1FDORDP.TXT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SF ASSIGN TO "L1FDORDS.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT QF ASSIGN TO "L1FDORDQ.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
           SELECT L1FDO-R ASSIGN TO "L1FDORDR.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD L1FDO-P
           LINAGE IS 5 LINES
           RECORD CONTAINS 6 CHARACTERS
           IS GLOBAL.
       01 PF-REC PIC X(6).
       FD SF
           CODE-SET IS L1FDO-STD
           RECORD CONTAINS 4 CHARACTERS
           BLOCK CONTAINS 2 RECORDS
           IS GLOBAL
           IS EXTERNAL.
       01 SF-REC PIC X(4).
       FD QF
           RECORD CONTAINS 4 CHARACTERS
           BLOCK CONTAINS 1 RECORDS
           IS GLOBAL.
       01 QF-REC PIC X(4).
       FD L1FDO-R
           REPORT IS L1FDO-R
           IS GLOBAL.
       WORKING-STORAGE SECTION.
       01 WS-LC PIC 9(4).
       REPORT SECTION.
       RD L1FDO-R.
       01 L1FDO-D TYPE DETAIL.
          02 LINE PLUS 1 COLUMN 1 PIC X(4) VALUE "RPT1".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT L1FDO-P.
           WRITE PF-REC FROM "LINE-1".
           CALL "L1FDORN".
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "LC=" WS-LC.
           CLOSE L1FDO-P.
           OPEN OUTPUT SF.
           WRITE SF-REC FROM "ABCD".
           WRITE SF-REC FROM "WXYZ".
           CLOSE SF.
           OPEN INPUT SF.
           READ SF.
           DISPLAY "R1=" SF-REC.
           READ SF.
           DISPLAY "R2=" SF-REC.
           READ SF
               AT END DISPLAY "EOF"
           END-READ.
           CLOSE SF.
           OPEN OUTPUT QF.
           WRITE QF-REC FROM "QRST".
           CLOSE QF.
           OPEN INPUT QF.
           READ QF.
           DISPLAY "Q1=" QF-REC.
           CLOSE QF.
           OPEN OUTPUT L1FDO-R.
           INITIATE L1FDO-R.
           GENERATE L1FDO-D.
           TERMINATE L1FDO-R.
           CLOSE L1FDO-R.
           DISPLAY "RPT-DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDORN.
       PROCEDURE DIVISION.
       N1.
           WRITE PF-REC FROM "LINE-2".
           GOBACK.
       END PROGRAM L1FDORN.
       END PROGRAM L1FDORD.
