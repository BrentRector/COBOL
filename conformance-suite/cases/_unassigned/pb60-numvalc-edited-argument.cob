*>--- conformance-case
*> id: pb60-numvalc-edited-argument
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.68.3 r1: "Argument-1 shall be of CATEGORY alphanumeric or
      *> national." The 15.3 class screen admits the EDITED categories (class
      *> alphanumeric spans alphanumeric-edited and numeric-edited, Table 2)
      *> that r1's CATEGORY wording excludes - the finer screen is PB60 /
      *> AR-15.68.3-1's. A ref-mod view is plain category alphanumeric
      *> (8.4.3.3.4 GR6) and stays admissible.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60NEGED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NE PIC ZZ9.99 VALUE 123.45.
       01 R     PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C(WS-NE)
           STOP RUN.
