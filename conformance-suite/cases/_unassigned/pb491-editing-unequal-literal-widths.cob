*>--- conformance-case
*> id: pb491-editing-unequal-literal-widths
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR12 a) - "Literal-2 and literal-3 shall occupy the same number of character
      *> positions."  The rule matters precisely because the literals are now RENDERED at their own width
      *> (13.18.40.4 GR14 'es'; kb/Work PB491): two different widths would give one data item two different
      *> sizes depending on the sign of the value stored in it.  reject-at names 2023 only because the
      *> EDITING phrase is itself a COBOL-2023 introduction.
      *> character-1 is written BARE, as the general format writes it (kb/Work PB568).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB491EDITINGUNEQUALLITERALWI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1 PIC L999.99 EDITING L FOR NEGATIVE IS "DEBIT " POSITIVE IS "CR".
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
