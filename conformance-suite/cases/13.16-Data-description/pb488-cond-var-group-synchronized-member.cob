*>--- conformance-case
*> id: pb488-cond-var-group-synchronized-member
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 d) - "A group containing items described with a JUSTIFIED or
*> SYNCHRONIZED clause."
*>
*> The SYNCHRONIZED half, written on the MEMBER. 13.18.55.4 GR2 lets a synchronized item leave unused bytes
*> between natural boundaries which "are included in the size of any alphanumeric group item ... that
*> contains the subject of the entry", so the containing group's image is implementor-positioned and the
*> rule excludes it from being a conditional variable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-GROUP-SYNC-MEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
       88 G-COND VALUE "ABCD".
          05 X PIC 9(4) SYNCHRONIZED.
          05 Y PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
