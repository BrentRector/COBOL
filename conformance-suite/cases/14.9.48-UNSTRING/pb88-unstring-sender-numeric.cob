*>--- conformance-case
*> id: pb88-unstring-sender-numeric
*> rules: [SR-14.9.48.3-2, SR-14.9.48.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2: identifier-1 (the sender) "shall reference data items of category alphanumeric or
      *> national" - a numeric item, usage DISPLAY or not, is neither. kb/Work PB88: COBOLNET1651 at bind (the
      *> program compiled and died at the UNSTRING before).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NUNSSND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(6) VALUE 123456.
       01 R PIC X(3).
       PROCEDURE DIVISION.
           UNSTRING N DELIMITED BY "3" INTO R.
           STOP RUN.
