*>--- conformance-case
*> id: pb184-group-value-subordinate-value
*> rules: [SR-13.18.63.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR13, SENTENCE 2: "The VALUE clause shall not be specified at subordinate levels within
      *> this group."  13.18.63.4 GR5 initializes the group AREA "without consideration for the individual
      *> elementary or group items contained within this group", so a subordinate VALUE has no defined effect;
      *> the rule forbids writing one rather than leaving the two initializations to race.
      *> MEASURED BEFORE THIS SCREEN: this compiled clean, and B's own VALUE "ZZ" was silently discarded (GV
      *> displayed "ABCD") - the right ANSWER by GR5, from source the standard does not permit, with nothing
      *> said about it.  Landed with SR14's two conjuncts because all three restrict the same subject's
      *> subtree; one screen, so a fourth case is automatic rather than a fourth unscreened arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB184N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GV VALUE "ABCD".
          05 A PIC X(2).
          05 B PIC X(2) VALUE "ZZ".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY GV
           STOP RUN.
