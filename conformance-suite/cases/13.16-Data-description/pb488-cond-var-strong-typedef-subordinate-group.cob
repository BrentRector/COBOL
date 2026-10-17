*>--- conformance-case
*> id: pb488-cond-var-strong-typedef-subordinate-group
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 g)'s SECOND arm - "or a group item subordinate to such a type
*> declaration". G is a group inside a STRONG type declaration, so it may not be a conditional variable even
*> though the declaration root is not the subject. (An ELEMENTARY member of the same template may be one -
*> the letter says "a group item" - which the positive golden pb488_condition_name_associations_ok pins.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-STRONG-SUB-GROUP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF STRONG.
          05 G.
       88 G-COND VALUE "AB".
             10 F1 PIC X(2).
       01 V TYPE T.
       PROCEDURE DIVISION.
           MOVE "AB" TO F1 OF G OF V
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
