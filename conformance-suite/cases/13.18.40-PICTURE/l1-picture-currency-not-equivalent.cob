      *>--- conformance-case
      *> id: l1-picture-currency-not-equivalent
      *> rules: [SR-13.18.40.3-28]
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
      *> THE VIOLATED LEG. SR28 is a syntax rule, so source that breaks
      *> it is not a conforming source program and the compiler owes a
      *> diagnostic. The satisfied leg is
      *> conformance-suite/cases/12.3.7-SPECIAL-NAMES/l1_picture_currency_equivalent.cob and
      *> its 2002 twin.
      *>
      *> HOW TWO NON-EQUIVALENT CURRENCY SYMBOLS ARE OBTAINED. The
      *> CURRENCY SIGN clause below makes 'W' a currency symbol, and
      *> §12.3.7.3 SR25 keeps '$' one as well: "If a source unit does
      *> not contain a CURRENCY SIGN clause that specifies '$' as the
      *> currency symbol (either as literal-7 or literal-8), the clause
      *> CURRENCY SIGN '$' PICTURE SYMBOL '$' is implied for that
      *> source unit."
      *>   python scripts/spec/cite.py --check 12.3.7.3 "If a source
      *>   unit does not contain a CURRENCY SIGN clause that specifies
      *>   '$' as the currency symbol (either as literal-7 or
      *>   literal-8), the clause CURRENCY SIGN '$' PICTURE SYMBOL '$'
      *>   is implied for that source unit."
      *>   -> OK  §12.3.7.3 25)  (Syntax rules)
      *> So PIC $$W9 is a floating insertion string of THREE currency
      *> symbols, '$', '$' and 'W'. '$' is a basic special character
      *> and 'W' a basic letter, and no case folding relates them
      *> (§8.1.3.2 GR3 folds letters only), so they are not equivalent
      *> characters and SR28 is violated.
      *>
      *> ⛔ AT THE reject-at EDITIONS, SR28 IS THE ONLY GROUND ON
      *> WHICH THIS PROGRAM CAN BE REJECTED — which is what keeps the
      *> fixture honest (see EDITIONS below for the 85 measurement):
      *>   SR27  forbids more than one string of two or more currency
      *>         symbols; '$$W' is ONE such string, so SR27 is met.
      *>   SR26  requires the currency symbol to be leftmost (or
      *>         rightmost) in character-string-1 for fixed editing
      *>         sign control; here it is leftmost.
      *>   GR13 a) of §13.18.40.4 is satisfied — "at least two
      *>         identical symbols from the set '+', '-', currency
      *>         symbol" — so the entry is a well-formed
      *>         numeric-edited item apart from SR28.
      *>   SR27 of §12.3.7.3 permits 'W' as literal-8: it is not a
      *>         digit and not one of A, B, C, D, E, N, P, R, S, V, X,
      *>         Z or the space.
      *>
      *> EDITIONS. CURRENCY SIGN ... WITH PICTURE SYMBOL enters at
      *> COBOL-2002, so the construct SR28 governs is only reachable
      *> from 2002 onward and the reject-at header names 2002, 2014 and
      *> 2023 only. MEASURED at --std 85 (not a reject-at edition, so
      *> the runner gates nothing on it): this source is rejected on
      *> BOTH grounds — COBOLNET0808 for SR28 first, then COBOLNET0893
      *> for the COBOL-2002 clause gate. 85 is therefore left out
      *> because its rejection does not DISCRIMINATE SR28, not because
      *> SR28 stops applying there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PCNE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "W" WITH PICTURE SYMBOL "W".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-BAD PIC $$W9.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "UNREACHABLE".
           STOP RUN.
