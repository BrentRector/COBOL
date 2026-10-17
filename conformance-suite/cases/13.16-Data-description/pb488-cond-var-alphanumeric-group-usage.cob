*>--- conformance-case
*> id: pb488-cond-var-alphanumeric-group-usage
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "A condition-name may be associated with any data description
*> entry that contains a level-number except the following: ... c) An alphanumeric group containing items
*> with a usage other than display."
*>
*> G is an alphanumeric group (no GROUP-USAGE clause, 13.18.29.4 GR3) and X is USAGE BINARY, so G has no
*> character image to compare the condition-name's literal against. kb/Work PB488 measured it compiling and
*> EVALUATING. COBOL-85 states the same exclusion ("a group containing items with descriptions including
*> JUSTIFIED, SYNCHRONIZED or USAGE (other than USAGE IS DISPLAY)"), so every edition rejects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-GROUP-USAGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
       88 G-COND VALUE "ABCDEF".
          05 X PIC 9(4) USAGE BINARY VALUE 1.
          05 Y PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
