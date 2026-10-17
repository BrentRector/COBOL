*>--- conformance-case
*> id: pb365_report_group_qualified
*> rules: [SR-14.9.49.3-9, GR-14.9.49.4-1, FMT-14.9.49.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.16.3 SR1: "Data-name-1 shall name a detail report group. It may be qualified by a
      *> report-name."  ISO §14.9.49.3 SR9: "Identifier-1 shall reference a report group."  Identifier-1 and
      *> data-name-1 are ordinary identifiers, so §8.4.2.2.1 governs both — "Identical user-defined names may be
      *> specified in a source unit; however, uniqueness shall be established through qualification for each
      *> user-defined name explicitly referenced" — and §8.4.2.2.2 Format 1's file-report-qualifier makes the
      *> REPORT-NAME the qualifier a level-01 report group has.  §8.4.2.2.3 SR3: "The words IN and OF are
      *> equivalent", so both spellings appear below on purpose.
      *>
      *> THE POINT OF THIS PROGRAM.  Two report description entries EACH describe a detail group named DET-A.
      *> Before kb/Work PB365 the qualified spelling did not parse at all (`GENERATE DET-A OF R-B` was COBOL0001
      *> "no viable alternative at input 'OF'" — legal COBOL rejected), and the USE and GENERATE binders each
      *> resolved the bare name by scanning the reports in written order and RETURNING ON THE FIRST MATCH, so
      *> the declarative silently attached to R-A's group because R-A is written first.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  The declarative names DET-A OF R-B, so §14.9.49.4 GR8 ("The
      *> declarative is invoked just before the named report group is produced") makes R-B's DET-A the only
      *> group that runs it.  GEN-A is displayed, then GENERATE DET-A OF R-A presents R-A's group with NO
      *> declarative — nothing further is displayed.  GEN-B is displayed, then GENERATE DET-A IN R-B presents
      *> R-B's group: GR9 c)/d) run the procedure before the group's LINE clauses are processed, so HOOK is
      *> displayed and WS-N becomes 1.  TERMINATE prints no further group (neither RD has RH/PH/CH/CF/PF/RF,
      *> §14.9.46.4 GR3).  Expected: GEN-A, GEN-B, HOOK, N=1 — in that order.  The ORDER is the discriminator:
      *> a first-report-wins binder produces GEN-A, HOOK, GEN-B, N=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB365RGQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA ASSIGN TO "pb365-rgq-a.rpt".
           SELECT RB ASSIGN TO "pb365-rgq-b.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RA REPORT IS R-A.
       FD RB REPORT IS R-B.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-A PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "A=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       RD R-B PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "B=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR SECTION.
           USE BEFORE REPORTING DET-A OF R-B.
       BR-P.
           DISPLAY "HOOK".
           ADD 1 TO WS-N.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RA.
           OPEN OUTPUT RB.
           INITIATE R-A.
           INITIATE R-B.
           DISPLAY "GEN-A".
           GENERATE DET-A OF R-A.
           DISPLAY "GEN-B".
           GENERATE DET-A IN R-B.
           TERMINATE R-A.
           TERMINATE R-B.
           CLOSE RA.
           CLOSE RB.
           DISPLAY "N=" WS-N.
           STOP RUN.
