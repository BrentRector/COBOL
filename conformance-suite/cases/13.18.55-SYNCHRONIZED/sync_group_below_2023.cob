*>--- conformance-case
*> id: sync_group_below_2023
*> rules: [SR-13.18.55.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> SYNCHRONIZED on a GROUP item (ISO Annex E.3.2 item 6 / VCR row 43) is a COBOL-2023 introduction; below
      *> 2023 it is rejected strict with COBOLNET0900 (P3 step 10 — was silently accepted). Under --permissive it
      *> is accepted-inert (SYNC is a no-op) — a warning, not an error — preserving INV-1 continuity.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G SYNCHRONIZED.
         05 A PIC X.
         05 B PIC X.
       PROCEDURE DIVISION.
       M. STOP RUN.
