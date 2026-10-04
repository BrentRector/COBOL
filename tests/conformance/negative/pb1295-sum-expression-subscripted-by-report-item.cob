      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1295 - ISO 13.18.54.3 SR6: "If the addend is arithmetic-expression-1, any identifiers it contains may reference entries in any section of the data
      *> division other than the report section."   cite.py: OK  13.18.54.3 6)  (Syntax rules)
      *> The subscript DETU of TE(DETU) is an identifier the expression contains and an entry of the report section (an unprintable SOURCE entry,
      *> neither a sum counter nor a counter register, so 8.4.2.3.3 SR8 does not name it): ReportSectionNameIn never read the words of a subscript.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1295SUMEXPRESSIONSUBSCRI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1295SUMEXPRESSIONSUBSCRI.rpt".
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
           03  DETU PIC 9 SOURCE WK.
           03  COLUMN 5 PIC 99 SUM WK + TE(DETU).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
