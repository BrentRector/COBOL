*>--- conformance-case
*> id: pb482-sum-upon-not-detail
*> rules: [SR-13.18.54.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR7 - "Data-name-2 shall be the name of a detail. It may
      *> be qualified only by a report-name."  CFT is a CONTROL FOOTING, and the
      *> rule bites because 13.18.54.4 GR7 c) 2) accumulates the addend
      *> "whenever any GENERATE statement is executed for a detail referenced by
      *> the UPON phrase" while 14.9.16.3 SR1 makes only a DETAIL the operand of
      *> a GENERATE statement: an UPON naming any other report group names an
      *> event that can never occur.
      *> MEASURED BEFORE kb/Work PB482: the UPON operand was reduced to its first
      *> word and never resolved at all, so this compiled clean and the counter
      *> printed 0000 - the addend was never added, silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB482N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb482n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 99 VALUE 11.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 COLUMN 7 PIC 9999 SUM WS-A UPON CFT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
