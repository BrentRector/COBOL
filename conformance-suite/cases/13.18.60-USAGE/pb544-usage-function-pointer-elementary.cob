*>--- conformance-case
*> id: pb544-usage-function-pointer-elementary
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB544 — §13.18.60.3 syntax rule 14's SECOND unverifiable arm, now reachable. The rule's arm B
      *> is the ELEMENTARY one: an item of one of SR14's five phrase classes at a level other than 1, not
      *> subordinate to a STRONG type declaration. Its FUNCTION-POINTER leg could not fire at all while
      *> `PictureAnalyzer.ParseUsage` staged the usage loud and left `Pic` null, so `Sr14PlacementClass` never
      *> saw the item and `05 F USAGE FUNCTION-POINTER TO proto.` was refused by the staging band rather than
      *> by SR14. USAGE FUNCTION-POINTER went LIVE with kb/Work PB452 + PB817, so the arm now reports the rule
      *> (COBOLNET1724) and the row's second residue has a witness.
      *> Pinned at 2014 and 2023: FUNCTION-POINTER is dated 2014 by its §8.9 reservation
      *> (constructs.json usage-function-pointer-2014), so below 2014 the entry names its edition instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB544FPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB544PRO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  F USAGE FUNCTION-POINTER TO PB544PRO.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
