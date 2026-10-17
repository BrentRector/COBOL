*>--- conformance-case
*> id: pb966-add-corr-level66-alias
*> rules: [SR-14.9.2.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB966 - ADD CORRESPONDING REFUSES A LEVEL-66 OPERAND OF EITHER RENAMES FORM.
      *> 14.9.2.3 SR6: "Identifier-4 and identifier-5 shall be alphanumeric group items, national group
      *> items, variable-length groups, or strongly-typed group items and shall not be described with
      *> level-number 66." GALIAS renames the group SG WITHOUT the THROUGH phrase, so by 13.18.45.4 GR1
      *> it takes SG's attributes and storage - but it is still an entry described with level-number 66.
      *> The screen asked the item whose STORAGE the reference uses (SG) instead of the entry the NAME
      *> denotes (GALIAS), and this program compiled; the THROUGH form was already refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB966NAD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 SG.
             10 A PIC 9(3) VALUE 1.
             10 B PIC 9(3) VALUE 2.
       66 GALIAS RENAMES SG.
       01 DST.
          05 A PIC 9(3) VALUE 10.
          05 B PIC 9(3) VALUE 20.
       PROCEDURE DIVISION.
           ADD CORRESPONDING GALIAS TO DST
           DISPLAY DST
           STOP RUN.
