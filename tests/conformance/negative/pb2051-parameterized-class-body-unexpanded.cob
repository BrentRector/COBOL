      *> reject-at: 2002 2014 2023
      *> kb/Work PB2051 - 13.16.3 SR16 (cite.py OK): "The BASED clause may be specified only in data
      *> description entries in the linkage section, in the working-storage section, and in the local-storage
      *> section. The level number of such data description entries shall be 1 or 77." B is a level-05 entry of
      *> the OBJECT paragraph's working-storage, in a PARAMETERIZED class that nothing expands. 9.3.12 makes the
      *> definition a skeleton, not a class, but it is a class definition all the same, and this syntax rule
      *> governs its text whether or not an expansion is ever created (before PB2051 it compiled clean).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2051CSK INHERITS FROM BASE USING ELT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELT.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE ELT.
       01 G.
          05 B BASED PIC X.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB2051CSK.
