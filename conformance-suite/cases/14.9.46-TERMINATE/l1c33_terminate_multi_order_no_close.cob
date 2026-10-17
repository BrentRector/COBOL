      *>--- conformance-case
      *> id: l1c33_terminate_multi_order_no_close
      *> rules: [GR-14.9.46.4-4, GR-14.9.46.4-6, FMT-14.9.46.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.46.4 GR4/GR6 — multi-name TERMINATE order; no CLOSE
      *> General format §14.9.46.2: TERMINATE { report-name-1 } ...
      *>   cite.py: OK  §14.9.46.2   (General format)
      *> GR4 sentence 1: "The result of executing a TERMINATE statement
      *> in which more than one report-name-1 is specified is as though
      *> a separate TERMINATE statement had been executed for each
      *> report-name-1 in the same order as specified in the statement."
      *>   cite.py: OK  §14.9.46.4 4)  (General rules)
      *> GR6: "The TERMINATE statement does not close the file
      *> associated with report-name-1."
      *>   cite.py: OK  §14.9.46.4 6)  (General rules)
      *> Supporting: §14.9.46.4 GR3 c) "The report footing is printed,
      *> if defined." (cite.py: OK  §14.9.46.4 3) c)) - each report
      *> had a GENERATE, so each TERMINATE prints its RF; §14.9.49.4
      *> GR8 "The declarative is invoked just before the named report
      *> group is produced" (cite.py: OK  §14.9.49.4 8)) - the USE
      *> BEFORE REPORTING declaratives make each RF's production order
      *> visible on DISPLAY.
      *> DERIVATION. R-A is declared first, R-B second; the statement
      *> names them R-B R-A. GR4: TERMINATE R-B runs first, so RF-B's
      *> declarative prints "RF-B" before "RF-A". (An implementation
      *> terminating in declaration order, or in reverse written order,
      *> prints RF-A first.) GR6: both files are still open, so the
      *> later INITIATE R-A / GENERATE / TERMINATE R-A works and prints
      *> "RF-A" again, and CLOSE of each file succeeds with status 00
      *> (a CLOSE of a file that TERMINATE had closed would give 42).
      *> EDITION: Report Writer, USE BEFORE REPORTING and FILE STATUS
      *> are all COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "L1C33AA.RPT" FILE STATUS IS FSA.
           SELECT RPTB ASSIGN TO "L1C33AB.RPT" FILE STATUS IS FSB.
       DATA DIVISION.
       FILE SECTION.
       FD RPTA REPORT IS R-A.
       FD RPTB REPORT IS R-B.
       WORKING-STORAGE SECTION.
       01 FSA PIC XX.
       01 FSB PIC XX.
       REPORT SECTION.
       RD R-A.
       01 DA TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DA".
       01 FA TYPE RF LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "RFA".
       RD R-B.
       01 DB TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DB".
       01 FB TYPE RF LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "RFB".
       PROCEDURE DIVISION.
       DECLARATIVES.
       UA SECTION.
           USE BEFORE REPORTING FA.
       UA-P.
           DISPLAY "RF-A".
       UB SECTION.
           USE BEFORE REPORTING FB.
       UB-P.
           DISPLAY "RF-B".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPTA RPTB.
           INITIATE R-A R-B.
           GENERATE DA.
           GENERATE DB.
           TERMINATE R-B R-A.
           DISPLAY "TERMINATED".
           INITIATE R-A.
           GENERATE DA.
           TERMINATE R-A.
           CLOSE RPTA.
           DISPLAY "CLOSE A FS=" FSA.
           CLOSE RPTB.
           DISPLAY "CLOSE B FS=" FSB.
           STOP RUN.
