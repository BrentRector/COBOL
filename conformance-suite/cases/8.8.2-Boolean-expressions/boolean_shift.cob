*>--- conformance-case
*> id: boolean_shift
*> rules: [GR-8.8.2-8, GR-14.9.8.4-3, FMT-14.9.8.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.2 rule 8 — boolean shift operators (COBOL-2023). Logical B-SHIFT-L/R fill boolean 0; circular
      *> B-SHIFT-LC/RC rotate. The result length = the first operand's length (rule 9). Annex A Table A.2 oracle
      *> (A = 1100): B-SHIFT-L 3 = 0000, B-SHIFT-R 3 = 0001, B-SHIFT-LC 3 = 0110, B-SHIFT-RC 3 = 1001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BSHIFT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 1(4) VALUE B"1100".
       01 R   PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A B-SHIFT-L 3.
           DISPLAY "SL3=" R.
           COMPUTE R = A B-SHIFT-R 3.
           DISPLAY "SR3=" R.
           COMPUTE R = A B-SHIFT-LC 3.
           DISPLAY "SLC=" R.
           COMPUTE R = A B-SHIFT-RC 3.
           DISPLAY "SRC=" R.
           COMPUTE R = A B-SHIFT-L 1.
           DISPLAY "SL1=" R.
           STOP RUN.
