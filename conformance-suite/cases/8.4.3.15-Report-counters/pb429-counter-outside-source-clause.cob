*>--- conformance-case
*> id: pb429-counter-outside-source-clause
*> rules: [SR-8.4.3.15.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE REPORT-SECTION HALF OF §8.4.3.15.3 SR1 (kb/Work PB429). The rule has two halves and this golden is
*> the second one: "In the report section, PAGE-COUNTER and LINE-COUNTER may be referenced only in a SOURCE
*> clause. In the procedure division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where an
*> integer data item may appear." conformance:cases/8.4.3.1-Identifier/pb429_page_counter_receiving is the procedure-division half —
*> ten contexts that admit an integer data item, all accepted; this one is the report section, where the
*> permission is narrow and a counter outside a SOURCE clause is not source at all.
*>
*> The clause that refuses it says the same thing from its own side — §13.18.54.3 SR6: "If the addend is
*> arithmetic-expression-1, any identifiers it contains may reference entries in any section of the data
*> division other than the report section." A report counter IS a report-section reference (§8.4.3.15.1 —
*> "generated automatically and exist independently for each report"), so the two rules agree that a SUM
*> addend may not name one; the compiler answers with SR6's code because SR6 is the rule the written
*> construct breaks. The DETAIL line above the control footing keeps the positive direction visible in the
*> same program: `SOURCE IS PAGE-COUNTER` is the one report-section context SR1 admits, and it is legal here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB429CSC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb429csc.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS RPT1.
       WORKING-STORAGE SECTION.
       01  WS-A PIC 99 VALUE 5.
       REPORT SECTION.
       RD  RPT1 CONTROL IS FINAL PAGE LIMIT 60 LINES.
       01  DETAIL-LINE TYPE IS DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE IS PAGE-COUNTER.
       01  CF-FINAL TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           02  COLUMN 1 PIC 999 SUM WS-A + PAGE-COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE RPT1.
           GENERATE DETAIL-LINE.
           TERMINATE RPT1.
           CLOSE RPT.
           STOP RUN.
