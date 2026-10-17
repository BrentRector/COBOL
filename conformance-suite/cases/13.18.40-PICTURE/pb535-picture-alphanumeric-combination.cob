*>--- conformance-case
*> id: pb535-picture-alphanumeric-combination
*> rules: [GR-13.18.40.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB535 - AN 'A'/'X' CHARACTER-STRING MIXED WITH SYMBOLS NO CATEGORY RULE ADMITS BESIDE THEM.
      *> ISO 1989:2023 13.18.40.4 GR6 defines category alphanumeric from "a combination of symbols from the
      *> set 'A', 'X', and '9'" and GR7 adds to that alphabet only character-1 and the symbols 'B', '0', '/';
      *> no general rule defines a category for a string that mixes 'A' or 'X' with a comma, a zero
      *> suppression symbol or an editing sign control symbol. 13.18.40.3 SR2's second obligation is what
      *> rejects them - "the allowable combinations of symbols for a PICTURE clause are specified in
      *> 13.18.40.6, Precedence rules" - so each is COBOLNET1935, naming the ordered pair whose Table 10 cell
      *> is blank.
      *>
      *> Before anything read symbol combination these bound category ALPHANUMERIC and their length came from
      *> a whitelist of the symbols that arm expected, so the offending symbols vanished from the size: the
      *> item was SHORTER than its picture and shifted every following member of a group image. 13.18.40.4
      *> GR4 with GR14 would make the sizes 5, 4, 4 and 5 - never the 4, 2, 2 and 3 observed.
      *>
      *> AC1  XX,XX - Table 10 row ',' has a blank 'A X' column: no 'A' or 'X' may precede a comma.
      *> AC2  AAZZ  - row 'Z *' left of the decimal point has a blank 'A X' column.
      *> AC3  XXCR  - the whole 'CR DB' column is blank, which is how the standard says CR/DB is last.
      *> AC4  ZZ9AA - row 'A X' has a blank 'Z *' column: no zero suppression symbol may precede an 'A'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB535AC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AC1 PIC XX,XX.
       01 AC2 PIC AAZZ.
       01 AC3 PIC XXCR.
       01 AC4 PIC ZZ9AA.
       PROCEDURE DIVISION.
           STOP RUN.
