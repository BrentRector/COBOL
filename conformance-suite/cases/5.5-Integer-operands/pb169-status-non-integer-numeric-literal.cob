*>--- conformance-case
*> id: pb169-status-non-integer-numeric-literal
*> rules: [SR-5.5-2, SR-14.9.42.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.42.3 syntax rule 3: "If literal-1 is numeric, it shall be an
      *> integer." The CONDITIONAL is what makes the NON-numeric form legal, and
      *> the same rule bars a numeric one that is not an integer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB169N2.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS 1.5.
