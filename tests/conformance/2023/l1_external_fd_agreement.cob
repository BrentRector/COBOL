      *> ISO §13.4.5.4 GR2 a)-d) — two programs' agreeing EXTERNAL FDs (BLOCK, CODE-SET, LINAGE, record size) share one connector
      *> RULE (§13.4.5.4 2), cite.py OK a) b) c) 1. c) 2. d)): "If the
      *> EXTERNAL clause is specified, all file description entries in the
      *> run unit that reference the same file connector as file-name shall
      *> obey the following rules: a) ... BLOCK CONTAINS ... same minimum
      *> and maximum size ... b) ... CODE-SET ... same character set. c) ...
      *> LINAGE ... 1. the same corresponding values for any literals ...
      *> 2. the same corresponding external data items. d) ... the same
      *> smallest and largest record size." The rule binds the PROGRAMS;
      *> the observable obligation on the implementation is that a run
      *> unit that OBEYS it works: both FDs name ONE external file
      *> connector (§13.18.22.4 4) a), 5): the externalized name is the FD
      *> name; cite.py OK). Main L1FDXMN and the separately described
      *> L1FDXSB both carry:
      *>   XF  BLOCK 2 RECORDS / RECORD 6 / LINAGE 20 (a literal, c) 1.) /
      *>       CODE-SET STANDARD-1
      *>   YF  BLOCK 2 RECORDS / RECORD 4 / CODE-SET STANDARD-1
      *>   ZF  LINAGE IS L1FDX-PG LINES, where L1FDX-PG is the SAME
      *>       EXTERNAL data item in both programs (c) 2.)
      *> b) is "the same CHARACTER SET", not the same alphabet-name: the
      *> main names STANDARD-1 L1FDX-STD, the sub names it L1FDX-ASC.
      *> DERIVATION:
      *>  LINAGE-COUNTER is EXTERNAL (§13.4.5.4 3), cite.py OK), set to 1
      *>  by OPEN OUTPUT and +1 per WRITE without ADVANCING (§13.18.34.4 7)
      *>  d) and c) 3., cite.py OK). Two files have LINAGE, so every
      *>  reference is qualified (§13.18.34.4 7) b), cite.py OK).
      *>  XF: main OPEN -> 1; main WRITE -> 2; sub WRITE -> 3:
      *>                                                    SUB LC=0003
      *>  ZF: main OPEN -> 1; main WRITE -> 2; sub WRITE -> 3:
      *>                                                    SUB ZLC=0003
      *>  XF: main WRITE -> 4:                              MAIN LC=0004
      *>  YF: main writes AAAA, the sub writes BBBB through ITS OWN FD on
      *>  the SAME open connector (it never opens it), main writes CCCC;
      *>  reading back yields the three records in write order, then EOF.
      *>  CODE-SET STANDARD-1 is a round trip, so content is unchanged.
      *> GR2 e) (REPORT) is pinned by l1_external_fd_report_agreement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDXMN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET L1FDX-STD IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "L1FDXX.TXT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT YF ASSIGN TO "L1FDXY.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT ZF ASSIGN TO "L1FDXZ.TXT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD XF IS EXTERNAL
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 6 CHARACTERS
           LINAGE IS 20 LINES
           CODE-SET IS L1FDX-STD.
       01 XF-REC PIC X(6).
       FD YF IS EXTERNAL
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 4 CHARACTERS
           CODE-SET IS L1FDX-STD.
       01 YF-REC PIC X(4).
       FD ZF IS EXTERNAL
           LINAGE IS L1FDX-PG LINES.
       01 ZF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 L1FDX-PG PIC 99 EXTERNAL.
       01 WS-LC PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 20 TO L1FDX-PG.
           OPEN OUTPUT XF YF ZF.
           WRITE XF-REC FROM "MAIN-1".
           WRITE YF-REC FROM "AAAA".
           WRITE ZF-REC FROM "ZZZ1".
           CALL "L1FDXSB".
           WRITE XF-REC FROM "MAIN-2".
           WRITE YF-REC FROM "CCCC".
           MOVE LINAGE-COUNTER OF XF TO WS-LC.
           DISPLAY "MAIN LC=" WS-LC.
           CLOSE XF YF ZF.
           OPEN INPUT YF.
           READ YF.
           DISPLAY "Y1=" YF-REC.
           READ YF.
           DISPLAY "Y2=" YF-REC.
           READ YF.
           DISPLAY "Y3=" YF-REC.
           READ YF
               AT END DISPLAY "EOF"
           END-READ.
           CLOSE YF.
           STOP RUN.
       END PROGRAM L1FDXMN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDXSB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET L1FDX-ASC IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "L1FDXX.TXT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT YF ASSIGN TO "L1FDXY.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT ZF ASSIGN TO "L1FDXZ.TXT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD XF IS EXTERNAL
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 6 CHARACTERS
           LINAGE IS 20 LINES
           CODE-SET IS L1FDX-ASC.
       01 XF-REC PIC X(6).
       FD YF IS EXTERNAL
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 4 CHARACTERS
           CODE-SET IS L1FDX-ASC.
       01 YF-REC PIC X(4).
       FD ZF IS EXTERNAL
           LINAGE IS L1FDX-PG LINES.
       01 ZF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 L1FDX-PG PIC 99 EXTERNAL.
       01 WS-LC PIC 9(4).
       PROCEDURE DIVISION.
       SB1.
           WRITE XF-REC FROM "SUB--1".
           WRITE YF-REC FROM "BBBB".
           MOVE LINAGE-COUNTER OF XF TO WS-LC.
           DISPLAY "SUB LC=" WS-LC.
           WRITE ZF-REC FROM "ZZZ2".
           MOVE LINAGE-COUNTER OF ZF TO WS-LC.
           DISPLAY "SUB ZLC=" WS-LC.
           GOBACK.
       END PROGRAM L1FDXSB.
