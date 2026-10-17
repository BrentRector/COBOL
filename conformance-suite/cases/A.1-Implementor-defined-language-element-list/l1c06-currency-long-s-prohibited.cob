      *>--- conformance-case
      *> id: l1c06-currency-long-s-prohibited
      *> rules: [DOC-A.1-44]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.1.2 (A.1 item 44) — the implementor-prohibited non-COBOL
      *>   currency symbol ſ
      *>   cite.py --check 8.1.2 "the implementor shall specify any such
      *>     additional
      *>   characters that are prohibited from use as a currency symbol"
      *>   -> OK  §8.1.2 3)  (Computer's coded character set)
      *>   cite.py --check 12.3.7.3 "alphabetic characters A, B, C, D,
      *>     E, N, P, R, S, V,
      *>   X, Z, or their lowercase equivalents"  -> OK  §12.3.7.3 22)
      *>     (Syntax rules)
      *> docs/CONFORMANCE.md DOC-A.1-44: exactly one non-COBOL character
      *>   is prohibited, ſ
      *> (U+017F LATIN SMALL LETTER LONG S), because its invariant
      *>   uppercase is the picture
      *> letter S excluded by SR22 b); it is refused with COBOLNET0891.
      *>   The bare
      *> single-character form exists in every edition, so every edition
      *>   rejects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "ſ".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
