*>--- conformance-case
*> id: pb94-value-alphanumeric-on-numeric
*> rules: [SR-13.18.63.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR2: "If the category of the subject of the entry is numeric, all literals in the VALUE
      *> clause shall be numeric" - an alphanumeric literal on a numeric item (a digits-only one is the
      *> --permissive vendor leniency, stored as the number with a warning). kb/Work PB94.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB94N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 99 VALUE "7".
       PROCEDURE DIVISION.
           DISPLAY W.
           STOP RUN.
