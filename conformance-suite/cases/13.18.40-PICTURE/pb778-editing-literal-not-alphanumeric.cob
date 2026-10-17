*>--- conformance-case
*> id: pb778-editing-literal-not-alphanumeric
*> rules: [SR-13.18.40.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR9: "... Otherwise, literal-1, literal-2, and
      *> literal-3 shall be alphanumeric literals."  The literal-position
      *> operand may be written as a constant-name (13.10.3 SR2), but the
      *> literal KN stands for (13.10.4 GR1) is the NUMERIC literal 7, and
      *> a numeric literal is not an alphanumeric literal: COBOLNET1955.
      *> Before kb/Work PB778 a numeric literal-1 was inserted as its
      *> source text with no diagnostic, and the constant-name spelling
      *> did not parse at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB778NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KN CONSTANT AS 7.
       01 E1 PIC 99T99 EDITING T IS KN.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
