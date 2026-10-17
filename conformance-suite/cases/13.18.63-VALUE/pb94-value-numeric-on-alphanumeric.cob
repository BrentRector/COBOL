*>--- conformance-case
*> id: pb94-value-numeric-on-alphanumeric
*> rules: [SR-13.18.63.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR4: "If the item is of category alphabetic, alphanumeric, or alphanumeric-edited literals in
      *> the VALUE clause shall be alphanumeric literals" - a numeric literal on a PIC X item (the --permissive vendor
      *> leniency stores its characters as MOVE would, with a warning). kb/Work PB94.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB94N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(2) VALUE 12.
       PROCEDURE DIVISION.
           DISPLAY X.
           STOP RUN.
