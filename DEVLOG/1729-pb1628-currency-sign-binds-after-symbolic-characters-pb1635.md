## Entry 1729 — 2026-09-27 04:32 PDT — PB1628: CURRENCY SIGN binds after SYMBOLIC CHARACTERS; PB1635 filed (cite.py misnumbers `N.` rules)

**PB1628.** `CURRENCY SIGN IS "US" & SYM-D PICTURE SYMBOL "$"` drew COBOLNET2473 ("not a literal") when the
SYMBOLIC CHARACTERS clause declaring SYM-D came later in the paragraph. The spec admits it: §12.3.7.3 SR18 bars only a
figurative constant as literal-7; a concatenation expression "may be used anywhere a literal of that class may be
used" (§8.8.3.3 GR3); and SR11's symbolic-character bar does not name literal-7 or literal-8. CURRENCY bound in the
walk, before the symbolic characters had values. It now binds in phase 3, after SYMBOLIC CHARACTERS, which is the
dependency-order rule of PB1558. Golden `2023/pb1628_currency_sign_symbolic_operand` displays `[US$012.50]`.
**PB1635 (new, process).** `cite.py --check 8.8.3.3 "equivalent to a literal"` answers `c)` for what is GR3. §8.8.3.2
and §8.8.3.3 number their rules `1.` rather than the transcription's `1\)`, and `cite.py`'s TOP pattern does not
match that form (263 such lines spec-wide). The GR3 citation in code is correct; I confirmed it by reading the clause.
The next step is to render the printed page to decide whether this is a transcription fix or a tool fix.
**Gate.** Characterization 33/33; Conformance (SpecialNames, Currency, Symbolic, Picture, Edited, CorpusRunner,
Negative, Drift, Alphabet, Nist, Locale) 4253/4253; Unit 29491/29491.

