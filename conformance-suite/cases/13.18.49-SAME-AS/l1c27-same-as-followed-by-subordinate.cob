      *>--- conformance-case
      *> id: l1c27-same-as-followed-by-subordinate
      *> rules: [SR-13.18.49.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.49.3 2) — a SAME AS entry followed by a subordinate
      *> entry
      *> "A data description entry that specifies the SAME AS clause
      *> shall not be immediately followed by a subordinate data
      *> description entry or level 88 entry."
      *> cite.py --check 13.18.49.3 "A data description entry that
      *>   specifies the SAME AS clause shall not be immediately
      *>   followed by a subordinate data description entry or level 88
      *>   entry"
      *>   -> OK §13.18.49.3 2)
      *> THE SUBORDINATE-ENTRY ARM. E is a level 1 group (SR7 holds) and
      *> W is level 1 in working-storage; only the level-05 entry under
      *> W is wrong (removing it compiles clean). The level-88 arm is
      *> the twin l1c27-same-as-followed-by-88. SAME AS is new in COBOL
      *> 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E.
          05 E1 PIC X.
       01 W SAME AS E.
          05 W1 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
