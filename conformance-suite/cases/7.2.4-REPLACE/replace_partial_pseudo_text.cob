*>--- conformance-case
*> id: replace_partial_pseudo_text
*> rules: [GR-7.2.4.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R36 - the ISO partial-word replacement subset, pinned: 7.2.2's REPLACE with
      *> LEADING/TRAILING PSEUDO-TEXT operands - replace, the trailing form, and delete (empty
      *> pseudo-text-2, via a second REPLACE statement swapping the active set mid-source). The
      *> GCOS/ACU vendor spellings (literal partial-words, BY SPACES deletion) are a different
      *> construct: never legal ISO in any edition (see R36/R39 in kb/Work).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R36PSEUDO.
       REPLACE LEADING ==PRE-== BY ==NEW-==
               TRAILING ==-OLD== BY ==-NXT==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PRE-VAR1 PIC X(2) VALUE "AB".
       01 VAR2-OLD PIC X(2) VALUE "CD".
       REPLACE LEADING ==DEL-== BY ====.
       01 DEL-VAR3 PIC X(2) VALUE "EF".
       PROCEDURE DIVISION.
           DISPLAY NEW-VAR1
           DISPLAY VAR2-NXT
           DISPLAY VAR3.
           STOP RUN.
