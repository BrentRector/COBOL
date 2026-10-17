*>--- conformance-case
*> id: pb488-cond-var-group-synchronized-self
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 d) - "A group containing items described with a JUSTIFIED or
*> SYNCHRONIZED clause."
*>
*> The SYNCHRONIZED half written on the GROUP ITSELF. 13.18.55.3 SR1 permits it there ("The SYNCHRONIZED
*> clause may be specified for group and elementary items") and 13.18.55.4 GR1 makes that a description of
*> the members: "When this clause is specified for a group item, it is treated as though it had instead been
*> separately specified for each of the subordinate elementary items for which this clause is permitted." So
*> G contains items described with a SYNCHRONIZED clause and d) excludes it. The binder records the clause on
*> the entry that wrote it rather than propagating it, so a screen reading only the members would miss this
*> spelling entirely. A group-level SYNCHRONIZED is a COBOL-2023 addition, so only 2023 reaches d) here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-GROUP-SYNC-SELF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G SYNCHRONIZED.
       88 G-COND VALUE "ABCD".
          05 X PIC X(2) VALUE "AB".
          05 Y PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           IF G-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
