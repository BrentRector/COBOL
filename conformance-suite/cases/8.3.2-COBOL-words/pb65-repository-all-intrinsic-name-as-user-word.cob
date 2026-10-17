*>--- conformance-case
*> id: pb65-repository-all-intrinsic-name-as-user-word
*> rules: [SR-8.3.2.1-5, FMT-15.58.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1 rule 5 under `FUNCTION ALL INTRINSIC` (12.3.8 GR14 - every intrinsic-function-name is
      *> identified): a paragraph named MOD, an index-name ABS and a file-name LENGTH-OF are user-defined words
      *> spelling identified intrinsic-function-names (LENGTH itself is a reserved word). kb/Work PB65
      *> (FMT-15.58.2): `01 LOWEST-ALGEBRAIC PIC S999 VALUE -12.` compiled clean under FUNCTION ALL INTRINSIC.
      *> COBOLNET1649 at each declaration funnel.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65NR5ALL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOWEST-ALGEBRAIC PIC S999 VALUE -12.
       01 T.
          05 E PIC 9 OCCURS 3 TIMES INDEXED BY ABS.
       PROCEDURE DIVISION.
       MOD.
           STOP RUN.
