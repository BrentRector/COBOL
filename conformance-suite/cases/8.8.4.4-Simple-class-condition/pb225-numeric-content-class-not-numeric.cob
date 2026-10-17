*>--- conformance-case
*> id: pb225-numeric-content-class-not-numeric
*> rules: [SR-8.8.4.4.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.8.4.4.3 SR6: "If FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE, or NEAREST-TO-ZERO is
      *> specified, identifier-1 shall reference a data item whose category is numeric." The operand here is a
      *> REFERENCE-MODIFIED numeric item, and 8.4.3.3.4 GR6 c) makes that unique data item "class and category
      *> alphanumeric" - the OPERAND's category is asked, never its base item's. COBOLNET2216 (kb/Work PB225).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB225NOTNUMERIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9(4) VALUE 9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF N1 (1:2) IS FARTHEST-FROM-ZERO DISPLAY "FAR" END-IF
           STOP RUN.
