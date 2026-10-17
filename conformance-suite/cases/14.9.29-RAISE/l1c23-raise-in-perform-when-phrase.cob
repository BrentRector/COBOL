*>--- conformance-case
*> id: l1c23-raise-in-perform-when-phrase
*> rules: [SR-14.9.29.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.29.3 4) — RAISE only in imperative-statement-1 of an exception-checking PERFORM
      *> Rule: "Within an exception-checking PERFORM statement, the RAISE statement shall not be
      *>   specified in any imperative statement other than imperative-statement-1."
      *>   cite.py: OK  §14.9.29.3 4)  (Syntax rules)
      *> The exception-checking PERFORM (format 3) is new in COBOL 2023, so 2023 is the only edition.
      *> The RAISE sits in the imperative statement of the WHEN phrase -- not imperative-statement-1.
      *> Everything else is valid: EC-SIZE-OVERFLOW is a level-3 exception-name (SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23K.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM
               DISPLAY "IMP-1"
           WHEN EC-SIZE-OVERFLOW
               RAISE EXCEPTION EC-SIZE-OVERFLOW
           END-PERFORM
           STOP RUN.
