*>--- conformance-case
*> id: pb73-move-display-boolean-view-to-numeric
*> rules: [GR-8.4.3.3.4-1, GR-8.4.3.3.4-6, SR-14.9.25.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.4 GR6: a reference-modified DISPLAY-form boolean item (PIC 1 without USAGE BIT) is a BOOLEAN
      *> unique data item - GR6's exception list (edited, numeric) is exhaustive and GR2's "as if redefined as
      *> alphanumeric" governs the operation's positions, not the result's category (kb/Work PB73, adjudicated
      *> 2026-08-18) - and Table 16 makes Boolean -> Numeric "No". The BIT-form twin is the same cell
      *> (pb73-move-bit-boolean-view-to-numeric); the legal cells are golden pb73_table16_function_type_and_boolean_view.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73DBN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B4  PIC 1(4) VALUE B"1010".
       01 N9  PIC 9.
       PROCEDURE DIVISION.
           MOVE B4(1:1) TO N9.
           STOP RUN.
