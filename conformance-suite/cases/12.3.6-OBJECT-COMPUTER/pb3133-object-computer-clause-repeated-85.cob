      *>--- conformance-case
      *> id: pb3133-object-computer-clause-repeated-85
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3133 - the COBOL-85 OBJECT-COMPUTER paragraph prints
      *>   `[ MEMORY SIZE integer-1 { WORDS | CHARACTERS | MODULES } ]`
      *>   in its own bracket with no ellipsis, so the clause may be
      *>   written at most once (ISO 5.2.6.2 and 5.2.7 state the bracket
      *>   and ellipsis conventions). DERIVED, NOT QUOTED: no 1985 text
      *>   is in the repository (docs/VERSION_CHANGE_REFERENCE.md row
      *>   7.7). A second MEMORY SIZE clause is COBOLNET2423; it
      *>   compiled clean at --std 85 before.
      *> cite.py --check 5.2.7 "the ellipsis represents the position at
      *>   which the user elects repetition of a portion of a format"
      *>   -> OK  5.2.7
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3133R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ MEMORY SIZE 10 WORDS
           MEMORY SIZE 20 WORDS.
       PROCEDURE DIVISION.
           STOP RUN.
