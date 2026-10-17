      *>--- conformance-case
      *> id: l1c06-currency-non-cobol-equivalent-rebound
      *> rules: [DOC-A.1-43]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR21 via SR20 (A.1 item 43) — equivalent
      *>   non-COBOL symbols, two strings
      *>   cite.py --check 12.3.7.3 "No two CURRENCY SIGN clauses within
      *>     a single source
      *>   unit shall specify equivalent currency symbols unless they
      *>     specify identical
      *>   currency strings"  -> OK  §12.3.7.3 21)  (Syntax rules)
      *>   cite.py --check 12.3.7.3 "any equivalence between that
      *>     character and any other
      *>   character in the computer's compile-time coded character set
      *>     is defined by the
      *>   implementor"  -> OK  §12.3.7.3 20)  (Syntax rules)
      *> Under the documented equivalence (docs/CONFORMANCE.md
      *>   DOC-A.1-43) 'é' ≡ 'É', so
      *> binding them to the different strings "EUR" and "USD" violates
      *>   SR21 -> COBOLNET0891.
      *> Each clause alone is legal (companion:
      *>   2002/l1c06_currency_non_cobol_symbols).
      *> Edition floor 2002: COBOL-85 has a single CURRENCY SIGN clause
      *>   and no PICTURE SYMBOL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "é"
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "É".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC é9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
