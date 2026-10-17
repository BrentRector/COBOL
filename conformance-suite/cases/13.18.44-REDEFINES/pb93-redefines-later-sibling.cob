*>--- conformance-case
*> id: pb93-redefines-later-sibling
*> rules: [SR-13.18.44.3-4, SR-13.18.44.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR10: "The entries giving the new descriptions of the storage area shall follow the entries
      *> defining the area of data-name-2" - a REDEFINES naming a LATER sibling is not a redefinition of a preceding
      *> area (it used to bind to the later entry silently — kb/Work PB93).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB93N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A REDEFINES B PIC X(2).
          05 B PIC X(2) VALUE "ab".
       PROCEDURE DIVISION.
           DISPLAY A.
           STOP RUN.
