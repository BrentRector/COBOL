*>--- conformance-case
*> id: pb73-move-alphabetic-view-to-numeric
*> rules: [SR-14.9.25.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.4 GR6 + 14.9.25.3 Table 16: an ALPHABETIC item's slice is alphabetic, and Alphabetic -> Numeric
      *> is "No" (as for the unsliced item). kb/Work PB73 (2026-08-18); --permissive warns and admits (GnuCOBOL's
      *> any-slice-is-alphanumeric reading).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73AVN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A  PIC A(4)  VALUE "ABCD".
       01 N9    PIC 9(2).
       PROCEDURE DIVISION.
           MOVE WS-A(1:2) TO N9.
           STOP RUN.
