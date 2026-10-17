*>--- conformance-case
*> id: pb770-alphabet-multichar-thru-operand
*> rules: [SR-12.3.7.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR14 b3: "Each alphanumeric literal, when a THROUGH or ALSO phrase is specified, shall be
      *> one character in length." Before kb/Work PB770 the alphanumeric arm GUARDED the range expansion with a
      *> length test and then `continue`d, so the whole entry vanished from the table with NO diagnostic - a
      *> silently different collating sequence, where the national twin already raised its own error.
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETMULTICH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALF IS "AB" THRU "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
