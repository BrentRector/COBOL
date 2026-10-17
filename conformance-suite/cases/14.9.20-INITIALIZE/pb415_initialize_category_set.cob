*>--- conformance-case
*> id: pb415_initialize_category_set
*> rules: [GR-14.9.20.4-2, GR-14.9.20.4-5, GR-14.9.20.4-6,
*>    FMT-14.9.20.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.20.2 category-name IS A SET, and eight of its thirteen words were unspellable.
      *> The printed general format (licensed PDF p667 / folio 637) encloses THIRTEEN underlined category
      *> names in a BRACE carrying CHOICE INDICATORS; §5.2.6.4 reads those "one or more of the alternatives
      *> contained within the choice indicators shall be specified, but any single alternative shall be
      *> specified only once". The grammar carried a single-alternative rule naming five, so this whole file
      *> was COBOL0001 before kb/Work PB415.
      *>
      *> EXPECTED VALUES, DERIVED BEFORE THE RUN:
      *> T1  REPLACING ALPHABETIC ALPHANUMERIC DATA BY "ZZ" — ONE category-name naming TWO categories
      *>     (§5.2.6.4). GR5c2 qualifies AB (§8.5.2.2 alphabetic) and AN (§8.5.2.3 alphanumeric); §14.9.20.4 GR6b's
      *>     sender is literal-1 "ZZ"; GR4's implicit MOVE left-justifies and space-fills to 3 → "ZZ ".
      *>     NU/NA/BO name no category in the phrase, so GR5c leaves them unchanged (GR5c4's premise is
      *>     false — the REPLACING phrase IS specified). → ZZ / ZZ / 123 / pqr / 0000
      *> T2  REPLACING NATIONAL DATA BY N"ab" — one of the eight words PB415 landed. Only NA is category
      *>     national (§8.5.2.10); the implicit MOVE pads with the national space → "ab ". → ZZ/ZZ/123/ab /0000
      *> T3  REPLACING NATIONAL-EDITED DATA BY N"zz" — conforming source that matches NO receiving operand:
      *>     national-edited (§8.5.2.11) is a DIFFERENT category from national (§8.5.2.10), so §14.9.20.4 GR5c2 selects
      *>     nothing and every item is left unchanged. Nothing changes.
      *> T4  REPLACING BOOLEAN DATA BY B"11" — only BO is category boolean (§8.5.2.5); the boolean MOVE
      *>     left-justifies and zero-fills right (§14.9.25.3) → "1100".
      *> T5  TWO REPLACING items, the first again a two-category category-name: AB and AN take "Q" → "Q  ",
      *>     NU takes 7 → 007 (§14.9.20.3 SR6 is satisfied — no category is repeated across the items).
      *> T6  ALL TO VALUE (the braced choice spelled in full, §14.9.20.2 + §5.2.6.3): GR5c1b qualifies every
      *>     item that carries a data-item format VALUE clause and GR6a3 restores that literal — AB/AN/NU/NA.
      *>     BO has no VALUE clause and is not one of GR5c1a's categorical categories, so it is NOT a
      *>     receiving operand and keeps T4's 1100.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415CATSET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 AB PIC A(3) VALUE "abc".
          05 AN PIC X(3) VALUE "xyz".
          05 NU PIC 9(3) VALUE 123.
          05 NA PIC N(3) VALUE N"pqr".
          05 BO PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING ALPHABETIC ALPHANUMERIC DATA BY "ZZ".
           DISPLAY "T1=[" AB "][" AN "][" NU "][" NA "][" BO "]".
           INITIALIZE G REPLACING NATIONAL DATA BY N"ab".
           DISPLAY "T2=[" AB "][" AN "][" NU "][" NA "][" BO "]".
           INITIALIZE G REPLACING NATIONAL-EDITED DATA BY N"zz".
           DISPLAY "T3=[" AB "][" AN "][" NU "][" NA "][" BO "]".
           INITIALIZE G REPLACING BOOLEAN DATA BY B"11".
           DISPLAY "T4=[" AB "][" AN "][" NU "][" NA "][" BO "]".
           INITIALIZE G REPLACING ALPHABETIC ALPHANUMERIC DATA BY "Q"
                                  NUMERIC DATA BY 7.
           DISPLAY "T5=[" AB "][" AN "][" NU "][" NA "][" BO "]".
           INITIALIZE G ALL TO VALUE.
           DISPLAY "T6=[" AB "][" AN "][" NU "][" NA "][" BO "]".
           STOP RUN.
