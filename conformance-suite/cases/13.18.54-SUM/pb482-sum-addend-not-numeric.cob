*>--- conformance-case
*> id: pb482-sum-addend-not-numeric
*> rules: [SR-13.18.54.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR5 - "If the addend is identifier-1, it shall specify a
      *> numeric data item not defined in the report section."  WS-TXT is
      *> PIC X(6): alphanumeric, not numeric.  The rule exists because
      *> 13.18.54.4 GR3 adds the addend's content into the counter by an implicit
      *> ADD, and 14.7.7's arithmetic has no meaning for an alphanumeric operand.
      *> MEASURED BEFORE kb/Work PB482: no category screen existed anywhere, so
      *> this compiled clean and the generated addend delegate read the item's
      *> numeric face - with WS-TXT holding "123456" the PIC 9999 counter printed
      *> 6912 after two GENERATEs.  A silent wrong answer on illegal source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB482N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb482n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-TXT PIC X(6) VALUE "123456".
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 COLUMN 7 PIC 9999 SUM WS-TXT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
