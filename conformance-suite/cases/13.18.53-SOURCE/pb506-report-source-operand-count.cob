*>--- conformance-case
*> id: pb506-report-source-operand-count
*> rules: [SR-13.18.53.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.53.3 SR6 — the SOURCE clause's twin of §13.18.63.3 SR35, written for the other clause: "If
      *> the SOURCE clause has more than one operand, the entry shall be a repeating entry or shall be
      *> subordinate to a repeating entry, and the number of operands of the SOURCE clause shall be equal to the
      *> number of repetitions of the repeating entry or the same number multiplied by the number of repetitions
      *> of any number of successive repeating entries at higher levels than the repeating entry."  The entry
      *> below is a repeating entry with TWO repetitions (§13.15.4 GR3 — a COLUMN clause with two operands) and
      *> writes THREE SOURCE operands, which is neither two nor two times anything.
      *> ⛔ THIS IS THE ARM THAT WAS NEVER THERE (kb/Work PB506).  Before it, `reportSourceClause` was
      *> `SOURCE IS? dataReference` — ONE operand, no SOURCES spelling — so a conforming multi-operand SOURCE
      *> clause was a raw parse error and SR6 had nothing at all to screen.  The two rules are now screened by
      *> ONE reader, with a diagnostic per clause so the reader is sent to the subclause actually written.
      *> SR6 carries no version proviso, so it is reported at all four editions; at 85 the two COBOLNET0900
      *> introduction gates this program trips - the multiple COLUMN clause and the multi-operand SOURCE /
      *> SOURCES spelling - are reported ALONGSIDE it, not instead of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB506N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb506n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-N3.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(3) VALUE "AAA".
       01  WS-B PIC X(3) VALUE "BBB".
       01  WS-C PIC X(3) VALUE "CCC".
       REPORT SECTION.
       RD  R-N3 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMNS ARE 1 6 PIC X(3) SOURCES ARE WS-A WS-B WS-C.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-N3.
           GENERATE DET.
           TERMINATE R-N3.
           CLOSE PRT.
           STOP RUN.
