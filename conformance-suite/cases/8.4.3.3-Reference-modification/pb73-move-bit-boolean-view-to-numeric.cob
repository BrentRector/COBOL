*>--- conformance-case
*> id: pb73-move-bit-boolean-view-to-numeric
*> rules: [GR-8.4.3.3.4-1, SR-14.9.25.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.4 GR1/GR5a/GR6: a reference-modified USAGE BIT boolean item is a boolean unique data item (bit
      *> positions; "the same class, category, and usage" - GR2's alphanumeric redefinition applies only to a
      *> usage-DISPLAY item), and Table 16 makes Boolean -> Numeric "No". The display-form twin (PIC 1 without
      *> USAGE BIT) IS alphanumeric under GR2 and moves legally - golden pb73_table16_function_type_and_boolean_view.
      *> kb/Work PB73.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73BBN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB4 PIC 1(4) USAGE BIT VALUE B"1010".
       01 N9  PIC 9.
       PROCEDURE DIVISION.
           MOVE BB4(1:1) TO N9.
           STOP RUN.
