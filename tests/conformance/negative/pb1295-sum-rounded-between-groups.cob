      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1295 - ISO 13.18.54.2: the SUM clause is { SUM OF { data-name-1 | identifier-1 | arithmetic-expression-1 } ... [ UPON { data-name-2 } ... ] } ...
      *> followed by [ RESET ON { data-name-3 | FINAL } ] [ rounded-phrase ]; the outer brace closes after the UPON phrase, so the RESET phrase and the
      *> rounded-phrase come once, after every SUM group (cite.py --check 13.18.54.2: the printed format; 13.18.54.3 SR1: "The whole clause is referred
      *> to as a SUM clause even though the SUM keyword may appear more than once").
      *> Here: ROUNDED written between two SUM groups.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1295SUMROUNDEDBETWEENGRO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1295SUMROUNDEDBETWEENGRO.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SUM WK ROUNDED SUM WK.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
