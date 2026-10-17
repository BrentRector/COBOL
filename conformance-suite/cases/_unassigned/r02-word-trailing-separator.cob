*>--- conformance-case
*> id: r02-word-trailing-separator
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1: "The hyphen or underscore shall not appear as the first or
      *> last character in such words." `1A-` ends on a hyphen.
      *>
      *> This was ACCEPTED before fix-queue R02, and the hyphen half predates the
      *> underscore work entirely: "nor last" was enforced only in the ALPHA-start
      *> lexer alternative, while the two DIGIT-start alternatives ended in a `*`
      *> over the separator class. Adding the underscore to that class without
      *> closing the hole would have inherited it for the new character too, so
      *> the tail was factored into NAME_TAIL - which cannot end on a separator -
      *> and all three alternatives now share it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R02TRAILSEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 1A- PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY 1A-.
           STOP RUN.
