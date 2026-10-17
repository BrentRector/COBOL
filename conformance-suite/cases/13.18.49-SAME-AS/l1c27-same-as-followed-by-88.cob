      *>--- conformance-case
      *> id: l1c27-same-as-followed-by-88
      *> rules: [SR-13.18.49.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.49.3 2) — a SAME AS entry followed by a level 88
      *> entry
      *> "A data description entry that specifies the SAME AS clause
      *> shall not be immediately followed by a subordinate data
      *> description entry or level 88 entry."
      *> cite.py --check 13.18.49.3 "A data description entry that
      *>   specifies the SAME AS clause shall not be immediately
      *>   followed by a subordinate data description entry or level 88
      *>   entry"
      *>   -> OK §13.18.49.3 2)
      *> THE LEVEL-88 ARM. E is elementary (SR7 and SR8 hold) and W is
      *> level 1 in working-storage; only the 88 entry under W is wrong
      *> (removing it compiles clean). The subordinate-entry arm is the
      *> twin l1c27-same-as-followed-by-subordinate. SAME AS is new in
      *> COBOL 2002 (the standard's new-features index lists it;
      *> negative/same-as-at-85 pins the 1985 refusal).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC 9 VALUE 1.
       01 W SAME AS E.
          88 W-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
