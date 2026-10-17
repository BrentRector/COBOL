      *>--- conformance-case
      *> id: qualified_unique_match
      *> rules: [SR-8.4.2.2.3-2, SR-8.4.2.2.3-3, FMT-8.4.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work R31 - the 8.4.2.2.1 uniqueness is of the qualified MATCH, never of a qualifier
      *> name in isolation: two groups each declare an X, exactly one X holds a Z, so Z IN X is a
      *> UNIQUE reference and legal ("All available qualifiers need not be specified so long as
      *> uniqueness is established"). The old walk resolved the qualifier X first, failed on its
      *> ambiguity, and rejected this - GnuCOBOL's own syn_definition "Unique reference with
      *> ambiguous qualifiers" case, caught by the differential when R30 made the failure loud.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QUALMATCH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
         02 X.
           03 Y PIC X VALUE "Y".
       01 G2.
         02 X.
           03 Z PIC X VALUE "Z".
       PROCEDURE DIVISION.
           DISPLAY Z IN X.
           DISPLAY Y OF X.
           DISPLAY Z IN X IN G2.
           STOP RUN.
