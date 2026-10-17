*>--- conformance-case
*> id: pb531-picture-item-too-large
*> rules: [SR-13.18.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> AN IMPLEMENTOR-DEFINED LIMIT, refused BY NAME.  The standard bounds a picture character-string two
      *> ways and neither bounds its EXPANSION: 13.18.40.3 SR4 bounds the 63 characters it is WRITTEN in,
      *> and SR14 bounds a numeric or fixed-point numeric-edited item to 1 through 31 DIGIT positions - an
      *> alphanumeric character-string has no such cap, and Annex A.1 carries no implementor-defined item
      *> for the maximum size of a data item.  COBOL.NET fixes the maximum at 134217728 (2^27) character
      *> positions.  MEASURED BEFORE the fix: an OutOfMemoryException out of StringBuilder, with no
      *> diagnostic and no source location (kb/Work PB531).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB531PICTUREITEMTOOLARGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1 PIC X(2000000000).
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
