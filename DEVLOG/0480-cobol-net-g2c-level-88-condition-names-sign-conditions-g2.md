## Entry 480 — COBOL.NET G2c: level-88 condition-names + sign conditions (G2 data-item checkpoint COMPLETE)

Level-88 condition-names (ISO §8.8.4.1.2 / §13.18.4) and sign conditions. This completes the **G2 checkpoint's
explicit data-item coverage** (COBOLNET_DESIGN §16: "groups/tables/signed-DISPLAY/88s bind and DISPLAY
byte-identically to the legacy") — all four now green vs the oracle.

- **Binder stops dropping 88s.** A level-88 entry is captured as a `Condition88` on its immediately-superior data
  item (the conditional variable), with the full VALUE set — singletons AND THRU ranges, multiple VALUEs — kept as
  raw operand text (decoded at emit). `DataBinder.Conditions` is a name→list multimap (qualified/duplicate 88s).
- **88 reference → membership test.** A bare condition-name in an IF renders as an OR of its VALUE-set tests over
  the parent's `Place`: equality for a singleton, an inclusive bound test for a THRU range — `CobolString.Compare`
  for an alphanumeric parent, scaled-`long` comparison for a numeric parent (the 88 value scaled to the parent's
  scale). `FLAG-YES`, `PASSING (60 THRU 100)`, `VOWEL ("A" "E" "I" "O" "U")` all verified.
- **`SET cond-name TO TRUE`** (ISO §14.9.39 Format 5) moves the 88's first VALUE into the parent. (TO FALSE → loud.)
- **Sign conditions** (IS [NOT] POSITIVE/NEGATIVE/ZERO) implemented inline.
- **Loud-failure for conditions (§1.4):** the `RenderCondition`/`RenderComparison` "false" fallbacks are replaced by
  `NotImplemented.Value<bool>(...)` — a class condition or any unhandled condition form now fails LOUD at run time,
  never silently `false`.

Verified: full-solution build clean Debug (0/0); Conformance 77 green (7 new 88/sign differential tests: boolean
flag + SET TO TRUE, numeric THRU range, multiple VALUEs, 88s in compound AND/OR/NOT, sign conditions) + Unit 3
green; legacy oracle untouched. RESUME AT → G2-1c (reference modification — needs `CobolString.RefMod`/`SpliceInto`
runtime), then class conditions (IS NUMERIC/ALPHABETIC), then the formal bound tree + emitter decomposition (G2-2).

DEVLOG 480.

