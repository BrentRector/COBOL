      *> reject-at: 85 2002 2014 2023
      *> ISO §13.15.3 4) — the first entry after an RD shall be level 1
      *> Rule: "The first entry that follows a report description entry
      *>   shall be a level 1 entry."
      *>   cite.py: OK  §13.15.3 4)  (Syntax rules)
      *> The entry that follows RD R-1 is a level-03 LINE entry with a
      *> level-05 COLUMN entry beneath it, so §13.15.3 9) ("Every
      *> elementary entry with a COLUMN clause but no LINE clause shall
      *> be subordinate to an entry with a LINE clause", OK 9)) and 10)
      *> (a SOURCE clause is present, OK 10)) hold. The level-01 detail
      *> DET-1 after it is complete and is the group the program
      *> GENERATEs; the only defect is the leading entry not being
      *> level 1.
      *> Expected diagnostic: the report-group arm (ReportGroupBefore01).
      *> Its code is the COBOLNET0899 "recognized but not implemented"
      *> band, a misfiling owned by kb/Work PB1559, so the .err pins the
      *> message head only, never the code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8NF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "l1m8nf.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WA PIC X VALUE "A".
       REPORT SECTION.
       RD R-1.
          03 LINE PLUS 1.
             05 COLUMN 1 PIC X SOURCE WA.
       01 DET-1 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC X SOURCE WA.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
