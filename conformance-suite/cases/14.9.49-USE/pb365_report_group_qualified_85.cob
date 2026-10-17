      *>--- conformance-case
      *> id: pb365_report_group_qualified_85
      *> rules: [SR-14.9.49.3-9, GR-14.9.49.4-1, FMT-8.4.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE OLDEST EDITION'S ARM of the same rule its 2023 twin (2023/pb365_report_group_qualified) proves.
      *> §8.4.2.2 qualification and §14.9.16.3 SR1's "It may be qualified by a report-name" are edition-invariant
      *> — neither the uniqueness rule nor the report-group qualifier has an introduction edition — so the
      *> qualified spelling shall parse and bind identically at --std 85, and the declarative shall attach to the
      *> report its qualifier names.  There is NO gating diagnostic to check: nothing here is edition-gated.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT is the 2023 twin's, unchanged: the declarative names DET-A OF R-B, so
      *> only R-B's presentation runs it (§14.9.49.4 GR8/GR9 c)-d)).  Expected: GEN-A, GEN-B, HOOK, N=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB365RG85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA ASSIGN TO "pb365-rg85-a.rpt".
           SELECT RB ASSIGN TO "pb365-rg85-b.rpt".
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
