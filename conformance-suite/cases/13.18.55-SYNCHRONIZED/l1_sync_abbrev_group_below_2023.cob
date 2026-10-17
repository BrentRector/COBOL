*>--- conformance-case
*> id: l1_sync_abbrev_group_below_2023
*> rules: [SR-13.18.55.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.55.3 SR2 - "SYNC is an abbreviation for SYNCHRONIZED." An
      *> abbreviation is the SAME clause spelled shorter, so it has to meet
      *> the SAME edition gate: SYNCHRONIZED on a GROUP item is a COBOL-2023
      *> introduction (ISO Annex E.3.2 item 6 / VCR row 43) and below 2023 it
      *> is rejected strict with COBOLNET0900 - the diagnostic
      *> negative/sync_group_below_2023 pins for the FULL spelling. This is
      *> the complement: if the two spellings were separate constructs the
      *> abbreviation could slip past the gate and silently accept a 2023-only
      *> construct at an older edition. A positive golden cannot show this
      *> half, because acceptance proves nothing about a gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SGAB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G SYNC.
         05 A PIC X.
         05 B PIC X.
       PROCEDURE DIVISION.
       M. STOP RUN.
