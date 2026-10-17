      *>--- conformance-case
      *> id: l1c33_terminate_single_in_exc_perform
      *> rules: [SR-14.9.46.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.46.3 SR3 — single-name TERMINATEs, exception PERFORM
      *> "A TERMINATE statement that specifies more than one report-name
      *> shall not be specified in an exception checking PERFORM
      *> statement."
      *>   cite.py: OK  §14.9.46.3 3)  (Syntax rules)
      *> The companion of conformance:negative/l1c33-terminate-multi-in-
      *> exc-perform: SR3 bans only the MULTI-name form, so two
      *> single-name TERMINATEs in the same PERFORM shape are legal and
      *> must compile and run (an implementation banning TERMINATE in
      *> the PERFORM altogether fails here).
      *> DERIVATION. Both reports are active, so neither TERMINATE
      *> raises EC-REPORT-INACTIVE (§14.9.46.4 GR1, cite.py:
      *> OK  §14.9.46.4 1));
      *> the WHEN EC-REPORT handler does not run -> no "EX"; each report
      *> had one GENERATE, so each RF is produced (§14.9.46.4 GR3 c),
      *> cite.py: OK  §14.9.46.4 3) c)) in statement order -> "RF-A",
      *> "RF-B" via USE BEFORE REPORTING (§14.9.49.4 GR8, cite.py:
      *> OK  §14.9.49.4 8)); then "OK".
      *> EDITION: the exception-checking PERFORM is COBOL-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "L1C33CA.RPT".
           SELECT RPTB ASSIGN TO "L1C33CB.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPTA REPORT IS R-A.
       FD RPTB REPORT IS R-B.
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
           INITIATE R-A.
           INITIATE R-B.
           GENERATE DA.
           GENERATE DB.
           PERFORM
               TERMINATE R-A
               TERMINATE R-B
             WHEN EC-REPORT
               DISPLAY "EX"
           END-PERFORM.
           CLOSE RPTA RPTB.
           DISPLAY "OK".
           STOP RUN.
