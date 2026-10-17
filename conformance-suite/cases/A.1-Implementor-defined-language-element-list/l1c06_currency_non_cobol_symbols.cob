      *>--- conformance-case
      *> id: l1c06_currency_non_cobol_symbols
      *> rules: [DOC-A.1-43, DOC-A.1-44]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR20/SR21 + §8.1.2 (A.1 items 43, 44) — non-COBOL
      *>   currency symbols
      *> Item 43 rule: "If the character specified as the currency
      *>   symbol is not a character
      *> in the COBOL character repertoire, any equivalence between that
      *>   character and any
      *> other character in the computer's compile-time coded character
      *>   set is defined by
      *> the implementor."
      *>   cite.py --check 12.3.7.3 "any equivalence between that
      *>     character and any other
      *>   character in the computer's compile-time coded character set
      *>     is defined by the
      *>   implementor"  -> OK  §12.3.7.3 20)  (Syntax rules)
      *>   cite.py --check 12.3.7.3 "No two CURRENCY SIGN clauses within
      *>     a single source
      *>   unit shall specify equivalent currency symbols unless they
      *>     specify identical
      *>   currency strings"  -> OK  §12.3.7.3 21)  (Syntax rules)
      *>   cite.py --check 12.3.7.3 "If the PICTURE SYMBOL phrase is
      *>     specified, literal-7
      *>   is the currency string and literal-8 is the associated
      *>     currency symbol."
      *>   -> OK  §12.3.7.3 23)  (Syntax rules)
      *> Item 44 rule: "the implementor shall specify any such
      *>   additional characters that
      *> are prohibited from use as a currency symbol"
      *>   cite.py --check 8.1.2 "the implementor shall specify any such
      *>     additional
      *>   characters that are prohibited from use as a currency symbol"
      *>   -> OK  §8.1.2 3)  (Computer's coded character set)
      *>   cite.py --check 12.3.7.3 "Literal-8 may be any character from
      *>     the computer's
      *>   coded character set except for the following"  -> OK
      *>     §12.3.7.3 27)
      *> Implementor definitions (docs/CONFORMANCE.md): DOC-A.1-43 -
      *>   equivalent to exactly
      *> the characters with the same .NET invariant uppercase mapping
      *>   (é≡É, ω≡Ω, full-width
      *> ｅ≡Ｅ, no width folding); DOC-A.1-44 - the only prohibited
      *>   non-COBOL character is
      *> ſ (U+017F); €, £, full-width letters and full-width digits are
      *>   allowed.
      *> Derivation of each expected line (currency STRING as written is
      *>   inserted):
      *>   EU=[EUR1.50]  symbol é, used as É (equivalent): 1.5 ->
      *>     EUR1.50.
      *>   OM=[OMG1.50]  symbol Ω, used as ω (equivalent): OMG1.50.
      *>   FW=[FWE1.50]  symbol ｅ, used as Ｅ (equivalent): FWE1.50. ｅ is
      *>     NOT
      *>                 equivalent to é (no normalization), so its
      *>                   clause with a
      *>                 different string does not violate SR21 and the
      *>                   program compiles.
      *>   EC=[€1.50]    bare CURRENCY SIGN "€" - non-COBOL, allowed
      *>     (item 44).
      *>   GB=[GBP1.50]  PICTURE SYMBOL "£" - allowed.
      *>   FZ=[FWZ1.50]  PICTURE SYMBOL full-width "ｚ" - allowed
      *>     although its
      *>                 ASCII counterpart z (≡ Z) is excluded by SR27
      *>                   b).
      *>   F0=[FWD1.50]  PICTURE SYMBOL full-width digit "０" - allowed;
      *>     only the
      *>                 digits 0-9 of SR27 a) are excluded.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "é"
           CURRENCY SIGN IS "OMG" WITH PICTURE SYMBOL "Ω"
           CURRENCY SIGN IS "FWE" WITH PICTURE SYMBOL "ｅ"
           CURRENCY SIGN IS "€"
           CURRENCY SIGN IS "GBP" WITH PICTURE SYMBOL "£"
           CURRENCY SIGN IS "FWZ" WITH PICTURE SYMBOL "ｚ"
           CURRENCY SIGN IS "FWD" WITH PICTURE SYMBOL "０".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-EU PIC É9.99.
       01 WS-OM PIC ω9.99.
       01 WS-FW PIC Ｅ9.99.
       01 WS-EC PIC €9.99.
       01 WS-GB PIC £9.99.
       01 WS-FZ PIC ｚ9.99.
       01 WS-F0 PIC ０9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO WS-EU WS-OM WS-FW WS-EC WS-GB WS-FZ WS-F0.
           DISPLAY "EU=[" WS-EU "]".
           DISPLAY "OM=[" WS-OM "]".
           DISPLAY "FW=[" WS-FW "]".
           DISPLAY "EC=[" WS-EC "]".
           DISPLAY "GB=[" WS-GB "]".
           DISPLAY "FZ=[" WS-FZ "]".
           DISPLAY "F0=[" WS-F0 "]".
           STOP RUN.
