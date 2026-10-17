*>--- conformance-case
*> id: pb546-national-boolean-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB546 - a boolean item of usage national (ISO 13.18.60.3 SR12) is a
      *> COBOL-2002 addition with the boolean data category; below 2002 the PICTURE
      *> symbol 1 is refused by the edition gate (COBOLNET0900), so the unit reader's
      *> boolean arm exists only from 2002 (positive golden
      *> 2002/pb546_national_leaf_one_width).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB546N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BN PIC 1(4) USAGE NATIONAL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY BN(1:2)
           STOP RUN.
