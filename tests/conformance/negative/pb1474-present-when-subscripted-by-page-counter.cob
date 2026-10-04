      *> reject-at: 2002 2014 2023
      *> kb/Work PB1474 - ISO 8.4.2.3.3 SR8: "In the report section, neither a sum counter nor the LINE-COUNTER and PAGE-COUNTER identifiers may be used as a subscript."
      *> cite.py: OK  8.4.2.3.3 8)  (Syntax rules)
      *> Here: PRESENT WHEN TE(PAGE-COUNTER) = 1 (COBOL-2002). 13.15.3 SR16 also refuses every reference to PAGE-COUNTER in the condition
      *> ("Condition-1 shall not reference any sum counter, LINE-COUNTER, PAGE-COUNTER, or other report section data item."), subscript or not;
      *> cite.py: OK  13.15.3 16)   It is that rule (COBOLNET1559) that reports it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1474PRESENTWHENSUBSCRIPT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1474PRESENTWHENSUBSCRIPT.rpt".
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
           03  COLUMN 1 PIC X VALUE 'P' PRESENT WHEN TE(PAGE-COUNTER)
           = 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
