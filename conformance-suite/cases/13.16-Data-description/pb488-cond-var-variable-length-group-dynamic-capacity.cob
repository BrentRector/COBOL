*>--- conformance-case
*> id: pb488-cond-var-variable-length-group-dynamic-capacity
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "... h) A variable-length group."
*>
*> 8.5.1.12.1 defines the shape: "A variable-length group is a group item whose data description has at
*> least one dynamic-length elementary item or dynamic-capacity table as a subordinate item." G holds a
*> dynamic-capacity table, so it has no fixed record window to compare a condition-name's literal against -
*> kb/Work PB488 measured it compiling and then crashing at run time in the group image.
*> OCCURS DYNAMIC is a COBOL-2014 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-VLG-DYNCAP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
       88 G-COND VALUE "AB".
          05 E PIC X(2) OCCURS DYNAMIC CAPACITY IN N FROM 1 TO 3.
       PROCEDURE DIVISION.
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
