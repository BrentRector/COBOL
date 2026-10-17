*>--- conformance-case
*> id: pb485-level-77-renames
*> rules: [GR-13.18.33.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.4 GR2a: "Level-number 77 is assigned to identify
      *> noncontiguous working storage data items, noncontiguous local
      *> storage data items, and noncontiguous linkage data items, and may
      *> be used only as described by the data description format of the
      *> data description entry." A RENAMES body is 13.16.2 format 2, not
      *> the data description format, so 77 may not head it -- and GR2b
      *> says the same thing from the other side, since format 2 is
      *> written "66 data-name-1 RENAMES ...".
      *> The 77 spelling is the third arm of GR2 and gets its own witness
      *> because 77 is the one special level whose SECTION set (SR5)
      *> admits it in working-storage: nothing but the FORMAT axis rejects
      *> this program. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485ND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A PIC X(2) VALUE "AB".
           05  B PIC X(2) VALUE "CD".
       77  R RENAMES A THRU B.
       PROCEDURE DIVISION.
           DISPLAY G
           STOP RUN.
