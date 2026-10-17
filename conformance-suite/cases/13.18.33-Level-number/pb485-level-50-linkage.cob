      *>--- conformance-case
      *> id: pb485-level-50-linkage
      *> rules: [SR-13.18.33.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.33.3 SR5, the ORDINARY out-of-range case: 50 is one
      *> past the 1 through 49 hierarchy range of 13.18.33.1 and is not
      *> one of the special levels 66, 77 or 88. The linkage section is
      *> the third section SR5 names, and it is the section reached by
      *> BOTH linkage grammar arms (dataDescriptionEntry and the
      *> COBOL-2002 linkageProcedureParameter), so the screen must be
      *> keyed on the section rather than on one entry rule.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N2.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-REC PIC X(4).
       50  L-BAD PIC X(3).
       PROCEDURE DIVISION USING L-REC.
           DISPLAY L-REC
           STOP RUN.
