      *>--- conformance-case
      *> id: l1c33-terminate-multi-in-exc-perform
      *> rules: [SR-14.9.46.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.46.3 SR3 — multi-name TERMINATE in exception PERFORM
      *> "A TERMINATE statement that specifies more than one report-name
      *> shall not be specified in an exception checking PERFORM
      *> statement."
      *>   cite.py: OK  §14.9.46.3 3)  (Syntax rules)
      *> The TERMINATE R-A R-B in imperative-statement-1 names two
      *> reports; everything else is legal (the single-name companion
      *> conformance:cases/14.9.46-TERMINATE/l1c33_terminate_single_in_exc_perform compiles
      *> and runs with the same PERFORM shape). Expected: COBOLNET1606.
      *> EDITION: the exception-checking PERFORM is COBOL-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "L1C33NA.RPT".
           SELECT RPTB ASSIGN TO "L1C33NB.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPTA REPORT IS R-A.
       FD RPTB REPORT IS R-B.
       REPORT SECTION.
       RD R-A.
       01 DA TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DA".
       RD R-B.
       01 DB TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DB".
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT RPTA RPTB.
           INITIATE R-A.
           INITIATE R-B.
           PERFORM
               TERMINATE R-A R-B
             WHEN EC-REPORT
               DISPLAY "EX"
           END-PERFORM.
           CLOSE RPTA RPTB.
           STOP RUN.
