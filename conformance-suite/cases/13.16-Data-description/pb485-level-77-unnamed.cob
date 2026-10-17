*>--- conformance-case
*> id: pb485-level-77-unnamed
*> rules: [SR-13.16.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR2 again, through SR4: "If no entry-name clause is
      *> specified, it is as though the filler format of the entry-name
      *> clause were specified." An OMITTED entry-name is therefore the
      *> filler format and fails SR2 exactly as the written FILLER does --
      *> the sibling spelling of the same rule, and the one a screen that
      *> only tested for the literal word FILLER would miss.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(3) VALUE "ABC".
       77  PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
