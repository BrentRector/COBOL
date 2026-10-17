      *>--- conformance-case
      *> id: l1c13-initiate-multi-in-exception-perform
      *> rules: [SR-14.9.21.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.21.3 SR3 — "An INITIATE statement that specifies
      *> more than one report-name-1 shall not be specified in an
      *> exception checking PERFORM statement."
      *>   cite.py: OK  §14.9.21.3 3)  (Syntax rules)
      *> INITIATE R-A R-B names two report-names inside the
      *> imperative-statement-1 of an exception-checking PERFORM
      *> (Format 3), which is exactly the forbidden placement.  The same
      *> program with the two names written as two INITIATE statements
      *> would be legal; nothing else in it is questionable.
      *> ONLY 2023: the exception-checking PERFORM is new in COBOL-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C13D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "L1C13D-A.RPT".
           SELECT F-B ASSIGN TO "L1C13D-B.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD F-A REPORT IS R-A.
       FD F-B REPORT IS R-B.
       REPORT SECTION.
       RD R-A.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(8) VALUE "A-DETAIL".
       RD R-B.
       01 DET-B TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(8) VALUE "B-DETAIL".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-A F-B.
           PERFORM
               INITIATE R-A R-B
           WHEN EC-REPORT-ACTIVE
               CONTINUE
           END-PERFORM.
           TERMINATE R-A.
           TERMINATE R-B.
           CLOSE F-A F-B.
           STOP RUN.
