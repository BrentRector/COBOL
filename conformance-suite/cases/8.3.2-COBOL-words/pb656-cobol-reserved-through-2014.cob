      *>--- conformance-case
      *> id: pb656-cobol-reserved-through-2014
      *> rules: [SR-8.3.2.1-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB656 - COBOL is a compiler-directive word at 2023
      *>   (8.12) and NOT in the 2023 8.9 list, so 8.3.2.1 2) lets it be
      *>   a user-defined word there (conformance:2023/pb656_directive_
      *>   words_as_user_words). Through 2014 it is a RESERVED word: the
      *>   2002 and 2014 texts are not in the repository, and CLAUDE.md
      *>   rule 1's precedence takes GnuCOBOL's per-standard word lists,
      *>   which reserve it at 85, 2002 and 2014 (cobc -std=cobol2014
      *>   refuses 01 COBOL too). 8.3.2.1 2)'s "except those that are
      *>   also reserved words" is exactly this edge: the reserved word
      *>   is refused as a data-name (COBOLNET0901). CONFORMANCE D-RW3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB656CBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COBOL PIC X(3) VALUE "CBL".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY COBOL
           STOP RUN.
