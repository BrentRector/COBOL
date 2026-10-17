*>--- conformance-case
*> id: pb589-bool-refmod-width-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB589's positive shape (tests/conformance/2002/pb589_bool_refmod_width) below its introducing
      *> edition: the boolean operators and category-boolean items are COBOL-2002 introductions (ISO 8.7.2 /
      *> 8.8.2), so a run-time-length boolean slice combined by B-OR is rejected at 85 with the edition-band code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB589NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 PIC 1(8) USAGE BIT VALUE B"00000000".
       01 R8 PIC 1(8) USAGE BIT.
       01 N  PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R8 = B8(1:N) B-OR B"1111111"
           STOP RUN.
