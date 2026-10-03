      *> reject-at: 85 2002 2014 2023
      *> ISO 8.4.2.2.1 - "a statement shall contain a reference that uniquely
      *> identifies that resource".  13.18.54.4 GR1 establishes "an independent
      *> sum counter" for EACH entry containing a SUM clause, so the two CF-T
      *> entries below establish TWO counters that happen to share a name.
      *> DECLARING them that way is legal - the uniqueness requirement bites on a
      *> REFERENCE, which is exactly why tests/conformance/85/pb882_report_sum_
      *> counter_identity.cob compiles and prints two different totals from an
      *> identically shaped report description.  What is illegal is the
      *> REFERENCE: `MOVE CF-T TO WS-SEEN` names neither counter in particular,
      *> and the qualifiers a sum counter takes - the report group entries above
      *> it and its report-name (8.4.2.2.3 SR4, kb/Work PB1454) - are the SAME
      *> for both counters here (CFT OF R-1), so no qualification separates them.
      *> COBOLNET2145.  Before kb/Work PB840 the reference drew COBOLNET1639
      *> "is not defined" instead - the right verdict under the wrong rule, and
      *> one that would have gone on being right for the wrong reason the day
      *> GR5's name was published.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB882N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb882n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 99 VALUE 11.
       01 WS-B PIC 99 VALUE 22.
       01 WS-SEEN PIC 9999 VALUE 0.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 CF-T COLUMN 1 PIC 9999 SUM WS-A.
          02 CF-T COLUMN 7 PIC 9999 SUM WS-B.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           MOVE CF-T TO WS-SEEN.
           CLOSE RPT.
           STOP RUN.
