*>--- conformance-case
*> id: pb488-cond-var-variable-length-group-dynamic-length
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 h) with 8.5.1.12.1 - the DYNAMIC LENGTH half of "a variable-length
*> group is a group item whose data description has at least one dynamic-length elementary item or
*> dynamic-capacity table as a subordinate item". The DYNAMIC LENGTH clause is a COBOL-2023 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-VLG-DYNLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
       88 G-COND VALUE "AB".
          05 D PIC X DYNAMIC LENGTH LIMIT IS 5.
       PROCEDURE DIVISION.
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
