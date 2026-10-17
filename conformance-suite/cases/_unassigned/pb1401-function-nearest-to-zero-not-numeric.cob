*>--- conformance-case
*> id: pb1401-function-nearest-to-zero-not-numeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.8.4.4.3 SR6: "If FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE, or NEAREST-TO-ZERO is
      *> specified, identifier-1 shall reference a data item whose category is numeric." The operand here
      *> is an ALPHANUMERIC function (15.2 item 1 - "of the class and category alphanumeric"). The screen
      *> used to classify a data-item reference only, so a function-identifier failed open and reached a
      *> renderer with no numeric description to test (kb/Work PB1401). COBOLNET2216.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1401NEGSR6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION UPPER-CASE(A) IS NEAREST-TO-ZERO DISPLAY "NEAR" END-IF
           STOP RUN.
