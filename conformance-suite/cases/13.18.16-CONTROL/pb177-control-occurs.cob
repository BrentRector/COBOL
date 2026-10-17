*>--- conformance-case
*> id: pb177-control-occurs
*> rules: [SR-13.18.16.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR3: "Data-name-1 shall not be subject to any OCCURS clauses." Unscreened until
      *> kb/Work PB177 arm C - measured: it compiled and ran silently. SR3 is also the clause TWO shipping
      *> diagnostics MISCITED (an unresolvable CONTROL operand, and a float/INDEX one) - a real clause
      *> answering a different question, the failure mode CLAUDE.md rule 1 names. Both are repaired.
      *> ⛔ THE OCCURS SITS ON AN 05-LEVEL ENTRY, DELIBERATELY. The first cut of this witness wrote
      *> "01 CG OCCURS 3 TIMES.", which 13.18.38.3 SR1 a) forbids outright ("The OCCURS clause shall not be
      *> specified in a data description entry that: a) Has a level-number of 01, 66, 77, or 88") - so the
      *> program was nonconforming for a reason unrelated to the rule under test, and green only because that
      *> rule is unscreened here (registered and scheduled to P14 Step 0b, the SR census). A negative fixture
      *> must isolate ITS rule; one that leans on a second gap stops witnessing the moment that gap closes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb177n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CG.
          05 CR OCCURS 3 TIMES.
             10 CT PIC X(3).
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           CONTROL IS CT
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
