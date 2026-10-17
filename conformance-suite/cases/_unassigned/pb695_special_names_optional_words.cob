      *>--- conformance-case
      *> id: pb695_special_names_optional_words
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> !! EVERY OPTIONAL WORD OF THE SPECIAL-NAMES PARAGRAPH'S COBOL-85 CLAUSES, OMITTED (kb/Work PB695).
      *> ISO 12.3.7.2 prints this paragraph, and 5.2.3 / 8.3.2.4.3 decide what may be left out of it:
      *> "Within each format, uppercase words that are not underlined are called optional words and may
      *> be specified at the user's option with no effect on the semantics of the format" (8.3.2.4.3).
      *> MEASURED, NOT ASSUMED - printed folio 290 carries underline rectangles under exactly
      *>     ALPHANUMERIC CLASS COMMA CRT CURRENCY CURSOR DECIMAL-POINT IN LOCALE NATIONAL OFF ON
      *>     ORDER PICTURE SPECIAL-NAMES. STATUS SYMBOL TABLE THROUGH THRU
      *> and folio 291/292 (the alphabet-name-clause and symbolic-characters-clause figures) under
      *> ALPHABET, ALPHANUMERIC, NATIONAL, LOCALE, NATIVE, STANDARD-1, STANDARD-2, UCS-4, UTF-8,
      *> UTF-16, SYMBOLIC and IN. IS appears in NONE of the three rosters, and neither does the
      *> CHARACTERS of `SYMBOLIC CHARACTERS` - the one rule on that heading line ends at x=112.24,
      *> three points short of CHARACTERS' left edge - nor either of the IS/ARE stacked inside the
      *> square bracket of `{ symbolic-character-1 } ... [ IS / ARE ] { integer-1 } ...`.
      *> So this program writes the paragraph with:
      *>   . DECIMAL-POINT's IS omitted           (folio 290: `[ DECIMAL-POINT IS COMMA ]`)
      *>   . CURRENCY's SIGN and IS omitted       (folio 290: `CURRENCY SIGN IS literal-7`)
      *>   . SYMBOLIC's CHARACTERS omitted and the IS/ARE connective omitted    (folio 292)
      *>   . ALPHABET's IS omitted                (folio 291)
      *>   . the implementor-switch entry's IS omitted   (folio 290: `switch-name-1 [ IS mnemonic ]`)
      *> Only the last of those was spellable before PB695; each of the others was a hard parse error.
      *> DERIVATION of the expected lines - from the rules, never from the compiler:
      *>  . 13.18.13 / 13.18.44 (DECIMAL-POINT IS COMMA): "the functions of the comma and period are
      *>    exchanged in the character-string of the PICTURE clause and in numeric literals", so 123,45
      *>    is one hundred twenty three and forty five hundredths and PIC ZZ9,99 edits it as `123,45`.
      *>  . 12.3.7.4 (CURRENCY SIGN): the currency sign literal replaces the currency symbol in an
      *>    edited picture, so PIC @@@9,99 over the same value yields three positions of leading
      *>    suppression, the sign floats to the position just left of the first significant digit, and
      *>    the result is `@123,45`.
      *>  . 12.3.7.4 6): "The implementor shall define the order of characters within the native
      *>    alphanumeric coded character set ..., associating each character with an ordinal position
      *>    within the character set." A COBOL ordinal position is ONE-BASED (8.4.3.2 FUNCTION ORD
      *>    returns the ordinal, and the first character of the set has ordinal 1), so the symbolic
      *>    character declared at ordinal 66 is the 66th character of the native alphanumeric set -
      *>    U+0041 LATIN CAPITAL LETTER A, this implementor's set being ISO/IEC 10646 - and MOVEing it
      *>    to a one-character item displays `A`.
      *>  . The ALPHABET and switch entries are declarations; the program's output does not depend on
      *>    them, which is the point - an optional word omitted changes nothing at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695SN85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT COMMA
           CURRENCY "@"
           SYMBOLIC SC-ORD66 66
           ALPHABET AL-NATV NATIVE
           SWITCH-1 MNEM-ONE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N   PIC 9(3)V99 VALUE 123,45.
       01 ED  PIC ZZ9,99.
       01 CUR PIC @@@9,99.
       01 SC  PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N TO ED
           DISPLAY "ED=" ED
           MOVE N TO CUR
           DISPLAY "CUR=" CUR
           MOVE SC-ORD66 TO SC
           DISPLAY "SC=" SC
           DISPLAY "DONE"
           STOP RUN.
