*>--- conformance-case
*> id: qualified-ambiguous-match
*> rules: [SR-8.4.2.2.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R31's other arm: TWO declarations of Y each sit under an X, so Y IN X matches
      *> twice and no qualifier set written here establishes uniqueness (ISO 8.4.2.2 -
      *> "uniqueness shall be established through qualification"). COBOLNET1639's
      *> multiple-match message names the count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R31NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
         02 X.
           03 Y PIC X VALUE "A".
       01 G2.
         02 X.
           03 Y PIC X VALUE "B".
       PROCEDURE DIVISION.
           DISPLAY Y IN X.
           STOP RUN.
