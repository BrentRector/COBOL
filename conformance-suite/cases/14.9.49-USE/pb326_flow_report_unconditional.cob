      *>--- conformance-case
      *> id: pb326_flow_report_unconditional
      *> rules: [GR-14.9.49.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.4 GR10: "If a GENERATE, INITIATE, or TERMINATE statement is executed within the range of
      *> a declarative procedure whose USE statement contains the BEFORE REPORTING phrase, the EC-FLOW-REPORT
      *> exception condition is set to exist, the result of the execution of the GENERATE, INITIATE, or
      *> TERMINATE statement is unsuccessful, and the state of the report is unchanged."
      *>
      *> THE POINT OF THIS PROGRAM IS THE UNCONDITIONAL HALF, AT THE OLDEST EDITION. §14.6.13.1.1 gates only the
      *> RAISING of the condition on checking being enabled; "the result … is unsuccessful, and the state of the
      *> report is unchanged" is stated outright and holds at every edition and every checking setting. COBOL-85
      *> has no >>TURN directive at all, so no checking can be enabled here — and the nested GENERATE must still
      *> produce nothing.  (Its checked twin is 2002/pb326_ec_flow_report.)
      *>
      *> The program is legal: §14.9.49.3 SR10 forbids GENERATE/INITIATE/TERMINATE only "in a paragraph within a
      *> USE BEFORE REPORTING procedure", and GEN-P is a paragraph of a DIFFERENT declarative section, which SR4
      *> ("Procedure-names within a declarative section may be referenced in a different declarative section …
      *> only with a PERFORM statement") expressly permits BR-P to PERFORM.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  The single GENERATE presents DET-1; §14.9.49.4 GR8/GR9d run the
      *> declarative just before the group's LINE clauses are processed, so BR-P sets WS-N to 1 and PERFORMs
      *> GEN-P, whose GENERATE is inside the range and therefore does nothing at all.  Control returns and the
      *> outer presentation composes DET-1 with WS-N = 1 (§13.18.53.4 GR1/GR3 — the implicit MOVE executes when
      *> the line is printed).  The report has no RH/PH/CH/CF/PF/RF, so TERMINATE prints nothing further
      *> (§14.9.46.4 GR3).  Expected: N=1.
      *>
      *> WHY N ALONE COUNTS THE DETAIL LINES.  §14.9.49.4 GR8/GR9d execute the USE BEFORE REPORTING procedure
      *> ONCE per PRESENTATION of the group, immediately before its LINE clauses are processed, so WS-N IS the
      *> number of DET-1 lines presented: N=1 says "exactly one detail line", and the nested GENERATE's
      *> presentation would have re-entered BR-P (N=3, its own recursion stopping at the WS-N < 3 guard).
      *> Before kb/Work PB326 the nested GENERATE succeeded and a SECOND detail line was produced.
      *> This program used to CONFIRM that by re-reading the report file through a second SELECT with
      *> ORGANIZATION IS LINE SEQUENTIAL; that organization is a COBOL-2023 introduction (§12.4.5.10.3 GR2 —
      *> the Foreword lists it among the main changes over ISO/IEC 1989:2014) and cannot appear in a COBOL-85
      *> program at all, so the read-back is gone (kb/Work PB688).  Nothing is lost: it observed the same
      *> count at the file that the declarative already observes at the source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB326RW85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb326-flow85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-F85.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-F85 PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-1.
       BR-P.
           ADD 1 TO WS-N.
           IF WS-N < 3
               PERFORM GEN-P
           END-IF.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT.
       GEN-P.
           GENERATE DET-1.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-F85.
           GENERATE DET-1.
           TERMINATE R-F85.
           CLOSE RPT.
           DISPLAY "N=" WS-N.
           STOP RUN.
