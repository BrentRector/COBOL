## Entry 1184 — 2026-08-05 21:44 PDT — the correct image of a figurative argument was already written down, one file away, and the loud copy beside it aborted legal programs at run time

**PB25.** `FUNCTION LOWER-CASE(SPACE)` compiled clean and aborted at run time with "intrinsic string argument
'BoundFigurative'". It is legal source: **§8.3.3.6.3 SR1** admits a figurative constant "whenever 'literal' appears
in a format", **§8.4.3.2.3 SR8** makes argument-1 "an identifier, a literal, a boolean expression, or an arithmetic
expression", and neither SR1 exception applies.

**⛔ THE FIX WAS TWO ARMS DELEGATING, BECAUSE THE ANSWER WAS ALREADY THERE.** `OperandText.AsString` has
implemented the correct image all along and cites the rule in its own comments — its entry intercepts
`BoundFigurative` PCS-aware, and its `BoundAllLiteral` arm is annotated "length-unspecified: the literal once
(GR3c)". `IntrinsicRenderer.StrArgVisitor` sat beside it with a second, LOUD copy of the same dispatch — and its
`BoundFieldOperand` arm *already* delegated to `OperandText.AsString`. So one rule was written in two places, one
copy was right, the other was a loud stage, and the delegation that fixes it was already the file's own idiom.

**The lengths are §8.3.3.6.4 GR3's**, the "length of the string is NOT specified in the rules for the context"
case — which is exactly a bare function argument, since nothing there fixes a size: **(b)** a figurative other
than `ALL literal-1` is ONE character; **(c)** otherwise the length of literal-1. So `SPACE` is one space and
`ALL "AB"` is the two characters "AB", padded by the MOVE and not by the function.

**The sweep found a SECOND copy of the mistake, in a different layer.** `FUNCTION LENGTH(SPACE)` failed too, from
the BINDER, and its comment asserted the rejection was spec-correct. Half of it was: §15.50.3 r1 admits "an
alphanumeric, national, or boolean literal", which excludes a NUMERIC literal (correct, kept) and says nothing
about figurative constants — while §8.3.3.6.4 GR1 makes SPACE an ALPHANUMERIC character value here, precisely what
r1 permits. **One code standing for two rules enforced neither**, which is §0's own PB1 lesson turning up again.

**MEASURED**, every value derived from the spec before the run: `LOWER-CASE(SPACE)`=" " · `LOWER-CASE(QUOTE)`=" ·
`LOWER-CASE(ALL "AB")`="ab" · `REVERSE(ALL "AB")`="BA" · and the pair that carries the rule rather than the
symptom, **`LENGTH(SPACE)`=1 versus `LENGTH(ALL "ABC")`=3** — GR3b against GR3c. "It no longer crashes" would have
passed with either.

**⛔ THE LOCALE HALF: THE ENTRY'S FRAMING WAS OFF BY ONE LEVEL, AND MEASURING SAID WHERE.** The entry read "the
bracketed `[LOCALE locale-name-1]` arm of §15.57.2 is REFUSED". The FUNCTION side was already correct — the
negative fixture `locale_keyword_a49` has pinned `FUNCTION LOWER-CASE(X LOCALE LOC1)` → COBOLNET1518 since PB37.
What was actually broken is the DECLARATION you would need to make that locale-name mean anything:

    SPECIAL-NAMES.  LOCALE FR IS "fr_FR".
      -> COBOL0308: unexpected '"fr_FR"'. A data-name is expected here, not a literal.

With no `localeClause` in the grammar the entry fell into `implementorSwitchEntry`, matched as far as the bare word
LOCALE, re-entered as `FR IS "fr_FR"` and died on the LITERAL — a parse error pointing at the wrong token, for a
clause the standard defines. **§A.4.9 item 10 is "SPECIAL-NAMES paragraph: LOCALE clause and LOCALE phrases in the
ALPHABET clause (12.3.7)"**, so this is an optional-locale element whose non-support is conformant per §4.2.7 /
§A.4.1 **only because it is DIAGNOSED**. An unexplained rejection is not documented non-support. It now
superset-parses and carries the same cited COBOLNET1518.

**⚠ THE OBVIOUS IMPLEMENTATION WOULD HAVE BROKEN THE WORKING HALF.** LOCALE *is* reserved from 2002 (§8.9;
`reserved-words.json` r2002/r2014/r2023 true, r85 false), so making it a lexer token looks like the tidy answer —
but `IntrinsicBinder.KeywordWordOf` detects the FUNCTION-side phrase precisely because LOCALE arrives as an
ordinary bare-word argument. Tokenizing it would silently kill that already-working diagnostic. So the clause is
recognised by an edition-gated TEXT predicate instead, and the reason is recorded where the next person will hit
it. Two comments — in `IntrinsicBinder` and in `locale_keyword_a49.cob` — asserted "LOCALE is not a reserved word",
which is wrong by the repo's own data; both now say *not a lexer token*, and say why.

**⚠ AND THE EDITION COUNTERPART IS TESTED, not assumed.** At COBOL-85 LOCALE is an ordinary user word, so
`SPECIAL-NAMES. LOCALE IS FOO.` is a legal implementor-switch entry there and must keep parsing —
`SpecialNamesLocaleEditionTests` pins that, plus the shape guard that a locale-name-less `LOCALE IS FOO` is not
annexed as a malformed locale clause at 2023 either. A predicate that fired at every edition would have turned
legal 85 source into a documented-non-support error, which is the exact class `edition_gate_sweep` exists for.

**▶ WHAT THIS DELIBERATELY DID NOT FIX — now PB48.** `FUNCTION LOWER-CASE(ZERO)` and `FUNCTION NUMVAL(ZERO)` still
fail, and not through any of these arms: `ZERO` is rewritten to an ARITHMETIC zero by the token rewriter before
binding, so it arrives as a `BoundNumericLiteral` and never reaches the figurative path. Different mechanism,
different layer, and the fix is genuinely delicate — `FUNCTION SQRT(ZERO)` needs exactly the numeric reading the
rewrite exists to provide, so the class required is per-function and per-argument, which a token pass cannot know.

**Gate (wave-local).** Conformance `~Intrinsic|~Corpus|~Negative|~Locale|~SpecialNames` **621/621** · Unit
`~Intrinsic|~Locale|~SpecialNames|~Drift|~VersionMatrix` **2828/2828**, zero skipped. Golden
`tests/conformance/2023/pb25_figurative_intrinsic_argument` (11 assertions) · negative fixture
`pb25-special-names-locale-a49` · `SpecialNamesLocaleEditionTests` (5).

