## Entry 1201 — 2026-08-07 09:40 PDT — R03: the hexadecimal literal formats, all three of them — and a data item named NX is what made the defect silent

**`MOVE FUNCTION MAX(NX"0041") TO R` returned `ZZZZ`.** `NX` lexed as an IDENTIFIER and `"0041"` as a separate
string, so ISO §8.3.3.5.2's Format 2 literal became TWO operands. Usually that surfaced as an arity complaint —
but with a data item actually named `NX` in scope, nothing complained at all:

```cobol
01 NX PIC X(4) VALUE "ZZZZ".
MOVE FUNCTION MAX(NX"0041") TO R.     *> MAX(NX, "0041") — silently
```

The lexer's own comment had recorded the gap in as many words — *"NX"…" (hex national) is deferred"* and
*"BX"…" (hex boolean) is deferred"* — **two arms, and the note named one** (`feedback_two_arm_dispatch`).

⭐ **FORMAT 2 IS THE SAME TOKEN, BECAUSE IT IS THE SAME LITERAL KIND.** §8.3.3.5.2 and §8.3.3.4.2 each print TWO
general formats, and their ALL-FORMATS general rules put both in ONE class and category (§8.3.3.5.4 GR2,
§8.3.3.4.4 GR2). Folding Format 2 into `NATLIT`/`BOOLLIT` is what the standard says AND what made the change
tractable: **32 call sites already route NATLIT→national and BOOLLIT→boolean, and every one stays correct
untouched** — including the COBOL-2002 introduction gate, verified at `--std 85` with NO national or boolean
PICTURE in the program, so it is the LITERAL's gate being proven rather than the data item's.
⚠ **The alternative is a mistake already in that file.** `HEXLIT` is its own token even though §8.3.3.2 makes a
hexadecimal literal "one FORM of an alphanumeric literal, not a separate kind of thing" — `CobolLiteral`'s own
words — and that split is why its decoding had to be added at FOUR call sites, the fourth a silent VALUE-clause
miscompile. A `NATHEXLIT` token would have bought the same bill twice.

⛔ **THE SWEEP FOUND TWO MORE, AND ONE PREDATES THIS ITEM ENTIRELY. Filing them would have been a half-fix.**

**① The grouping rule was enforced NOWHERE, in any of the three formats.** The decoders answer `""` for a
malformed digit count and every caller took that as the value: `FUNCTION LENGTH(X"414")` → **1**, silently, on
source the standard rejects — behaviour X"…" has had since it was introduced, which NX inherited the moment its
token started matching. And the three clauses do NOT state the same rule:

```
X"…"   §8.3.3.2.3 r6   2 digits   (one byte per alphanumeric character)
NX"…"  §8.3.3.5.3 r5   4 digits   (D-N1: one UTF-16 unit per national position)
BX"…"  §8.3.3.4.3 r3   NONE       (§8.3.3.4.4 GR5 maps each digit independently to four boolean characters)
```

One shared check would have rejected legal `BX"5AB"`. `COBOLNET1635` reports per format, citing that format's own
rule; **BX stays deliberately unscreened** and the golden pins its ungrouped counts so a later "symmetry" fix
cannot break them.
⚠ **THE CHECK IS AT BIND, NOT IN THE LEXER, AND THAT IS THE LOAD-BEARING DECISION.** A lexer rule refusing an odd
digit count would not reject the program — the token would simply fail to match, `X"414"` would split into an
IDENTIFIER and a STRINGLIT, and the literal would be back in the silent-degradation hole this item exists to
close. **The token must match so that something is left to diagnose.**

**② `X""` was refused outright.** §8.3.3.2.3 NOTE 2 says "Hexadecimal-alphanumeric literals can be of zero
length"; the lexer's `[0-9a-f]+` said otherwise, so `X""` split exactly as NX/BX did. Now `*`, matching the NOTEs
in all three clauses. `FUNCTION LENGTH(X"")` answers 1 — identical to `FUNCTION LENGTH("")`, i.e. consistent with
the existing zero-length-literal behaviour rather than inventing a new answer for it.

⚙ **A DRIFT TEST CAUGHT MY OWN OMISSION AND NAMED THE FIX.** Adding COBOLNET1635 without regenerating
`docs/DIAGNOSTICS.md` failed `DiagnosticRegistryDriftTests` with "regenerate: pwsh scripts/gen-diagnostics-doc.ps1".
A new diagnostic code that is undocumented is exactly what that guard exists for.

**GATE.** Conformance `~Corpus|~Negative|~Literal|~Lexer|~Intrinsic|~VersionMatrix` **2686/2686** zero skipped ·
Unit **4029/4029** zero skipped. Golden `r03_hexadecimal_national_and_boolean_literals` — 16 assertions,
including the trap item named `NX` still reading as an ordinary data item. Negatives
`r03-hex-national-literal-at-85` and `r03-hex-literal-digit-grouping`.

