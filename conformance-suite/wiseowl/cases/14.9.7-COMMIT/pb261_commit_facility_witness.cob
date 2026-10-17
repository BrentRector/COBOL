      *>--- conformance-case
      *> id: pb261_commit_facility_witness
      *> rules: [GR-14.9.7.4-3, GR-14.9.7.4-4, GR-14.9.7.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.7.4 GR3/GR4/GR5 — the commit and rollback facility is
      *> DECLINED. It is PROCESSOR-DEPENDENT (Annex A.3 item 6, "The
      *> commit and rollback facility is dependent upon the capabilities
      *> of the processor and its storage devices") and OPTIONAL (Annex
      *> A.4.3 item 4, "COMMIT statement 14.9.7"), with A.4.1 carrying
      *> the licence to GR3-GR5. Because it is processor-dependent,
      *> §4.2.6 third paragraph makes the compile-time WARNING MECHANISM
      *> MANDATORY: "An implementation shall provide a warning mechanism
      *> at compile time to indicate use of syntactically-detectable
      *> processor-dependent language elements not supported by that
      *> implementation." That named warning (COBOLNET1579) is the
      *> DECLINE half, pinned by conformance-test
      *> DocumentedNonSupportWitnessTests.
      *> THE OUTPUT IS SPEC-DERIVED, not merely observed. §14.9.7.4 GR1
      *> and §14.9.36.4 GR1 are identical: "If this statement is
      *> executed when there is no active APPLY COMMIT clause, then it
      *> has the same effect as a CONTINUE statement with no additional
      *> phrases." This program declares no APPLY COMMIT clause, so
      *> GR3-GR5 (and §14.9.36.4 GR3's restore) have EMPTY SCOPE and a
      *> fully conforming processor also leaves WS-N at 3: control flows
      *> through both statements and nothing is saved or restored.
      *> That is exactly why the warning, not this .out, is what closes
      *> the rows — at zero active APPLY COMMIT clauses the declined
      *> statement is indistinguishable from the conforming one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CMTW01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BEFORE".
           ADD 1 TO WS-N.
           COMMIT.
           ADD 1 TO WS-N.
           ROLLBACK.
           ADD 1 TO WS-N.
           DISPLAY "N=" WS-N.
           DISPLAY "AFTER".
           STOP RUN.
