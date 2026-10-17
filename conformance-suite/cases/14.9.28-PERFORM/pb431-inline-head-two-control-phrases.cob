*>--- conformance-case
*> id: pb431-inline-head-two-control-phrases
*> rules: [FMT-14.9.28.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB431 — ISO §14.9.28.2 Format 2 prints ONE pair of square brackets over three STACKED
      *> alternatives: PERFORM [ times-phrase | until-phrase | varying-phrase ] imperative-statement-1
      *> END-PERFORM. At most one loop-control phrase may be written (rendered from the printed page 682 /
      *> PDF 712). This program writes two, and used to COMPILE AND RUN with the UNTIL silently deleted from
      *> the bound tree — X ended at 0003, and no diagnostic was issued at any --std.
      *> Refused at every edition: the format did not change shape across 1985/2002/2014/2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB431NEGTWO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM 3 TIMES UNTIL X > 100
               ADD 1 TO X
           END-PERFORM
           STOP RUN.
