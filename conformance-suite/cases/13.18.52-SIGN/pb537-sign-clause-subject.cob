*>--- conformance-case
*> id: pb537-sign-clause-subject
*> rules: [SR-13.18.52.3-1, SR-13.18.52.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.52.3 SR1: "The SIGN clause may be specified only for: - a numeric data or screen description
      *> entry whose picture character-string contains the symbol 'S' - a numeric report group description
      *> entry whose picture character-string contains the symbol 'S' - an alphanumeric group item, national
      *> group item, or strongly-typed group item."  SR2: "The usage of an elementary item for which the SIGN
      *> clause is specified shall be display or national."  Each entry below violates one of the two, and
      *> each compiled clean at every edition before kb/Work PB537 - COBOLNET2422:
      *>   U  - numeric, no 'S' (SR1)          X  - alphanumeric ELEMENTARY item (SR1; only a GROUP is admitted)
      *>   E  - numeric-edited, no 'S' (SR1)   C  - S9(4) COMP (SR2)       P - S9(4) COMP-3 (SR2)
      *>   I  - USAGE INDEX, no PICTURE (SR1)  GC - inherits COMP from its group (SR2 reads the usage it HAS)
      *>   the report entry - numeric, no 'S' (SR1's report bullet).
      *> The positive twin is conformance/85/pb537_sign_clause_subjects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB537NSG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb537nsg.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 U  PIC 9(3) SIGN IS LEADING.
       01 X  PIC X(3) SIGN IS LEADING.
       01 E  PIC ZZ9 SIGN IS LEADING.
       01 C  PIC S9(4) USAGE COMP SIGN IS LEADING SEPARATE.
       01 P  PIC S9(4) USAGE COMP-3 SIGN IS TRAILING.
       01 I  USAGE INDEX SIGN IS LEADING.
       01 G  USAGE COMP.
          05 GC PIC S9(4) SIGN IS LEADING.
       01 WN PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 30 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9(3) SIGN IS LEADING SEPARATE SOURCE WN.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
