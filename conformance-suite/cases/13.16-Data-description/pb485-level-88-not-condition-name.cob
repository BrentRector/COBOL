*>--- conformance-case
*> id: pb485-level-88-not-condition-name
*> rules: [SR-13.16.3-1, GR-13.16.4-3, GR-13.18.33.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.4 GR2c: "Level-number 88 may be used only as
      *> described by the condition-name format or the validation format
      *> of the data description entry." 13.16.2 gives both formats the
      *> same shape -- "88 [condition-name] value-clause ." -- so a body
      *> carrying a PICTURE is neither, and the entry is a format-1 entry
      *> at level 88, which 13.16.3 SR1 forbids. The test is "every clause
      *> is a value clause", NOT "the level is 88": deciding the format
      *> from the level-number would make GR2c vacuous. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(3) VALUE "ABC".
       88  BAD PIC 9.
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
