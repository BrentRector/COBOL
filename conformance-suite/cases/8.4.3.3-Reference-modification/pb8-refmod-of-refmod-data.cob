*>--- conformance-case
*> id: pb8-refmod-of-refmod-data
*> rules: [SR-8.4.3.3.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 SR3 on a DATA REFERENCE - the sibling PB8's sweep found (CLAUDE.md rule 4). This
      *> COMPILED CLEAN and silently returned A(2:2): dataReferenceSuffix* admits unlimited ref-mods and the
      *> resolver kept only the first of each carrier, with the DEFAULT-mode form outranking the SUBSCRIPT-mode
      *> one, so the SECOND modifier won. Neither the composition nor the rejection the standard requires.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB8NEGSR3D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(10) VALUE "ABCDEFGHIJ".
       01 T PIC X(2).
       PROCEDURE DIVISION.
           MOVE A (3:4)(2:2) TO T
           STOP RUN.
