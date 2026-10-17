*>--- conformance-case
*> id: pb504-value-implied-picture-below-2002
*> rules: [SR-13.16.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB504 - the INTRODUCTION edge of ISO 13.16.3 SR9, the VALUE-implied PICTURE clause.
      *> SR9 grants "The PICTURE clause may be omitted for an elementary item when an alphanumeric, boolean, or
      *> national literal that is not a zero-length literal is specified in the data-item format of the VALUE
      *> clause", implying 'PICTURE X(length)' / '1(length)' / 'N(length)'.  COBOL-85 made no such grant - a
      *> PICTURE was required for every elementary item bar an index data item and the subject of a RENAMES
      *> clause - so at --std 85 this program names the construct and the edition that introduces it
      *> (COBOLNET0900, the introduction band) rather than the generic SR8 rejection or, worse, compiling.
      *> The construct row is value-implied-picture-2002; SR9's boolean and national arms are gated a second
      *> time and independently by boolean-data-2002 / national-data-2002, the literal forms themselves being
      *> 2002 additions, which is why the witness here is the ALPHANUMERIC arm.
      *> At 2002 and later the identical source compiles and prints HELLO - tests/conformance/2002/
      *> pb504_value_implied_picture.cob is that positive witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB504LO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IMPLIED-A VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE AT 85 " IMPLIED-A
           STOP RUN.
