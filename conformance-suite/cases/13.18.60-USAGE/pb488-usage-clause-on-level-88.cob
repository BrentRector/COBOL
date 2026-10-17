*>--- conformance-case
*> id: pb488-usage-clause-on-level-88
*> rules: [SR-13.18.60.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60.3 syntax rule 1 - "The USAGE clause shall not be specified in a data description
*> entry that has a level-number of 66 or 88."
*>
*> The LEVEL-88 half. It is refused by the level-number domain screen: 13.16.2 formats 3 and 4 are the only
*> shapes level-number 88 may take and both are written `88 [condition-name] value-clause .`, so an entry
*> whose body is not a value clause alone is a format-1 entry and 13.18.33.4 GR2c then restricts its
*> level-number to 77 or 1 through 49. kb/Work PB488 adjudicated SR1 as PARTIAL against a tree where this
*> program compiled AND RAN with the clause inert; the level-number domain landed with PB485 and closed it.
*> This golden is the witness that makes the rule's enforcement observable rather than incidental.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-USAGE-ON-88.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "ABC".
       88 C VALUE "ABC" USAGE DISPLAY.
       PROCEDURE DIVISION.
           IF C DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
