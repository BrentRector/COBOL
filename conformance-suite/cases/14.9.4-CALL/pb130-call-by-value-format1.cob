*>--- conformance-case
*> id: pb130-call-by-value-format1
*> rules: [GR-14.9.4.4-5, FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 1's USING brace prints BY REFERENCE and BY CONTENT only (the repaired figure
      *> notes' required-word list has no VALUE; SR21-SR23 sit under Format 2). The old binder accepted
      *> BY VALUE without the AS phrase and passed a GR5-impossible mode (kb/Work PB130).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB130NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           CALL "X" USING BY VALUE N
           STOP RUN.
