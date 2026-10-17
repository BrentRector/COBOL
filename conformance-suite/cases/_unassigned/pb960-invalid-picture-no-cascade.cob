*>--- conformance-case
*> id: pb960-invalid-picture-no-cascade
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB960 - AN INVALID PICTURE IS DIAGNOSED AT ITS DECLARATION, AND ONLY THERE.
      *> 13.18.40.3 SR12 b): "Each of the symbols from the set 'CR', 'DB', 'E', 'S', 'V' '.' may appear
      *> only once in character-string-1" - so PIC 9V9V9 is rejected (COBOLNET1934). The item's category
      *> is then UNKNOWN, not alphanumeric: the binder's recovery profile is a storage placeholder. The
      *> ADD below used to draw a second, false error - "item 'W-BAD' of category alphanumeric is not a
      *> numeric operand" (8.8.1.1) - about a numeric picture the user wrote. The corpus asserts the true
      *> diagnostic; RecoveryCategoryCascadeTests asserts the false one is ABSENT in every operand position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB960NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAD PIC 9V9V9.
       01 W-N   PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD W-BAD TO W-N
           STOP RUN.
