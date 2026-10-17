*>--- conformance-case
*> id: pb2548_template_counter_per_use_site
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2548 (train 1055 review) - a TYPEDEF template's OCCURS
      *> DEPENDING ON counter that the declaration does not define is
      *> found where each use site PLACES the type: ISO 13.18.57.4 GR1
      *> ("as though the data description identified by type-name-1 had
      *> been coded in place") and ISO 8.4.2.2.1 rule 5 (the groups
      *> superordinate to the subject are implicit qualifiers). Two use
      *> sites, G1 and G2, each supply their own sibling CNT, so the
      *> program's two CNTs are not an ambiguity of the declaration.
      *> V holds CNT OF G1 = 3 occurrences ("ABC"); W holds 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548PU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 E PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 G1.
          05 CNT PIC 99 VALUE 3.
          05 V TYPE T.
       01 G2.
          05 CNT PIC 99 VALUE 2.
          05 W TYPE T.
       PROCEDURE DIVISION.
           MOVE "ABCDE" TO V
           MOVE "XYZ" TO W
           DISPLAY CNT OF G1 " " CNT OF G2 " [" V "] [" W "]"
           STOP RUN.
