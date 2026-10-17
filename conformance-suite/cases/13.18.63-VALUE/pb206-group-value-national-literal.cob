*>--- conformance-case
*> id: pb206-group-value-national-literal
*> rules: [SR-13.18.63.3-5, SR-13.18.63.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR13, sentence 1: a group-level VALUE's "literal-1 shall be of the same category as the
      *> group item or shall be a figurative constant that is permitted in a MOVE statement to a receiving item
      *> of that category."  GE is an alphanumeric group item (13.18.29.4 GR3; 8.5.2.1 - "class and category
      *> alphanumeric") and N"AB" is a NATIONAL literal (8.3.3.5), a different category, and not a figurative
      *> constant.  SR4's first sentence agrees for the same subject - an alphanumeric item's VALUE literals
      *> "shall be alphanumeric literals".
      *> MEASURED BEFORE THIS SCREEN (kb/Work PB206, on 1d949007): compiled CLEAN, E2 = AB - the national
      *> literal's characters deposited into an alphanumeric area with no conversion and no diagnostic, which
      *> is exactly the crossing 14.9.25.3 Table 16 refuses in a MOVE ("national" row, "alphanumeric" column).
      *> The whole category axis was unscreened for group subjects: DataBinder's elementary funnel is guarded
      *> `pic is not null` and a group has no PICTURE, so no group VALUE ever reached it.
      *> National literals are COBOL-2002 (national-data-2002), hence the reject-at band.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GE VALUE N"AB".
          05 E2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY E2
           STOP RUN.
