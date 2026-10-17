*>--- conformance-case
*> id: pb485-renames-not-level-66
*> rules: [GR-13.18.33.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.2 format 2 is written "66 data-name-1 RENAMES ...",
      *> and 13.18.33.4 GR2b assigns level-number 66 to identify RENAMES
      *> entries. A RENAMES body under any other level-number is neither
      *> format 2 nor format 1.
      *> This is the case that reached the EMITTER: before the screen it
      *> bound as an ordinary subordinate item and generated uncompilable
      *> C# ("_T_0 does not contain a definition for 'R'", CS1061) -- a
      *> backend crash rather than any diagnostic, which is the same shape
      *> the level-78 hole took before PB201 unmasked it. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A PIC X(2) VALUE "AB".
           05  B PIC X(2) VALUE "CD".
       05  R RENAMES A THRU B.
       PROCEDURE DIVISION.
           DISPLAY R
           STOP RUN.
