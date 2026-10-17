*>--- conformance-case
*> id: pb177-redefines-vl-group-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR17: "Neither data-name-2 nor the subject of the entry shall be a variable-length
      *> group or a dynamic-length elementary item." B is a variable-length group by 8.5.1.12.1 - "a group item
      *> whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a
      *> subordinate item". kb/Work PB177 arm C: the SUBJECT arm of the two-sided rule.
      *> BEFORE THE SCREEN this was not merely under-rejected but SILENTLY MIS-MODELLED - StorageFormPass
      *> classifies IsDynamicLength BEFORE its Tier-B view arm, so B/D got its OWN disjoint native string:
      *> MOVE "ZZ" TO D left A unchanged, two storages for the one area 13.18.44.4 GR1 defines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(8).
       01 B REDEFINES A.
          05 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           STOP RUN.
