      *>--- conformance-case
      *> id: l1_picture_currency_equivalent
      *> rules: [SR-12.3.7.3-20, SR-13.18.40.3-28]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.40.3 SR28 PICTURE clause — "For floating insertion,
      *> when the currency symbol is used as the floating insertion
      *> symbol, all occurrences of the currency symbol within
      *> character-string-1 shall be equivalent characters."
      *>   python scripts/spec/cite.py --check 13.18.40.3 "For floating
      *>   insertion, when the currency symbol is used as the floating
      *>   insertion symbol, all occurrences of the currency symbol
      *>   within character-string-1 shall be equivalent characters."
      *>   -> OK  §13.18.40.3 28)  (Syntax rules)
      *>
      *> THE SATISFIED DIRECTION. SR28 is a syntax rule, so it has two
      *> legs: source whose occurrences ARE equivalent is conforming
      *> and shall be compiled as one floating insertion string, and
      *> source whose occurrences are NOT equivalent shall be
      *> diagnosed. This fixture is the first leg; the second is
      *> negative/l1-picture-currency-not-equivalent.cob.
      *>
      *> WHAT "EQUIVALENT" MEANS HERE. §13.18.40.3 SR3 sends the
      *> question to §12.3.7.3 SR20 — "In all other cases, compile-time
      *> equivalence to that currency symbol is determined as specified
      *> in 8.1.3, COBOL character repertoire, General rules."
      *> ('W' is a COBOL basic letter, so this is the "all other cases"
      *> arm, not the implementor-defined one.) §8.1.3.2 GR3 then
      *> says "Equivalence of uppercase and lowercase basic letters is
      *> achieved by folding from uppercase to lowercase in accordance
      *> with the case mapping described in Annex C." So 'w' and 'W'
      *> are equivalent characters and PIC wwW9 satisfies SR28.
      *>   cite checks:
      *>   --check 12.3.7.3 "In all other cases, compile-time
      *>     equivalence to that currency symbol is determined as
      *>     specified in 8.1.3, COBOL character repertoire, General
      *>     rules."   -> OK  §12.3.7.3 20)
      *>   --check 8.1.3.2 "Equivalence of uppercase and lowercase
      *>     basic letters is achieved by folding from uppercase to
      *>     lowercase in accordance with the case mapping described in
      *>     Annex C."   -> OK  §8.1.3.2 3) b)
      *>
      *> THE EDITED IMAGE, DERIVED. Being equivalent, the three
      *> occurrences form ONE floating insertion string (§13.18.40.5
      *> editing rule 6: "Floating insertion editing is indicated by
      *> specifying a string of at least two identical floating
      *> insertion editing symbols"), whose second symbol "represents
      *> the leftmost limit of the numeric data that may be stored in
      *> the item". Positions 2, 3 and the trailing '9' at position 4
      *> are therefore the three digit positions; 12 aligns as 012.
      *> Rule 6a places a single occurrence of the replacement
      *> character immediately preceding the first nonzero numeric
      *> character — the '1' at position 3 — so the currency character
      *> lands at position 2 and "any character positions preceding
      *> this insertion character will contain the space character".
      *> The inserted character is the currency STRING literal-7, which
      *> §12.3.7.3 SR23 distinguishes from the picture SYMBOL
      *> literal-8, and literal-7 here is "W". Hence [ W12] — and the
      *> all-uppercase control PIC WWW9 shall give the same four
      *> characters, which is what equivalence MEANS.
      *>   --check 12.3.7.3 "If the PICTURE SYMBOL phrase is specified,
      *>     literal-7 is the currency string and literal-8 is the
      *>     associated currency symbol."   -> OK  §12.3.7.3 23)
      *>
      *> EDITIONS. CURRENCY SIGN ... WITH PICTURE SYMBOL enters at
      *> COBOL-2002, so 'w'/'W' equivalence is first exercisable there;
      *> the 2002 leg is tests/conformance/2002/
      *> l1_picture_currency_equivalent_2002.cob. At COBOL-85 the only
      *> currency symbol is '$' and every occurrence of it is the same
      *> character, so SR28 has no distinguishable 85 leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PCEQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "W" WITH PICTURE SYMBOL "W".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MIX PIC wwW9.
       01 WS-UPP PIC WWW9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12 TO WS-MIX.
           MOVE 12 TO WS-UPP.
           DISPLAY "MIX=[" WS-MIX "]".
           DISPLAY "UPP=[" WS-UPP "]".
           STOP RUN.
