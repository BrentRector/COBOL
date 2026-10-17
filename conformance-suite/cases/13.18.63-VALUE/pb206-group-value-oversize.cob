*>--- conformance-case
*> id: pb206-group-value-oversize
*> rules: [SR-13.18.63.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR4, sentence 3: "Alphanumeric literals in the VALUE clause of an alphanumeric group
      *> item shall not exceed the size of the group item."  GZ is an alphanumeric group item (13.18.29.4 GR3;
      *> 8.5.2.1) of FOUR character positions - O1's two plus O2's two - and "ABCDEF" is six.
      *> MEASURED BEFORE THIS SCREEN (kb/Work PB206, on 1d949007): this program compiled CLEAN and `DISPLAY GZ`
      *> printed ABCD - the literal SILENTLY TRUNCATED, with no diagnostic.  The rule is a "shall", so the only
      *> conforming response is a compile-time rejection; a truncating store is the answer to a question the
      *> standard does not ask.  Not dialect-gated: SR4 carries this sentence at every edition, and Annex E.2
      *> item 27's COBOL-2023 change is scoped to NUMERIC-EDITED items (COBOLNET1570), not to this one.
      *> The conforming boundary - a literal of EXACTLY the group's size, and the `ALL "literal"` form that
      *> 8.3.3.6.4 GR2 repeats and truncates BY RULE - is pinned by 85/pb206_value_size_alphanumeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GZ VALUE "ABCDEF".
          05 O1 PIC X(2).
          05 O2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY O1
           STOP RUN.
