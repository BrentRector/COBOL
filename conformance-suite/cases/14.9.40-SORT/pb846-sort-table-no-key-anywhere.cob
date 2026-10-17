*>--- conformance-case
*> id: pb846-sort-table-no-key-anywhere
*> rules: [SR-14.9.40.3-15]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.40.3 SR15 - "The KEY phrase may be omitted only if the description of the table
      *> referenced by data-name-2 contains a KEY phrase." TE's OCCURS clause has no KEY phrase and the SORT
      *> statement omits its own, so neither source can supply GR21's sequence. kb/Work PB846: the omission is
      *> a named syntax-rule diagnostic, not the parse error it was while the grammar required the phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB846N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE OCCURS 5 INDEXED BY IX.
             10 TK PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE
           STOP RUN.
