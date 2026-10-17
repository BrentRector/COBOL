      *>--- conformance-case
      *> id: pb228-add-standard-decimal-below-2014
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.2.4 GR1-GR3 second paragraphs exist only when
      *> standard-decimal arithmetic is in effect, and STANDARD-DECIMAL
      *> is a COBOL-2014 keyword of the ARITHMETIC clause: below 2014
      *> the clause is refused with 0900 (and at 85 the OPTIONS
      *> paragraph's 0804 fires beside it). The positive companion is
      *> 2014/pb228_add_standard_decimal (kb/Work PB228).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB228SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC S9(3) VALUE 100.
       01 Z  PIC SV9(4) VALUE 0.0004.
       01 R1 PIC 9(3)V9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD X Z TO R1
           DISPLAY R1
           STOP RUN.
