*>--- conformance-case
*> id: pb73-move-alphabetic-view-to-boolean
*> rules: [GR-8.4.3.3.4-2, GR-8.4.3.3.4-6, SR-14.9.25.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.4 GR6: a reference-modified ALPHABETIC item keeps class and category alphabetic (the exception
      *> list names alphanumeric-edited, national-edited, numeric and numeric-edited - not alphabetic; GR2's
      *> alphanumeric redefinition governs the ref-mod OPERATION), so Table 16's Alphabetic -> Boolean "No" refuses
      *> it exactly as it refuses the unsliced item. This cell was pinned ADMITTED by golden pb72_table16_admitted_cells
      *> (RA) from 2026-08-09 to 2026-08-18; kb/Work PB73 re-adjudicated it. --permissive keeps the GnuCOBOL reading
      *> (any slice is alphanumeric) with a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73AVB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A  PIC A(4)  VALUE "ABCD".
       01 WS-B  PIC 1(2).
       PROCEDURE DIVISION.
           MOVE WS-A(1:2) TO WS-B.
           STOP RUN.
