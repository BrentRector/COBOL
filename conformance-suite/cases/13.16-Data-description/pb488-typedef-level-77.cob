*>--- conformance-case
*> id: pb488-typedef-level-77
*> rules: [SR-13.16.3-15]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 15 - "The TYPEDEF clause may be specified only in a data description
*> entry whose level-number is 1 and for which the data-name format of the entry-name clause is specified."
*>
*> The LEVEL-NUMBER half, level-77 spelling: 77 is a level-number the entry may otherwise carry, and it is
*> not 1. TYPEDEF is a COBOL-2002 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-TYPEDEF-LEVEL-77.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 T IS TYPEDEF PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
