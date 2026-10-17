*>--- conformance-case
*> id: pb177-redefines-dynamic-length-target
*> rules: [SR-13.18.44.3-17]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR17, the DATA-NAME-2 arm of the same two-sided rule (kb/Work PB177 arm C). Here the
      *> redefined item is itself a DYNAMIC-LENGTH ELEMENTARY item, the second shape SR17 names. Two fixtures
      *> for two arms - the two-arm discipline applied to the fixture set, since a screen written for one side
      *> only is exactly this repo's most reproducible defect shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH.
       01 B REDEFINES D PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           STOP RUN.
