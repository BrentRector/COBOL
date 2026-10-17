*>--- conformance-case
*> id: pb88-string-into-strong-group
*> rules: [SR-14.9.43.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR6: "Identifier-3 shall not reference a strongly-typed group item." kb/Work PB88: this rule
      *> was not checked at all; COBOLNET1651 at bind now (the same Reject site as SR4/SR5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NSTRSTG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST-T TYPEDEF STRONG.
          05 SA PIC X(4).
          05 SB PIC X(4).
       01 ST TYPE ST-T.
       PROCEDURE DIVISION.
           STRING "AB" DELIMITED SIZE INTO ST.
           STOP RUN.
