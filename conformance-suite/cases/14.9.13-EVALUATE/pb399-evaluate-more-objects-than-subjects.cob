*>--- conformance-case
*> id: pb399-evaluate-more-objects-than-subjects
*> rules: [SR-14.9.13.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR2: "The number of selection objects within each set of selection objects shall be
      *> equal to the number of selection subjects."  One selection subject, two selection objects.
      *>
      *> A SYNTAX RULE VIOLATION IS A COMPILE-TIME DIAGNOSTIC (§4.2.2).  This used to be caught by a
      *> DEFENSIVE INDEX GUARD inside the pairing loop, which returned a BoundUnsupported into the bound
      *> tree — so the "diagnostic" arrived as an unhandled NotImplementedCobolFeatureException at RUN TIME,
      *> and only if that WHEN phrase was actually reached (kb/Work PB399).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399MORE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-A
               WHEN 1 ALSO 2
                   DISPLAY "MATCHED"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
